/*   
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
using System.Diagnostics;

using System.IO;
using System.Text;
using Terraria;
using TShockAPI.Net;
using System.IO.Streams;

namespace TShockAPI
{
    public delegate bool GetDataHandlerDelegate(GetDataHandlerArgs args);
    public class GetDataHandlerArgs : EventArgs
    {
        public TSPlayer Player { get; private set; }
        public MemoryStream Data { get; private set; }

        public Player TPlayer
        {
            get { return Player.TPlayer; }
        }

        public GetDataHandlerArgs(TSPlayer player, MemoryStream data)
        {
            Player = player;
            Data = data;
        }
    }
    public static class GetDataHandlers
    {
        private static Dictionary<PacketTypes, GetDataHandlerDelegate> GetDataHandlerDelegates;
        public static bool[] BlacklistTiles;

        public static void InitGetDataHandler()
        {
            // Need to update to 1.1 tiles
            #region Blacklisted tiles

            BlacklistTiles = new bool[Main.maxTileSets];
            BlacklistTiles[0] = true;
            BlacklistTiles[1] = true;
            BlacklistTiles[2] = true;
            BlacklistTiles[6] = true;
            BlacklistTiles[7] = true;
            BlacklistTiles[8] = true;
            BlacklistTiles[9] = true;
            BlacklistTiles[22] = true;
            BlacklistTiles[23] = true;
            BlacklistTiles[25] = true;
            BlacklistTiles[30] = true;
            BlacklistTiles[37] = true;
            BlacklistTiles[38] = true;
            BlacklistTiles[39] = true;
            BlacklistTiles[40] = true;
            BlacklistTiles[41] = true;
            BlacklistTiles[43] = true;
            BlacklistTiles[44] = true;
            BlacklistTiles[45] = true;
            BlacklistTiles[46] = true;
            BlacklistTiles[47] = true;
            BlacklistTiles[53] = true;
            BlacklistTiles[54] = true;
            BlacklistTiles[56] = true;
            BlacklistTiles[57] = true;
            BlacklistTiles[58] = true;
            BlacklistTiles[59] = true;
            BlacklistTiles[60] = true;
            BlacklistTiles[63] = true;
            BlacklistTiles[64] = true;
            BlacklistTiles[65] = true;
            BlacklistTiles[66] = true;
            BlacklistTiles[67] = true;
            BlacklistTiles[68] = true;
            BlacklistTiles[70] = true;
            BlacklistTiles[75] = true;
            BlacklistTiles[76] = true;

            #endregion Blacklisted tiles

            GetDataHandlerDelegates = new Dictionary<PacketTypes, GetDataHandlerDelegate>
            {
                {PacketTypes.PlayerInfo, HandlePlayerInfo},
                {PacketTypes.TileSendSection, HandleSendSection},
                {PacketTypes.PlayerUpdate, HandlePlayerUpdate},
                {PacketTypes.Tile, HandleTile},
                {PacketTypes.TileSendSquare, HandleSendTileSquare},
                {PacketTypes.NpcUpdate, HandleNpcUpdate},
                {PacketTypes.PlayerDamage, HandlePlayerDamage},
                {PacketTypes.ProjectileNew, HandleProjectileNew},
                {PacketTypes.TogglePvp, HandleTogglePvp},
                {PacketTypes.TileKill, HandleTileKill},
                {PacketTypes.PlayerKillMe, HandlePlayerKillMe},
                {PacketTypes.LiquidSet, HandleLiquidSet},
                {PacketTypes.PlayerSpawn, HandleSpawn},
                {PacketTypes.SyncPlayers, HandleSync},
                {PacketTypes.ChestGetContents, HandleChest},
                {PacketTypes.ChestItem, HandleChestItem},
                {PacketTypes.SignNew, HandleSign},
                {PacketTypes.SignRead, HandleSignRead},
                {PacketTypes.PlayerSlot, HandlePlayerSlot},
                {PacketTypes.TileGetSection, HandleGetSection},
                {PacketTypes.UpdateNPCHome, UpdateNPCHome },
                {PacketTypes.PlayerAddBuff, HandlePlayerBuff},
                {PacketTypes.ItemDrop, HandleItemDrop},
            };
        }

        public static bool HandlerGetData(PacketTypes type, TSPlayer player, MemoryStream data)
        {
            GetDataHandlerDelegate handler;
            if (GetDataHandlerDelegates.TryGetValue(type, out handler))
            {
                try
                {
                    return handler(new GetDataHandlerArgs(player, data));
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
            return false;
        }

        private static bool HandleSync(GetDataHandlerArgs args)
        {
            return TShock.Config.EnableAntiLag;
        }

        private static bool HandlePlayerSlot(GetDataHandlerArgs args)
        {
            int plr = args.Data.ReadInt8();
            int slot = args.Data.ReadInt8();
            int stack = args.Data.ReadInt8();
            short prefix = (short) args.Data.ReadInt8();
            int type = (int) args.Data.ReadInt16();

            var it = new Item();
            it.netDefaults(type);
            var itemname = it.name;

            if (!args.Player.Group.HasPermission(Permissions.usebanneditem) && TShock.Itembans.ItemIsBanned(itemname))
            {
                //args.Player.SavePlayer();
                //args.Player.Disconnect("Using banned item: " + itemname + ", remove it and rejoin");
            }
            if (stack>it.maxStack)
            {
                string reason = string.Format("Item Stack Hack Detected: player has {0} {1}(s) in one stack", stack,itemname);
				if (TShock.Config.EnableItemStackChecks)
				{
					TShock.Utils.HandleCheater(args.Player, reason);
				}
            }

            return false;
        }

        private static bool HandlePlayerInfo(GetDataHandlerArgs args)
        {
            byte playerid = args.Data.ReadInt8();
            byte hair = args.Data.ReadInt8();
            byte male = args.Data.ReadInt8();
            args.Data.Position += 21;
            byte difficulty = args.Data.ReadInt8();
            string name = Encoding.ASCII.GetString(args.Data.ReadBytes((int)(args.Data.Length - args.Data.Position - 1)));

            if (hair >= Main.maxHair)
            {
                TShock.Utils.ForceKick(args.Player, "Hair crash exploit.");
                return true;
            }
            if (!TShock.Utils.ValidString(name))
            {
                TShock.Utils.ForceKick(args.Player, "Unprintable character in name");
                return true;
            }
            if (name.Length > 32)
            {
                TShock.Utils.ForceKick(args.Player, "Name exceeded 32 characters.");
                return true;
            }
            if (name.Trim().Length == 0)
            {
                TShock.Utils.ForceKick(args.Player, "Empty Name.");
                return true;
            }
            if (name.Contains("'") || name.Contains("/"))
            {
                TShock.Utils.ForceKick(args.Player, "Forbidden characters in the name.");
                return true;
            }
            var ban = TShock.Bans.GetBanByName(name);
            if (ban != null)
            {
                TShock.Utils.ForceKick(args.Player, string.Format("You are banned: {0}", ban.Reason));
                return true;
            }
            if (args.Player.ReceivedInfo)
            {
                return TShock.Utils.HandleGriefer(args.Player, "Sent client info more than once");
            }
            if (TShock.Config.MediumcoreOnly && difficulty < 1)
            {
                TShock.Utils.ForceKick(args.Player, "Server is set to mediumcore and above characters only!");
                return true;
            }
            if (TShock.Config.HardcoreOnly && difficulty < 2)
            {
                TShock.Utils.ForceKick(args.Player, "Server is set to hardcore characters only!");
                return true;
            }
            args.Player.Difficulty = difficulty;
            args.Player.ReceivedInfo = true;
            return false;
        }

        private static bool HandleSendTileSquare(GetDataHandlerArgs args)
        {
        	
            short size = args.Data.ReadInt16();
            int tilex = args.Data.ReadInt32();
            int tiley = args.Data.ReadInt32();

            if (size > 5)
                return true;

            var tiles = new NetTile[size, size];

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    tiles[x, y] = new NetTile(args.Data);
                }
            }

            bool changed = false;
            for (int x = 0; x < size; x++)
            {
                int realx = tilex + x;
                if (realx < 0 || realx >= Main.maxTilesX)
                    continue;

                for (int y = 0; y < size; y++)
                {
                	int realy = tiley + y;
                	if (realy < 0 || realy >= Main.maxTilesY)
                		continue;

                	var tile = Main.tile[realx, realy];
                	var newtile = tiles[x, y];
                    string RegionName, Owner;
                    if (!args.Player.Group.HasPermission(Permissions.editspawn) && !TShock.Regions.CanBuild(x, y, args.Player, out Owner) && TShock.Regions.InArea(x, y, out RegionName))
                    {
                        if ((DateTime.UtcNow - args.Player.LastTileChangeNotify).TotalMilliseconds > 1000)
                        {
                            args.Player.SendMessage("Region Name: " + RegionName + " protected from changes.", Color.Red);
                            args.Player.LastTileChangeNotify = DateTime.UtcNow;
                        }
                        args.Player.SendTileSquare(x, y);
                        continue;
                    }
                    if (TShock.Config.DisableBuild)
                    {
                        if (!args.Player.Group.HasPermission(Permissions.editspawn))
                        {
                            if ((DateTime.UtcNow - args.Player.LastTileChangeNotify).TotalMilliseconds > 1000)
                            {
                                args.Player.SendMessage("World protected from changes.", Color.Red);
                                args.Player.LastTileChangeNotify = DateTime.UtcNow;
                            }
                            args.Player.SendTileSquare(x, y);
                            continue;
                        }
                    }
                    if (TShock.Config.SpawnProtection)
                    {
                        if (!args.Player.Group.HasPermission(Permissions.editspawn))
                        {
                            var flag = TShock.CheckSpawn(x, y);
                            if (flag)
                            {
                                if ((DateTime.UtcNow - args.Player.LastTileChangeNotify).TotalMilliseconds > 1000)
                                {
                                    args.Player.SendMessage("Spawn protected from changes.", Color.Red);
                                    args.Player.LastTileChangeNotify = DateTime.UtcNow;
                                }
                                args.Player.SendTileSquare(x, y);
                                continue;
                            }
                        }
                    }
                	if ((tile.type == 128 && newtile.Type == 128) || (tile.type == 105 && newtile.Type == 105))
                	{
						//Console.WriteLine("SendTileSquareCalled on a 128 or 105.");
						if (TShock.Config.EnableInsecureTileFixes)
						{
							return false;
						}
                	}

            		if (tile.type == 0x17 && newtile.Type == 0x2)
                    {
                        tile.type = 0x2;
                        changed = true;
                    }
                    else if (tile.type == 0x19 && newtile.Type == 0x1)
                    {
                        tile.type = 0x1;
                        changed = true;
                    }
                    else if ((tile.type == 0xF && newtile.Type == 0xF) ||
                             (tile.type == 0x4F && newtile.Type == 0x4F))
                    {
                        tile.frameX = newtile.FrameX;
                        tile.frameY = newtile.FrameY;
                        changed = true;
                    }
                    else if (tile.type == 1 && newtile.Type == 117)
                    {
                        tile.type = 117;
                        changed = true;
                    }
                    else if (tile.type == 1 && newtile.Type == 25)
                    {
                        tile.type = 25;
                        changed = true;
                    }
                    else if (tile.type == 117 && newtile.Type == 25)
                    {
                        tile.type = 25;
                        changed = true;
                    }
                    else if (tile.type == 25 && newtile.Type == 117)
                    {
                        tile.type = 117;
                        changed = true;
                    }
                    else if (tile.type == 2 && newtile.Type == 23)
                    {
                        tile.type = 23;
                        changed = true;
                    }
                    else if (tile.type == 2 && newtile.Type == 109)
                    {
                        tile.type = 109;
                        changed = true;
                    }
                    else if (tile.type == 23 && newtile.Type == 109)
                    {
                        tile.type = 109;
                        changed = true;
                    }
                    else if (tile.type == 109 && newtile.Type == 23)
                    {
                        tile.type = 23;
                        changed = true;
                    }
                    else if (tile.type == 23 && newtile.Type == 109)
                    {
                        tile.type = 109;
                        changed = true;
                    }
                    else if (tile.type == 53 && newtile.Type == 116)
                    {
                        tile.type = 116;
                        changed = true;
                    }
                    else if (tile.type == 53 && newtile.Type == 112)
                    {
                        tile.type = 112;
                        changed = true;
                    }
                    else if (tile.type == 112 && newtile.Type == 116)
                    {
                        tile.type = 116;
                        changed = true;
                    }
                    else if (tile.type == 116 && newtile.Type == 112)
                    {
                        tile.type = 112;
                        changed = true;
                    }
                    else if (tile.type == 112 && newtile.Type == 53)
                    {
                        tile.type = 53;
                        changed = true;
                    }
                }
            }

			if (changed)
			{
				TSPlayer.All.SendTileSquare(tilex, tiley, 3);
				WorldGen.RangeFrame(tilex, tiley, tilex + size, tiley + size);
			}
        	return true;
        }

        private static bool HandleTile(GetDataHandlerArgs args)
        {
            byte type = args.Data.ReadInt8();
            int x = args.Data.ReadInt32();
            int y = args.Data.ReadInt32();
            byte tiletype = args.Data.ReadInt8();
            string Owner = string.Empty;
            string RegionName = string.Empty;
            Item heart = TShock.Utils.GetItemById(58);
            Item star = TShock.Utils.GetItemById(184);
            Random Rand = new Random();

            if (args.Player.AwaitingName)
            {
                if (TShock.Regions.InArea(args.Player.TileX, args.Player.TileY, out RegionName) && TShock.Regions.CanBuild(args.Player.TileX, args.Player.TileY, args.Player, out Owner) || !TShock.Regions.CanBuild(args.Player.TileX, args.Player.TileY, args.Player, out Owner))
                args.Player.SendMessage("This region <" + RegionName + "> is protected by " + Owner, Color.Yellow);
                args.Player.SendTileSquare(x, y);
                args.Player.AwaitingName = false;
                return true;
            }

            if (args.Player.AwaitingTempPoint > 0)
            {
                args.Player.TempPoints[args.Player.AwaitingTempPoint - 1].X = x;
                args.Player.TempPoints[args.Player.AwaitingTempPoint - 1].Y = y;
                args.Player.SendMessage("Set Temp Point " + args.Player.AwaitingTempPoint, Color.Yellow);
                args.Player.SendTileSquare(x, y);
                args.Player.AwaitingTempPoint = 0;
                return true;
            }

            if (!args.Player.Group.HasPermission(Permissions.canbuild))
            {
                if (!args.Player.HasBeenSpammedWithBuildMessage)
                {
                    args.Player.SendMessage("You do not have permission to build!", Color.Red);
                    args.Player.HasBeenSpammedWithBuildMessage = true;
                }
                args.Player.SendTileSquare(x, y);
                return true;
            }
			
            if (type == 1 || type == 3)
            {
                int plyX = Math.Abs(args.Player.TileX);
                int plyY = Math.Abs(args.Player.TileY);
                int tileX = Math.Abs(x);
                int tileY = Math.Abs(y);

                if (tiletype >= ((type == 1) ? Main.maxTileSets : Main.maxWallTypes))
                {
                    TShock.Utils.HandleGriefer(args.Player, string.Format(TShock.Config.TileAbuseReason, "Invalid tile type"));
                    return true;
                }
                if (TShock.Config.RangeChecks && ((Math.Abs(plyX - tileX) > 32) || (Math.Abs(plyY - tileY) > 32)))
                {
					if ((type == 1 && ((tiletype == 0 && args.Player.TPlayer.selectedItem == 114) || (tiletype == 127 && args.Player.TPlayer.selectedItem == 496)|| (tiletype == 53 && args.Player.TPlayer.selectedItem == 266))))
					{
						if (!TShock.Config.EnableRangeCheckOverrides)
						{
							args.Player.SendMessage("This item has been disabled by the server owner.");
							return true;
						}
					} else
                    {
                        Log.Debug(string.Format("TilePlaced(PlyXY:{0}_{1}, TileXY:{2}_{3}, Result:{4}_{5}, Type:{6})",
                                                plyX, plyY, tileX, tileY, Math.Abs(plyX - tileX), Math.Abs(plyY - tileY), tiletype));
                        return TShock.Utils.HandleGriefer(args.Player, TShock.Config.RangeCheckBanReason);
                    }
                }
                if (tiletype == 48 && !args.Player.Group.HasPermission(Permissions.canspike))
                {
                    args.Player.SendMessage("You do not have permission to place spikes.", Color.Red);
                    TShock.Utils.SendLogs(string.Format("{0} tried to place spikes", args.Player.Name), Color.Red);
                    args.Player.SendTileSquare(x, y);
                    return true;
                }
                if (type == 1 && tiletype == 21 && TShock.Utils.MaxChests())
                {
                    args.Player.SendMessage("Reached world's max chest limit, unable to place more!", Color.Red);
                    Log.Info("Reached world's chest limit, unable to place more.");
                    args.Player.SendTileSquare(x, y);
                    return true;
                }
                if (tiletype == 37 && !args.Player.Group.HasPermission(Permissions.canmeteor))
                {
                    args.Player.SendMessage("You do not have permission to place meteorite.", Color.Red);
                    TShock.Utils.SendLogs(string.Format("{0} tried to place meteorite", args.Player.Name), Color.Red);
                    args.Player.SendTileSquare(x, y);
                    return true;
                }
                if (tiletype == 23 /*Corrupt Seeds*/ && !args.Player.Group.HasPermission(Permissions.cancorruption) || tiletype == 109 /*Hallowed Seeds*/ && !args.Player.Group.HasPermission(Permissions.cancorruption))
                {
                    args.Player.SendMessage("You do not have permission to place corruptions.", Color.Red);
                    TShock.Utils.SendLogs(string.Format("{0} tried to place corruption", args.Player.Name), Color.Red);
                    args.Player.SendTileSquare(x, y);
                    return true;
                }
                if (type == 1 && tiletype == 29 && !args.Player.Group.HasPermission(Permissions.adminstatus))
                {
                    args.Player.SendMessage("You do not have permission to place piggy bank.", Color.Red);
                    TShock.Utils.SendLogs(string.Format("{0} tried to place piggy bank", args.Player.Name), Color.Red);
                    args.Player.SendTileSquare(x, y);
                    return true;
                }
                if (type == 1 && tiletype == 97 && !args.Player.Group.HasPermission(Permissions.adminstatus))
                {
                    args.Player.SendMessage("You do not have permission to place safe.", Color.Red);
                    TShock.Utils.SendLogs(string.Format("{0} tried to place safe", args.Player.Name), Color.Red);
                    args.Player.SendTileSquare(x, y);
                    return true;
                }
                if (tiletype == 53 && !args.Player.Group.HasPermission(Permissions.cansand) && TShock.Regions.CanBuild(x, y, args.Player, out Owner))
                {
                    if ((DateTime.UtcNow - args.Player.LastTileChangeNotify).TotalMilliseconds > 2000 || TShock.Regions.InArea(x, y, out RegionName))
                    {
                        //args.Player.SendMessage("You do not have permission to place sand.", Color.Red);
                        //TShock.Utils.SendLogs(string.Format("{0} tried to place sand", args.Player.Name), Color.Red);
                        args.Player.LastTileChangeNotify = DateTime.UtcNow;
                        return true;
                    }
                    args.Player.SendMessage("Please wait another " + Math.Round(2 - ((DateTime.UtcNow - args.Player.LastTileChangeNotify).TotalSeconds), 1) + " seconds.", Color.Red);
                    args.Player.SendTileSquare(x, y, 1);
                    args.Player.LastTileChangeNotify = DateTime.UtcNow;
                    Log.ConsoleInfo(string.Format("{0} tried to place sand in [{1};{2}]", args.Player.Name, x, y));
                    return true;
                }
            }
            if (!args.Player.Group.HasPermission(Permissions.manageregion) && !args.Player.IsLoggedIn)
            {
                if ((DateTime.UtcNow - args.Player.LastTileChangeNotify).TotalMilliseconds > 1000)
                {
                    args.Player.SendMessage("Login to change this region", Color.Red);
                    args.Player.LastTileChangeNotify = DateTime.UtcNow;
                }
                args.Player.SendTileSquare(x, y);
                return true;
            }
            #region AltarDispenser
            if (TShock.Utils.Altar(x, y, 45, 39, 41) && !args.Player.Group.HasPermission(Permissions.altaredit))
            {
                args.Player.SendTileSquare(x, y);
                if (TShock.Users.Buy(args.Player.Name, 3, true))
                {
                    if ((DateTime.UtcNow - args.Player.LastTileChangeNotify).TotalMilliseconds > 1000)
                    {
                        args.Player.LastTileChangeNotify = DateTime.UtcNow;
                        if ((DateTime.UtcNow - Convert.ToDateTime(TShock.Utils.DispencerTime(args.Player.Name))).TotalMilliseconds > TShock.disptime)
                        {
                            TShock.DispenserTime.Remove(args.Player.Name + ";" + Convert.ToString(TShock.Utils.DispencerTime(args.Player.Name)));
                            TShock.DispenserTime.Add(args.Player.Name + ";" + Convert.ToString(DateTime.UtcNow));
                            args.Player.Dispenser++;
                            if (args.Player.Dispenser >= 2)
                            {
                                return true;
                            }
                            int rand = Rand.Next(1, 327);
                            do
                                rand = Rand.Next(1, 327);

                            while (rand == 2 || rand == 3 || rand == 9 || rand == 11 || rand == 12 || rand == 13 || rand == 14 ||
                                rand == 19 || rand == 20 || rand == 21 || rand == 22 || rand == 26 || rand == 30 || rand == 56 ||
                                rand == 57 || rand == 59 || rand == 61 || rand == 93 || rand == 94 || rand == 116 || rand == 117 ||
                                rand == 126 || rand == 129 || rand == 130 || rand == 131 || rand == 132 || rand == 133 || rand == 134 ||
                                rand == 135 || rand == 137 || rand == 138 || rand == 139 || rand == 140 || rand == 141 || rand == 142 ||
                                rand == 143 || rand == 144 || rand == 145 || rand == 146 || rand == 166 || rand == 167 || rand == 172 ||
                                rand == 173 || rand == 174 || rand == 175 || rand == 176 || rand == 197 || rand == 205 || rand == 207 ||
                                rand == 222 || rand == 235 || rand == 266 || rand == 297);

                            Item Prize = TShock.Utils.GetItemById(rand);
                            if (Prize.maxStack == 1)
                            {
                                args.Player.GiveItem(Prize.type, Prize.name, Prize.width, Prize.height, 1);
                            }
                            else
                            {
                                args.Player.GiveItem(Prize.type, Prize.name, Prize.width, Prize.height, 10);
                            }
                            args.Player.SendMessage("You spent 3 RCoins.", Color.BlanchedAlmond);
                            TShock.Users.Buy(args.Player.Name, 3);
                            TShock.Utils.Broadcast(string.Format("WINNER! {0} win a prize - {1}.", args.Player.Name, Prize.name), Color.LightCoral);
                            args.Player.SendMessage("You win " + Prize.name);
                            return true;
                        }
                        args.Player.Dispenser = 0;
                        double minutes = Math.Round(15 - (DateTime.UtcNow - Convert.ToDateTime(TShock.Utils.DispencerTime(args.Player.Name))).TotalMinutes, 0);
                        double seconds = Math.Round(900 - (DateTime.UtcNow - Convert.ToDateTime(TShock.Utils.DispencerTime(args.Player.Name))).TotalSeconds, 0) - (minutes * 60 - 30);
                        args.Player.SendMessage(string.Format("Please wait for {0} minutes {1} seconds", minutes, seconds), Color.Orchid);
                        return true;
                    }
                }
                else
                {
                    args.Player.SendMessage("You need 3 RCoins to use dispenser.", Color.Red);
                    return true;
                } 
                return true;
            }
            #endregion
            #region HardcoreSpawner
            if (TShock.Utils.Altar(x, y, 58, 8, 1) && !args.Player.Group.HasPermission(Permissions.altaredit))
            {
                args.Player.SendTileSquare(x, y);
                if ((DateTime.UtcNow - args.Player.LastTileChangeNotify).TotalMilliseconds > 1000)
                {
                    if ((DateTime.UtcNow - TShock.Spawner).TotalMilliseconds > 1000 * 60 * 30)
                    {
                        args.Player.DamagePlayer(100);
                        NPC skeletron = TShock.Utils.GetNPCById(35);
                        NPC slime = TShock.Utils.GetNPCById(50);
                        NPC eye = TShock.Utils.GetNPCById(4);
                        NPC eater = TShock.Utils.GetNPCById(13);
                        TSPlayer.Server.SetTime(false, 0.0);
                        TSPlayer.Server.SpawnNPC(skeletron.type, skeletron.name, 3, (int)args.Player.TileX, (int)args.Player.TileY);
                        TSPlayer.Server.SpawnNPC(slime.type, slime.name, 3, (int)args.Player.TileX, (int)args.Player.TileY + 20);
                        TSPlayer.Server.SpawnNPC(eye.type, eye.name, 3, (int)args.Player.TileX, (int)args.Player.TileY);
                        TSPlayer.Server.SpawnNPC(eater.type, eater.name, 3, (int)args.Player.TileX, (int)args.Player.TileY);
                        TShock.Utils.Broadcast(string.Format("{0} awakened an ancient evil in PVP arena!", args.Player.Name), Color.Moccasin);
                        TShock.Spawner = DateTime.UtcNow;
                        args.Player.LastTileChangeNotify = DateTime.UtcNow;
                        return true;
                    }
                    args.Player.LastTileChangeNotify = DateTime.UtcNow;
                    double minutes = Math.Round(30 - (DateTime.UtcNow - TShock.Spawner).TotalMinutes, 0);
                    double seconds = Math.Round(1800 - (DateTime.UtcNow - TShock.Spawner).TotalSeconds, 0) - (minutes * 60 - 30);
                    args.Player.SendMessage(string.Format("Please wait for {0} minutes {1} seconds", minutes, seconds), Color.Orchid);
                    return true;
                }

                if (tiletype == 141 && !args.Player.Group.HasPermission(Permissions.canexplosive))
                {
                    args.Player.SendMessage("You do not have permission to place explosives.", Color.Red);
                    TShock.Utils.SendLogs(string.Format("{0} tried to place explosives", args.Player.Name), Color.Red);
                    args.Player.SendTileSquare(x, y);
                    return true;
                }
			return true;
                
            }
            #endregion
            #region Healstone
            if (Main.tile[x, y].type == 0x55 && !args.Player.Group.HasPermission(Permissions.altaredit))
            {
                args.Player.SendTileSquare(x, y);
                if ((DateTime.UtcNow - args.Player.LastTileChangeNotify).TotalMilliseconds > 1000)
                {
                    for (int i = 0; i < 20; i++)
                        args.Player.GiveItem(heart.type, heart.name, heart.width, heart.height, heart.maxStack);
                    for (int i = 0; i < 10; i++)
                        args.Player.GiveItem(star.type, star.name, star.width, star.height, star.maxStack);
                    args.Player.SendMessage("You healed by Black Roger's soul :D");
                    args.Player.LastTileChangeNotify = DateTime.UtcNow;
                    return true;
                }
                return true;
            }
            #endregion
            
            if (!args.Player.Group.HasPermission(Permissions.editspawn) && !TShock.Regions.CanBuild(x, y, args.Player, out Owner) && TShock.Regions.InArea(x, y, out RegionName))
            {
                if ((DateTime.UtcNow - args.Player.LastTileChangeNotify).TotalMilliseconds > 1000)
                {
                    args.Player.SendMessage("This region <" + RegionName + "> is protected by " + Owner, Color.Red);
                    args.Player.LastTileChangeNotify = DateTime.UtcNow;
                }
                args.Player.SendTileSquare(x, y);
                return true;
            }
            if (TShock.Config.DisableBuild)
            {
                if (!args.Player.Group.HasPermission(Permissions.editspawn))
                {
                    if ((DateTime.UtcNow - args.Player.LastTileChangeNotify).TotalMilliseconds > 1000)
                    {
                        args.Player.SendMessage("World protected from changes.", Color.Red);
                        args.Player.LastTileChangeNotify = DateTime.UtcNow;
                    }
                    args.Player.SendTileSquare(x, y);
                    return true;
                }
            }
            if (TShock.Config.SpawnProtection)
            {
                if (!args.Player.Group.HasPermission(Permissions.editspawn))
                {
                    var flag = TShock.CheckSpawn(x, y);
                    if (flag)
                    {
                        if ((DateTime.UtcNow - args.Player.LastTileChangeNotify).TotalMilliseconds > 1000)
                        {
                            args.Player.SendMessage("Spawn protected from changes.", Color.Red);
                            args.Player.LastTileChangeNotify = DateTime.UtcNow;
                        }
                        args.Player.SendTileSquare(x, y);
                        return true;
                    }
                }
            }
            if (type == 0 && BlacklistTiles[Main.tile[x, y].type] && args.Player.Active)
            {
                args.Player.TileThreshold++;
                var coords = new Vector2(x, y);
                if (!args.Player.TilesDestroyed.ContainsKey(coords))
                    args.Player.TilesDestroyed.Add(coords, Main.tile[x, y]);
            }

            if ((DateTime.UtcNow - args.Player.LastExplosive).TotalMilliseconds < 1000)
            {
                args.Player.SendMessage("Please wait another " + (1000 - (DateTime.UtcNow - args.Player.LastExplosive).TotalMilliseconds) + " milliseconds before placing/destroying tiles", Color.Red);
                args.Player.SendTileSquare(x, y);
                return true;
            }
            return false;
        }

        private static bool HandleTogglePvp(GetDataHandlerArgs args)
        {
            int id = args.Data.ReadByte();
            bool pvp = args.Data.ReadBoolean();

            long seconds = (long)(DateTime.UtcNow - args.Player.LastPvpChange).TotalSeconds;
            if (TShock.Config.PvpThrottle > 0 && seconds < TShock.Config.PvpThrottle)
            {
                args.Player.SendMessage(string.Format("You cannot change pvp status for {0} seconds", TShock.Config.PvpThrottle - seconds), 255, 0, 0);
                args.Player.SetPvP(id != args.Player.Index || TShock.Config.AlwaysPvP ? true : args.TPlayer.hostile);
            }
            else
            {
                args.Player.SetPvP(id != args.Player.Index || TShock.Config.AlwaysPvP ? true : pvp);
            }
            return true;
        }

        private static bool HandleSendSection(GetDataHandlerArgs args)
        {
            return TShock.Utils.HandleGriefer(args.Player, TShock.Config.SendSectionAbuseReason);
        }

        private static bool HandleNpcUpdate(GetDataHandlerArgs args)
        {
            return TShock.Utils.HandleGriefer(args.Player, TShock.Config.NPCSpawnAbuseReason);
        }

        private static bool HandlePlayerUpdate(GetDataHandlerArgs args)
        {
            byte plr = args.Data.ReadInt8();
            byte control = args.Data.ReadInt8();
            byte item = args.Data.ReadInt8();
            float posx = args.Data.ReadSingle();
            float posy = args.Data.ReadSingle();
            float velx = args.Data.ReadSingle();
            float vely = args.Data.ReadSingle();

            if (Main.verboseNetplay)
                Debug.WriteLine("Update: {{{0},{1}}} {{{2}, {3}}}", (int)posx, (int)posy, (int)velx, (int)vely);

            if (plr != args.Player.Index)
            {
                return TShock.Utils.HandleGriefer(args.Player, TShock.Config.UpdatePlayerAbuseReason);
            }

            if (item < 0 || item >= args.TPlayer.inventory.Length)
            {
                TShock.Utils.HandleGriefer(args.Player, TShock.Config.UpdatePlayerAbuseReason);
                return true;
            }

            return false;
        }

        private static bool HandleProjectileNew(GetDataHandlerArgs args)
        {
            short ident = args.Data.ReadInt16();
            float posx = args.Data.ReadSingle();
            float posy = args.Data.ReadSingle();
            float velx = args.Data.ReadSingle();
            float vely = args.Data.ReadSingle();
            float knockback = args.Data.ReadSingle();
            short dmg = args.Data.ReadInt16();
            byte owner = args.Data.ReadInt8();
            byte type = args.Data.ReadInt8();
            var index = TShock.Utils.SearchProjectile(ident);

            if (index > Main.maxProjectiles || index < 0)
            {
                TShock.Utils.HandleGriefer(args.Player, TShock.Config.ExplosiveAbuseReason);
                return true;
            }

            if (dmg > 100) // random number, if false positives, increase
            {
                TShock.Utils.SendLogs(string.Format("{0} sent a projectile with more than 80 damage.", args.Player.Name), Color.Red);
                if (dmg > 175)
                {
                    TShock.Utils.HandleCheater(args.Player, TShock.Config.ProjectileAbuseReason);
                    return true;
                }
            }

            if (type == 23)
            {
                if (velx == 0f && vely == 0f && dmg == 99)
                {
                    TShock.Utils.HandleGriefer(args.Player, TShock.Config.ProjectileAbuseReason);
                    return true;
                }
                else if (velx == 0f || vely == 0f)
                    return true;
            }

            if (type == 29 || type == 28 || type == 37) //need more explosives from 1.1
            {
                Log.Debug(string.Format("Explosive(PlyXY:{0}_{1}, Type:{2})", args.Player.TileX, args.Player.TileY, type));
                if (TShock.Config.DisableExplosives && (!args.Player.Group.HasPermission(Permissions.useexplosives) && !args.Player.Group.HasPermission(Permissions.ignoregriefdetection)))
                {
                    //Main.projectile[index].SetDefaults(0);
                    Main.projectile[index].type = 0;
                    //Main.projectile[index].owner = 255;
                    //Main.projectile[index].position = new Vector2(0f, 0f);
                    Main.projectile[index].identity = ident;
                    args.Player.SendData(PacketTypes.ProjectileNew, "", index);
                    args.Player.SendMessage("Explosives are disabled!", Color.Red);
                    args.Player.LastExplosive = DateTime.UtcNow;
                    return true;
                }
                else
                    return TShock.Utils.HandleExplosivesUser(args.Player, TShock.Config.ExplosiveAbuseReason);
            }

            if (type == 69 /*Holy water*/ || type == 70 /*Unholy Water*/)
            {
                Log.Debug(string.Format("Corruption(PlyXY:{0}_{1}, Type:{2})", args.Player.TileX, args.Player.TileY, type));
                if (TShock.Config.DisableCorruption && (!args.Player.Group.HasPermission(Permissions.usecorruption) && !args.Player.Group.HasPermission(Permissions.ignoregriefdetection)))
                {
                    //Main.projectile[index].SetDefaults(0);
                    Main.projectile[index].type = 0;
                    //Main.projectile[index].owner = 255;
                    //Main.projectile[index].position = new Vector2(0f, 0f);
                    Main.projectile[index].identity = ident;
                    args.Player.SendData(PacketTypes.ProjectileNew, "", index);
                    args.Player.SendMessage("Corruptions are disabled!", Color.Red);
                    args.Player.LastCorruption = DateTime.UtcNow;
                    return true;
                }
                else
                    return TShock.Utils.HandleCorruptionUser(args.Player, TShock.Config.CorruptionAbuseReason);
            }
            if (args.Player.Index != owner)//ignores projectiles whose senders aren't the same as their owners
            {
                TShock.Players[args.Player.Index].SendData(PacketTypes.ProjectileNew, "", index);//update projectile on senders end so he knows it didnt get created
                return true;
            }
            Projectile proj = new Projectile();
            proj.SetDefaults(type);
            if (proj.hostile)//ignores all hostile projectiles from the client they shouldn't be sending them anyways
            {
                TShock.Players[args.Player.Index].SendData(PacketTypes.ProjectileNew, "", index);
                return true;
            }
            return false;
        }
        
        private static bool HandlePlayerKillMe(GetDataHandlerArgs args)
        {
            byte id = args.Data.ReadInt8();
            byte direction = args.Data.ReadInt8();
            short dmg = args.Data.ReadInt16();
            bool pvp = args.Data.ReadInt8() == 0;
            int textlength = (int)(args.Data.Length - args.Data.Position - 1);
            string deathtext = "";
            if (textlength > 0)
            {
                deathtext = Encoding.ASCII.GetString(args.Data.ReadBytes(textlength));
                if (!TShock.Utils.ValidString(deathtext))
                {
                    TShock.Utils.HandleGriefer(args.Player, "Death text exploit.");
                    return true;
                }
            }
            if (id != args.Player.Index)
            {
                return TShock.Utils.HandleGriefer(args.Player, TShock.Config.KillMeAbuseReason);
            }
            args.Player.LastDeath = DateTime.Now;
            if (args.Player.Difficulty != 2)
                args.Player.ForceSpawn = true;
            return false;
        }

        private static bool HandlePlayerDamage(GetDataHandlerArgs args)
        {
            byte playerid = args.Data.ReadInt8();
            if (TShock.Players[playerid] == null)
                return true;

            return !TShock.Players[playerid].TPlayer.hostile;
        }

        private static bool HandleLiquidSet(GetDataHandlerArgs args)
        {
            int x = args.Data.ReadInt32();
            int y = args.Data.ReadInt32();
            byte liquid = args.Data.ReadInt8();
            bool lava = args.Data.ReadBoolean();

            //The liquid was picked up.
            if (liquid == 0)
                return false;

            int plyX = Math.Abs(args.Player.TileX);
            int plyY = Math.Abs(args.Player.TileY);
            int tileX = Math.Abs(x);
            int tileY = Math.Abs(y);

            bool bucket = false;
            for (int i = 0; i < 49; i++)
            {
                if (args.TPlayer.inventory[i].type >= 205 && args.TPlayer.inventory[i].type <= 207)
                {
                    bucket = true;
                    break;
                }
            }

            if (!args.Player.Group.HasPermission(Permissions.canbuild))
            {
                args.Player.SendMessage("You do not have permission to build!", Color.Red);
                args.Player.SendTileSquare(x, y);
                return true;
            }

            if (lava && !args.Player.Group.HasPermission(Permissions.canlava))
            {
                args.Player.SendMessage("You do not have permission to use lava", Color.Red);
                TShock.Utils.SendLogs(string.Format("{0} tried using lava", args.Player.Name), Color.Red);
                args.Player.SendTileSquare(x, y);
                return true;
            }
            if (!lava && !args.Player.Group.HasPermission(Permissions.canwater))
            {
                args.Player.SendMessage("You do not have permission to use water", Color.Red);
                TShock.Utils.SendLogs(string.Format("{0} tried using water", args.Player.Name), Color.Red);
                args.Player.SendTileSquare(x, y);
                return true;
            }

            if (!bucket)
            {
                Log.Debug(string.Format("{0}(PlyXY:{1}_{2}, TileXY:{3}_{4}, Result:{5}_{6}, Amount:{7})",
                                        lava ? "Lava" : "Water", plyX, plyY, tileX, tileY,
                                        Math.Abs(plyX - tileX), Math.Abs(plyY - tileY), liquid));
                return TShock.Utils.HandleGriefer(args.Player, TShock.Config.IllogicalLiquidUseReason); ;
            }
            if (TShock.Config.RangeChecks && ((Math.Abs(plyX - tileX) > 32) || (Math.Abs(plyY - tileY) > 32)))
            {
                Log.Debug(string.Format("Liquid(PlyXY:{0}_{1}, TileXY:{2}_{3}, Result:{4}_{5}, Amount:{6})",
                                        plyX, plyY, tileX, tileY, Math.Abs(plyX - tileX), Math.Abs(plyY - tileY), liquid));
                return TShock.Utils.HandleGriefer(args.Player, TShock.Config.LiquidAbuseReason);
            }

            if (TShock.Config.SpawnProtection)
            {
                if (!args.Player.Group.HasPermission(Permissions.editspawn))
                {
                    var flag = TShock.CheckSpawn(x, y);
                    if (flag)
                    {
                        args.Player.SendMessage("The spawn is protected!", Color.Red);
                        args.Player.SendTileSquare(x, y);
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool HandleTileKill(GetDataHandlerArgs args)
        {
            int tilex = args.Data.ReadInt32();
            int tiley = args.Data.ReadInt32();
            string Owner = string.Empty;
            string RegionName = string.Empty;
            if (tilex < 0 || tilex >= Main.maxTilesX || tiley < 0 || tiley >= Main.maxTilesY)
                return false;

            if (Main.tile[tilex, tiley].type != 0x15 && (!TShock.Utils.MaxChests() && Main.tile[tilex, tiley].type != 0)) //Chest
            {
                Log.Debug(string.Format("TileKill(TileXY:{0}_{1}, Type:{2})",
                                        tilex, tiley, Main.tile[tilex, tiley].type));
                TShock.Utils.ForceKick(args.Player, string.Format(TShock.Config.TileKillAbuseReason, Main.tile[tilex, tiley].type));
                return true;
            }
            if (!args.Player.Group.HasPermission(Permissions.canbuild))
            {
                args.Player.SendMessage("You do not have permission to build!", Color.Red);
                args.Player.SendTileSquare(tilex, tiley);
                return true;
            }
            if (!args.Player.Group.HasPermission(Permissions.editspawn) && !TShock.Regions.CanBuild(tilex, tiley, args.Player, out Owner) && TShock.Regions.InArea(tilex, tiley, out RegionName))
            {
                args.Player.SendMessage("Region protected from changes.", Color.Red);
                args.Player.SendTileSquare(tilex, tiley);
                return true;
            }
            if (TShock.Config.DisableBuild)
            {
                if (!args.Player.Group.HasPermission(Permissions.editspawn))
                {
                    args.Player.SendMessage("World protected from changes.", Color.Red);
                    args.Player.SendTileSquare(tilex, tiley);
                    return true;
                }
            }
            if (TShock.Config.SpawnProtection)
            {
                if (!args.Player.Group.HasPermission(Permissions.editspawn))
                {
                    var flag = TShock.CheckSpawn(tilex, tiley);
                    if (flag)
                    {
                        args.Player.SendMessage("Spawn protected from changes.", Color.Red);
                        args.Player.SendTileSquare(tilex, tiley);
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool HandleSpawn(GetDataHandlerArgs args)
        {
            byte player = args.Data.ReadInt8();
            int spawnx = args.Data.ReadInt32();
            int spawny = args.Data.ReadInt32();

            if (args.Player.InitSpawn && args.TPlayer.inventory[args.TPlayer.selectedItem].type != 50)
            {
                if (args.TPlayer.difficulty == 1 && (TShock.Config.KickOnMediumcoreDeath || TShock.Config.BanOnMediumcoreDeath))
                {
                    if (args.TPlayer.selectedItem != 50)
                    {
                        if (TShock.Config.BanOnMediumcoreDeath)
                        {
                            if (!TShock.Utils.Ban(args.Player, TShock.Config.MediumcoreBanReason))
                                TShock.Utils.ForceKick(args.Player, "Death results in a ban, but can't ban you");
                        }
                        else
                        {
                            TShock.Utils.ForceKick(args.Player, TShock.Config.MediumcoreKickReason);
                        }
                        return true;
                    }
                }
                if (TShock.Config.RememberHome)
                {
                    if (TShock.HomeManager.GetHome(args.Player.Name) != Vector2.Zero)
                    {
                        var pos = TShock.HomeManager.GetHome(args.Player.Name);
                        args.Player.Teleport((int)pos.X, (int)pos.Y);
                        args.Player.SendTileSquare((int)pos.X, (int)pos.Y);
                        return false;
                    }
                }
            }
            else
            {
                args.Player.InitSpawn = true;
            }

            return false;
        }

        private static bool HandleChest(GetDataHandlerArgs args)
        {
            var x = args.Data.ReadInt32();
            var y = args.Data.ReadInt32();
            string Owner = string.Empty;
            string RegionName = string.Empty;
            string PlayerName = string.Empty;
            string[] ItemName;
            double Price = 0;
            int id = Chest.FindChest(x, y);
            Item[] item = new Item[5];
            int[] Quantity = new int[5];
            int Count = 0;
            int[] index = new int[5];
            
            if (TShock.Config.RangeChecks && ((Math.Abs(args.Player.TileX - x) > 32) || (Math.Abs(args.Player.TileY - y) > 32)))
            {
                return TShock.Utils.HandleGriefer(args.Player, TShock.Config.RangeCheckBanReason);
            }
            
            if (TShock.Utils.Signs(x, y, out  PlayerName, out  ItemName, out  Price))
            {
                var players = TShock.Utils.FindPlayer(PlayerName);
                item = new Item[ItemName.Length];
                Quantity = new int[ItemName.Length];
                index = new int[ItemName.Length];
                
                for (int i = 0; i < ItemName.Length; i++)
                {
                    Quantity[i] = int.Parse(ItemName[i].Split(':')[1]);
                    ItemName[i] = ItemName[i].Split(':')[0];
                    if (ItemName[i].Equals(string.Empty))
                    {
                        break;
                    }
                        var items = TShock.Utils.GetItemByIdOrName(ItemName[i]);
                        if (items.Count == 0)
                        {
                            args.Player.SendMessage("Invalid item name <" + ItemName[i] + ">", Color.Red);
                            return true;
                        }
                        item[i] = items[0];

                }
                if (TShock.Users.GetUserByName(PlayerName) == null)
                {
                   args.Player.SendMessage("User by that name <" + PlayerName + "> does not exist", Color.Red);
                   return true;
                }
                if ((!TShock.Users.Buy(args.Player.Name, Price, true)))
                {
                    args.Player.SendMessage("You need " + Price + " RCoins to buy items", Color.Red);
                    return true;
                }
                if (!args.Player.InventorySlotAvailable)
                {
                    args.Player.SendMessage("You don't have free slots!", Color.Red);
                    return true;
                }
                for (int i = 0; i < Chest.maxItems; i++)
                {
                    for (int d = 0; d < item.Length; d++)
                    {
                        if (Main.chest[id].item[i].name == item[d].name)
                        {
                            if (Main.chest[id].item[i].stack >= Quantity[d])
                            {
                                Count++;
                                index[d] = i;
                            }
                            else
                            {
                                args.Player.SendMessage("Not enough <" + item[d].name + "> in the chest", Color.Red);
                                return true;
                            }
                        }
                    }

                    if (Count >= item.Length)
                    {
                        TShock.Users.Buy(args.Player.Name, Price);
                        args.Player.SendMessage("You spent " + Price + " RCoins.", Color.BlanchedAlmond);
                        TShock.Users.SetRCoins(PlayerName, Price);
                        if (players.Count == 1)
                        {
                            players[0].SendMessage("Sold items to " + args.Player.Name + " for " + Price + " RCoins.", Color.Green);
                        }
                        for (int m = 0; m < item.Length; m++)
                        {
                            args.Player.GiveItem(item[m].type, item[m].name, item[m].width, item[m].height, Quantity[m]);
                            Main.chest[id].item[index[m]].stack = Main.chest[id].item[index[m]].stack - Quantity[m];
                        }
                        args.Player.SendMessage("You buy items successfully.", Color.Green);

                        return true;
                    }
                }

                args.Player.SendMessage("Not enough items in the chest", Color.Red);
                return true;
            }
            if (!args.Player.Group.HasPermission(Permissions.manageregion) && !TShock.Regions.CanBuild(x, y, args.Player, out Owner) && TShock.Regions.InArea(x, y, out RegionName) && RegionName != "Sell")
            {
                args.Player.SendMessage("Chest protected from changes by " + Owner, Color.Red);
                args.Player.SendMessage("Log in to use it", Color.Red);
                return true;
            }
            
            return false;
        }

        private static bool HandleSign(GetDataHandlerArgs args)
        {
            var id = args.Data.ReadInt16();
            var x = args.Data.ReadInt32();
            var y = args.Data.ReadInt32();
            string Owner = string.Empty;
            string RegionName = string.Empty;
            
            if (TShock.Config.RangeChecks && ((Math.Abs(args.Player.TileX - x) > 32) || (Math.Abs(args.Player.TileY - y) > 32)))
            {
                return TShock.Utils.HandleGriefer(args.Player, TShock.Config.RangeCheckBanReason);
            }

            if (TShock.Utils.SignName(x, y, args.Player.Name) || args.Player.Group.HasPermission(Permissions.adminstatus))
            {
                return false;
            }
                if (!args.Player.Group.HasPermission(Permissions.manageregion) && !TShock.Regions.CanBuild(x, y, args.Player, out Owner) && TShock.Regions.InArea(x, y, out RegionName))
            {
                args.Player.SendMessage("Sign protected from changes by " + Owner, Color.Red);
                args.Player.SendMessage("Log in to use it", Color.Red);
                return true;
            }
            
            return false;
        }

        private static bool HandleGetSection(GetDataHandlerArgs args)
        {
            var x = args.Data.ReadInt32();
            var y = args.Data.ReadInt32();

            if (args.Player.RequestedSection)
            {
                TShock.Utils.ForceKick(args.Player, "Requested sections more than once.");
                return true;
            }
            args.Player.RequestedSection = true;
            return false;
        }


        private static bool UpdateNPCHome( GetDataHandlerArgs args )
        {
            if (!args.Player.Group.HasPermission(Permissions.movenpc))
            {
                args.Player.SendMessage("You do not have permission to relocate NPCs.", Color.Red);
                return true;
            }
            return false;
        }

        private static bool HandlePlayerBuff(GetDataHandlerArgs args)
        {
            return !args.Player.Group.HasPermission(Permissions.ignoregriefdetection);
        }

        private static bool HandleItemDrop(GetDataHandlerArgs args)
        {
            var id = args.Data.ReadInt16();
            var pos = new Vector2(args.Data.ReadSingle(), args.Data.ReadSingle());
            var vel = new Vector2(args.Data.ReadSingle(), args.Data.ReadSingle());
            var stacks = args.Data.ReadInt8();
            var prefix = args.Data.ReadInt8();
            var type = args.Data.ReadInt16();

            var item = new Item();
            item.netDefaults(type);
            //if (!args.Player.IsLoggedIn)
            //{
                //args.Player.SendMessage("Login to drop the items.", Color.Red);
                //return true;
            //}
            if (TShock.Config.EnableItemStackChecks)
            {
                if (stacks > item.maxStack)
                {
                    TShock.Utils.HandleCheater(args.Player, "Dropped illegal stack of item");
                    return true;
                }
            }
            //if (TShock.Itembans.ItemIsBanned(item.name))
                //TShock.Utils.HandleCheater(args.Player, "Dropped banned item");
            return false;
        }

        private static bool HandleSignRead(GetDataHandlerArgs args)
        {
            var x = args.Data.ReadInt32();
            var y = args.Data.ReadInt32();
            return false;
        }

        private static bool HandleChestItem(GetDataHandlerArgs args)
        {
            var chestid = args.Data.ReadInt16();
            var itemslot = args.Data.ReadInt8();
            var stacks = args.Data.ReadInt8();
            var prefix = args.Data.ReadInt8();
            var type = args.Data.ReadInt16();
            string RegionName;
            var item = new Item();
            item.netDefaults(type);
            if (TShock.Regions.InArea(args.Player.TileX, args.Player.TileY, out RegionName))
            {
                if (RegionName == "Sell" && item.type != 328 && item.type != 48 && item.type != 306 && item.type != 71 && item.type != 72 && item.type != 73 && item.type != 74 && item.type != 2 && item.type != 30 && item.type != 0)
                {
                    if (args.Player.LastSellItem != item.name && args.Player.LastSellItemStack != stacks)
                    {
                        args.Player.SendData(PacketTypes.ChestItem, "", chestid, itemslot);
                        Log.ConsoleInfo("[Sell] " + args.Player.Name + " sold " + stacks + " " + item.name);
                        args.Player.SendMessage("You sold " + stacks + " " + item.name + " items for " + stacks * 0.01 + " RCoins.");
                        TShock.Users.SetRCoins(args.Player.Name, stacks * 0.01);
                        args.Player.LastSellItem = item.name;
                        args.Player.LastSellItemStack = stacks;
                        args.Player.LastChestItem = DateTime.Now;
                    }
                
                    if ((DateTime.Now - args.Player.LastChestItem).TotalMilliseconds > 1000)
                    {
                        //args.Player.LastSellItem = string.Empty;
                        //args.Player.LastSellItemStack = 0;
                    }
                    
                    return true;
                }
            }
            return false;
        }
        
    }
}
