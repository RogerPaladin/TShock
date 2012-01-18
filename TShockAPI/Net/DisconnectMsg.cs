﻿/*
TShock, a server mod for Terraria
Copyright (C) 2011 The TShock Team

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using System.IO;
using System.IO.Streams;
using System.Text;

namespace TShockAPI.Net
{
	internal class DisconnectMsg : BaseMsg
	{
		public override PacketTypes ID
		{
			get { return PacketTypes.Disconnect; }
		}

		public string Reason { get; set; }

		public override void Pack(Stream stream)
		{
			stream.WriteBytes(Encoding.UTF8.GetBytes(Reason));
		}
	}
}