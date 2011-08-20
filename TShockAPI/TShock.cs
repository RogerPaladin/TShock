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
/* TShock wouldn't be possible without:
 * Github
 * Microsoft Visual Studio 2010
 * HostPenda
 * And you, for your continued support and devotion to the evolution of TShock
 * Kerplunc Gaming
 * TerrariaGSP
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Linq;
using System.Threading;
using Community.CsharpSqlite.SQLiteClient;
using Microsoft.Xna.Framework;
using MySql.Data.MySqlClient;
using Terraria;
using TerrariaAPI;
using TerrariaAPI.Hooks;
using TShockAPI.DB;
using TShockAPI.Net;

namespace TShockAPI
{
    [APIVersion(1, 7)]
    public class TShock : TerrariaPlugin
    {
        public static readonly Version VersionNum = Assembly.GetExecutingAssembly().GetName().Version;
        public static readonly string VersionCodename = "Yes, we're adding Logblock style functionality soon, don't worry.";

        public static string SavePath = "tshock";

        public static int id = Process.GetCurrentProcess().Id;
        public static TSPlayer[] Players = new TSPlayer[Main.maxPlayers];
        public static BanManager Bans;
        public static WarpManager Warps;
        public static RegionManager Regions;
        public static BackupManager Backups;
        public static GroupManager Groups;
        public static UserManager Users;
        public static ItemManager Itembans;
        public static RemeberedPosManager RememberedPos;
        public static ConfigFile Config { get; set; }
        public static IDbConnection DB;
        public static bool OverridePort;
        public static int disptime = 1000 * 60 * 15;
        public static List<string> DispenserTime = new List<string>();
        public static DateTime Spawner = new DateTime();
        public static DateTime StackCheatChecker = new DateTime();
        public static RestartManager Restart;
        public static PacketBufferer PacketBuffer;

        /// <summary>
        /// Called after TShock is initialized. Useful for plugins that needs hooks before tshock but also depend on tshock being loaded.
        /// </summary>
        public static event Action Initialized;

        public override Version Version
        {
            get { return VersionNum; }
        }

        public override string Name
        {
            get { return "TShock"; }
        }

        public override string Author
        {
            get { return "The TShock Team"; }
        }

        public override string Description
        {
            get { return "The administration modification of the future."; }
        }

        public TShock(Main game)
            : base(game)
        {
            Config = new ConfigFile();
            Order = 0;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        public override void Initialize()
        {
            if (!Directory.Exists(SavePath))
                Directory.CreateDirectory(SavePath);

#if DEBUG
            Log.Initialize(Path.Combine(SavePath, "log.txt"), LogLevel.All, false);
#else
            Log.Initialize(Path.Combine(SavePath, "log.txt"), LogLevel.All & ~LogLevel.Debug, false);
#endif
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;


            try
            {

                if (File.Exists(Path.Combine(SavePath, "tshock.pid")))
                {
                    Log.ConsoleInfo("TShock was improperly shut down. Deleting invalid pid file...");
                    File.Delete(Path.Combine(SavePath, "tshock.pid"));
                }
                File.WriteAllText(Path.Combine(SavePath, "tshock.pid"), Process.GetCurrentProcess().Id.ToString());

                ConfigFile.ConfigRead += OnConfigRead;
                FileTools.SetupConfig();

                HandleCommandLine(Environment.GetCommandLineArgs());

                if (Config.StorageType.ToLower() == "sqlite")
                {
                    string sql = Path.Combine(SavePath, "tshock.sqlite");
                    DB = new SqliteConnection(string.Format("uri=file://{0},Version=3", sql));
                }
                else if (Config.StorageType.ToLower() == "mysql")
                {
                    try
                    {
                        var hostport = Config.MySqlHost.Split(':');
                        DB = new MySqlConnection();
                        DB.ConnectionString =
                            String.Format("Server='{0}'; Port='{1}'; Database='{2}'; Uid='{3}'; Pwd='{4}';",
                                          hostport[0],
                                          hostport.Length > 1 ? hostport[1] : "3306",
                                          Config.MySqlDbName,
                                          Config.MySqlUsername,
                                          Config.MySqlPassword
                                );
                    }
                    catch (MySqlException ex)
                    {
                        Log.Error(ex.ToString());
                        throw new Exception("MySql not setup correctly");
                    }
                }
                else
                {
                    throw new Exception("Invalid storage type");
                }

                Backups = new BackupManager(Path.Combine(SavePath, "backups"));
                Backups.KeepFor = Config.BackupKeepFor;
                Backups.Interval = Config.BackupInterval;
                Bans = new BanManager(DB);
                Warps = new WarpManager(DB);
                Users = new UserManager(DB);
                Groups = new GroupManager(DB);
                Groups.LoadPermisions();
                Regions = new RegionManager(DB);
                Itembans = new ItemManager(DB);
                RememberedPos = new RemeberedPosManager(DB);
                Restart = new RestartManager();

                Log.ConsoleInfo(string.Format("TShock Version {0} ({1}) now running.", Version, VersionCodename));

                GameHooks.PostInitialize += OnPostInit;
                GameHooks.Update += OnUpdate;
                ServerHooks.Join += OnJoin;
                ServerHooks.Leave += OnLeave;
                ServerHooks.Chat += OnChat;
                ServerHooks.Command += ServerHooks_OnCommand;
                NetHooks.GetData += OnGetData;
                NetHooks.SendData += NetHooks_SendData;
                NetHooks.GreetPlayer += OnGreetPlayer;
                NpcHooks.StrikeNpc += NpcHooks_OnStrikeNpc;

                GetDataHandlers.InitGetDataHandler();
                Commands.InitCommands();
                RconHandler.StartThread();

                if (Config.BufferPackets)
                    PacketBuffer = new PacketBufferer();

                do
                {
                    Users.DeletePlayersAfterMinutes(TShock.Config.DeleteUserAfterMinutes);
                }
                while (Users.DeletePlayersAfterMinutes(TShock.Config.DeleteUserAfterMinutes) != true);
                
                Log.ConsoleInfo("AutoSave " + (Config.AutoSave ? "Enabled" : "Disabled"));
                Log.ConsoleInfo("Backups " + (Backups.Interval > 0 ? "Enabled" : "Disabled"));

                if (Initialized != null)
                    Initialized();
            }
            
            catch (Exception ex)
            {
                Log.Error("Fatal Startup Exception");
                Log.Error(ex.ToString());
                Environment.Exit(1);
            }
        }

        public override void DeInitialize()
        {
            GameHooks.PostInitialize -= OnPostInit;
            GameHooks.Update -= OnUpdate;
            ServerHooks.Join -= OnJoin;
            ServerHooks.Leave -= OnLeave;
            ServerHooks.Chat -= OnChat;
            ServerHooks.Command -= ServerHooks_OnCommand;
            NetHooks.GetData -= OnGetData;
            NetHooks.SendData -= NetHooks_SendData;
            NetHooks.GreetPlayer -= OnGreetPlayer;
            NpcHooks.StrikeNpc -= NpcHooks_OnStrikeNpc;
            if (File.Exists(Path.Combine(SavePath, "tshock.pid")))
            {
                Console.WriteLine("Thanks for using TShock! Process ID file is now being destroyed.");
                File.Delete(Path.Combine(SavePath, "tshock.pid"));
            }
        }

        /// <summary>
        /// Handles exceptions that we didn't catch or that Red fucked up
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Error(e.ExceptionObject.ToString());

            if (e.ExceptionObject.ToString().Contains("Terraria.Netplay.ListenForClients") ||
                e.ExceptionObject.ToString().Contains("Terraria.Netplay.ServerLoop"))
            {
                var sb = new List<string>();
                for (int i = 0; i < Netplay.serverSock.Length; i++)
                {
                    if (Netplay.serverSock[i] == null)
                    {
                        sb.Add("Sock[" + i + "]");
                    }
                    else if (Netplay.serverSock[i].tcpClient == null)
                    {
                        sb.Add("Tcp[" + i + "]");
                    }
                }
                Log.Error(string.Join(", ", sb));
            }

            if (e.IsTerminating)
            {
                if (Main.worldPathName != null && Config.SaveWorldOnCrash)
                {
                    //Main.worldPathName += ".crash";
                    WorldGen.saveWorld();
                }
                Restart.DoRestart();
                DeInitialize();
            }
        }

        private void HandleCommandLine(string[] parms)
        {
            for (int i = 0; i < parms.Length; i++)
            {
                if (parms[i].ToLower() == "-ip")
                {
                    IPAddress ip;
                    if (IPAddress.TryParse(parms[++i], out ip))
                    {
                        Netplay.serverListenIP = ip;
                        Console.Write("Using IP: {0}", ip);
                    }
                    else
                    {
                        Console.WriteLine("Bad IP: {0}", parms[i]);
                    }
                }
                if (parms[i].ToLower() == "-configpath")
                {
                    var path = parms[++i];
                    if (path.IndexOfAny(Path.GetInvalidPathChars()) == -1)
                    {
                        SavePath = path;
                        Log.ConsoleInfo("Config path has been set to " + path);
                    }
                }
                if (parms[i].ToLower() == "-worldpath")
                {
                    var path = parms[++i];
                    if (path.IndexOfAny(Path.GetInvalidPathChars()) == -1)
                    {
                        Main.WorldPath = path;
                        Log.ConsoleInfo("World path has been set to " + path);
                    }
                }
                if (parms[i].ToLower() == "-port")
                {
                    int port = Convert.ToInt32(parms[++i]);
                    Netplay.serverPort = port;
                    Config.ServerPort = port;
                    OverridePort = true;
                    Log.ConsoleInfo("Port overridden by startup argument. Set to " + port);
                }
            }
        }

        /*
         * Hooks:
         * 
         */

        public static int AuthToken = -1;

        private void OnPostInit()
        {
            if (!File.Exists(Path.Combine(SavePath, "auth.lck")) && !File.Exists(Path.Combine(SavePath, "authcode.txt")))
            {
                var r = new Random((int)DateTime.Now.ToBinary());
                AuthToken = r.Next(100000, 10000000);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("TShock Notice: To become SuperAdmin, join the game and type /auth " + AuthToken);
                Console.WriteLine("This token will display until disabled by verification. (/auth-verify)");
                Console.ForegroundColor = ConsoleColor.Gray;
                FileTools.CreateFile(Path.Combine(SavePath, "authcode.txt"));
                using (var tw = new StreamWriter(Path.Combine(SavePath, "authcode.txt")))
                {
                    tw.WriteLine(AuthToken);
                }
            }
            else if (File.Exists(Path.Combine(SavePath, "authcode.txt")))
            {
                using (var tr = new StreamReader(Path.Combine(SavePath, "authcode.txt")))
                {
                    AuthToken = Convert.ToInt32(tr.ReadLine());
                }
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(
                    "TShock Notice: authcode.txt is still present, and the AuthToken located in that file will be used.");
                Console.WriteLine("To become superadmin, join the game and type /auth " + AuthToken);
                Console.WriteLine("This token will display until disabled by verification. (/auth-verify)");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            else
            {
                AuthToken = 0;
            }
            Regions.ReloadAllRegions();
        }


        private DateTime LastCheck = DateTime.UtcNow;

        private void OnUpdate(GameTime time)
        {
            string item;
            int itemcount;

            UpdateManager.UpdateProcedureCheck();

            if (Backups.IsBackupTime)
                Backups.Backup();
            
            if (Restart.PrepareToRestart)
            {
                Console.WriteLine("The server will be restarted in 5 minutes");
                Tools.Broadcast("The server will be restarted in 5 minutes");
                Log.Info("The server will be restarted in 5 minutes");
            }

            if (Restart.IsRestartTime)
                Restart.Restart();
            
            //call these every second, not every update
            if ((DateTime.UtcNow - LastCheck).TotalSeconds >= 1)
            {
                LastCheck = DateTime.UtcNow;
                foreach (TSPlayer player in Players)
                {
                    if (player != null && player.Active)
                    {
                        if (player.TilesDestroyed != null)
                        {
                            if (player.TileThreshold >= Config.TileThreshold)
                            {
                                if (Tools.HandleTntUser(player, "Kill tile abuse detected."))
                                {
                                    TSPlayer.Server.RevertKillTile(player.TilesDestroyed);
                                }
                            }
                            if (player.TileThreshold > 0)
                            {
                                player.TileThreshold = 0;
                                player.TilesDestroyed.Clear();
                            }
                        }

                        if ((DateTime.UtcNow - StackCheatChecker).TotalMilliseconds > 5000)
                        {
                            StackCheatChecker = DateTime.UtcNow;
                            if (player.StackCheat(out item, out itemcount))
                            {
                                Tools.Broadcast(string.Format("{0} cheater!!! {1} x {2}", player.Name, item, itemcount), Color.Yellow);
                                //Tools.Ban(player, "Stack Cheat.", "Server", Convert.ToString(DateTime.Now));
                                Tools.ForceKick(player, string.Format("Stack Cheat. {0} x {1}", item, itemcount));
                            }
                        }
                        
                        if (!player.Group.HasPermission(Permissions.usebanneditem))
                        {
                            var inv = player.TPlayer.inventory;

                            for (int i = 0; i < inv.Length; i++)
                            {
                                if (inv[i] != null && Itembans.ItemIsBanned(inv[i].name))
                                {
                                    player.Disconnect("Using banned item: " + inv[i].name + ", remove it and rejoin");
                                    break;
                                }
                            }
                        }
                        if (!player.IsLoggedIn)
                        {
                            if ((DateTime.UtcNow - player.Interval).TotalMilliseconds > 5000)
                            {
                                player.SendMessage(string.Format("Login in {0} seconds", TShock.Config.TimeToLogin * 60 - Math.Round((DateTime.UtcNow - player.LoginTime).TotalSeconds, 0)), Color.Red);
                                player.Interval = DateTime.UtcNow;
                            }
                            if ((DateTime.UtcNow - player.LoginTime).TotalMinutes >= TShock.Config.TimeToLogin)
                            {
                                Tools.Broadcast(player.Name + " Not logged in.", Color.Yellow);
                                Tools.ForceKick(player, "Not logged in.");
                            }
                        }
                    }
                }
            }
        }
        private void OnJoin(int ply, HandledEventArgs handler)
        {
            var player = new TSPlayer(ply);
            if (Config.EnableDNSHostResolution)
            {
                player.Group = Users.GetGroupForIPExpensive(player.IP);
            }
            else
            {
                player.Group = Users.GetGroupForIP(player.IP);
            }

            if (Tools.ActivePlayers() + 1 > Config.MaxSlots && !player.Group.HasPermission(Permissions.reservedslot))
            {
                Tools.ForceKick(player, Config.ServerFullReason);
                handler.Handled = true;
                return;
            }

            var ban = Bans.GetBanByIp(player.IP);
            if (ban != null)
            {
                Tools.ForceKick(player, string.Format("You are banned: {0}", ban.Reason));
                handler.Handled = true;
                return;
            }
            
            /*  if (!Tools.Ping(player.IP))
             {
            Tools.ForceKick(player, "Bad ping");
            handler.Handled = true;
            return;
            }
            */ 
            
            if (!FileTools.OnWhitelist(player.IP))
            {
                Tools.ForceKick(player, "Not on whitelist.");
                handler.Handled = true;
                return;
            }

            Players[ply] = player;
            player.LoginTime = DateTime.UtcNow;
        }

        private void OnLeave(int ply)
        {
            var tsplr = Players[ply];
            Players[ply] = null;

            if (tsplr != null && tsplr.ReceivedInfo)
            {
                Log.Info(string.Format("{0} left.", tsplr.Name));
                TShock.Users.PlayingTime(tsplr.Name, Convert.ToInt32((DateTime.UtcNow - tsplr.LoginTime).TotalMinutes));
                
                if (Config.RememberLeavePos)
                {
                    RememberedPos.InsertLeavePos(tsplr.Name, tsplr.IP, (int)(tsplr.X / 16), (int)(tsplr.Y / 16));
                }
            }
        }

        private void OnChat(messageBuffer msg, int ply, string text, HandledEventArgs e)
        {
            if (e.Handled)
                return;

            var tsplr = Players[msg.whoAmI];
            if (tsplr == null)
            {
                e.Handled = true;
                return;
            }

            if (!Tools.ValidString(text))
            {
                Tools.Kick(tsplr, "Unprintable character in chat");
                e.Handled = true;
                return;
            }

            if (msg.whoAmI != ply)
            {
                e.Handled = Tools.HandleGriefer(tsplr, "Faking Chat");
                return;
            }

            if (tsplr.Group.HasPermission(Permissions.adminchat) && !text.StartsWith("/") && Config.AdminChatEnabled)
            {
                Tools.Broadcast(Config.AdminChatPrefix + "<" + tsplr.Name + "> " + text,
                                tsplr.Group.R, tsplr.Group.G,
                                tsplr.Group.B);
                e.Handled = true;
                return;
            }

            if (text.StartsWith("/"))
            {
                foreach (TSPlayer Player in TShock.Players)
                {
                    if (Player != null && Player.Active && Player.Group.HasPermission(Permissions.adminchat))
                        Player.SendMessage(string.Format("*<{0}> /{1}", tsplr.Name, text.Remove(0, 1)), Color.Red);
                }
                Console.WriteLine(string.Format("*<{0}> /{1}", tsplr.Name, text.Remove(0, 1)));
                try
                {
                    e.Handled = Commands.HandleCommand(tsplr, text);
                }
                catch (Exception ex)
                {
                    Log.ConsoleError("Command exception");
                    Log.Error(ex.ToString());
                }
                e.Handled = true;
            }
            else
            {
                //Tools.Broadcast("{2}<{0}> {1}".SFormat(tsplr.Name, text, Config.ChatDisplayGroup ? "[{0}] ".SFormat(tsplr.Group.Name) : ""));
                                //tsplr.Group.R, tsplr.Group.G,
                                //tsplr.Group.B);
                Log.Info(string.Format("{0} said: {1}", tsplr.Name, text));
                //e.Handled = true;
            }
        }

        /// <summary>
        /// When a server command is run.
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="e"></param>
        private void ServerHooks_OnCommand(string text, HandledEventArgs e)
        {
            if (e.Handled)
                return;

            // Damn you ThreadStatic and Redigit
            if (Main.rand == null)
            {
                Main.rand = new Random();
            }
            if (WorldGen.genRand == null)
            {
                WorldGen.genRand = new Random();
            }

            if (text.StartsWith("exit"))
            {
                Tools.ForceKickAll("Server shutting down!");
            }
            else if (text.StartsWith("playing") || text.StartsWith("/playing"))
            {
                int count = 0;
                foreach (TSPlayer player in Players)
                {
                    if (player != null && player.Active)
                    {
                        count++;
                        TSPlayer.Server.SendMessage(string.Format("{0} ({1}) [{2}] <{3}>", player.Name, player.IP,
                                                                  player.Group.Name, player.UserAccountName));
                    }
                }
                TSPlayer.Server.SendMessage(string.Format("{0} players connected.", count));
                e.Handled = true;
            }
            else if (text.StartsWith("say "))
            {
                Log.Info(string.Format("Server said: {0}", text.Remove(0, 4)));
            }
            else if (text == "autosave")
            {
                Main.autoSave = Config.AutoSave = !Config.AutoSave;
                Log.ConsoleInfo("AutoSave " + (Config.AutoSave ? "Enabled" : "Disabled"));
                e.Handled = true;
            }
            else if (text.StartsWith("/"))
            {
                if (Commands.HandleCommand(TSPlayer.Server, text))
                    e.Handled = true;
            }
        }

        private void OnGetData(GetDataEventArgs e)
        {
            if (e.Handled)
                return;

            PacketTypes type = e.MsgID;

            Debug.WriteLine("Recv: {0:X}: {2} ({1:XX})", e.Msg.whoAmI, (byte)type, type);

            var player = Players[e.Msg.whoAmI];
            if (player == null)
            {
                e.Handled = true;
                return;
            }

            if (!player.ConnectionAlive)
            {
                e.Handled = true;
                return;
            }

            

            // Stop accepting updates from player as this player is going to be kicked/banned during OnUpdate (different thread so can produce race conditions)
            if ((Config.BanKillTileAbusers || Config.KickKillTileAbusers) &&
                player.TileThreshold >= Config.TileThreshold && !player.Group.HasPermission(Permissions.ignoregriefdetection))
            {
                Log.Debug("Rejecting " + type + " from " + player.Name + " as this player is about to be kicked");
                e.Handled = true;
            }
            else
            {
                using (var data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
                {
                    try
                    {
                        if (GetDataHandlers.HandlerGetData(type, player, data))
                            e.Handled = true;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                }
            }
        }

        private void OnGreetPlayer(int who, HandledEventArgs e)
        {
            var player = Players[who];
            if (player == null)
            {
                e.Handled = true;
                return;
            }

            NetMessage.SendData((int)PacketTypes.TimeSet, -1, -1, "", 0, 0, Main.sunModY, Main.moonModY);
            NetMessage.syncPlayers();

            Log.Info(string.Format("{0} ({1}) from '{2}' group joined.", player.Name, player.IP, player.Group.Name));

            Tools.ShowFileToUser(player, "motd.txt");
            if (HackedHealth(player))
            {
                Tools.HandleCheater(player, "Hacked health.");
            }
            if (Config.AlwaysPvP)
            {
                player.SetPvP(true);
                player.SendMessage(
                    "PvP is forced! Enable PvP else you can't deal damage to other people. (People can kill you)",
                    Color.Red);
            }
            if (player.Group.HasPermission(Permissions.causeevents) && Config.InfiniteInvasion)
            {
                StartInvasion();
            }
            if (!DispenserTime.Contains(player.Name))
            {
                DispenserTime.Add(player.Name + ";" + Convert.ToString(DateTime.UtcNow.AddMilliseconds(-disptime)));
            }
            if (Config.RememberLeavePos)
            {
                var pos = RememberedPos.GetLeavePos(player.Name, player.IP);
                player.Teleport((int)pos.X, (int)pos.Y);
                player.SendTileSquare((int)pos.X, (int)pos.Y);
            }
            e.Handled = true;
        }

        private void NpcHooks_OnStrikeNpc(NpcStrikeEventArgs e)
        {
            if (Config.InfiniteInvasion)
            {
                IncrementKills();
                if (Main.invasionSize < 10)
                {
                    Main.invasionSize = 20000000;
                }
            }
        }

        /// <summary>
        /// Send bytes to client using packetbuffering if available
        /// </summary>
        /// <param name="client">socket to send to</param>
        /// <param name="bytes">bytes to send</param>
        /// <returns>False on exception</returns>
        public static bool SendBytes(ServerSock client, byte[] bytes)
        {
            if (PacketBuffer != null)
            {
                PacketBuffer.BufferBytes(client, bytes);
                return true;
            }

            return SendBytesBufferless(client,bytes);
        }
        /// <summary>
        /// Send bytes to a client ignoring the packet buffer
        /// </summary>
        /// <param name="client">socket to send to</param>
        /// <param name="bytes">bytes to send</param>
        /// <returns>False on exception</returns>
        public static bool SendBytesBufferless(ServerSock client, byte[] bytes)
        {
            try
            {
                if (client.tcpClient.Connected)
                    client.networkStream.Write(bytes, 0, bytes.Length);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn("This is a normal exception");
                Log.Warn(ex.ToString());
            }
            return false;
        }

        void NetHooks_SendData(SendDataEventArgs e)
        {
            if (e.MsgID == PacketTypes.Disconnect)
            {
                Action<ServerSock,string> senddisconnect = (sock, str) =>
                {
                    if (sock == null || !sock.active)
                        return;
                    using (var ms = new MemoryStream())
                    {
                        new DisconnectMsg {Reason = str}.PackFull(ms);
                        SendBytesBufferless(sock, ms.ToArray());
                    }
                };

                if (e.remoteClient != -1)
                {
                    senddisconnect(Netplay.serverSock[e.remoteClient], e.text);
                }
                else
                {
                    for (int i = 0; i < Netplay.serverSock.Length; i++)
                    {
                        if (e.ignoreClient != -1 && e.ignoreClient == i)
                            continue;

                        senddisconnect(Netplay.serverSock[i], e.text);
                    }
                }
                e.Handled = true;
            }
        }

        private void OnSaveWorld(bool resettime, HandledEventArgs e)
        {
            Tools.Broadcast("Saving world. Momentary lag might result from this.", Color.Red);
            Thread SaveWorld = new Thread(Tools.SaveWorld);
            SaveWorld.Start();
            e.Handled = true;
        }

        /*
         * Useful stuff:
         * */

        public static void StartInvasion()
        {
            Main.invasionType = 1;
            if (Config.InfiniteInvasion)
            {
                Main.invasionSize = 20000000;
            }
            else
            {
                Main.invasionSize = 100 + (Config.InvasionMultiplier * Tools.ActivePlayers());
            }

            Main.invasionWarn = 0;
            if (new Random().Next(2) == 0)
            {
                Main.invasionX = 0.0;
            }
            else
            {
                Main.invasionX = Main.maxTilesX;
            }
        }

        private static int KillCount;

        public static void IncrementKills()
        {
            KillCount++;
            Random r = new Random();
            int random = r.Next(5);
            if (KillCount % 100 == 0)
            {
                switch (random)
                {
                    case 0:
                        Tools.Broadcast(string.Format("You call that a lot? {0} goblins killed!", KillCount));
                        break;
                    case 1:
                        Tools.Broadcast(string.Format("Fatality! {0} goblins killed!", KillCount));
                        break;
                    case 2:
                        Tools.Broadcast(string.Format("Number of 'noobs' killed to date: {0}", KillCount));
                        break;
                    case 3:
                        Tools.Broadcast(string.Format("Duke Nukem would be proud. {0} goblins killed.", KillCount));
                        break;
                    case 4:
                        Tools.Broadcast(string.Format("You call that a lot? {0} goblins killed!", KillCount));
                        break;
                    case 5:
                        Tools.Broadcast(string.Format("{0} copies of Call of Duty smashed.", KillCount));
                        break;
                }
            }
        }

        public static bool CheckSpawn(int x, int y)
        {
            Vector2 tile = new Vector2(x, y);
            Vector2 spawn = new Vector2(Main.spawnTileX, Main.spawnTileY);
            return Vector2.Distance(spawn, tile) <= Config.SpawnProtectionRadius;
        }

        public static bool HackedHealth(TSPlayer player)
        {
            return (player.TPlayer.statManaMax > 400) ||
                   (player.TPlayer.statMana > 400) ||
                   (player.TPlayer.statLifeMax > 400) ||
                   (player.TPlayer.statLife > 400);
        }

        public void OnConfigRead(ConfigFile file)
        {
            NPC.defaultMaxSpawns = file.DefaultMaximumSpawns;
            NPC.defaultSpawnRate = file.DefaultSpawnRate;

            Main.autoSave = file.AutoSave;
            if (Backups != null)
            {
                Backups.KeepFor = file.BackupKeepFor;
                Backups.Interval = file.BackupInterval;
            }

            if (Restart != null)
            {
                Restart.Interval = file.AutoRestart;
            }
            
            if (!OverridePort)
            {
                Netplay.serverPort = file.ServerPort;
            }

            Netplay.spamCheck = file.SpamChecks;

            RconHandler.Password = file.RconPassword;
            RconHandler.ListenPort = file.RconPort;

            Tools.HashAlgo = file.HashAlgorithm;
        }
    }
}