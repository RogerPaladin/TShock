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
using System.Threading;
using Terraria;

using TShockAPI.Net;

using Microsoft.Xna.Framework.Graphics;

namespace TShockAPI
{
    public class TSPlayer
    {
        public static readonly TSServerPlayer Server = new TSServerPlayer();
        public static readonly TSPlayer All = new TSPlayer("All");
        public int TileThreshold { get; set; }
        public Dictionary<Vector2, Tile> TilesDestroyed { get; protected set; }
        public bool SyncHP { get; set; }
        public bool SyncMP { get; set; }
        public Group Group { get; set; }
        public bool DisplayChat = true;
        public bool ReceivedInfo { get; set; }
        public int Index { get; protected set; }
        public DateTime LastPvpChange { get; protected set; }
        public Point[] TempPoints = new Point[2];
        public int AwaitingTempPoint { get; set; }
        public bool AwaitingName { get; set; }
        public DateTime LastExplosive { get; set; }
        public DateTime LastCorruption { get; set; }
        public DateTime LastTileChangeNotify { get; set; }
        public DateTime LoginTime { get; set; }
        public DateTime Interval { get; set; }
        public bool InitSpawn;
        public bool DisplayLogs = true;
        public Vector2 oldSpawn = Vector2.Zero;
        public TSPlayer LastWhisper;
        public int LoginAttempts { get; set; }
        public int Dispenser { get; set; }
        public Vector2 TeleportCoords = new Vector2(-1, -1);
        public string UserAccountName { get; set; }
        public bool HasBeenSpammedWithBuildMessage;
        public bool IsLoggedIn; 
        public int UserID = -1;
        public bool HasBeenNaggedAboutLoggingIn;
        public bool TPAllow = true;
        public bool TpLock = false;
        Player FakePlayer;
        public bool RequestedSection = false;
        public DateTime LastDeath { get; set; }
        public bool ForceSpawn = false;
        public string Country = "??";
        public int Difficulty;
        private string CacheIP;
        public Vector2 LastTilePos = new Vector2(-1,-1);
        public string CurrentRegion;
        public bool InRegion = false;
        public int LastSignX { get; set; }
        public int LastSignY { get; set; }
        
        public bool RealPlayer
        {
            get { return Index >= 0 && Index < Main.maxNetPlayers && Main.player[Index] != null; }
        }
        public bool ConnectionAlive
        {
            get { return RealPlayer && (Netplay.serverSock[Index] != null && Netplay.serverSock[Index].active && !Netplay.serverSock[Index].kill); }
        }
        public string IP
        {
            get
            {
                if (string.IsNullOrEmpty(CacheIP))
                    return CacheIP = RealPlayer ? (Netplay.serverSock[Index].tcpClient.Connected ? TShock.Utils.GetRealIP(Netplay.serverSock[Index].tcpClient.Client.RemoteEndPoint.ToString()) : "") : "";
                else
                    return CacheIP;
            }
        }
        /// <summary>
        /// Terraria Player
        /// </summary>
        public Player TPlayer
        {
            get
            {
                return FakePlayer ?? Main.player[Index];
            }
        }
        public string Name
        {
            get { return TPlayer.name; }
        }
        public bool Active
        {
            get { return TPlayer != null && TPlayer.active; }
        }
        public int Team
        {
            get { return TPlayer.team; }
        }
        public float X
        {
            get
            {

                return RealPlayer ? TPlayer.position.X : Main.spawnTileX * 16;
            }
        }
        public float Y
        {
            get
            {
                return RealPlayer ? TPlayer.position.Y : Main.spawnTileY * 16;
            }
        }
        public int TileX
        {
            get { return (int)(X / 16); }
        }
        public int TileY
        {
            get { return (int)(Y / 16); }
        }
        public int StatMana
        {
            get { return TPlayer.statMana; }
        }
        public bool InventorySlotAvailable
        {
            get
            {
                bool flag = false;
                if (RealPlayer)
                {
                    for (int i = 0; i < 40; i++) //41 is trash can, 42-45 is coins, 46-49 is ammo
                    {
                        if (TPlayer.inventory[i] == null || !TPlayer.inventory[i].active || TPlayer.inventory[i].name == "")
                        {
                            flag = true;
                            break;
                        }
                    }
                }
                return flag;
            }
        }
        public bool StackCheat(out string item, out int itemcount)
        {
            item = string.Empty;
            itemcount = 0;
            for (int i = 0; i < 40; i++)
            {
                if (TPlayer.inventory[i].stack > TPlayer.inventory[i].maxStack)
                {
                    item = TPlayer.inventory[i].name;
                    itemcount = TPlayer.inventory[i].stack;
                    return true;
                }
            }

            return false;
        }
        
