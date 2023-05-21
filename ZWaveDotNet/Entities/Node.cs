﻿using Serilog;
using System.Collections.ObjectModel;
using ZWaveDotNet.CommandClasses;
using ZWaveDotNet.Enums;
using ZWaveDotNet.SerialAPI;
using ZWaveDotNet.SerialAPI.Enums;
using ZWaveDotNet.SerialAPI.Messages;

namespace ZWaveDotNet.Entities
{
    public class Node
    {
        public readonly ushort ID;
        protected readonly Controller controller;
        protected Dictionary<CommandClass, CommandClassBase> commandClasses = new Dictionary<CommandClass, CommandClassBase>();

        public Node(ushort id, Controller controller, CommandClass[]? commandClasses = null)
        {
            ID = id;
            this.controller = controller;
            if (commandClasses != null)
            {
                foreach (CommandClass cc in commandClasses)
                {
                    if (!this.commandClasses.ContainsKey(cc))
                        this.commandClasses.Add(cc, CommandClassBase.Create(cc, controller, ID, 0));
                }
            }
        }

        private async Task DeleteReturnRoute(CancellationToken cancellationToken)
        {
            await controller.Flow.SendAcknowledged(Function.DeleteReturnRoute, (byte)ID );
        }

        private async Task AssignReturnRoute(ushort associatedNodeId, CancellationToken cancellationToken)
        {
            await controller.Flow.SendAcknowledged(Function.AssignReturnRoute, (byte)ID, (byte)associatedNodeId );
        }

        internal void HandleApplicationUpdate(ApplicationUpdate update)
        {
            Log.Information($"Node {ID} Updated: {update}");
            if (update is NodeInformationUpdate NIF)
            {
                foreach (CommandClass cc in NIF.CommandClasses)
                {
                    if (!commandClasses.ContainsKey(cc))
                        commandClasses.Add(cc, CommandClassBase.Create(cc, controller, ID, 0));
                }
            }
        }

        internal void HandleApplicationCommand(ApplicationCommand cmd)
        {
            ReportMessage? msg = new ReportMessage(cmd);
            Log.Information(msg.ToString());

            //Encapsulation Order (inner to outer) - MultiCommand, Supervision, Multichannel, security, transport, crc16
            if (CRC16.IsEncapsulated(msg))
                msg = CRC16.Free(msg);
            else
            {
                if (TransportService.IsEncapsulated(msg))
                {
                    msg = TransportService.Process(msg);
                    if (msg == null)
                        return; //Not Complete Yet
                }
                //TODO Security
            }
            if (MultiChannel.IsEncapsulated(msg))
                msg = MultiChannel.Free(msg);
            if (Supervision.IsEncapsulated(msg))
                msg = Supervision.Free(msg);
            if (MultiCommand.IsEncapsulated(msg))
            {
                ReportMessage[] msgs = MultiCommand.Free(msg);
                foreach (ReportMessage r in msgs)
                    HandleReport(r);
            }
            else
                HandleReport(msg);
        }

        private void HandleReport(ReportMessage msg)
        {
            if (msg.SourceEndpoint == 0)
            {
                if (commandClasses.ContainsKey(msg.CommandClass))
                    commandClasses[msg.CommandClass].Handle(msg);
            }
            //TODO - Route to EndPoints
        }

        public ReadOnlyDictionary<CommandClass, CommandClassBase> CommandClasses
        {
            get { return new ReadOnlyDictionary<CommandClass, CommandClassBase>(commandClasses); }
        }

        public override string ToString()
        {
            return $"Node: {ID}, CommandClasses: {string.Join(',', commandClasses.Keys)}";
        }
    }
}
