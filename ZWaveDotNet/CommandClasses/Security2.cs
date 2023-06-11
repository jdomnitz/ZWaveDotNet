﻿using Serilog;
using System.Security;
using System.Security.Cryptography;
using ZWaveDotNet.CommandClasses.Enums;
using ZWaveDotNet.CommandClassReports;
using ZWaveDotNet.Entities;
using ZWaveDotNet.Enums;
using ZWaveDotNet.Security;
using ZWaveDotNet.SerialAPI;
using ZWaveDotNet.Util;

namespace ZWaveDotNet.CommandClasses
{
    [CCVersion(CommandClass.Security2, 1, 1, false)]
    public class Security2 : CommandClassBase
    {
        public event CommandClassEvent? BootstrapComplete;
        public event CommandClassEvent? SecurityError;
        TaskCompletionSource bootstrapComplete = new TaskCompletionSource();

        public enum Security2Command
        {
            NonceGet = 0x01,
            NonceReport = 0x02,
            MessageEncap = 0x03,
            KEXGet = 0x04,
            KEXReport = 0x05,
            KEXSet = 0x06,
            KEXFail = 0x07,
            PublicKeyReport = 0x08,
            NetworkKeyGet = 0x09,
            NetworkKeyReport = 0x0A,
            NetworkKeyVerify = 0x0B,
            TransferEnd = 0x0C,
            CommandsSupportedGet = 0x0D,
            CommandsSupportedReport = 0x0E
        }

        public Security2(Node node, byte endpoint) : base(node, endpoint, CommandClass.Security2) { }

        public async Task<List<CommandClass>> GetSupportedCommands(CancellationToken cancellationToken = default)
        {
            ReportMessage msg = await SendReceive(Security2Command.CommandsSupportedGet, Security2Command.CommandsSupportedReport, cancellationToken);
            return PayloadConverter.GetCommandClasses(msg.Payload);
        }

        internal async Task<KeyExchangeReport> KexGet(CancellationToken cancellationToken = default)
        {
            Log.Information("Requesting Supported Curves and schemes");
            ReportMessage msg = await SendReceive(Security2Command.KEXGet, Security2Command.KEXReport, cancellationToken);
            Log.Information("Curves and schemes Received");
            return new KeyExchangeReport(msg.Payload);
        }

        internal async Task<Memory<byte>> KexSet(KeyExchangeReport report, CancellationToken cancellationToken = default)
        {
            Log.Information($"Granting Keys {report.Keys}");
            ReportMessage msg = await SendReceive(Security2Command.KEXSet, Security2Command.PublicKeyReport,  cancellationToken, report.ToBytes());
            Log.Information("Received Public Key "+ MemoryUtil.Print(msg.Payload.Slice(1)));
            return msg.Payload.Slice(1);
        }

        internal async Task SendPublicKey(CancellationToken cancellationToken = default)
        {
            if (controller.SecurityManager == null)
                throw new InvalidOperationException("Security Manager does not exist");
            Log.Information("Sending Public Key");
            byte[] resp = new byte[33];
            resp[0] = 0x1;
            Array.Copy(controller.SecurityManager.PublicKey, 0, resp, 1, 32);
            await SendCommand(Security2Command.PublicKeyReport, cancellationToken, resp);
        }

        public static bool IsEncapsulated(ReportMessage msg)
        {
            return msg.CommandClass == CommandClass.Security2 && msg.Command == (byte)Security2Command.MessageEncap;
        }

        public async Task Transmit(List<byte> payload, SecurityManager.RecordType? type, CancellationToken cancellationToken = default)
        {
            await Encapsulate(payload, type, cancellationToken);
            if (payload.Count > 2)
                payload.RemoveRange(0, 2);
            await SendCommand(Security2Command.MessageEncap, cancellationToken, payload.ToArray());
            Log.Debug("Transmit Complete");
        }