        public TSPlayer(int index)
        {
            TilesDestroyed = new Dictionary<Vector2, Tile>();
            Index = index;
            Group = new Group("null");
        }

        protected TSPlayer(String playerName)
        {
            TilesDestroyed = new Dictionary<Vector2, Tile>();
            Index = -1;
            FakePlayer = new Player { name = playerName, whoAmi = -1 };
            Group = new Group("null");
        }

        public virtual void Disconnect(string reason)
        {
            SendData(PacketTypes.Disconnect, reason);
        }

        public virtual void Flush()
        {
            var sock = Netplay.serverSock[Index];
            if (sock == null)
                return;

            TShock.PacketBuffer.Flush(sock);
        }


        void SendWorldInfo(int tilex, int tiley, bool fakeid)
        {
            using (var ms = new MemoryStream())
            {
                var msg = new WorldInfoMsg
                {
                    Time = (int)Main.time,
                    DayTime = Main.dayTime,
                    MoonPhase = (byte)Main.moonPhase,
                    BloodMoon = Main.bloodMoon,
                    MaxTilesX = Main.maxTilesX,
                    MaxTilesY = Main.maxTilesY,
                    SpawnX = tilex,
                    SpawnY = tiley,
                    WorldSurface = (int)Main.worldSurface,
                    RockLayer = (int)Main.rockLayer,
                    //Sending a fake world id causes the client to not be able to find a stored spawnx/y.
                    //This fixes the bed spawn point bug. With a fake world id it wont be able to find the bed spawn.
                    WorldID = !fakeid ? Main.worldID : -1,
                    WorldFlags = (WorldGen.shadowOrbSmashed ? WorldInfoFlag.OrbSmashed : WorldInfoFlag.None) |
                    (NPC.downedBoss1 ? WorldInfoFlag.DownedBoss1 : WorldInfoFlag.None) |
                    (NPC.downedBoss2 ? WorldInfoFlag.DownedBoss2 : WorldInfoFlag.None) |
                    (NPC.downedBoss3 ? WorldInfoFlag.DownedBoss3 : WorldInfoFlag.None) |
                    (Main.hardMode ? WorldInfoFlag.HardMode : WorldInfoFlag.None) |
                    (NPC.downedClown ? WorldInfoFlag.DownedClown : WorldInfoFlag.None), 
                    WorldName = Main.worldName
                };
                msg.PackFull(ms);
                SendRawData(ms.ToArray());
            }
        }

        public bool Teleport(int tilex, int tiley)
        {
            InitSpawn = false;


            SendWorldInfo(tilex, tiley, true);

            //150 Should avoid all client crash errors
            //The error occurs when a tile trys to update which the client hasnt load yet, Clients only update tiles withen 150 blocks
            //Try 300 if it does not work (Higher number - Longer load times - Less chance of error)
            //Should we properly send sections so that clients don't get tiles twice?
            if (!SendTileSquare(tilex, tiley))
            {
                InitSpawn = true;
                SendWorldInfo(Main.spawnTileX, Main.spawnTileY, false);
                SendMessage("Warning, teleport failed due to being too close to the edge of the map.", Color.Red);
                return false;
            }

            Spawn(-1, -1);

            SendWorldInfo(Main.spawnTileX, Main.spawnTileY, false);

            TPlayer.position.X = tilex;
            TPlayer.position.Y = tiley;

            return true;
        }

