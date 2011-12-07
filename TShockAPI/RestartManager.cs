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


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Terraria;
using System.Diagnostics;

namespace TShockAPI
{
    public class RestartManager
    {
        public int Interval { get; set; }

        DateTime lastrestart = DateTime.UtcNow;

        public bool IsRestartTime
        {
            get
            {
                return (Interval > 0) && ((DateTime.UtcNow - lastrestart).TotalMinutes >= Interval);
            }
        }

        public bool PrepareToRestart
        {
            get
            {
                return (Interval > 0) && (Interval - (DateTime.UtcNow - lastrestart).TotalMinutes <= 5);
            }
        }

        public void Restart()
        {
            lastrestart = DateTime.UtcNow;
            DoRestart();
        }

        public void DoRestart()
        {
            Console.WriteLine("Server is being restarted!");
            Log.Info("Server is being restarted!");
            TShock.Utils.ForceKickAll("Server is being restarted!");
            foreach (TSPlayer player in TShock.Players)
            {
                if (player != null && player.Active)
                {
                    if (TShock.Config.StoreInventory)
                        TShock.Inventory.UpdateInventory(player);
                    player.SavePlayer();
                }
            }
            WorldGen.saveWorld();
            Netplay.disconnect = true;
            Process.GetProcessById(TShock.id).Kill();
        }
    }
}