        public async Task Encapsulate(List<byte> payload, SecurityManager.RecordType? type, CancellationToken cancellationToken = default)
        {
            List<byte> extensionData = new List<byte>();
            Log.Information("Encrypting Payload for " + node.ID.ToString());
            if (controller.SecurityManager == null)
                throw new InvalidOperationException("Security Manager does not exist");
            
            SecurityManager.NetworkKey? networkKey;
            if (type == null)
                networkKey = controller.SecurityManager.GetHighestKey(node.ID);
            else
                networkKey = controller.SecurityManager.GetKey(node.ID, type.Value);
            if (networkKey == null)
            {
                Log.Error("Unable to encrypt message without network key");
                return;
            }
            else
                Log.Information("Using Key " + networkKey.Key.ToString());

            (Memory<byte> output, byte sequence)? nonce = controller.SecurityManager.NextSpanNonce(node.ID, networkKey.Key);
            if (nonce == null)
            {
                //We need a new Nonce
                Log.Information("Requesting new Nonce");
                ReportMessage msg = await SendReceive(Security2Command.NonceGet, Security2Command.NonceReport, cancellationToken, (byte)new Random().Next());
                NonceReport nr = new NonceReport(msg.Payload);
                var entropy = controller.SecurityManager.CreateEntropy(node.ID);
                Memory<byte> MEI = AES.CKDFMEIExpand(AES.CKDFMEIExtract(entropy.Bytes, nr.Entropy));
                controller.SecurityManager.CreateSpan(node.ID, entropy.Sequence, MEI, networkKey.PString, networkKey.Key);
                nonce = controller.SecurityManager.NextSpanNonce(node.ID, networkKey.Key);
                if (nonce == null)
                {
                    Log.Error("Unable to create new Nonce");
                    return;
                }

                extensionData.Add(nonce.Value.sequence);
                extensionData.Add(0x1);
                extensionData.Add(18);
                extensionData.Add(0x41); //SPAN Ext
                extensionData.AddRange(entropy.Bytes.ToArray());
            }
            else
            {
                extensionData.Add(nonce.Value.sequence);
                extensionData.Add(0x0);
            }

            //                                                        8(tag) + 1 (command class) + 1 (command) + extension len
            AdditionalAuthData ad = new AdditionalAuthData(node, controller, true, payload.Count + 10 + extensionData.Count, extensionData.ToArray()); //FIXME - Implement unencrypted here too
            Memory<byte> encoded = EncryptCCM(payload.ToArray(),  nonce.Value.output, networkKey!.KeyCCM, ad);

            byte[] securePayload = new byte[extensionData.Count + encoded.Length];
            extensionData.CopyTo(securePayload);
            encoded.CopyTo(securePayload.AsMemory().Slice(extensionData.Count));

            payload.Clear();
            payload.Add((byte)commandClass);
            payload.Add((byte)Security2Command.MessageEncap);
            payload.AddRange(securePayload);
        }

        internal static ReportMessage? Free(ReportMessage msg, Controller controller)
        {
            if (controller.SecurityManager == null)
                throw new InvalidOperationException("Security Manager does not exist");
            SecurityManager.NetworkKey? networkKey = controller.SecurityManager.GetHighestKey(msg.SourceNodeID);
            if (networkKey == null)
            {
                Log.Error("Unable to decrypt message without network key");
                return null;
            }
            Log.Information("Decrypting Secure2 Message with key (" + networkKey.Key + ")");
            int messageLen = msg.Payload.Length + 2;
            byte sequence = msg.Payload.Span[0];
            bool unencryptedExt = (msg.Payload.Span[1] & 0x1) == 0x1;
            bool encryptedExt = (msg.Payload.Span[1] & 0x2) == 0x2;
            Memory<byte> unencrypted = msg.Payload;
            if (!controller.SecurityManager.IsSequenceNew(msg.SourceNodeID, sequence))
            {
                Log.Error("Duplicate S2 Message Skipped");
                return null; //Duplicate Message
            }
            msg.Payload = msg.Payload.Slice(2);
            byte? groupId = null;
            if (unencryptedExt)
            {
                while (processExtension(msg.Payload, msg.SourceNodeID, controller.SecurityManager, networkKey, out byte? group))
                {
                    msg.Payload = msg.Payload.Slice(msg.Payload.Span[0]);
                    if (group != null)
                        groupId = group;
                }
                msg.Payload = msg.Payload.Slice(msg.Payload.Span[0]);
            }
            unencrypted = unencrypted.Slice(0, unencrypted.Length - msg.Payload.Length);
            AdditionalAuthData ad = new AdditionalAuthData(controller.Nodes[msg.SourceNodeID], controller, false, messageLen, unencrypted);
            Memory<byte> decoded;
            try
            {
                decoded = DecryptCCM(msg.Payload,
                                                    controller.SecurityManager.NextSpanNonce(msg.SourceNodeID, networkKey.Key)!.Value.output,
                                                    networkKey!.KeyCCM,
                                                    ad);
            }catch(Exception ex)
            {
                Log.Error(ex, "Failed to decode message");
                return null;
            }
            if (encryptedExt)
            {
                groupId = decoded.Span[2];
                Memory<byte> mpan = decoded.Slice(3, 16);
                //TODO - Process the MPAN
                decoded = decoded.Slice(19);
            }

            msg.Update(decoded);
            msg.Flags |= ReportFlags.Security;
            msg.SecurityLevel = SecurityManager.TypeToKey(networkKey.Key);
            Log.Warning("Decoded Message: " + msg.ToString());
            return msg;
        }

