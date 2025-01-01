﻿// ZWaveDotNet Copyright (C) 2025
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY, without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Data;
using ZWaveDotNet.Util;

namespace ZWaveDotNet.CommandClassReports
{
    public class BatteryHealthReport : ICommandClassReport
    {
        public readonly byte CapacityPercent;
        public readonly float[] Temperatures;

        internal BatteryHealthReport(Span<byte> payload)
        {
            if (payload.Length < 2)
                throw new DataException($"Battery Health Report was not in the expected format. Payload: {MemoryUtil.Print(payload)}");

            CapacityPercent = payload[0];
            Temperatures = PayloadConverter.ToFloats(payload.Slice(1), out byte scale);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Max Capacity: {CapacityPercent}, Battery Temps: {string.Join(',', Temperatures)}";
        }
    }
}
