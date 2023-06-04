﻿using ZWaveDotNet.Util;

namespace ZWaveDotNet.CommandClassReports
{
    public class NodeNamingLocationReport : ICommandClassReport
    {
        public readonly string Location;

        internal NodeNamingLocationReport(Memory<byte> payload)
        {
            if (payload.Length < 1)
                throw new FormatException($"The response was not in the expected format. {GetType().Name}: Payload: {BitConverter.ToString(payload.ToArray())}");
            Location = PayloadConverter.ToEncodedString(payload, 16);
        }

        public override string ToString()
        {
            return $"Location: {Location}";
        }
    }
}
