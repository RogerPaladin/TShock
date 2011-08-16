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
using Microsoft.Xna.Framework;
using Terraria;
using TerrariaAPI;
using TShockAPI.Net;
using XNAHelpers;

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
        private static bool[] BlacklistTiles;

        public static void InitGetDataHandler()
        {
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
                {PacketTypes.SignNew, HandleSign},
                {PacketTypes.PlayerSlot, HandlePlayerSlot},
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
            int namelength = (int)(args.Data.Length - args.Data.Position - 1);

            if (namelength > 0)
            {
                string itemname = Encoding.ASCII.GetString(args.Data.ReadBytes(namelength));

                if (!args.Player.Group.HasPermission("usebanneditem") && TShock.Itembans.ItemIsBanned(itemname))
                {
                    args.Player.Disconnect("Using banned item: " + itemname + ", remove it and rejoin");
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
                Tools.ForceKick(args.Player, "Hair crash exploit.");
                return true;
            }
            if (!Tools.ValidString(name))
            {
                Tools.ForceKick(args.Player, "Unprintable character in name");
                return true;
            }
            if (name.Length > 32)
            {
                Tools.ForceKick(args.Player, "Name exceeded 32 characters.");
                return true;
            }
            if (name.Trim().Length == 0)
            {
                Tools.ForceKick(args.Player, "Empty Name.");
                return true;
            }
            var ban = TShock.Bans.GetBanByName(name);
            if (ban != null)
            {
                Tools.ForceKick(args.Player, string.Format("You are banned: {0}", ban.Reason));
                return true;
            }
            if (args.Player.ReceivedInfo)
            {
                return Tools.HandleGriefer(args.Player, "Sent client info more than once");
            }
            if (TShock.Config.MediumcoreOnly && difficulty < 1)
            {
                Tools.ForceKick(args.Player, "Server is set to mediumcore and above characters only!");
                return true;
            }
            if (TShock.Config.HardcoreOnly && difficulty < 2)
            {
                Tools.ForceKick(args.Player, "Server is set to hardcore characters only!");
                return true;
            }

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
                }
            }

            if (changed)
                TSPlayer.All.SendTileSquare(tilex, tiley, 3);

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
            Item heart = Tools.GetItemById(58);
            Item star = Tools.GetItemById(184);
            Random Rand = new Random();

            if (args.Player.AwaitingName)
            {
                if (TShock.Regions.InArea(args.Player.TileX, args.Player.TileY, out RegionName) && TShock.Regions.CanBuild(args.Player.TileX, args.Player.TileY, args.Player, out Owner) || !TShock.Regions.CanBuild(args.Player.TileX, args.Player.TileY, args.Player, out Owner))
                args.Player.SendMessage("This region <" + RegionName + "> is protected by" + Owner, Color.Yellow);
                args.Player.SendTileSquare(x, y);
                args.Player.AwaitingName = false;
                return true;
            }

            if (args.Player.AwaitingTemp1)
            {
                args.Player.TempArea.X = x;
                args.Player.TempArea.Y = y;
                args.Player.SendMessage("Set Temp Point 1", Color.Yellow);
                args.Player.SendTileSquare(x, y);
                args.Player.AwaitingTemp1 = false;
                return true;
            }

            if (args.Player.AwaitingTemp2)
            {
                if (x > args.Player.TempArea.X && y > args.Player.TempArea.Y)
                {
                    args.Player.TempArea.Width = x - args.Player.TempArea.X;
                    args.Player.TempArea.Height = y - args.Player.TempArea.Y;
                    args.Player.SendMessage("Set Temp Point 2", Color.Yellow);
                    args.Player.SendTileSquare(x, y);
                    args.Player.AwaitingTemp2 = false;
                }
                else
                {
                    args.Player.SendMessage("Point 2 must be below and right of Point 1", Color.Yellow);
                    args.Player.SendMessage("Use /region clear to start again", Color.Yellow);
                    args.Player.SendTileSquare(x, y);
                    args.Player.AwaitingTemp2 = false;
                }
                return true;
            }

            if (!args.Player.Group.HasPermission("canbuild"))
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
                    Tools.HandleGriefer(args.Player, string.Format(TShock.Config.TileAbuseReason, "Invalid tile type"));
                    return true;
                }
                if (TShock.Config.RangeChecks && ((Math.Abs(plyX - tileX) > 32) || (Math.Abs(plyY - tileY) > 32)))
                {
                    if (!(type == 1 && ((tiletype == 0 && args.Player.TPlayer.selectedItem == 114) || (tiletype == 53 && args.Player.TPlayer.selectedItem == 266))))
                    {
                        Log.Debug(string.Format("TilePlaced(PlyXY:{0}_{1}, TileXY:{2}_{3}, Result:{4}_{5}, Type:{6})",
                                                plyX, plyY, tileX, tileY, Math.Abs(plyX - tileX), Math.Abs(plyY - tileY), tiletype));
                        return Tools.HandleGriefer(args.Player, TShock.Config.RangeCheckBanReason);
                    }
                }
                if (tiletype == 48 && !args.Player.Group.HasPermission("canspike"))
                {
                    args.Player.SendMessage("You do not have permission to place spikes.", Color.Red);
                    Tools.SendLogs(string.Format("{0} tried to place spikes", args.Player.Name), Color.Red);
                    args.Player.SendTileSquare(x, y);
                    return true;
                }
                if (tiletype == 37 && !args.Player.Group.HasPermission("canmeteor"))
                {
                    args.Player.SendMessage("You do not have permission to place meteorite.", Color.Red);
                    Tools.SendLogs(string.Format("{0} tried to place meteorite", args.Player.Name), Color.Red);
                    args.Player.SendTileSquare(x, y);
                    return true;
                }
            }
            if (!args.Player.Group.HasPermission("editspawn") && !args.Player.IsLoggedIn)
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
            if (Tools.Altar(x, y, 45, 39, 41) && !args.Player.Group.HasPermission("altaredit"))
            {
                args.Player.SendTileSquare(x, y);
                if ((DateTime.UtcNow - args.Player.LastTileChangeNotify).TotalMilliseconds > 1000)
                {
                    args.Player.LastTileChangeNotify = DateTime.UtcNow;
                    if ((DateTime.UtcNow - Convert.ToDateTime(Tools.DispencerTime(args.Player.Name))).TotalMilliseconds > TShock.disptime)
                    {
                        TShock.DispenserTime.Remove(args.Player.Name + ";" + Convert.ToString(Tools.DispencerTime(args.Player.Name)));
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

                        Item Prize = Tools.GetItemById(rand);
                        if (Prize.maxStack == 1)
                        {
                            args.Player.GiveItem(Prize.type, Prize.name, Prize.width, Prize.height, 1);
                        }
                        else
                        {
                            args.Player.GiveItem(Prize.type, Prize.name, Prize.width, Prize.height, 10);
                        }
                        Tools.Broadcast(string.Format("WINNER! {0} win a prize - {1}.", args.Player.Name, Prize.name), Color.LightCoral);
                        args.Player.SendMessage("You win " + Prize.name);
                        return true;
                    }
                    args.Player.Dispenser = 0;
                    double minutes = Math.Round(15 - (DateTime.UtcNow - Convert.ToDateTime(Tools.DispencerTime(args.Player.Name))).TotalMinutes, 0);
                    double seconds = Math.Round(900 - (DateTime.UtcNow - Convert.ToDateTime(Tools.DispencerTime(args.Player.Name))).TotalSeconds, 0) - (minutes * 60 - 30);
                    args.Player.SendMessage(string.Format("Please wait for {0} minutes {1} seconds", minutes, seconds), Color.Orchid);
                    return true;
                }
                return true;
            }
            #endregion
            #region HardcoreSpawner
            if (Tools.Altar(x, y, 58, 8, 1) && !args.Player.Group.HasPermission("altaredit"))
            {
                args.Player.SendTileSquare(x, y);
                if ((DateTime.UtcNow - args.Player.LastTileChangeNotify).TotalMilliseconds > 1000)
                {
                    if ((DateTime.UtcNow - TShock.Spawner).TotalMilliseconds > 1000 * 60 * 30)
                    {
                        args.Player.DamagePlayer(100);
                        NPC skeletron = Tools.GetNPCById(35);
                        NPC slime = Tools.GetNPCById(50);
                        NPC eye = Tools.GetNPCById(4);
                        NPC eater = Tools.GetNPCById(13);
                        TSPlayer.Server.SetTime(false, 0.0);
                        TSPlayer.Server.SpawnNPC(skeletron.type, skeletron.name, 3, (int)args.Player.TileX, (int)args.Player.TileY);
                        TSPlayer.Server.SpawnNPC(slime.type, slime.name, 3, (int)args.Player.TileX, (int)args.Player.TileY + 20);
                        TSPlayer.Server.SpawnNPC(eye.type, eye.name, 3, (int)args.Player.TileX, (int)args.Player.TileY);
                        TSPlayer.Server.SpawnNPC(eater.type, eater.name, 3, (int)args.Player.TileX, (int)args.Player.TileY);
                        Tools.Broadcast(string.Format("{0} awakened an ancient evil in PVP arena!", args.Player.Name), Color.Moccasin);
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
                return true;
            }
            #endregion
            #region Healstone
            if (Main.tile[x, y].type == 0x55 && !args.Player.Group.HasPermission("altaredit"))
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
            
            if (!args.Player.Group.HasPermission("editspawn") && !TShock.Regions.CanBuild(x, y, args.Player, out Owner) && TShock.Regions.InArea(x, y, out RegionName))
            {
                if ((DateTime.UtcNow - args.Player.LastTileChangeNotify).TotalMilliseconds > 1000)
                {
                    args.Player.SendMessage("This region <" + RegionName + "> is protected by" + Owner, Color.Red);
                    args.Player.LastTileChangeNotify = DateTime.UtcNow;
                }
                args.Player.SendTileSquare(x, y);
                return true;
            }
            if (TShock.Config.DisableBuild)
            {
                if (!args.Player.Group.HasPermission("editspawn"))
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
                if (!args.Player.Group.HasPermission("editspawn"))
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
            return Tools.HandleGriefer(args.Player, TShock.Config.SendSectionAbuseReason);
        }

        private static bool HandleNpcUpdate(GetDataHandlerArgs args)
        {
            return Tools.HandleGriefer(args.Player, TShock.Config.NPCSpawnAbuseReason);
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
                return Tools.HandleGriefer(args.Player, TShock.Config.UpdatePlayerAbuseReason);
            }

            if (item < 0 || item >= args.TPlayer.inventory.Length)
            {
                Tools.HandleGriefer(args.Player, TShock.Config.UpdatePlayerAbuseReason);
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

            if (ident > Main.maxProjectiles || ident < 0)
            {
                Tools.HandleGriefer(args.Player, TShock.Config.ExplosiveAbuseReason);
                return true;
            }

            if (type == 23 && float.IsNaN((float)Math.Sqrt((double)(velx * velx + vely * vely))))
            {
                Tools.HandleGriefer(args.Player, TShock.Config.ProjectileAbuseReason);
                return true;
            }

            if (type == 29 || type == 28 || type == 37)
            {
                Log.Debug(string.Format("Explosive(PlyXY:{0}_{1}, Type:{2})", args.Player.TileX, args.Player.TileY, type));
                if (TShock.Config.DisableExplosives && (!args.Player.Group.HasPermission("useexplosives") || !args.Player.Group.HasPermission("ignoregriefdetection")))
                {
                    Main.projectile[ident].type = 0;
                    args.Player.SendData(PacketTypes.ProjectileNew, "", ident);
                    args.Player.SendMessage("Explosives are disabled!", Color.Red);
                    args.Player.LastExplosive = DateTime.UtcNow;
                    //return true;
                }
                else
                    return Tools.HandleExplosivesUser(args.Player, TShock.Config.ExplosiveAbuseReason);
            }
            return false;
        }

        private static bool HandlePlayerKillMe(GetDataHandlerArgs args)
        {
            byte id = args.Data.ReadInt8();
            if (id != args.Player.Index)
            {
                return Tools.HandleGriefer(args.Player, TShock.Config.KillMeAbuseReason);
            }
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
            for (int i = 0; i < 44; i++)
            {
                if (args.TPlayer.inventory[i].type >= 205 && args.TPlayer.inventory[i].type <= 207)
                {
                    bucket = true;
                    break;
                }
            }

            if (!args.Player.Group.HasPermission("canbuild"))
            {
                args.Player.SendMessage("You do not have permission to build!", Color.Red);
                args.Player.SendTileSquare(x, y);
                return true;
            }

            if (lava && !args.Player.Group.HasPermission("canlava"))
            {
                args.Player.SendMessage("You do not have permission to use lava", Color.Red);
                Tools.SendLogs(string.Format("{0} tried using lava", args.Player.Name), Color.Red);
                args.Player.SendTileSquare(x, y);
                return true;
            }
            if (!lava && !args.Player.Group.HasPermission("canwater"))
            {
                args.Player.SendMessage("You do not have permission to use water", Color.Red);
                Tools.SendLogs(string.Format("{0} tried using water", args.Player.Name), Color.Red);
                args.Player.SendTileSquare(x, y);
                return true;
            }

            if (!bucket)
            {
                Log.Debug(string.Format("{0}(PlyXY:{1}_{2}, TileXY:{3}_{4}, Result:{5}_{6}, Amount:{7})",
                                        lava ? "Lava" : "Water", plyX, plyY, tileX, tileY,
                                        Math.Abs(plyX - tileX), Math.Abs(plyY - tileY), liquid));
                return Tools.HandleGriefer(args.Player, TShock.Config.IllogicalLiquidUseReason); ;
            }
            if (TShock.Config.RangeChecks && ((Math.Abs(plyX - tileX) > 32) || (Math.Abs(plyY - tileY) > 32)))
            {
                Log.Debug(string.Format("Liquid(PlyXY:{0}_{1}, TileXY:{2}_{3}, Result:{4}_{5}, Amount:{6})",
                                        plyX, plyY, tileX, tileY, Math.Abs(plyX - tileX), Math.Abs(plyY - tileY), liquid));
                return Tools.HandleGriefer(args.Player, TShock.Config.LiquidAbuseReason);
            }

            if (TShock.Config.SpawnProtection)
            {
                if (!args.Player.Group.HasPermission("editspawn"))
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

            if (args.Player.AwaitingName)
            {
                if (TShock.Regions.InArea(args.Player.TileX, args.Player.TileY, out RegionName) && TShock.Regions.CanBuild(args.Player.TileX, args.Player.TileY, args.Player, out Owner) || !TShock.Regions.CanBuild(args.Player.TileX, args.Player.TileY, args.Player, out Owner))
                args.Player.SendMessage("This region <" + RegionName + "> is protected by" + Owner, Color.Yellow);
                args.Player.SendTileSquare(tilex, tiley);
                args.Player.AwaitingName = false;
                return true;
            }

            if (args.Player.AwaitingTemp1)
            {
                args.Player.TempArea.X = tilex;
                args.Player.TempArea.Y = tiley;
                args.Player.SendMessage("Set Temp Point 1", Color.Yellow);
                args.Player.SendTileSquare(tilex, tiley);
                args.Player.AwaitingTemp1 = false;
                return true;
            }

            if (args.Player.AwaitingTemp2)
            {
                if (tilex > args.Player.TempArea.X && tiley > args.Player.TempArea.Y)
                {
                    args.Player.TempArea.Width = tilex - args.Player.TempArea.X;
                    args.Player.TempArea.Height = tiley - args.Player.TempArea.Y;
                    args.Player.SendMessage("Set Temp Point 2", Color.Yellow);
                    args.Player.SendTileSquare(tilex, tiley);
                    args.Player.AwaitingTemp2 = false;
                }
                else
                {
                    args.Player.SendMessage("Point 2 must be below and right of Point 1", Color.Yellow);
                    args.Player.SendMessage("Use /region clear to start again", Color.Yellow);
                    args.Player.SendTileSquare(tilex, tiley);
                    args.Player.AwaitingTemp2 = false;
                }
                return true;
            }

            if (Main.tile[tilex, tiley].type != 0x15 && (!Tools.MaxChests() && Main.tile[tilex, tiley].type != 0)) //Chest
            {
                Log.Debug(string.Format("TileKill(TileXY:{0}_{1}, Type:{2})",
                                        tilex, tiley, Main.tile[tilex, tiley].type));
                Tools.ForceKick(args.Player, string.Format(TShock.Config.TileKillAbuseReason, Main.tile[tilex, tiley].type));
                return true;
            }
            if (!args.Player.Group.HasPermission("canbuild"))
            {
                args.Player.SendMessage("You do not have permission to build!", Color.Red);
                args.Player.SendTileSquare(tilex, tiley);
                return true;
            }
            if (!args.Player.Group.HasPermission("editspawn") && !TShock.Regions.CanBuild(tilex, tiley, args.Player, out Owner) && TShock.Regions.InArea(tilex, tiley, out RegionName))
            {
                args.Player.SendMessage("Region protected from changes.", Color.Red);
                args.Player.SendTileSquare(tilex, tiley);
                return true;
            }
            if (TShock.Config.DisableBuild)
            {
                if (!args.Player.Group.HasPermission("editspawn"))
                {
                    args.Player.SendMessage("World protected from changes.", Color.Red);
                    args.Player.SendTileSquare(tilex, tiley);
                    return true;
                }
            }
            if (TShock.Config.SpawnProtection)
            {
                if (!args.Player.Group.HasPermission("editspawn"))
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
                            if (!Tools.Ban(args.Player, TShock.Config.MediumcoreBanReason))
                                Tools.ForceKick(args.Player, "Death results in a ban, but can't ban you");
                        }
                        else
                        {
                            Tools.ForceKick(args.Player, TShock.Config.MediumcoreKickReason);
                        }
                        return true;
                    }
                }
            }
            else
                args.Player.InitSpawn = true;

            return false;
        }

        private static bool HandleChest(GetDataHandlerArgs args)
        {
            var x = args.Data.ReadInt32();
            var y = args.Data.ReadInt32();
            if (TShock.Config.RangeChecks && ((Math.Abs(args.Player.TileX - x) > 32) || (Math.Abs(args.Player.TileY - y) > 32)))
            {
                return Tools.HandleGriefer(args.Player, TShock.Config.RangeCheckBanReason);
            }
            return false;
        }

        private static bool HandleSign(GetDataHandlerArgs args)
        {
            var id = args.Data.ReadInt16();
            var x = args.Data.ReadInt32();
            var y = args.Data.ReadInt32();
            if (TShock.Config.RangeChecks && ((Math.Abs(args.Player.TileX - x) > 32) || (Math.Abs(args.Player.TileY - y) > 32)))
            {
                return Tools.HandleGriefer(args.Player, TShock.Config.RangeCheckBanReason);
            }
            return false;
        }
    }
}
