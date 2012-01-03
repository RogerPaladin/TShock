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
using System.IO;
using System.IO.Streams;
using System.Text;
using Terraria;
using TShockAPI.Net;

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
        public static int[] WhitelistBuffMaxTime;

        public static void InitGetDataHandler()
        {
            #region Blacklists

            WhitelistBuffMaxTime = new int[Main.maxBuffs];
            WhitelistBuffMaxTime[20] = 600;
            WhitelistBuffMaxTime[0x18] = 1200;
            WhitelistBuffMaxTime[0x1f] = 120;
            WhitelistBuffMaxTime[0x27] = 420;

            #endregion Blacklists

            GetDataHandlerDelegates = new Dictionary<PacketTypes, GetDataHandlerDelegate>
            {
                {PacketTypes.PlayerInfo, HandlePlayerInfo},
                {PacketTypes.PlayerUpdate, HandlePlayerUpdate},
                {PacketTypes.Tile, HandleTile},
                {PacketTypes.TileSendSquare, HandleSendTileSquare},
                {PacketTypes.ProjectileNew, HandleProjectileNew},
                {PacketTypes.TogglePvp, HandleTogglePvp},
                {PacketTypes.TileKill, HandleTileKill},
                {PacketTypes.PlayerKillMe, HandlePlayerKillMe},
                {PacketTypes.LiquidSet, HandleLiquidSet},
                {PacketTypes.PlayerSpawn, HandleSpawn},
                {PacketTypes.SyncPlayers, HandleSync},
                {PacketTypes.ChestGetContents, HandleChestOpen},
                {PacketTypes.ChestItem, HandleChestItem},
                {PacketTypes.SignNew, HandleSign},
                {PacketTypes.PlayerSlot, HandlePlayerSlot},
                {PacketTypes.TileGetSection, HandleGetSection},
                {PacketTypes.UpdateNPCHome, UpdateNPCHome},
                {PacketTypes.PlayerAddBuff, HandlePlayerBuff},
                {PacketTypes.ItemDrop, HandleItemDrop},
                {PacketTypes.PlayerHp, HandlePlayerHp},
                {PacketTypes.PlayerMana, HandlePlayerMana},
                {PacketTypes.PlayerDamage, HandlePlayerDamage},
                {PacketTypes.NpcStrike, HandleNpcStrike},
                {PacketTypes.NpcSpecial, HandleSpecial},
                {PacketTypes.PlayerAnimation, HandlePlayerAnimation},
                {PacketTypes.PlayerBuff, HandlePlayerBuffUpdate},
                {PacketTypes.PasswordSend, HandlePassword},
                {PacketTypes.ContinueConnecting2, HandleConnecting}
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
            short prefix = args.Data.ReadInt8();
            int type = args.Data.ReadInt16();

            if (plr != args.Player.Index)
            {
                return true;
            }

            if (slot < 0 || slot > NetItem.maxNetInventory)
            {
                return true;
            }

            var item = new Item();
            item.netDefaults(type);
            item.Prefix(prefix);

            if (args.Player.IsLoggedIn)
            {
                args.Player.PlayerData.StoreSlot(slot, type, prefix, stack);
            }

            return false;
        }

        private static bool HandlePlayerHp(GetDataHandlerArgs args)
        {
            int plr = args.Data.ReadInt8();
            int cur = args.Data.ReadInt16();
            int max = args.Data.ReadInt16();

            if (args.Player.FirstMaxHP == 0)
                args.Player.FirstMaxHP = max;

            if (max > 400 && max > args.Player.FirstMaxHP)
            {
                TShock.Utils.ForceKick(args.Player, "Hacked Client Detected.");
                return false;
            }

            if (args.Player.IsLoggedIn)
            {
                args.Player.PlayerData.maxHealth = max;
            }

            return false;
        }

        private static bool HandlePlayerMana(GetDataHandlerArgs args)
        {
            int plr = args.Data.ReadInt8();
            int cur = args.Data.ReadInt16();
            int max = args.Data.ReadInt16();

            if (args.Player.FirstMaxMP == 0)
                args.Player.FirstMaxMP = max;

            if (max > 400 && max > args.Player.FirstMaxMP)
            {
                TShock.Utils.ForceKick(args.Player, "Hacked Client Detected.");
                return false;
            }

            return false;
        }

        private static bool HandlePlayerInfo(GetDataHandlerArgs args)
        {
            var playerid = args.Data.ReadInt8();
            var hair = args.Data.ReadInt8();
            var male = args.Data.ReadInt8();
            args.Data.Position += 21;
            var difficulty = args.Data.ReadInt8();
            string name = Encoding.ASCII.GetString(args.Data.ReadBytes((int)(args.Data.Length - args.Data.Position - 1)));

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
                return true;
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
            args.TPlayer.name = name;
            args.Player.ReceivedInfo = true;

            return false;
        }

        private static bool HandleConnecting(GetDataHandlerArgs args)
        {
            var user = TShock.Users.GetUserByName(args.Player.Name);
            if (user != null && !TShock.Config.DisableLoginBeforeJoin)
            {
                args.Player.RequiresPassword = true;
                NetMessage.SendData((int)PacketTypes.PasswordRequired, args.Player.Index);
                return true;
            }
            else if (!string.IsNullOrEmpty(TShock.Config.ServerPassword))
            {
                args.Player.RequiresPassword = true;
                NetMessage.SendData((int)PacketTypes.PasswordRequired, args.Player.Index);
                return true;
            }

            if (args.Player.State == 1)
                args.Player.State = 2;
            NetMessage.SendData((int)PacketTypes.WorldInfo, args.Player.Index);
            return true;
        }

        private static bool HandlePassword(GetDataHandlerArgs args)
        {
            if (!args.Player.RequiresPassword)
                return true;

            string password = Encoding.ASCII.GetString(args.Data.ReadBytes((int)(args.Data.Length - args.Data.Position - 1)));
            var user = TShock.Users.GetUserByName(args.Player.Name);
            if (user != null)
            {
                string encrPass = TShock.Utils.HashPassword(password);
                if (user.Password.ToUpper() == encrPass.ToUpper())
                {
                    args.Player.RequiresPassword = false;

                    if (args.Player.State == 1)
                        args.Player.State = 2;
                    NetMessage.SendData((int)PacketTypes.WorldInfo, args.Player.Index);

                    if (TShock.Config.ServerSideInventory)
                    {
                        if (args.Player.Group.HasPermission(Permissions.bypassinventorychecks))
                        {
                            args.Player.IgnoreActionsForClearingTrashCan = false;
                        }
                        else if (!TShock.CheckInventory(args.Player))
                        {
                            args.Player.SendMessage("Login Failed, Please fix the above errors then /login again.", Color.Cyan);
                            args.Player.IgnoreActionsForClearingTrashCan = true;
                            return true;
                        }
                    }

                    if (args.Player.Group.HasPermission(Permissions.ignorestackhackdetection))
                        args.Player.IgnoreActionsForCheating = "none";

                    if (args.Player.Group.HasPermission(Permissions.usebanneditem))
                        args.Player.IgnoreActionsForDisabledArmor = "none";

                    args.Player.Group = TShock.Utils.GetGroup(user.Group);
                    args.Player.UserAccountName = args.Player.Name;
                    args.Player.UserID = TShock.Users.GetUserID(args.Player.UserAccountName);
                    args.Player.IsLoggedIn = true;
                    args.Player.IgnoreActionsForInventory = "none";

                    args.Player.PlayerData.CopyInventory(args.Player);

                    args.Player.SendMessage("Authenticated as " + args.Player.Name + " successfully.", Color.LimeGreen);
                    Log.ConsoleInfo(args.Player.Name + " authenticated successfully as user: " + args.Player.Name);
                    return true;
                }
                TShock.Utils.ForceKick(args.Player, "Incorrect User Account Password");
                return true;
            }
            if (!string.IsNullOrEmpty(TShock.Config.ServerPassword))
            {
                if(TShock.Config.ServerPassword == password)
                {
                    args.Player.RequiresPassword = false;
                    if (args.Player.State == 1)
                        args.Player.State = 2;
                    NetMessage.SendData((int)PacketTypes.WorldInfo, args.Player.Index);
                    return true;
                }
                TShock.Utils.ForceKick(args.Player, "Incorrect Server Password");
                return true;
            }

            TShock.Utils.ForceKick(args.Player, "Bad Password Attempt");
            return true;
        }

        private static bool HandleGetSection(GetDataHandlerArgs args)
        {
            if (args.Player.RequestedSection)
                return true;

            args.Player.RequestedSection = true;
            if (TShock.HackedHealth(args.Player) && !args.Player.Group.HasPermission(Permissions.ignorestathackdetection))
            {
                TShock.Utils.ForceKick(args.Player, "You have Hacked Health/Mana, Please use a different character.");
            }

            if (!args.Player.Group.HasPermission(Permissions.ignorestackhackdetection))
            {
                TShock.HackedInventory(args.Player);
            }

            if (TShock.Utils.ActivePlayers() + 1 > TShock.Config.MaxSlots && !args.Player.Group.HasPermission(Permissions.reservedslot))
            {
                TShock.Utils.ForceKick(args.Player, TShock.Config.ServerFullReason);
                return true;
            }

            NetMessage.SendData((int)PacketTypes.TimeSet, -1, -1, "", 0, 0, Main.sunModY, Main.moonModY);

            if (TShock.Config.EnableGeoIP && TShock.Geo != null)
            {
                Log.Info(string.Format("{0} ({1}) from '{2}' group from '{3}' joined. ({4}/{5})", args.Player.Name, args.Player.IP, args.Player.Group.Name, args.Player.Country, TShock.Utils.ActivePlayers(), TShock.Config.MaxSlots));
                TShock.Utils.Broadcast(args.Player.Name + " has joined from the " + args.Player.Country, Color.Yellow);
            }
            else
            {
                Log.Info(string.Format("{0} ({1}) from '{2}' group joined. ({3}/{4})", args.Player.Name, args.Player.IP, args.Player.Group.Name, TShock.Utils.ActivePlayers(), TShock.Config.MaxSlots));
                TShock.Utils.Broadcast(args.Player.Name + " has joined", Color.Yellow);
            }

            if (TShock.Config.DisplayIPToAdmins)
                TShock.Utils.SendLogs(string.Format("{0} has joined. IP: {1}", args.Player.Name, args.Player.IP), Color.Blue);

            return false;
        }

        private static bool HandleSendTileSquare(GetDataHandlerArgs args)
        {
            if (args.Player.Group.HasPermission(Permissions.allowclientsideworldedit))
                return false;

            var size = args.Data.ReadInt16();
            var tileX = args.Data.ReadInt32();
            var tileY = args.Data.ReadInt32();

            if (size > 5)
                return true;

            if ((DateTime.UtcNow - args.Player.LastThreat).TotalMilliseconds < 5000)
            {
                args.Player.SendTileSquare(tileX, tileY, size);
                return true;
            }

            if (TShock.CheckIgnores(args.Player))
            {
                args.Player.SendTileSquare(tileX, tileY);
                return true;
            }

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
                int realx = tileX + x;
                if (realx < 0 || realx >= Main.maxTilesX)
                    continue;

                for (int y = 0; y < size; y++)
                {
                	int realy = tileY + y;
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
                    // Holy water/Unholy water
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
                    else if (tile.type == 109 && newtile.Type == 2)
                    {
                        tile.type = 2;
                        changed = true;
                    }
                    else if (tile.type == 116 && newtile.Type == 53)
                    {
                        tile.type = 53;
                        changed = true;
                    }
                    else if (tile.type == 117 && newtile.Type == 1)
                    {
                        tile.type = 1;
                        changed = true;
                    }
                }
            }

			if (changed)
			{
				TSPlayer.All.SendTileSquare(tileX, tileY, size);
				WorldGen.RangeFrame(tileX, tileY, tileX + size, tileY + size);
			}
			else
			{
			    args.Player.SendTileSquare(tileX, tileY, size);
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

            if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY)
                return false;

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

            if (type == 1 || type == 3)
            {
                if (tiletype >= ((type == 1) ? Main.maxTileSets : Main.maxWallTypes))
                {
                    return true;
                }
                if (tiletype == 29 && tiletype == 97 && TShock.Config.ServerSideInventory)
                {
                    args.Player.SendMessage("You cannot place this tile, Server side inventory is enabled.", Color.Red);
                    args.Player.SendTileSquare(x, y);
                    return true;
                }
                if (tiletype == 48 && !args.Player.Group.HasPermission(Permissions.usebanneditem) && TShock.Itembans.ItemIsBanned("Spike", args.Player))
                {
                    args.Player.Disable();
                    args.Player.SendTileSquare(x, y);
                    return true;
                }
                if (type == 1 && tiletype == 21 && TShock.Utils.MaxChests())
                {
                    args.Player.SendMessage("Reached world's max chest limit, unable to place more!", Color.Red);
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
                    args.Player.Disable();
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

                if (tiletype == 141 && !args.Player.Group.HasPermission(Permissions.usebanneditem) && TShock.Itembans.ItemIsBanned("Explosives"))
                {
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

            if ((tiletype == 127 || Main.tileCut[tiletype]) && (type == 0 || type == 4))
            {
                return false;
            }

            if (TShock.CheckRangePermission(args.Player, x, y))
            {
                args.Player.SendTileSquare(x, y);
                return true;
            }

            if (args.Player.TileKillThreshold >= TShock.Config.TileKillThreshold)
            {
                args.Player.Disable();
                args.Player.SendTileSquare(x, y);
                return true;
            }

            if (args.Player.TilePlaceThreshold >= TShock.Config.TilePlaceThreshold)
            {
                args.Player.Disable();
                args.Player.SendTileSquare(x, y);
                return true;
            }

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

                    if (type == 0 && Main.tileSolid[Main.tile[x, y].type] && args.Player.Active)
                    {
                        args.Player.ProjectileThreshold++;
                        var coords = new Vector2(x, y);
                        if (!args.Player.TilesDestroyed.ContainsKey(coords))
                            args.Player.TilesDestroyed.Add(coords, Main.tile[x, y].Data);
                    }

                    if ((DateTime.UtcNow - args.Player.LastThreat).TotalMilliseconds < 5000)
                    {
                        args.Player.SendTileSquare(x, y);
                        return true;
                    }

                    if (type == 1 && !args.Player.Group.HasPermission(Permissions.ignoreplacetiledetection))
                    {
                        args.Player.TilePlaceThreshold++;
                        var coords = new Vector2(x, y);
                        if (!args.Player.TilesCreated.ContainsKey(coords))
                            args.Player.TilesCreated.Add(coords, Main.tile[x, y].Data);
                    }

                    if ((type == 0 || type == 4) && Main.tileSolid[Main.tile[x, y].type] && !args.Player.Group.HasPermission(Permissions.ignorekilltiledetection))
                    {
                        args.Player.TileKillThreshold++;
                        var coords = new Vector2(x, y);
                        if (!args.Player.TilesDestroyed.ContainsKey(coords))
                            args.Player.TilesDestroyed.Add(coords, Main.tile[x, y].Data);
                    }
                    return false;
                }
            }
            return false;
        }
        private static bool HandleTogglePvp(GetDataHandlerArgs args)
        {
            int id = args.Data.ReadByte();
            bool pvp = args.Data.ReadBoolean();

            if (id != args.Player.Index)
            {
                return true;
            }

            if (TShock.Config.PvPMode == "disabled")
            {
                return true;
            }

            if (args.TPlayer.hostile != pvp)
            {
                long seconds = (long)(DateTime.UtcNow - args.Player.LastPvpChange).TotalSeconds;
                if (seconds > 5)
                {
                    TSPlayer.All.SendMessage(string.Format("{0} has {1} PvP!", args.Player.Name, pvp ? "enabled" : "disabled"), Main.teamColor[args.Player.Team]);
                }
                args.Player.LastPvpChange = DateTime.UtcNow;
            }

            args.TPlayer.hostile = pvp;

            if (TShock.Config.PvPMode == "always")
            {
                if (!pvp)
                    args.Player.Spawn();
            }

            NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", args.Player.Index);

            return true;
        }

        private static bool HandlePlayerUpdate(GetDataHandlerArgs args)
        {
            var plr = args.Data.ReadInt8();
            var control = args.Data.ReadInt8();
            var item = args.Data.ReadInt8();
            var pos = new Vector2(args.Data.ReadSingle(), args.Data.ReadSingle());
            var vel = new Vector2(args.Data.ReadSingle(), args.Data.ReadSingle());

            if (item < 0 || item >= args.TPlayer.inventory.Length)
            {
                return true;
            }

            if(args.Player.LastNetPosition == Vector2.Zero)
            {
                return true;
            }

            if (!pos.Equals(args.Player.LastNetPosition))
            {
                float distance = Vector2.Distance(new Vector2(pos.X / 16f, pos.Y / 16f), new Vector2(args.Player.LastNetPosition.X / 16f, args.Player.LastNetPosition.Y / 16f));
                if (TShock.CheckIgnores(args.Player))
                {
                    if (distance > TShock.Config.MaxRangeForDisabled)
                    {
                        if (args.Player.IgnoreActionsForCheating != "none")
                        {
                            args.Player.SendMessage("Disabled for cheating: " + args.Player.IgnoreActionsForCheating,
                                                    Color.Red);
                        }
                        else if (args.Player.IgnoreActionsForDisabledArmor != "none")
                        {
                            args.Player.SendMessage(
                                "Disabled for banned armor: " + args.Player.IgnoreActionsForDisabledArmor, Color.Red);
                        }
                        else if (args.Player.IgnoreActionsForInventory != "none")
                        {
                            args.Player.SendMessage(
                                "Disabled for Server Side Inventory: " + args.Player.IgnoreActionsForInventory,
                                Color.Red);
                        }
                        else if (TShock.Config.RequireLogin && !args.Player.IsLoggedIn)
                        {
                            args.Player.SendMessage("Please /register or /login to play!", Color.Red);
                        }
                        else if (args.Player.IgnoreActionsForClearingTrashCan)
                        {
                            args.Player.SendMessage("You need to rejoin to ensure your trash can is cleared!", Color.Red);
                        }
                        else if (TShock.Config.PvPMode == "always" && !args.TPlayer.hostile)
                        {
                            args.Player.SendMessage("PvP is forced! Enable PvP else you can't move or do anything!",
                                                    Color.Red);
                        }
                        int lastTileX = (int) (args.Player.LastNetPosition.X/16f);
                        int lastTileY = (int) (args.Player.LastNetPosition.Y/16f);
                        if (!args.Player.Teleport(lastTileX, lastTileY))
                        {
                            args.Player.Spawn();
                        }
                        return true;
                    }
                    return true;
                }

                if (args.Player.Dead)
                {
                    return true;
                }

                if (!args.Player.Group.HasPermission(Permissions.ignorenoclipdetection) && Collision.SolidCollision(pos, args.TPlayer.width, args.TPlayer.height))
                {
                    int lastTileX = (int)(args.Player.LastNetPosition.X / 16f);
                    int lastTileY = (int)(args.Player.LastNetPosition.Y / 16f);
                    if (!args.Player.Teleport(lastTileX, lastTileY + 3))
                    {
                        args.Player.SendMessage("You got stuck in a solid object, Sent to spawn point.");
                        args.Player.Spawn();
                    }
                    return true;
                }
                args.Player.LastNetPosition = pos;
            }

            if ((control & 32) == 32)
            {
				if (!args.Player.Group.HasPermission(Permissions.usebanneditem) && TShock.Itembans.ItemIsBanned(args.TPlayer.inventory[item].name, args.Player))
                {
                    control -= 32;
                    args.Player.Disable();
                    args.Player.SendMessage(string.Format("You cannot use {0} on this server. Your actions are being ignored.", args.TPlayer.inventory[item].name), Color.Red);
                }
            }
            
            args.TPlayer.selectedItem = item;
            args.TPlayer.position = pos;
            args.TPlayer.velocity = vel;
            args.TPlayer.oldVelocity = args.TPlayer.velocity;
            args.TPlayer.fallStart = (int)(pos.Y / 16f);
            args.TPlayer.controlUp = false;
            args.TPlayer.controlDown = false;
            args.TPlayer.controlLeft = false;
            args.TPlayer.controlRight = false;
            args.TPlayer.controlJump = false;
            args.TPlayer.controlUseItem = false;
            args.TPlayer.direction = -1;
            if ((control & 1) == 1)
            {
                args.TPlayer.controlUp = true;
            }
            if ((control & 2) == 2)
            {
                args.TPlayer.controlDown = true;
            }
            if ((control & 4) == 4)
            {
                args.TPlayer.controlLeft = true;
            }
            if ((control & 8) == 8)
            {
                args.TPlayer.controlRight = true;
            }
            if ((control & 16) == 16)
            {
                args.TPlayer.controlJump = true;
            }
            if ((control & 32) == 32)
            {
                args.TPlayer.controlUseItem = true;
            }
            if ((control & 64) == 64)
            {
                args.TPlayer.direction = 1;
            }
            NetMessage.SendData((int)PacketTypes.PlayerUpdate, -1, args.Player.Index, "", args.Player.Index);

            return true;
        }

        private static bool HandleProjectileNew(GetDataHandlerArgs args)
        {
            var ident = args.Data.ReadInt16();
            var pos = new Vector2(args.Data.ReadSingle(), args.Data.ReadSingle());
            var vel = new Vector2(args.Data.ReadSingle(), args.Data.ReadSingle());
            var knockback = args.Data.ReadSingle();
            var dmg = args.Data.ReadInt16();
            var owner = args.Data.ReadInt8();
            var type = args.Data.ReadInt8();

            var index = TShock.Utils.SearchProjectile(ident);

            if (index > Main.maxProjectiles || index < 0)
            {
                return true;
            }

            if (args.Player.Index != owner)
            {
                args.Player.Disable();
                args.Player.RemoveProjectile(ident, owner);
                return true;
            }

            if (dmg > 175)
            {
                args.Player.Disable();
                args.Player.RemoveProjectile(ident, owner);
                return true;
            }

            if (TShock.CheckIgnores(args.Player))
            {
                args.Player.RemoveProjectile(ident, owner);
                return true;
            }

            if (TShock.CheckProjectilePermission(args.Player, index, type))
            {
                args.Player.Disable();
                args.Player.RemoveProjectile(ident, owner);
                return true;
            }

            if (args.Player.ProjectileThreshold >= TShock.Config.ProjectileThreshold)
            {
                args.Player.Disable();
                args.Player.RemoveProjectile(ident, owner);
                return true;
            }

            if ((DateTime.UtcNow - args.Player.LastThreat).TotalMilliseconds < 5000)
            {
                args.Player.RemoveProjectile(ident, owner);
                return true;
            }

            if (!args.Player.Group.HasPermission(Permissions.ignoreprojectiledetection))
            {
                args.Player.ProjectileThreshold++;
            }

            return false;
        }

        private static bool HandleProjectileKill(GetDataHandlerArgs args)
        {
            var ident = args.Data.ReadInt16();
            var owner = args.Data.ReadInt8();

            if (args.Player.Index != owner)
            {
                args.Player.Disable();
                return true;
            }

            var index = TShock.Utils.SearchProjectile(ident);

            if (index > Main.maxProjectiles || index < 0)
            {
                return true;
            }

            int type = Main.projectile[index].type;

            if (args.Player.Index != Main.projectile[index].owner)
            {
                args.Player.Disable();
                args.Player.RemoveProjectile(ident, owner);
                return true;
            }

            if (type == 69 /*Holy water*/ || type == 70 /*Unholy Water*/)
            {
                Log.Debug(string.Format("Corruption(PlyXY:{0}_{1}, Type:{2})", args.Player.TileX, args.Player.TileY, type));
                if (TShock.Config.DisableCorruption && (!args.Player.Group.HasPermission(Permissions.usecorruption) && !args.Player.Group.HasPermission(Permissions.ignorekilltiledetection)))
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
                args.Player.RemoveProjectile(ident, owner);
                return true;
            }

            if (TShock.CheckProjectilePermission(args.Player, index, type))
            {
                args.Player.Disable();
                args.Player.RemoveProjectile(ident, owner);
                return true;
            }

            if ((DateTime.UtcNow - args.Player.LastThreat).TotalMilliseconds < 5000)
            {
                args.Player.RemoveProjectile(ident, owner);
                return true;
            }

            return false;
        }
        
        private static bool HandlePlayerKillMe(GetDataHandlerArgs args)
        {
            var id = args.Data.ReadInt8();
            var direction = args.Data.ReadInt8();
            var dmg = args.Data.ReadInt16();
            var pvp = args.Data.ReadInt8() == 0;

            int textlength = (int)(args.Data.Length - args.Data.Position - 1);
            string deathtext = "";
            if (textlength > 0)
            {
                deathtext = Encoding.ASCII.GetString(args.Data.ReadBytes(textlength));
                if (!TShock.Utils.ValidString(deathtext))
                {
                    return true;
                }
            }

            args.Player.LastDeath = DateTime.Now;
            args.Player.Dead = true;

            return false;
        }

        private static bool HandleLiquidSet(GetDataHandlerArgs args)
        {
            int tileX = args.Data.ReadInt32();
            int tileY = args.Data.ReadInt32();
            byte liquid = args.Data.ReadInt8();
            bool lava = args.Data.ReadBoolean();

            //The liquid was picked up.
            if (liquid == 0)
                return false;

            if (tileX < 0 || tileX >= Main.maxTilesX || tileY < 0 || tileY >= Main.maxTilesY)
                return false;

            if (TShock.CheckIgnores(args.Player))
            {
                args.Player.SendTileSquare(tileX, tileY);
                return true;
            }

            if (args.Player.TileLiquidThreshold >= TShock.Config.TileLiquidThreshold)
            {
                args.Player.Disable();
                args.Player.SendTileSquare(tileX, tileY);
                return true;
            }

            if (!args.Player.Group.HasPermission(Permissions.ignoreliquidsetdetection))
            {
                args.Player.TileLiquidThreshold++;
            }

            int bucket = 0;
            if (args.TPlayer.inventory[args.TPlayer.selectedItem].type == 206)
            {
                bucket = 1;
            }
            else if (args.TPlayer.inventory[args.TPlayer.selectedItem].type == 207)
            {
                bucket = 2;
            }

			if (lava && bucket != 2 && !args.Player.Group.HasPermission(Permissions.usebanneditem) && TShock.Itembans.ItemIsBanned("Lava Bucket", args.Player))
            {
                args.Player.Disable();
                args.Player.SendTileSquare(tileX, tileY);
                return true;
            }

			if (!lava && bucket != 1 && !args.Player.Group.HasPermission(Permissions.usebanneditem) && TShock.Itembans.ItemIsBanned("Water Bucket", args.Player))
            {
                args.Player.Disable();
                args.Player.SendTileSquare(tileX, tileY);
                return true;
            }

            if (TShock.CheckTilePermission(args.Player, tileX, tileY))
            {
                args.Player.SendTileSquare(tileX, tileY);
                return true;
            }

            if (TShock.CheckRangePermission(args.Player, tileX, tileY, 16))
            {
                args.Player.SendTileSquare(tileX, tileY);
                return true;
            }

            if ((DateTime.UtcNow - args.Player.LastThreat).TotalMilliseconds < 5000)
            {
                args.Player.SendTileSquare(tileX, tileY);
                return true;
            }
            
            return false;
        }

        private static bool HandleTileKill(GetDataHandlerArgs args)
        {
            var tileX = args.Data.ReadInt32();
            var tileY = args.Data.ReadInt32();
            string Owner = string.Empty;
            string RegionName = string.Empty;
            if (tileX < 0 || tileX >= Main.maxTilesX || tileY < 0 || tileY >= Main.maxTilesY)
                return false;

            if (TShock.CheckIgnores(args.Player))
            {
                args.Player.SendTileSquare(tileX, tileY);
                return true;
            }

            if (Main.tile[tileX, tileY].type != 0x15 && (!TShock.Utils.MaxChests() && Main.tile[tileX, tileY].type != 0)) //Chest
            {
                args.Player.SendTileSquare(tileX, tileY);
                return true;
            }
            if (!args.Player.Group.HasPermission(Permissions.editspawn) && !TShock.Regions.CanBuild(tileX, tileY, args.Player, out Owner) && TShock.Regions.InArea(tileX, tileY, out RegionName))
            {
                args.Player.SendMessage("Region protected from changes.", Color.Red);
                args.Player.SendTileSquare(tileX, tileY);
                return true;
            }
            if (TShock.Config.DisableBuild)
            {
                if (!args.Player.Group.HasPermission(Permissions.editspawn))
                {
                    args.Player.SendMessage("World protected from changes.", Color.Red);
                args.Player.SendTileSquare(tileX, tileY);
                return true;
            }
            }
            if (TShock.Config.SpawnProtection)
            {
                if (!args.Player.Group.HasPermission(Permissions.editspawn))
                {
                    var flag = TShock.CheckSpawn(tileX, tileY);
                    if (flag)
                    {
                        args.Player.SendMessage("Spawn protected from changes.", Color.Red);
                        args.Player.SendTileSquare(tileX, tileY);
                        return true;
                    }
                }
            }

            if (TShock.CheckRangePermission(args.Player, tileX, tileY))
            {
                args.Player.SendTileSquare(tileX, tileY);
                return true;
            }

            return false;
        }

        private static bool HandleSpawn(GetDataHandlerArgs args)
        {
            var player = args.Data.ReadInt8();
            var spawnx = args.Data.ReadInt32();
            var spawny = args.Data.ReadInt32();

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

            args.Player.Dead = false;
            return false;
        }

        private static bool HandleChestOpen(GetDataHandlerArgs args)
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

            if (TShock.CheckIgnores(args.Player))
            {
                return true;
            }

            if (TShock.CheckRangePermission(args.Player, x, y))
            {
                return true;
            }

            if (TShock.CheckTilePermission(args.Player, x, y) && TShock.Config.RegionProtectChests)
            {
                return true;
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

        private static bool HandleChestItem(GetDataHandlerArgs args)
        {
            var id = args.Data.ReadInt16();
            var slot = args.Data.ReadInt8();
            var stacks = args.Data.ReadInt8();
            var prefix = args.Data.ReadInt8();
            var type = args.Data.ReadInt16();
            string RegionName;
            if (args.TPlayer.chest != id)
            {
                return false;
            }

            if (TShock.CheckIgnores(args.Player))
            {
                args.Player.SendData(PacketTypes.ChestItem, "", id, slot);
                return true;
            }

            Item item = new Item();
            item.netDefaults(type);
            if (stacks > item.maxStack || TShock.Itembans.ItemIsBanned(item.name, args.Player))
            {
                return false;
            }

            if (TShock.CheckTilePermission(args.Player, Main.chest[id].x, Main.chest[id].y) && TShock.Config.RegionProtectChests)
            {
                return false;
            }

            if (TShock.CheckRangePermission(args.Player, Main.chest[id].x, Main.chest[id].y))
            {
                return false;
            }

            if (TShock.Regions.InArea(args.Player.TileX, args.Player.TileY, out RegionName))
            {
                if (RegionName == "Sell" && item.type != 328 && item.type != 48 && item.type != 306 && item.type != 71 && item.type != 72 && item.type != 73 && item.type != 74 && item.type != 2 && item.type != 30 && item.type != 0)
                {
                    if (args.Player.LastSellItem != item.name && args.Player.LastSellItemStack != stacks)
                    {
                        args.Player.SendData(PacketTypes.ChestItem, "", id, slot);
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

        private static bool HandleSign(GetDataHandlerArgs args)
        {
            var id = args.Data.ReadInt16();
            var x = args.Data.ReadInt32();
            var y = args.Data.ReadInt32();

            if (TShock.CheckTilePermission(args.Player, x, y))
            {
                args.Player.SendData(PacketTypes.SignNew, "", id);
                return true;
            }

            if (TShock.CheckRangePermission(args.Player, x, y))
            {
                args.Player.SendData(PacketTypes.SignNew, "", id);
                return true;
            }
            return false;
        }


        private static bool UpdateNPCHome(GetDataHandlerArgs args)
        {
            var id = args.Data.ReadInt16();
            var x = args.Data.ReadInt16();
            var y = args.Data.ReadInt16();
            var homeless = args.Data.ReadInt8();

            if (!args.Player.Group.HasPermission(Permissions.movenpc))
            {
                args.Player.SendMessage("You do not have permission to relocate NPCs.", Color.Red);
                args.Player.SendData(PacketTypes.UpdateNPCHome, "", id, Main.npc[id].homeTileX, Main.npc[id].homeTileY, Convert.ToByte(Main.npc[id].homeless));
                return true;
            }

            if (TShock.CheckTilePermission(args.Player, x, y))
            {
                args.Player.SendData(PacketTypes.UpdateNPCHome, "", id, Main.npc[id].homeTileX, Main.npc[id].homeTileY, Convert.ToByte(Main.npc[id].homeless));
                return true;
            }

            if (TShock.CheckRangePermission(args.Player, x, y))
            {
                args.Player.SendData(PacketTypes.UpdateNPCHome, "", id, Main.npc[id].homeTileX, Main.npc[id].homeTileY, Convert.ToByte(Main.npc[id].homeless));
                return true;
            }
            return false;
        }

        private static bool HandlePlayerBuff(GetDataHandlerArgs args)
        {
            var id = args.Data.ReadInt8();
            var type = args.Data.ReadInt8();
            var time = args.Data.ReadInt16();

            if (TShock.CheckIgnores(args.Player))
            {
                args.Player.SendData(PacketTypes.PlayerBuff, "", id);
                return true;
            }
            if (!TShock.Players[id].TPlayer.hostile)
            {
                args.Player.SendData(PacketTypes.PlayerBuff, "", id);
                return true;
            }
            if (TShock.CheckRangePermission(args.Player, TShock.Players[id].TileX, TShock.Players[id].TileY, 50))
            {
                args.Player.SendData(PacketTypes.PlayerBuff, "", id);
                return true;
            }
            if ((DateTime.UtcNow - args.Player.LastThreat).TotalMilliseconds < 5000)
            {
                args.Player.SendData(PacketTypes.PlayerBuff, "", id);
                return true;
            }

            if (WhitelistBuffMaxTime[type] > 0 && time <= WhitelistBuffMaxTime[type])
            {
                return false;
            }

            args.Player.SendData(PacketTypes.PlayerBuff, "", id);
            return true;
        }

        private static bool HandleItemDrop(GetDataHandlerArgs args)
        {
            var id = args.Data.ReadInt16();
            var pos = new Vector2(args.Data.ReadSingle(), args.Data.ReadSingle());
            var vel = new Vector2(args.Data.ReadSingle(), args.Data.ReadSingle());
            var stacks = args.Data.ReadInt8();
            var prefix = args.Data.ReadInt8();
            var type = args.Data.ReadInt16();

            if (type == 0) //Item removed, let client do this to prevent item duplication client side
            {
                return false;
            }

            if (TShock.CheckRangePermission(args.Player, (int)(pos.X / 16f), (int)(pos.Y / 16f)))
            {
                args.Player.SendData(PacketTypes.ItemDrop, "", id);
                return true;
            }

            Item item = new Item();
            item.netDefaults(type);
            if (stacks > item.maxStack || TShock.Itembans.ItemIsBanned(item.name, args.Player))
            {
                args.Player.SendData(PacketTypes.ItemDrop, "", id);
                return true;
            }

            if (TShock.CheckIgnores(args.Player))
            {
                args.Player.SendData(PacketTypes.ItemDrop, "", id);
                return true;
            }

            return false;
        }

        private static bool HandlePlayerDamage(GetDataHandlerArgs args)
        {
            var id = args.Data.ReadInt8();
            var direction = args.Data.ReadInt8();
            var dmg = args.Data.ReadInt16();
            var pvp = args.Data.ReadInt8();
            var crit = args.Data.ReadInt8();

            int textlength = (int)(args.Data.Length - args.Data.Position - 1);
            string deathtext = "";
            if (textlength > 0)
            {
                deathtext = Encoding.ASCII.GetString(args.Data.ReadBytes(textlength));
                if (!TShock.Utils.ValidString(deathtext))
                {
                    return true;
                }
            }


            if (TShock.Players[id] == null)
                return true;

            if (dmg > 175)
            {
                args.Player.Disable();
                args.Player.SendData(PacketTypes.PlayerHp, "", id);
                args.Player.SendData(PacketTypes.PlayerUpdate, "", id);
                return true;
            }

            if (!TShock.Players[id].TPlayer.hostile)
            {
                args.Player.SendData(PacketTypes.PlayerHp, "", id);
                args.Player.SendData(PacketTypes.PlayerUpdate, "", id);
                return true;
            }

            if (TShock.CheckIgnores(args.Player))
            {
                args.Player.SendData(PacketTypes.PlayerHp, "", id);
                args.Player.SendData(PacketTypes.PlayerUpdate, "", id);
                return true;
            }

            if (TShock.CheckRangePermission(args.Player, TShock.Players[id].TileX, TShock.Players[id].TileY, 100))
            {
                args.Player.SendData(PacketTypes.PlayerHp, "", id);
                args.Player.SendData(PacketTypes.PlayerUpdate, "", id);
                return true;
            }

            if ((DateTime.UtcNow - args.Player.LastThreat).TotalMilliseconds < 5000)
            {
                args.Player.SendData(PacketTypes.PlayerHp, "", id);
                args.Player.SendData(PacketTypes.PlayerUpdate, "", id);
                return true;
            }

            return false;
        }

        private static bool HandleNpcStrike(GetDataHandlerArgs args)
        {
            var id = args.Data.ReadInt8();
            var direction = args.Data.ReadInt8();
            var dmg = args.Data.ReadInt16();
            var pvp = args.Data.ReadInt8();
            var crit = args.Data.ReadInt8();

            if (Main.npc[id] == null)
                return true;

            if (dmg > 175)
            {
                args.Player.Disable();
                args.Player.SendData(PacketTypes.NpcUpdate, "", id);
                return true;
            }

            if (TShock.CheckIgnores(args.Player))
            {
                args.Player.SendData(PacketTypes.NpcUpdate, "", id);
                return true;
            }

            if (Main.npc[id].townNPC && !args.Player.Group.HasPermission(Permissions.movenpc))
            {
                args.Player.SendData(PacketTypes.NpcUpdate, "", id);
                return true;
            }

            if (TShock.Config.RangeChecks && TShock.CheckRangePermission(args.Player, (int)(Main.npc[id].position.X / 16f), (int)(Main.npc[id].position.Y / 16f), 100))
            {
                args.Player.SendData(PacketTypes.NpcUpdate, "", id);
                return true;
            }

            if ((DateTime.UtcNow - args.Player.LastThreat).TotalMilliseconds < 5000)
            {
                args.Player.SendData(PacketTypes.NpcUpdate, "", id);
                return true;
            }

            return false;
        }

        private static bool HandleSpecial(GetDataHandlerArgs args)
        {
            var id = args.Data.ReadInt8();
            var type = args.Data.ReadInt8();

            if (type == 1 && TShock.Config.DisableDungeonGuardian)
            {
                args.Player.SendMessage("The Dungeon Guardian returned you to your spawn point", Color.Purple);
                args.Player.Spawn();
                return true;
            }

            return false;
        }

        private static bool HandlePlayerAnimation(GetDataHandlerArgs args)
        {
            if (TShock.CheckIgnores(args.Player))
            {
                args.Player.SendData(PacketTypes.PlayerAnimation, "", args.Player.Index);
                return true;
            }

            if ((DateTime.UtcNow - args.Player.LastThreat).TotalMilliseconds < 5000)
            {
                args.Player.SendData(PacketTypes.PlayerAnimation, "", args.Player.Index);
                return true;
            }
            return false;
        }

        private static bool HandlePlayerBuffUpdate(GetDataHandlerArgs args)
        {
            var id = args.Data.ReadInt8();
            for (int i = 0; i < 10; i++)
            {
                var buff = args.Data.ReadInt8();

                if (buff == 10)
                {
                    if (!args.Player.Group.HasPermission(Permissions.usebanneditem) && TShock.Itembans.ItemIsBanned("Invisibility Potion", args.Player) )
                        buff = 0;
                    else if (TShock.Config.DisableInvisPvP && args.TPlayer.hostile)
                        buff = 0;
                }

                args.TPlayer.buffType[i] = buff;
                if (args.TPlayer.buffType[i] > 0)
                {
                    args.TPlayer.buffTime[i] = 60;
                }
                else
                {
                    args.TPlayer.buffTime[i] = 0;
                }
            }
            NetMessage.SendData((int)PacketTypes.PlayerBuff, -1, args.Player.Index, "", args.Player.Index);
            return true;
        }
    }
}
