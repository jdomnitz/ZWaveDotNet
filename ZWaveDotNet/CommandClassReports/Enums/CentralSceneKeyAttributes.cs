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

namespace ZWaveDotNet.CommandClassReports.Enums
{
    public enum CentralSceneKeyAttributes
    {
        KeyPressed = 0x00,
        KeyReleased = 0x01,
        KeyHeldDown = 0x02,
        Pressed2Times = 0x03,
        Pressed3Times = 0x04,
        Pressed4Times = 0x05,
        Pressed5Times = 0x06
    }
}