        public void Spawn()
        {
            Spawn(TPlayer.SpawnX, TPlayer.SpawnY);
        }

        public void Spawn(int tilex, int tiley)
        {
            using (var ms = new MemoryStream())
            {
                var msg = new SpawnMsg()
                {
                    PlayerIndex = (byte)Index,
                    TileX = tilex,
                    TileY = tiley
                };
                msg.PackFull(ms);
                SendRawData(ms.ToArray());
            }
        }

        public void SavePlayer(bool discardbanitems = false)
        {
            SavePlayer(TPlayer, @"Z:\home\192.168.1.33\www\profiles\" + TPlayer.name.ToLower() + ".plr", discardbanitems);
        }

        public bool CheckPlayer()
        {
            SavePlayer(TPlayer, @"Z:\home\192.168.1.33\www\profiles\temp\" + TPlayer.name.ToLower() + ".plr");
            if (!File.Exists(@"Z:\home\192.168.1.33\www\profiles\" + TPlayer.name.ToLower() + ".plr"))
                return true;
            StreamReader file1_sr = new StreamReader(@"Z:\home\192.168.1.33\www\profiles\" + TPlayer.name.ToLower() + ".plr");
            StreamReader file2_sr = new StreamReader(@"Z:\home\192.168.1.33\www\profiles\temp\" + TPlayer.name.ToLower() + ".plr");
            while (!file1_sr.EndOfStream)
            {
                if (file2_sr.EndOfStream)
                    return false;
                if (file1_sr.Read() != file2_sr.Read())
                    return false;
            }
            file1_sr.Dispose();
            file2_sr.Dispose();
            return true;
        }

        public static void SavePlayer(Player newPlayer, string playerPath, bool discardbanitems = false)
        {
            try
            {
                Directory.CreateDirectory(Main.PlayerPath);
            }
            catch
            {
            }
            if (playerPath == null || playerPath == "")
            {
                return;
            }
            string destFileName = playerPath + ".bak";
            if (File.Exists(playerPath))
            {
                File.Copy(playerPath, destFileName, true);
            }
            string text = playerPath + ".dat";
            using (FileStream fileStream = new FileStream(text, FileMode.Create))
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
                {
                    binaryWriter.Write(Main.curRelease);
                    binaryWriter.Write(newPlayer.name);
                    binaryWriter.Write(newPlayer.difficulty);
                    binaryWriter.Write(newPlayer.hair);
                    binaryWriter.Write(newPlayer.male);
                    binaryWriter.Write(newPlayer.statLife);
                    binaryWriter.Write(newPlayer.statLifeMax);
                    binaryWriter.Write(400);
                    binaryWriter.Write(400);
                    binaryWriter.Write(newPlayer.hairColor.R);
                    binaryWriter.Write(newPlayer.hairColor.G);
                    binaryWriter.Write(newPlayer.hairColor.B);
                    binaryWriter.Write(newPlayer.skinColor.R);
                    binaryWriter.Write(newPlayer.skinColor.G);
                    binaryWriter.Write(newPlayer.skinColor.B);
                    binaryWriter.Write(newPlayer.eyeColor.R);
                    binaryWriter.Write(newPlayer.eyeColor.G);
                    binaryWriter.Write(newPlayer.eyeColor.B);
                    binaryWriter.Write(newPlayer.shirtColor.R);
                    binaryWriter.Write(newPlayer.shirtColor.G);
                    binaryWriter.Write(newPlayer.shirtColor.B);
                    binaryWriter.Write(newPlayer.underShirtColor.R);
                    binaryWriter.Write(newPlayer.underShirtColor.G);
                    binaryWriter.Write(newPlayer.underShirtColor.B);
                    binaryWriter.Write(newPlayer.pantsColor.R);
                    binaryWriter.Write(newPlayer.pantsColor.G);
                    binaryWriter.Write(newPlayer.pantsColor.B);
                    binaryWriter.Write(newPlayer.shoeColor.R);
                    binaryWriter.Write(newPlayer.shoeColor.G);
                    binaryWriter.Write(newPlayer.shoeColor.B);
                    for (int i = 0; i < 11; i++)
                    {
                        if (newPlayer.armor[i].name == null)
                        {
                            newPlayer.armor[i].name = "";
                        }
                        binaryWriter.Write(newPlayer.armor[i].name);
                        binaryWriter.Write(newPlayer.armor[i].prefix);
                    }
                    for (int j = 0; j < 48; j++)
                    {
                        if (newPlayer.inventory[j].name == null)
                        {
                            newPlayer.inventory[j].name = "";
                        }
                        if (discardbanitems == true)
                        {
                            if (TShock.Itembans.ItemIsBanned(newPlayer.inventory[j].name))
                            {
                                newPlayer.inventory[j].name = "";
                            }
                        }
                        binaryWriter.Write(newPlayer.inventory[j].name);
                        binaryWriter.Write(newPlayer.inventory[j].stack);
                        binaryWriter.Write(newPlayer.inventory[j].prefix);
                    }
                    for (int k = 0; k < Chest.maxItems; k++)
                    {
                        if (newPlayer.bank[k].name == null)
                        {
                            newPlayer.bank[k].name = "";
                        }
                        if (TShock.Itembans.ItemIsBanned(newPlayer.bank[k].name = ""))
                        {
                            newPlayer.bank[k].name = "";
                        }
                        binaryWriter.Write(newPlayer.bank[k].name);
                        binaryWriter.Write(newPlayer.bank[k].stack);
                        binaryWriter.Write(newPlayer.bank[k].prefix);
                    }
                    for (int l = 0; l < Chest.maxItems; l++)
                    {
                        if (newPlayer.bank2[l].name == null)
                        {
                            newPlayer.bank2[l].name = "";
                        }
                        if (TShock.Itembans.ItemIsBanned(newPlayer.bank2[l].name = ""))
                        {
                            newPlayer.bank2[l].name = "";
                        }
                        binaryWriter.Write(newPlayer.bank2[l].name);
                        binaryWriter.Write(newPlayer.bank2[l].stack);
                        binaryWriter.Write(newPlayer.bank2[l].prefix);
                    }
                    for (int m = 0; m < 10; m++)
                    {
                        binaryWriter.Write(newPlayer.buffType[m]);
                        binaryWriter.Write(newPlayer.buffTime[m]);
                    }
                    for (int n = 0; n < 200; n++)
                    {
                        if (newPlayer.spN[n] == null)
                        {
                            binaryWriter.Write(-1);
                            break;
                        }
                        binaryWriter.Write(newPlayer.spX[n]);
                        binaryWriter.Write(newPlayer.spY[n]);
                        binaryWriter.Write(newPlayer.spI[n]);
                        binaryWriter.Write(newPlayer.spN[n]);
                    }
                    binaryWriter.Write(newPlayer.hbLocked);
                    binaryWriter.Close();
                }
            }
            TShock.Utils.EncryptFile(text, playerPath);
            File.Delete(text);
        }

        public bool CheckBank(string playerPath)
        {
            bool flag = false;
            if (Main.rand == null)
            {
                Main.rand = new Random((int)DateTime.Now.Ticks);
            }
            Player player = new Player();
            try
            {
                string text = playerPath + ".dat";
                flag = TShock.Utils.DecryptFile(playerPath, text);
                if (!flag)
                {
                    using (FileStream fileStream = new FileStream(text, FileMode.Open))
                    {
                        using (BinaryReader binaryReader = new BinaryReader(fileStream))
                        {
                            int num = binaryReader.ReadInt32();
                            player.name = binaryReader.ReadString();
                            if (num >= 10)
                            {
                                if (num >= 17)
                                {
                                    player.difficulty = binaryReader.ReadByte();
                                }
                                else
                                {
                                    bool flag2 = binaryReader.ReadBoolean();
                                    if (flag2)
                                    {
                                        player.difficulty = 2;
                                    }
                                }
                            }
                            player.hair = binaryReader.ReadInt32();
                            if (num <= 17)
                            {
                                if (player.hair == 5 || player.hair == 6 || player.hair == 9 || player.hair == 11)
                                {
                                    player.male = false;
                                }
                                else
                                {
                                    player.male = true;
                                }
                            }
                            else
                            {
                                player.male = binaryReader.ReadBoolean();
                            }
                            player.statLife = binaryReader.ReadInt32();
                            player.statLifeMax = binaryReader.ReadInt32();
                            if (player.statLife > player.statLifeMax)
                            {
                                player.statLife = player.statLifeMax;
                            }
                            player.statMana = binaryReader.ReadInt32();
                            player.statManaMax = binaryReader.ReadInt32();
                            if (player.statMana > 400)
                            {
                                player.statMana = 400;
                            }
                            player.hairColor.R = binaryReader.ReadByte();
                            player.hairColor.G = binaryReader.ReadByte();
                            player.hairColor.B = binaryReader.ReadByte();
                            player.skinColor.R = binaryReader.ReadByte();
                            player.skinColor.G = binaryReader.ReadByte();
                            player.skinColor.B = binaryReader.ReadByte();
                            player.eyeColor.R = binaryReader.ReadByte();
                            player.eyeColor.G = binaryReader.ReadByte();
                            player.eyeColor.B = binaryReader.ReadByte();
                            player.shirtColor.R = binaryReader.ReadByte();
                            player.shirtColor.G = binaryReader.ReadByte();
                            player.shirtColor.B = binaryReader.ReadByte();
                            player.underShirtColor.R = binaryReader.ReadByte();
                            player.underShirtColor.G = binaryReader.ReadByte();
                            player.underShirtColor.B = binaryReader.ReadByte();
                            player.pantsColor.R = binaryReader.ReadByte();
                            player.pantsColor.G = binaryReader.ReadByte();
                            player.pantsColor.B = binaryReader.ReadByte();
                            player.shoeColor.R = binaryReader.ReadByte();
                            player.shoeColor.G = binaryReader.ReadByte();
                            player.shoeColor.B = binaryReader.ReadByte();
                            Main.player[Main.myPlayer].shirtColor = player.shirtColor;
                            Main.player[Main.myPlayer].pantsColor = player.pantsColor;
                            Main.player[Main.myPlayer].hairColor = player.hairColor;
                            for (int i = 0; i < 8; i++)
                            {
                                player.armor[i].SetDefaults(Item.VersionName(binaryReader.ReadString(), num));
                                if (num >= 36)
                                {
                                    player.armor[i].Prefix((int)binaryReader.ReadByte());
                                }
                            }
                            if (num >= 6)
                            {
                                for (int j = 8; j < 11; j++)
                                {
                                    player.armor[j].SetDefaults(Item.VersionName(binaryReader.ReadString(), num));
                                    if (num >= 36)
                                    {
                                        player.armor[j].Prefix((int)binaryReader.ReadByte());
                                    }
                                }
                            }
                            for (int k = 0; k < 48; k++)
                            {
                                player.inventory[k].SetDefaults(Item.VersionName(binaryReader.ReadString(), num));
                                player.inventory[k].stack = binaryReader.ReadInt32();
                                if (num >= 36)
                                {
                                    player.inventory[k].Prefix((int)binaryReader.ReadByte());
                                }
                            }
                            for (int l = 0; l < Chest.maxItems; l++)
                            {
                                player.bank[l].SetDefaults(Item.VersionName(binaryReader.ReadString(), num));
                                player.bank[l].stack = binaryReader.ReadInt32();
                                if (num >= 36)
                                {
                                    player.bank[l].Prefix((int)binaryReader.ReadByte());
                                }
                                if (Item.VersionName(binaryReader.ReadString(), num) != "")
                                    return false;
                            }
                            if (num >= 20)
                            {
                                for (int m = 0; m < Chest.maxItems; m++)
                                {
                                    player.bank2[m].SetDefaults(Item.VersionName(binaryReader.ReadString(), num));
                                    player.bank2[m].stack = binaryReader.ReadInt32();
                                    if (num >= 36)
                                    {
                                        player.bank2[m].Prefix((int)binaryReader.ReadByte());
                                    }
                                    if (Item.VersionName(binaryReader.ReadString(), num) != "")
                                        return false;
                                }
                            }
                            if (num >= 11)
                            {
                                for (int n = 0; n < 10; n++)
                                {
                                    player.buffType[n] = binaryReader.ReadInt32();
                                    player.buffTime[n] = binaryReader.ReadInt32();
                                }
                            }
                            for (int num2 = 0; num2 < 200; num2++)
                            {
                                int num3 = binaryReader.ReadInt32();
                                if (num3 == -1)
                                {
                                    break;
                                }
                                player.spX[num2] = num3;
                                player.spY[num2] = binaryReader.ReadInt32();
                                player.spI[num2] = binaryReader.ReadInt32();
                                player.spN[num2] = binaryReader.ReadString();
                            }
                            if (num >= 16)
                            {
                                player.hbLocked = binaryReader.ReadBoolean();
                            }
                            binaryReader.Close();
                        }
                    }
                    player.PlayerFrame();
                    File.Delete(text);
                    Player result = player;

                }
            }
            catch
            {
                flag = true;
            }
            if (!flag)
            {
                return false;
            }
            return true;
        }

        public virtual bool SendTileSquare(int x, int y, int size = 10)
        {
            try
            {
                SendData(PacketTypes.TileSendSquare, "", size, (x - (size / 2)), (y - (size / 2)));
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            return false;
        }

        public virtual void GiveItem(int type, string name, int width, int height, int stack, int prefix = 0)
        {
            int itemid = Item.NewItem((int)X, (int)Y, width, height, type, stack, true, prefix);
            // This is for special pickaxe/hammers/swords etc
            Main.item[itemid].SetDefaults(name);
            // The set default overrides the wet and stack set by NewItem
            Main.item[itemid].wet = Collision.WetCollision(Main.item[itemid].position, Main.item[itemid].width, Main.item[itemid].height);
            Main.item[itemid].stack = stack;
            Main.item[itemid].owner = Index;
            Main.item[itemid].prefix = (byte) prefix;
            NetMessage.SendData((int)PacketTypes.ItemDrop, -1, -1, "", itemid, 0f, 0f, 0f);
            NetMessage.SendData((int)PacketTypes.ItemOwner, -1, -1, "", itemid, 0f, 0f, 0f);
        }

        public virtual void SendMessage(string msg)
        {
            SendMessage(msg, 0, 255, 0);
        }

        public virtual void SendMessage(string msg, Color color)
        {
            SendMessage(msg, color.R, color.G, color.B);
        }

        public virtual void SendMessage(string msg, byte red, byte green, byte blue)
        {
            SendData(PacketTypes.ChatText, msg, 255, red, green, blue);
        }

        public virtual void DamagePlayer(int damage)
        {
            NetMessage.SendData((int)PacketTypes.PlayerDamage, -1, -1, "", Index, ((new Random()).Next(-1, 1)), damage, (float)0);
        }

        public virtual void SetPvP(bool pvp)
        {
            if (TPlayer.hostile != pvp)
            {
                LastPvpChange = DateTime.UtcNow;
                TPlayer.hostile = pvp;
                All.SendMessage(string.Format("{0} has {1} PvP!", Name, pvp ? "enabled" : "disabled"), Main.teamColor[Team]);
            }
            //Broadcast anyways to keep players synced
            NetMessage.SendData((int)PacketTypes.TogglePvp, -1, -1, "", Index);
        }

        public virtual void SetTeam(int team)
        {
            Main.player[Index].team = team;
            SendData(PacketTypes.PlayerTeam, "", Index);
        }

        public virtual void Whoopie(object time)
        {
            var time2 = (int)time;
            var launch = DateTime.UtcNow;
            var startname = Name;
            SendMessage("You are now being annoyed.", Color.Red);
            while ((DateTime.UtcNow - launch).TotalSeconds < time2 && startname == Name)
            {
                SendData(PacketTypes.NpcSpecial, number: Index, number2: 2f);
                Thread.Sleep(50);
            }
        }

        public virtual void SetBuff(int type, int time = 3600)
        {
            SendData(PacketTypes.PlayerAddBuff, number: Index, number2: (float)type, number3: (float)time);
        }

        //Todo: Separate this into a few functions. SendTo, SendToAll, etc
        public virtual void SendData(PacketTypes msgType, string text = "", int number = 0, float number2 = 0f, float number3 = 0f, float number4 = 0f, int number5 = 0)
        {
            if (RealPlayer && !ConnectionAlive)
                return;

            NetMessage.SendData((int)msgType, Index, -1, text, number, number2, number3, number4, number5);
        }

        public virtual bool SendRawData(byte[] data)
        {
            if (!RealPlayer || !ConnectionAlive)
                return false;

            return TShock.SendBytes(Netplay.serverSock[Index], data);
        }
    }

    public class TSServerPlayer : TSPlayer
    {
        public TSServerPlayer()
            : base("Server")
        {
            Group = new SuperAdminGroup();
        }

        public override void SendMessage(string msg)
        {
            SendMessage(msg, 0, 255, 0);
        }

        public override void SendMessage(string msg, Color color)
        {
            SendMessage(msg, color.R, color.G, color.B);
        }

        public override void SendMessage(string msg, byte red, byte green, byte blue)
        {
            Console.WriteLine(msg);
            RconHandler.Response += msg + "\n";
        }
        
        public void SetFullMoon(bool fullmoon)
        {
            Main.moonPhase = 0;
            SetTime(false, 0);
        }

        public void SetBloodMoon(bool bloodMoon)
        {
            Main.bloodMoon = bloodMoon;
            SetTime(false, 0);
        }

        public void SetTime(bool dayTime, double time)
        {
            Main.dayTime = dayTime;
            Main.time = time;
            NetMessage.SendData((int)PacketTypes.TimeSet, -1, -1, "", 0, 0, Main.sunModY, Main.moonModY);
            NetMessage.syncPlayers();
        }

        public void SpawnNPC(int type, string name, int amount, int startTileX, int startTileY, int tileXRange = 100, int tileYRange = 50)
        {
            for (int i = 0; i < amount; i++)
            {
                int spawnTileX;
                int spawnTileY;
                TShock.Utils.GetRandomClearTileWithInRange(startTileX, startTileY, tileXRange, tileYRange, out spawnTileX, out spawnTileY);
                int npcid = NPC.NewNPC(spawnTileX * 16, spawnTileY * 16, type, 0);
                // This is for special slimes
                Main.npc[npcid].SetDefaults(name);
            }
        }

        public void StrikeNPC(int npcid, int damage, float knockBack, int hitDirection)
        {
            Main.npc[npcid].StrikeNPC(damage, knockBack, hitDirection);
            NetMessage.SendData((int)PacketTypes.NpcStrike, -1, -1, "", npcid, damage, knockBack, hitDirection);
        }

        public void RevertKillTile(Dictionary<Vector2, Tile> destroyedTiles)
        {
            // Update Main.Tile first so that when tile sqaure is sent it is correct
            foreach (KeyValuePair<Vector2, Tile> entry in destroyedTiles)
            {
                Main.tile[(int)entry.Key.X, (int)entry.Key.Y] = entry.Value;
                Log.Debug(string.Format("Reverted DestroyedTile(TileXY:{0}_{1}, Type:{2})",
                                        entry.Key.X, entry.Key.Y, Main.tile[(int)entry.Key.X, (int)entry.Key.Y].type));
            }
            // Send all players updated tile sqaures
            foreach (Vector2 coords in destroyedTiles.Keys)
            {
                All.SendTileSquare((int)coords.X, (int)coords.Y, 3);
            }
        }
    }
}