        private static bool processExtension(Memory<byte> payload, ushort nodeId, SecurityManager sm, SecurityManager.NetworkKey netKey, out byte? groupId)
        {
            byte len = payload.Span[0];
            bool more = (payload.Span[1] & 0x80) == 0x80;
            byte type = (byte)(0x3F & payload.Span[1]);
            groupId = null;
            switch (type)
            {
                case 0x01: //SPAN
                    Memory<byte> sendersEntropy = payload.Slice(2, 16);
                    var result = sm.GetEntropy(nodeId);
                    Memory<byte> MEI = AES.CKDFMEIExpand(AES.CKDFMEIExtract(sendersEntropy, result!.Value.bytes));
                    sm.CreateSpan(nodeId, result!.Value.sequence, MEI, netKey.PString, netKey.Key);
                    Log.Warning("Created new SPAN");
                    Log.Warning("Senders Entropy: " + MemoryUtil.Print(sendersEntropy));
                    Log.Warning("Receivers Entropy: " + MemoryUtil.Print(result!.Value.bytes));
                    Log.Warning("Mixed Entropy: " + MemoryUtil.Print(MEI));
                    break;
                case 0x03: //MGRP
                    groupId = payload.Span[2];
                    break;
                case 0x04: //MOS
                    //TODO - Send MPAN
                    break;
            }
            return more;
        }

