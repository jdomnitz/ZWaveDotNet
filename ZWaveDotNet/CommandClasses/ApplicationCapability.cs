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



using ZWaveDotNet.CommandClassReports;
using ZWaveDotNet.CommandClassReports.Enums;
using ZWaveDotNet.Entities;
using ZWaveDotNet.Enums;
using ZWaveDotNet.SerialAPI;

namespace ZWaveDotNet.CommandClasses
{
    /// <summary>
    /// The Application Capability Command Class comprises commands for handling issues related to dynamic support for command classes.
    /// </summary>
    [CCVersion(CommandClass.ApplicationCapability)]
    public class ApplicationCapability : CommandClassBase
    {
        public event CommandClassEvent<ApplicationCapabilityReport>? CommandClassUnsupported;

        internal ApplicationCapability(Node node, byte endpoint) : base(node, endpoint, CommandClass.ApplicationCapability) { }

        ///
        /// <inheritdoc />
        /// 
        internal override async Task<SupervisionStatus> Handle(ReportMessage message)
        {
            await FireEvent(CommandClassUnsupported, new ApplicationCapabilityReport(message.Payload.Span));
            return SupervisionStatus.Success;
        }
    }
}