        protected override async Task Handle(ReportMessage message)
        {
            switch ((Security2Command)message.Command)
            {
                case Security2Command.KEXGet:
                    Log.Error("Unexpected KEX Get"); //FIXME - Do we need this?
                    await SendCommand(Security2Command.KEXReport, CancellationToken.None, 0x0, 0x2, 0x1, (byte)SecurityKey.S2Unauthenticated);
                    break;
                case Security2Command.KEXSet:
                    KeyExchangeReport? kexReport = new KeyExchangeReport(message.Payload);
                    Log.Information("Kex Set Received: " + kexReport.ToString());
                    if (kexReport.Echo)
                    {
                        if (controller.SecurityManager == null)
                            return;
                        kexReport = controller.SecurityManager.GetRequestedKeys(node.ID);
                        if (kexReport != null)
                        {
                            kexReport.Echo = true;
                            Log.Information("Responding: " + kexReport.ToString());
                            CommandMessage reportKex = new CommandMessage(controller, node.ID, endpoint, commandClass, (byte)Security2Command.KEXReport, false, kexReport.ToBytes());
                            await Transmit(reportKex.Payload, SecurityManager.RecordType.ECDH_TEMP);
                        }
                    }
                    else
                        await SendCommand(Security2Command.KEXReport, CancellationToken.None, kexReport.ToBytes());
                    break;
                case Security2Command.NetworkKeyGet:
                    if (controller.SecurityManager == null)
                        return;
                    Log.Information("Network Key Get Received");
                    byte[] resp = new byte[17];
                    SecurityKey key = (SecurityKey)message.Payload.Span[0];
                    //TODO - Verify this was granted
                    resp[0] = (byte)key;
                    AES.KeyTuple permKey;
                    switch (key)
                    {
                        case SecurityKey.S0:
                            controller.NetworkKeyS0.CopyTo(resp, 1);
                            permKey = AES.CKDFExpand(controller.NetworkKeyS0, false);
                            break;
                        case SecurityKey.S2Unauthenticated:
                            controller.NetworkKeyS2UnAuth.CopyTo(resp, 1);
                            permKey = AES.CKDFExpand(controller.NetworkKeyS2UnAuth, false);
                            break;
                        case SecurityKey.S2Authenticated:
                            controller.NetworkKeyS2Auth.CopyTo(resp, 1);
                            permKey = AES.CKDFExpand(controller.NetworkKeyS2Auth, false);
                            break;
                        case SecurityKey.S2Access:
                            controller.NetworkKeyS2Access.CopyTo(resp, 1);
                            permKey = AES.CKDFExpand(controller.NetworkKeyS2Access, false);
                            break;
                        default:
                            return; //Invalid Key Type - Ignore this
                    }
                    controller.SecurityManager.StoreKey(node.ID, SecurityManager.KeyToType(key), permKey.KeyCCM, permKey.PString, permKey.MPAN);
                    CommandMessage data = new CommandMessage(controller, node.ID, endpoint, commandClass, (byte)Security2Command.NetworkKeyReport, false, resp);
                    await Transmit(data.Payload, SecurityManager.RecordType.ECDH_TEMP);
                    break;
                case Security2Command.NetworkKeyVerify:
                    if (controller.SecurityManager == null)
                        return;
                    Log.Information("Network Key Verified!");
                    SecurityManager.NetworkKey? nk = controller.SecurityManager.GetHighestKey(node.ID);
                    if (nk != null && nk.Key != SecurityManager.RecordType.Entropy && nk.Key != SecurityManager.RecordType.ECDH_TEMP)
                        controller.SecurityManager.RevokeKey(node.ID, nk.Key);
                    CommandMessage transferEnd = new CommandMessage(controller, node.ID, endpoint, commandClass, (byte)Security2Command.TransferEnd, false, 0x2); //Key Verified
                    await Task.Factory.StartNew(() => Transmit(transferEnd.Payload, SecurityManager.RecordType.ECDH_TEMP));
                    break;
                case Security2Command.NonceGet:
                    //TODO - Validate sequence number
                    if (controller.SecurityManager == null)
                        return;
                    Log.Warning("Creating new Nonce");
                    var entropy = controller.SecurityManager.CreateEntropy(node.ID);
                    NonceReport nonceGetReport = new NonceReport(entropy.Sequence, true, false, entropy.Bytes);
                    await SendCommand(Security2Command.NonceReport, CancellationToken.None, nonceGetReport.GetBytes());
                    break;
                case Security2Command.TransferEnd:
                    if (controller.SecurityManager == null)
                        return;
                    KeyExchangeReport? kex = controller.SecurityManager.GetRequestedKeys(node.ID);
                    if (kex == null)
                    {
                        Log.Error("Transfer Complete but no keys were requested");
                        return;
                    }
                    if (message.Payload.Length < 1 || message.Payload.Span[0] != 0x1)
                    {
                        Log.Error("Transfer Complete but key transfer failed");
                        return;
                    }

                    if ((kex.Keys & SecurityKey.S2Unauthenticated) == SecurityKey.S2Unauthenticated)
                    {
                        AES.KeyTuple unauthKey = AES.CKDFExpand(controller.NetworkKeyS2UnAuth, false);
                        controller.SecurityManager.StoreKey(node.ID, SecurityManager.RecordType.S2UnAuth, unauthKey.KeyCCM, unauthKey.PString, unauthKey.MPAN);
                    }
                    if((kex.Keys & SecurityKey.S2Authenticated) == SecurityKey.S2Authenticated)
                    {
                        AES.KeyTuple authKey = AES.CKDFExpand(controller.NetworkKeyS2Auth, false);
                        controller.SecurityManager.StoreKey(node.ID, SecurityManager.RecordType.S2Auth, authKey.KeyCCM, authKey.PString, authKey.MPAN);
                    }
                    if((kex.Keys & SecurityKey.S2Access) == SecurityKey.S2Access)
                    {
                        AES.KeyTuple accessKey = AES.CKDFExpand(controller.NetworkKeyS2Access, false);
                        controller.SecurityManager.StoreKey(node.ID, SecurityManager.RecordType.S2Access, accessKey.KeyCCM, accessKey.PString, accessKey.MPAN);
                    }

                    Log.Information("Transfer Complete");
                    await FireEvent(BootstrapComplete, null);
                    bootstrapComplete.TrySetResult();
                    break;
                case Security2Command.KEXFail:
                    ErrorReport errorMessage;
                    switch (message.Payload.Span[0])
                    {
                        case 0x1:
                            errorMessage = new ErrorReport(0x1, "Key Failure");
                            break;
                        case 0x2:
                            errorMessage = new ErrorReport(0x2, "Scheme Failure");
                            break;
                        case 0x3:
                            errorMessage = new ErrorReport(0x3, "Curve Failure");
                            break;
                        case 0x5:
                            errorMessage = new ErrorReport(0x5, "Decryption Failure");
                            break;
                        case 0x6:
                            errorMessage = new ErrorReport(0x6, "Key Cancel");
                            break;
                        case 0x7:
                            errorMessage = new ErrorReport(0x7, "Auth Failure");
                            break;
                        case 0x8:
                            errorMessage = new ErrorReport(0x8, "Key Get Failure");
                            break;
                        case 0x9:
                            errorMessage = new ErrorReport(0x9, "Key Verify");
                            break;
                        case 0xA:
                            errorMessage = new ErrorReport(0xA, "Key Report");
                            break;
                        default:
                            errorMessage = new ErrorReport(message.Payload.Span[0], "Unknown Key Exchange Failure");
                            break;
                    }
                    Log.Error("Key Exchange Failure " +  errorMessage);
                    await FireEvent(SecurityError, errorMessage);
                    bootstrapComplete.TrySetException(new SecurityException(errorMessage.ErrorMessage));
                    break;
            }
        }

        protected override bool IsSecure(byte command)
        {
            switch ((Security2Command)command)
            {
                case Security2Command.CommandsSupportedGet:
                case Security2Command.CommandsSupportedReport:
                case Security2Command.NetworkKeyGet:
                case Security2Command.NetworkKeyReport:
                case Security2Command.NetworkKeyVerify:
                    return true;
            }
            return false;
        }

        public async Task WaitForBootstrap(CancellationToken cancellationToken)
        {
            bootstrapComplete = new TaskCompletionSource();
            await bootstrapComplete.Task.WaitAsync(cancellationToken);
        }

        public static Memory<byte> EncryptCCM(Memory<byte> plaintext, Memory<byte> nonce, Memory<byte> key, AdditionalAuthData ad)
        {
            Memory<byte> ret = new byte[plaintext.Length + 8];
            using (AesCcm aes = new AesCcm(key.Span))
                aes.Encrypt(nonce.Span, plaintext.Span, ret.Slice(0, plaintext.Length).Span, ret.Slice(plaintext.Length, 8).Span, ad.GetBytes().Span);
            return ret;
        }

        public static Memory<byte> DecryptCCM(Memory<byte> cipherText, Memory<byte> nonce, Memory<byte> key, AdditionalAuthData ad)
        {
            Memory<byte> ret = new byte[cipherText.Length - 8];
            using (AesCcm aes = new AesCcm(key.Span))
                aes.Decrypt(nonce.Span, cipherText.Slice(0, cipherText.Length - 8).Span, cipherText.Slice(cipherText.Length - 8, 8).Span, ret.Span, ad.GetBytes().Span);
            return ret;
        }
    }
}
