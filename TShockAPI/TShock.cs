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
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using Hooks;
using MaxMind;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using Rests;
using Terraria;
using TShockAPI.DB;
using TShockAPI.Net;
using System.Text;

namespace TShockAPI
{
	[APIVersion(1, 11)]
	public class TShock : TerrariaPlugin
	{
		public static readonly Version VersionNum = Assembly.GetExecutingAssembly().GetName().Version;
		public static readonly string VersionCodename = "1.1.2 was sudden";

		public static string SavePath = "tshock";

        public static Process proc = Process.GetCurrentProcess();
        public static TSPlayer[] Players = new TSPlayer[Main.maxPlayers];
		public static BanManager Bans;
		public static WarpManager Warps;
		public static RegionManager Regions;
		public static BackupManager Backups;
		public static GroupManager Groups;
		public static UserManager Users;
		public static ItemManager Itembans;
		public static RemeberedPosManager RememberedPos;
		public static InventoryManager Inventory;
        public static HomeManager HomeManager;
        public static ArmorShopManager ArmorShopManager;
        public static WeaponShopManager WeaponShopManager;
        public static ItemShopManager ItemShopManager;
        public static BlockShopManager BlockShopManager;
        public static OtherShopManager OtherShopManager;
        public static ChatManager Chat;
        public static TownManager Towns;
        public static int disptime = 1000 * 60 * 15;
        public static List<string> DispenserTime = new List<string>();
        public static List<string> InventoryAllow = new List<string>();
        public static List<string> MutedPlayers = new List<string>();
        public static List<string> isGhost = new List<string>();
        public static DateTime Spawner = new DateTime();
        public static DateTime StackCheatChecker = new DateTime();
        public static DateTime InventoryCheckTime = new DateTime();
        public static RestartManager Restart;
		public static ConfigFile Config { get; set; }
		public static IDbConnection DB;
		public static bool OverridePort;
		public static PacketBufferer PacketBuffer;
		public static GeoIPCountry Geo;
		public static SecureRest RestApi;
		public static RestManager RestManager;
		public static Utils Utils = new Utils();
		public static StatTracker StatTracker = new StatTracker();
        public static string profiles;
        public static string temp;

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
            get { return "The Nyx Team. Moded by RogerPaladin"; }
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


		[SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
		public override void Initialize()
		{
			HandleCommandLine(Environment.GetCommandLineArgs());

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
					Log.ConsoleInfo(
						"TShock was improperly shut down. Please avoid this in the future, world corruption may result from this.");
					File.Delete(Path.Combine(SavePath, "tshock.pid"));
				}
				File.WriteAllText(Path.Combine(SavePath, "tshock.pid"), Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture));

				ConfigFile.ConfigRead += OnConfigRead;
				FileTools.SetupConfig();

				HandleCommandLine_Port(Environment.GetCommandLineArgs());

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
							String.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4};",
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
				Inventory = new InventoryManager(DB);
                HomeManager = new HomeManager(DB);
                ArmorShopManager = new ArmorShopManager(DB);
                WeaponShopManager = new WeaponShopManager(DB);
                ItemShopManager = new ItemShopManager(DB);
                BlockShopManager = new BlockShopManager(DB);
                OtherShopManager = new OtherShopManager(DB);
                Towns = new TownManager(DB);
                Chat = new ChatManager(DB);
                Restart = new RestartManager();
				RestApi = new SecureRest(Netplay.serverListenIP, 8080);
				RestApi.Verify += RestApi_Verify;
				RestApi.Port = Config.RestApiPort;
				RestManager = new RestManager(RestApi);
				RestManager.RegisterRestfulCommands();

				var geoippath = Path.Combine(SavePath, "GeoIP.dat");
				if (Config.EnableGeoIP && File.Exists(geoippath))
					Geo = new GeoIPCountry(geoippath);

                    profiles = @"Z:\profiles\";
                    temp = @"Z:\profiles\temp\";

				Console.Title = string.Format("TerrariaShock Version {0} ({1})", Version, VersionCodename);
				Log.ConsoleInfo(string.Format("TerrariaShock Version {0} ({1}) now running.", Version, VersionCodename));

				GameHooks.PostInitialize += OnPostInit;
				GameHooks.Update += OnUpdate;
				ServerHooks.Connect += OnConnect;
				ServerHooks.Join += OnJoin;
				ServerHooks.Leave += OnLeave;
				ServerHooks.Chat += OnChat;
				ServerHooks.Command += ServerHooks_OnCommand;
				NetHooks.GetData += OnGetData;
				NetHooks.SendData += NetHooks_SendData;
				NetHooks.GreetPlayer += OnGreetPlayer;
				NpcHooks.StrikeNpc += NpcHooks_OnStrikeNpc;
			    NpcHooks.SetDefaultsInt += OnNpcSetDefaults;
				ProjectileHooks.SetDefaults += OnProjectileSetDefaults;
				WorldHooks.StartHardMode += OnStartHardMode;
                WorldHooks.SaveWorld += OnSaveWorld;

				GetDataHandlers.InitGetDataHandler();
				Commands.InitCommands();

				if (Config.BufferPackets)
					PacketBuffer = new PacketBufferer();
               
                Users.DeletePlayersAfterMinutes(TShock.Config.DeleteUserAfterMinutes);

				Log.ConsoleInfo("AutoSave " + (Config.AutoSave ? "Enabled" : "Disabled"));
				Log.ConsoleInfo("Backups " + (Backups.Interval > 0 ? "Enabled" : "Disabled"));

				if (Initialized != null)
					Initialized();
			}
			catch (Exception ex)
			{
				Log.Error("Fatal Startup Exception");
				Log.Error(ex.ToString());
                TShock.Backups.Backup();
                Environment.Exit(1);
			}
		}

		private RestObject RestApi_Verify(string username, string password)
		{
			var userAccount = Users.GetUserByName(username);
			if (userAccount == null)
			{
				return new RestObject("401")
						{Error = "Invalid username/password combination provided. Please re-submit your query with a correct pair."};
			}

			if (Utils.HashPassword(password).ToUpper() != userAccount.Password.ToUpper())
			{
				return new RestObject("401")
						{Error = "Invalid username/password combination provided. Please re-submit your query with a correct pair."};
			}

			if (!Utils.GetGroup(userAccount.Group).HasPermission("api") && userAccount.Group != "superadmin")
			{
				return new RestObject("403")
						{
							Error =
								"Although your account was successfully found and identified, your account lacks the permission required to use the API. (api)"
						};
			}

			return new RestObject("200") {Response = "Successful login"}; //Maybe return some user info too?
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (Geo != null)
				{
					Geo.Dispose();
				}
				GameHooks.PostInitialize -= OnPostInit;
				GameHooks.Update -= OnUpdate;
                ServerHooks.Connect -= OnConnect;
				ServerHooks.Join -= OnJoin;
				ServerHooks.Leave -= OnLeave;
				ServerHooks.Chat -= OnChat;
				ServerHooks.Command -= ServerHooks_OnCommand;
				NetHooks.GetData -= OnGetData;
				NetHooks.SendData -= NetHooks_SendData;
				NetHooks.GreetPlayer -= OnGreetPlayer;
				NpcHooks.StrikeNpc -= NpcHooks_OnStrikeNpc;
                NpcHooks.SetDefaultsInt -= OnNpcSetDefaults;
				ProjectileHooks.SetDefaults -= OnProjectileSetDefaults;
                WorldHooks.StartHardMode -= OnStartHardMode;
                WorldHooks.SaveWorld -= OnSaveWorld;
				if (File.Exists(Path.Combine(SavePath, "tshock.pid")))
				{
					File.Delete(Path.Combine(SavePath, "tshock.pid"));
				}
				RestApi.Dispose();
				Log.Dispose();
			}

			base.Dispose(disposing);
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
					Main.worldPathName += ".crash";
					WorldGen.saveWorld();
				}
			}
		}

		private void HandleCommandLine(string[] parms)
		{
			for (int i = 0; i < parms.Length; i++)
			{
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
				if (parms[i].ToLower() == "-dump")
				{
					ConfigFile.DumpDescriptions();
					Permissions.DumpDescriptions();
				}
			}
		}

		private void HandleCommandLine_Port(string[] parms)
		{
			for (int i = 0; i < parms.Length; i++)
			{
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
				var r = new Random((int) DateTime.UtcNow.ToBinary());
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
            Towns.ReloadAllTowns();
			if (Config.RestApiEnabled)
				RestApi.Start();

			//StatTracker.CheckIn();

			FixChestStacks();
		}

		private void FixChestStacks()
		{
			foreach (Chest chest in Main.chest)
			{
				if (chest != null)
				{
					foreach (Item item in chest.item)
					{
						if (item != null && item.stack > item.maxStack)
							item.stack = item.maxStack;
					}
				}
			}
		}

		private DateTime LastCheck = DateTime.UtcNow;
		private DateTime LastSave = DateTime.UtcNow;

		private void OnUpdate()
		{
			UpdateManager.UpdateProcedureCheck();
			//StatTracker.CheckIn();
			if (Backups.IsBackupTime)
				Backups.Backup();
            if (proc.PagedMemorySize64 > 1073741824)
                Restart.Restart();

            #region Thx2Twitchy
            foreach (TSPlayer player in Players)
            {
                if (player != null && player.Active)
                {
                    if (player.LastTilePos != new Vector2(player.TileX, player.TileY))
                    {
                        bool InRegion = false;
                        string RegionName;
                        bool InTown = false;
                        string TownName;
                        string Mayor;

                        if (TShock.Towns.InArea(player.TileX, player.TileY, out TownName))
                        {
                            if (player.CurrentTown != TownName)
                            {
                                player.CurrentTown = TownName;
                                player.InTown = true;
                                player.SendMessage("Entering " + player.CurrentTown + " town. Current mayor is <" + TShock.Towns.GetMayor(TownName) + ">", Color.MediumSlateBlue);
                            }
                            InTown = true;
                        }
                            if (!InTown && player.InTown)
                            {
                                player.SendMessage("Leaving " + player.CurrentTown + " town.", Color.MediumSlateBlue);
                                player.CurrentTown = "";
                                player.InTown = false;
                            }

                        if (TShock.Regions.InArea(player.TileX, player.TileY, out RegionName))
                        {
                            if (player.CurrentRegion != RegionName)
                            {
                                player.CurrentRegion = RegionName;
                                player.InRegion = true;
                                player.SendMessage("Entering " + player.CurrentRegion + " region.", Color.Magenta);
                            }
                            InRegion = true;
                        }
                        if (!InRegion && player.InRegion)
                        {
                            player.SendMessage("Leaving " + player.CurrentRegion + " region.", Color.Magenta);
                            player.CurrentRegion = "";
                            player.InRegion = false;
                        }
                        player.LastTilePos = new Vector2(player.TileX, player.TileY);
                        player.LastChangePos = DateTime.UtcNow;
                    }
                }
            }
            #endregion

            //call these every second, not every update
			if ((DateTime.UtcNow - LastCheck).TotalSeconds >= 1)
			{
				OnSecondUpdate();
				LastCheck = DateTime.UtcNow;
			}

            if (Restart.PrepareToRestart && !Restart.Prepared)
            {
                Console.WriteLine("The server will be restarted in 5 minutes");
                TShock.Utils.Broadcast("The server will be restarted in 5 minutes");
                Log.Info("The server will be restarted in 5 minutes");
                Restart.Prepared = true;
            }

            if (Restart.IsRestartTime)
                Restart.Restart();

			if ((DateTime.UtcNow - LastSave).TotalMinutes >= 15)
			{
				foreach (TSPlayer player in Players)
				{
					// prevent null point exceptions
					if (player != null && player.IsLoggedIn && Config.StoreInventory)
					{
                        Inventory.UpdateInventory(player);
                        TShock.Users.PlayingTime(player.Name, Convert.ToInt32((DateTime.UtcNow - player.LoginTime).TotalMinutes));
                        TShock.Users.SetRCoins(player.Name, Math.Round(0.1 * (DateTime.UtcNow - player.LoginTime).TotalMinutes, 2));
                        player.LoginTime = DateTime.UtcNow;
					}
				}
				LastSave = DateTime.UtcNow;
			}
		}

		private void OnSecondUpdate()
		{
            string item;
            int itemcount;

            if (Config.ForceTime != "normal")
			{
				switch (Config.ForceTime)
				{
					case "day":
						TSPlayer.Server.SetTime(true, 27000.0);
						break;
					case "night":
						TSPlayer.Server.SetTime(false, 16200.0);
						break;
				}
			}
			int count = 0;
			foreach (TSPlayer player in Players)
			{
				if (player != null && player.Active)
				{
					count++;
					if (player.TilesDestroyed != null)
					{
						if (player.TileKillThreshold >= Config.TileKillThreshold)
						{
							player.Disable("Reached TileKill threshold");
							TSPlayer.Server.RevertTiles(player.TilesDestroyed);
							player.TilesDestroyed.Clear();
						}
					}
					if (player.TileKillThreshold > 0)
					{
						player.TileKillThreshold = 0;
					}
					if (player.TilesCreated != null)
					{
						if (player.TilePlaceThreshold >= Config.TilePlaceThreshold)
						{
							player.Disable("Reached TilePlace threshold");
							TSPlayer.Server.RevertTiles(player.TilesCreated);
							player.TilesCreated.Clear();
						}
					}
					if (player.TilePlaceThreshold > 0)
					{
						player.TilePlaceThreshold = 0;
					}
					if (player.TileLiquidThreshold >= Config.TileLiquidThreshold)
					{
						player.Disable("Reached TileLiquid threshold");
					}
					if (player.TileLiquidThreshold > 0)
					{
						player.TileLiquidThreshold = 0;
					}
					if (player.ProjectileThreshold >= Config.ProjectileThreshold)
					{
						player.Disable("Reached Projectile threshold");
					}
					if (player.ProjectileThreshold > 0)
					{
						player.ProjectileThreshold = 0;
					}
					if (player.Dead && (DateTime.UtcNow - player.LastDeath).Seconds >= 3 && player.Difficulty != 2)
					{
						player.Spawn();
					}
					/*string check = "none";
					foreach (Item item in player.TPlayer.inventory)
					{
						if (!player.Group.HasPermission(Permissions.ignorestackhackdetection) && item.stack > item.maxStack &&
							item.type != 0)
						{
							check = "Remove Item " + item.name + " (" + item.stack + ") exceeds max stack of " + item.maxStack;
						}
					}
					player.IgnoreActionsForCheating = check;
					check = "none";
					foreach (Item item in player.TPlayer.armor)
					{
						if (!player.Group.HasPermission(Permissions.usebanneditem) && Itembans.ItemIsBanned(item.name, player))
						{
							player.SetBuff(30, 120); //Bleeding
							player.SetBuff(36, 120); //Broken Armor
							check = "Remove Armor/Accessory " + item.name;
						}
					}
					player.IgnoreActionsForDisabledArmor = check;
					if (CheckIgnores(player))
					{
						player.SetBuff(33, 120); //Weak
						player.SetBuff(32, 120); //Slow
						player.SetBuff(23, 120); //Cursed
					}
					else if (!player.Group.HasPermission(Permissions.usebanneditem) &&
							 Itembans.ItemIsBanned(player.TPlayer.inventory[player.TPlayer.selectedItem].name, player))
					{
						player.SetBuff(23, 120); //Cursed
					}*/

                    if ((DateTime.UtcNow - StackCheatChecker).TotalMilliseconds > 5000)
                    {
                        StackCheatChecker = DateTime.UtcNow;
                        if (player.StackCheat(out item, out itemcount))
                        {
                            TShock.Utils.Broadcast(string.Format("{0} cheater!!! {1} x {2}", player.Name, item, itemcount), Color.Yellow);
                            //TShock.Utils.Ban(player, "Stack Cheat.", "Server", Convert.ToString(DateTime.UtcNow));
                            TShock.Utils.ForceKick(player, string.Format("Stack Cheat. {0} x {1}", item, itemcount));
                        }
                    }
                    
                    if ((DateTime.UtcNow - player.LastChangePos).TotalMinutes > 10)
                    {
                        player.SavePlayer();
                        TShock.Utils.ForceKick(player, "Kick for omission more than 10 minutes");
                        Log.Info(player.Name + "was kicked for omission more than 10 minutes");
                    }

                    if (!player.Group.HasPermission(Permissions.usebanneditem))
                    {
                        var inv = player.TPlayer.inventory;
                        var user = TShock.Users.GetUserByName(player.Name);
                        for (int i = 0; i < inv.Length; i++)
                        {
                            if (inv[i] != null && Itembans.ItemIsBanned(inv[i].name))
                            {
                                if (user != null)
                                    player.SavePlayer(true);
                                player.Disconnect("Using banned item: " + inv[i].name + ", reload profile.");
                                Log.Info(player.Name + " was kicked for using banned item " + inv[i].name);
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
                            TShock.Utils.Broadcast(player.Name + " Not logged in.", Color.Yellow);
                            TShock.Utils.Kick(player, "Not logged in.");
                        }

                    }
				}
			}
			//Console.Title = string.Format("TerrariaShock Version {0} ({1}) ({2}/{3})", Version, VersionCodename, count,
										  //Config.MaxSlots);
            Console.Title = string.Format("{0} ({1}/{2})", Config.ServerName, count, Config.MaxSlots);
		}

		private void OnConnect(int ply, HandledEventArgs handler)
		{
			var player = new TSPlayer(ply);
			/*if (Config.EnableDNSHostResolution)
			{
				player.Group = Users.GetGroupForIPExpensive(player.IP);
			}
			else
			{
				player.Group = Users.GetGroupForIP(player.IP);
			}*/

			if (Utils.ActivePlayers() + 1 > Config.MaxSlots + 20)
			{
				Utils.ForceKick(player, Config.ServerFullNoReservedReason);
				handler.Handled = true;
				return;
			}

			var ipban = Bans.GetBanByIp(player.IP);
			Ban ban = null;
			if (ipban != null && Config.EnableIPBans)
				ban = ipban;

			if (ban != null)
			{
				Utils.ForceKick(player, string.Format("You are banned: {0}", ban.Reason));
				handler.Handled = true;
				return;
			}

			if (!FileTools.OnWhitelist(player.IP))
			{
				Utils.ForceKick(player, "Not on whitelist.");
				handler.Handled = true;
				return;
			}

			if (Geo != null)
			{
				var code = Geo.TryGetCountryCode(IPAddress.Parse(player.IP));
				player.Country = code == null ? "N/A" : GeoIPCountry.GetCountryNameByCode(code);
				if (code == "A1")
				{
					if (Config.KickProxyUsers)
					{
						Utils.ForceKick(player, "Proxies are not allowed");
						handler.Handled = true;
						return;
					}
				}
			}
			Players[ply] = player;
            player.LoginTime = DateTime.UtcNow;
		}

		private void OnJoin(int ply, HandledEventArgs handler)
		{
			var player = Players[ply];
			if (player == null)
			{
				handler.Handled = true;
				return;
			}

            player.Group = TShock.Utils.GetGroup("default");
			var nameban = Bans.GetBanByName(player.Name);
			Ban ban = null;
			if (nameban != null && Config.EnableBanOnUsernames)
				ban = nameban;

			if (ban != null)
			{
				Utils.ForceKick(player, string.Format("You are banned: {0}", ban.Reason));
				handler.Handled = true;
				return;
			}
		}

		private void OnLeave(int ply)
		{

			var tsplr = Players[ply];
			Players[ply] = null;

			if (tsplr != null && tsplr.ReceivedInfo)
			{
				if (!tsplr.SilentKickInProgress)
				{
					Utils.Broadcast(tsplr.Name + " left", Color.Yellow);
				}
				Log.Info(string.Format("{0} left.", tsplr.Name));

                if (tsplr.IsLoggedIn)
                {
                    TShock.Users.PlayingTime(tsplr.Name, Convert.ToInt32((DateTime.UtcNow - tsplr.LoginTime).TotalMinutes));
                    TShock.Users.SetRCoins(tsplr.Name, Math.Round(0.1 * (DateTime.UtcNow - tsplr.LoginTime).TotalMinutes, 2));
                    if (Config.StoreInventory)
                        Inventory.UpdateInventory(tsplr);
                    tsplr.SavePlayer();
                }

				if (Config.RememberLeavePos)
				{
					RememberedPos.InsertLeavePos(tsplr.Name, tsplr.IP, (int) (tsplr.X/16), (int) (tsplr.Y/16));
				}
			}
		}

		private void OnChat(messageBuffer msg, int ply, string text, HandledEventArgs e)
		{
            Rectangle rect;
            rect = new Rectangle();

            if (e.Handled)
				return;

			var tsplr = Players[msg.whoAmI];
			if (tsplr == null)
			{
				e.Handled = true;
				return;
			}
            /*Encoding win1251 = Encoding.GetEncoding("Windows-1251");
            char[] cArray = System.Text.Encoding.UTF8.GetChars(msg.readBuffer, 8, msg.messageLength);
            foreach (char c in cArray)
            {
                Console.WriteLine(Transliteration.Front(c.ToString()));
            }*/
			/*if (!Utils.ValidString(text))
			{
				e.Handled = true;
				return;
			}*/

			if (text.StartsWith("/"))
			{
                foreach (TSPlayer Player in TShock.Players)
                {
                    if (Player != null && Player.Active && Player.Group.HasPermission(Permissions.adminchat))
                        Player.SendMessage(string.Format("*<{0}> /{1}", tsplr.Name, text.Remove(0, 1)), Color.Red);
                }
                Console.WriteLine(string.Format("*<{0}> /{1}", tsplr.Name, text.Remove(0, 1)));
                Log.Info(string.Format("*<{0}> /{1}", tsplr.Name, text.Remove(0, 1)));
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
            else if (!tsplr.mute)
            {
                Chat.AddMessage(tsplr.Name, "", text);
                rect = new Rectangle((tsplr.TileX - 50), (tsplr.TileY - 50), 100, 100);
                foreach (TSPlayer Player in TShock.Players)
                {
                    if (Player != null && Player.Active)
                    {
                        if (Player.DisplayChat && Player.Group.HasPermission(Permissions.adminstatus))
                        {
                            Player.SendMessage("(Ranged){2}<{0}> {1}".SFormat(tsplr.Name, text, Config.ChatDisplayGroup ? "[{0}] ".SFormat(tsplr.Group.Name) : ""),
                                                                      tsplr.Group.R, tsplr.Group.G,
                                                                      tsplr.Group.B);
                        }
                        else
                            if (Player.TileX >= rect.Left && Player.TileX <= rect.Right &&
                                Player.TileY >= rect.Top && Player.TileY <= rect.Bottom)
                            {
                                Player.SendMessage("{2}<{0}> {1}".SFormat(tsplr.Name, text, Config.ChatDisplayGroup ? "[{0}] ".SFormat(tsplr.Group.Name) : ""),
                                tsplr.Group.R, tsplr.Group.G,
                                tsplr.Group.B);
                            }
                    }
                }
                Console.WriteLine(string.Format("{0} said: {1}", tsplr.Name, text));
                Log.Info(string.Format("{0} said: {1}", tsplr.Name, text));
            }
            else if (tsplr.mute)
            {
                tsplr.SendMessage("You are muted!");
                e.Handled = true;
            }
            e.Handled = true;
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

			if (text.StartsWith("playing") || text.StartsWith("/playing"))
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
			}
			else if (text == "autosave")
			{
				Main.autoSave = Config.AutoSave = !Config.AutoSave;
				Log.ConsoleInfo("AutoSave " + (Config.AutoSave ? "Enabled" : "Disabled"));
			}
			else if (text.StartsWith("/"))
			{
				Commands.HandleCommand(TSPlayer.Server, text);
			}
			else
			{
				Commands.HandleCommand(TSPlayer.Server, "/" + text);
			}
			e.Handled = true;
		}

		private void OnGetData(GetDataEventArgs e)
		{
			if (e.Handled)
				return;

			PacketTypes type = e.MsgID;

			Debug.WriteLine("Recv: {0:X}: {2} ({1:XX})", e.Msg.whoAmI, (byte) type, type);

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

			if (player.RequiresPassword && type != PacketTypes.PasswordSend)
			{
				e.Handled = true;
				return;
			}

			if ((player.State < 10 || player.Dead) && (int) type > 12 && (int) type != 16 && (int) type != 42 && (int) type != 50 &&
				(int) type != 38 && (int) type != 5 && (int) type != 21)
			{
				e.Handled = true;
				return;
			}

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

		private void OnGreetPlayer(int who, HandledEventArgs e)
		{
			var player = Players[who];
			if (player == null)
			{
				e.Handled = true;
				return;
			}

			Utils.ShowFileToUser(player, "motd.txt");

			if (Config.PvPMode == "always" && !player.TPlayer.hostile)
			{
				player.SendMessage("PvP is forced! Enable PvP else you can't move or do anything!", Color.Red);
			}

			if (!player.IsLoggedIn)
			{
				if (Config.ServerSideInventory)
				{
					player.SendMessage(
						player.IgnoreActionsForInventory = "Server Side Inventory is enabled! Please /register or /login to play!",
						Color.Red);
				}
				else if (Config.RequireLogin)
				{
					player.SendMessage("Please /register or /login to play!", Color.Red);
				}
			}

			if (player.Group.HasPermission(Permissions.causeevents) && Config.InfiniteInvasion)
			{
				StartInvasion();
			}

            if (!DispenserTime.Contains(player.Name))
            {
                DispenserTime.Add(player.Name + ";" + Convert.ToString(DateTime.UtcNow.AddMilliseconds(-disptime)));
            }

            if (MutedPlayers.Contains(player.Name))
            {
                player.mute = true;
            }

			player.LastNetPosition = new Vector2(Main.spawnTileX*16f, Main.spawnTileY*16f);

            if (Config.OnlyNewCharacter)
            {
                if (Inventory.NewPlayer(player))
                {
                    if (!Inventory.UserExist(player))
                        Inventory.NewInventory(player);
                }
                else
                {
                    if (!Inventory.UserExist(player) || !File.Exists(profiles + player.Name.ToLower() + ".plr"))
                    {
                        TShock.Utils.Kick(player, "New players only!");
                        return;
                    }
                }

            }
            if (!player.CheckPlayerNew())
            {
                TShock.Utils.Kick(player, "Your profile was modified! Login to launcher!");
            }
            else
            {

                var user = TShock.Users.GetUserByName(player.Name);
                if (user != null && player.IP.Equals(user.Address))
                {
                    player.Group = TShock.Utils.GetGroup(user.Group);
                    player.UserAccountName = player.Name;
                    player.UserID = TShock.Users.GetUserID(player.UserAccountName);
                    player.IsLoggedIn = true;
                    player.SendMessage("Authenticated successfully.", Color.LimeGreen);
                    player.SendMessage(string.Format("Hello {0}. Your last login is {1}.", player.Name, Convert.ToDateTime(user.LastLogin)));
                    TShock.Users.Login(player);
                    Log.ConsoleInfo(player.Name + " authenticated successfully.");
                }
            }
            
            if (Config.RememberHome)
            {
                if (HomeManager.GetHome(player.Name) != Vector2.Zero)
                {
                    var pos = HomeManager.GetHome(player.Name);
                    player.Teleport((int)pos.X, (int)pos.Y);
                    player.SendTileSquare((int)pos.X, (int)pos.Y);
                }
            }

			if (Config.RememberLeavePos)
			{
				var pos = RememberedPos.GetLeavePos(player.Name, player.IP);
				player.LastNetPosition = pos;
				player.Teleport((int) pos.X, (int) pos.Y + 3);
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

		private void OnProjectileSetDefaults(SetDefaultsEventArgs<Projectile, int> e)
		{
			if (e.Info == 43)
				if (Config.DisableTombstones)
					e.Object.SetDefaults(0);
			if (e.Info == 75)
				if (Config.DisableClownBombs)
					e.Object.SetDefaults(0);
			if (e.Info == 109)
				if (Config.DisableSnowBalls)
					e.Object.SetDefaults(0);
		}

		private void OnNpcSetDefaults(SetDefaultsEventArgs<NPC, int> e)
		{
			if (Itembans.ItemIsBanned(e.Object.name, null))
			{
				e.Object.SetDefaults(0);
			}
            if (e.Object.name.Contains("Giant Worm") || e.Object.name.Contains("Digger") || e.Object.name.Equals("Voodoo Demon"))
            {
                 e.Object.SetDefaults(0);
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

			return SendBytesBufferless(client, bytes);
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

		private void NetHooks_SendData(SendDataEventArgs e)
		{
            if (e.MsgID == PacketTypes.Disconnect)
			{
				Action<ServerSock, string> senddisconnect = (sock, str) =>
																{
																	if (sock == null || !sock.active)
																		return;
																	sock.kill = true;
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
            if (e.MsgID == PacketTypes.PlayerSpawn)
            {
                if (TShock.isGhost.Contains(Players[e.number].Name))
                    e.Handled = true;
            }

		}

		private void OnStartHardMode(HandledEventArgs e)
		{
			if (Config.DisableHardmode)
				e.Handled = true;
		}

        void OnSaveWorld(bool resettime, HandledEventArgs e)
        {
            if (!Utils.saving)
            {
                Utils.Broadcast("Saving world. Momentary lag might result from this.", Color.Red);
                var SaveWorld = new Thread(Utils.SaveWorld);
                SaveWorld.Start();
            }
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
				Main.invasionSize = 100 + (Config.InvasionMultiplier*Utils.ActivePlayers());
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
			if (KillCount%100 == 0)
			{
				switch (random)
				{
					case 0:
						Utils.Broadcast(string.Format("You call that a lot? {0} goblins killed!", KillCount));
						break;
					case 1:
						Utils.Broadcast(string.Format("Fatality! {0} goblins killed!", KillCount));
						break;
					case 2:
						Utils.Broadcast(string.Format("Number of 'noobs' killed to date: {0}", KillCount));
						break;
					case 3:
						Utils.Broadcast(string.Format("Duke Nukem would be proud. {0} goblins killed.", KillCount));
						break;
					case 4:
						Utils.Broadcast(string.Format("You call that a lot? {0} goblins killed!", KillCount));
						break;
					case 5:
						Utils.Broadcast(string.Format("{0} copies of Call of Duty smashed.", KillCount));
						break;
				}
			}
		}

		public static bool CheckProjectilePermission(TSPlayer player, int index, int type)
		{
			if (type == 43)
			{
				return true;
			}

			if (type == 17 && !player.Group.HasPermission(Permissions.usebanneditem) && Itembans.ItemIsBanned("Dirt Rod", player))
				//Dirt Rod Projectile
			{
				return true;
			}

			if ((type == 42 || type == 65 || type == 68) && !player.Group.HasPermission(Permissions.usebanneditem) &&
				Itembans.ItemIsBanned("Sandgun", player)) //Sandgun Projectiles
			{
				return true;
			}

			Projectile proj = new Projectile();
			proj.SetDefaults(type);

			if (!player.Group.HasPermission(Permissions.usebanneditem) && Itembans.ItemIsBanned(proj.name, player))
			{
				return true;
			}

			if (Main.projHostile[type])
			{
                //player.SendMessage( proj.name, Color.Yellow);
				return true;
			}

			return false;
		}

		public static bool CheckRangePermission(TSPlayer player, int x, int y, int range = 32)
		{
			if (Config.RangeChecks && ((Math.Abs(player.TileX - x) > range) || (Math.Abs(player.TileY - y) > range)))
			{
				return true;
			}
			return false;
		}

		public static bool CheckTilePermission(TSPlayer player, int tileX, int tileY)
		{
            string CoOwner = string.Empty;
            string RegionName = string.Empty;
            string Mayor = string.Empty;
            string TownName = string.Empty;

            if (!player.Group.HasPermission(Permissions.canbuild))
			{
                if ((DateTime.UtcNow - player.LastTileChangeNotify).TotalMilliseconds > 1000)
                {
                    player.SendMessage("You do not have permission to build!", Color.Red);
                    player.LastTileChangeNotify = DateTime.UtcNow;
                }
				return true;
			}
			if (Config.DisableBuild)
			{
				if (!player.Group.HasPermission(Permissions.editspawn))
				{
                    if ((DateTime.UtcNow - player.LastTileChangeNotify).TotalMilliseconds > 1000)
                    {
                        player.SendMessage("World protected from changes.", Color.Red);
                        player.LastTileChangeNotify = DateTime.UtcNow;
                    }
                    return true;
				}
			}
			if (Config.SpawnProtection)
			{
				if (!player.Group.HasPermission(Permissions.editspawn))
				{
					var flag = CheckSpawn(tileX, tileY);
					if (flag)
					{
				        if ((DateTime.UtcNow - player.LastTileChangeNotify).TotalMilliseconds > 1000)
                        {
                            player.SendMessage("Spawn protected from changes.", Color.Red);
                            player.LastTileChangeNotify = DateTime.UtcNow;
                        }
                        return true;
					}
				}
			}

            if (!player.Group.HasPermission(Permissions.editspawn) && !Regions.CanBuild(tileX, tileY, player, out CoOwner) && Regions.InArea(tileX, tileY, out RegionName))
            {
                if (Towns.CanBuild(tileX, tileY, player, out Mayor) && Towns.InArea(tileX, tileY, out TownName))
                {
                    return false;
                }

                if ((DateTime.UtcNow - player.LastTileChangeNotify).TotalMilliseconds > 1000)
                {
                    player.SendMessage("This region <" + RegionName + "> is protected by " + CoOwner, Color.Yellow);
                    player.LastTileChangeNotify = DateTime.UtcNow;
                }
                return true;
            }

            if (Regions.CanBuild(tileX, tileY, player, out CoOwner) && Regions.InArea(tileX, tileY, out RegionName))
            {
                return false;
            }

            if (!player.Group.HasPermission(Permissions.editspawn) && !Towns.CanBuild(tileX, tileY, player, out Mayor) && Towns.InArea(tileX, tileY, out TownName))
            {
                if ((DateTime.UtcNow - player.LastTileChangeNotify).TotalMilliseconds > 1000)
                {
                    player.SendMessage("This town <" + TownName + "> is protected by <" + Mayor + ">", Color.Yellow);
                    player.LastTileChangeNotify = DateTime.UtcNow;
                }
                return true;
            }

			return false;
		}
		public static bool CheckSpawn(int x, int y)
		{
			Vector2 tile = new Vector2(x, y);
			Vector2 spawn = new Vector2(Main.spawnTileX, Main.spawnTileY);
			return Distance(spawn, tile) <= Config.SpawnProtectionRadius;
		}

		public static float Distance(Vector2 value1, Vector2 value2)
		{
			float num2 = value1.X - value2.X;
			float num = value1.Y - value2.Y;
			float num3 = (num2*num2) + (num*num);
			return (float) Math.Sqrt(num3);
		}

		public static bool HackedHealth(TSPlayer player)
		{
			return (player.TPlayer.statManaMax > 400) ||
				   (player.TPlayer.statMana > 400) ||
				   (player.TPlayer.statLifeMax > 400) ||
				   (player.TPlayer.statLife > 400);
		}

		public static bool HackedInventory(TSPlayer player)
		{
			bool check = false;

			Item[] inventory = player.TPlayer.inventory;
			Item[] armor = player.TPlayer.armor;
			for (int i = 0; i < NetItem.maxNetInventory; i++)
			{
				if (i < 49)
				{
					Item item = new Item();
					if (inventory[i] != null && inventory[i].netID != 0)
					{
						item.netDefaults(inventory[i].netID);
						item.Prefix(inventory[i].prefix);
						item.AffixName();
						if (inventory[i].stack > item.maxStack)
						{
							check = true;
							player.SendMessage(
								String.Format("Stack cheat detected. Remove item {0} ({1}) and then rejoin", item.name, inventory[i].stack),
								Color.Cyan);
						}
					}
				}
				else
				{
					Item item = new Item();
					if (armor[i - 48] != null && armor[i - 48].netID != 0)
					{
						item.netDefaults(armor[i - 48].netID);
						item.Prefix(armor[i - 48].prefix);
						item.AffixName();
						if (armor[i - 48].stack > item.maxStack)
						{
							check = true;
							player.SendMessage(
								String.Format("Stack cheat detected. Remove armor {0} ({1}) and then rejoin", item.name, armor[i - 48].stack),
								Color.Cyan);
						}
					}
				}
			}

			return check;
		}

		public static bool CheckInventory(TSPlayer player)
		{
			PlayerData playerData = player.PlayerData;
			bool check = true;

			if (player.TPlayer.statLifeMax > playerData.maxHealth)
			{
				player.SendMessage("Error: Your max health exceeded (" + playerData.maxHealth + ") which is stored on server",
								   Color.Cyan);
				check = false;
			}

			Item[] inventory = player.TPlayer.inventory;
			Item[] armor = player.TPlayer.armor;
			for (int i = 0; i < NetItem.maxNetInventory; i++)
			{
				if (i < 49)
				{
					Item item = new Item();
					Item serverItem = new Item();
					if (inventory[i] != null && inventory[i].netID != 0)
					{
						if (playerData.inventory[i].netID != inventory[i].netID)
						{
							item.netDefaults(inventory[i].netID);
							item.Prefix(inventory[i].prefix);
							item.AffixName();
							player.SendMessage(player.IgnoreActionsForInventory = "Your item (" + item.name + ") needs to be deleted.",
											   Color.Cyan);
							check = false;
						}
						else if (playerData.inventory[i].prefix != inventory[i].prefix)
						{
							item.netDefaults(inventory[i].netID);
							item.Prefix(inventory[i].prefix);
							item.AffixName();
							player.SendMessage(player.IgnoreActionsForInventory = "Your item (" + item.name + ") needs to be deleted.",
											   Color.Cyan);
							check = false;
						}
						else if (inventory[i].stack > playerData.inventory[i].stack)
						{
							item.netDefaults(inventory[i].netID);
							item.Prefix(inventory[i].prefix);
							item.AffixName();
							player.SendMessage(
								player.IgnoreActionsForInventory =
								"Your item (" + item.name + ") (" + inventory[i].stack + ") needs to have it's stack decreased to (" +
								playerData.inventory[i].stack + ").", Color.Cyan);
							check = false;
						}
					}
				}
				else
				{
					Item item = new Item();
					Item serverItem = new Item();
					if (armor[i - 48] != null && armor[i - 48].netID != 0)
					{
						if (playerData.inventory[i].netID != armor[i - 48].netID)
						{
							item.netDefaults(armor[i - 48].netID);
							item.Prefix(armor[i - 48].prefix);
							item.AffixName();
							player.SendMessage(player.IgnoreActionsForInventory = "Your armor (" + item.name + ") needs to be deleted.",
											   Color.Cyan);
							check = false;
						}
						else if (playerData.inventory[i].prefix != armor[i - 48].prefix)
						{
							item.netDefaults(armor[i - 48].netID);
							item.Prefix(armor[i - 48].prefix);
							item.AffixName();
							player.SendMessage(player.IgnoreActionsForInventory = "Your armor (" + item.name + ") needs to be deleted.",
											   Color.Cyan);
							check = false;
						}
						else if (armor[i - 48].stack > playerData.inventory[i].stack)
						{
							item.netDefaults(armor[i - 48].netID);
							item.Prefix(armor[i - 48].prefix);
							item.AffixName();
							player.SendMessage(
								player.IgnoreActionsForInventory =
								"Your armor (" + item.name + ") (" + inventory[i].stack + ") needs to have it's stack decreased to (" +
								playerData.inventory[i].stack + ").", Color.Cyan);
							check = false;
						}
					}
				}
			}

			return check;
		}

		public static bool CheckIgnores(TSPlayer player)
		{
			bool check = false;
			if (Config.PvPMode == "always" && !player.TPlayer.hostile)
				check = true;
			if (player.IgnoreActionsForInventory != "none")
				check = true;
			if (player.IgnoreActionsForCheating != "none")
				check = true;
			if (player.IgnoreActionsForDisabledArmor != "none")
				check = true;
			if (player.IgnoreActionsForClearingTrashCan)
				check = true;
			if (!player.IsLoggedIn && Config.RequireLogin)
				check = true;
			return check;
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

			if (file.MaxSlots > 235)
				file.MaxSlots = 235;
			Main.maxNetPlayers = file.MaxSlots + 20;
			Netplay.password = "";
			Netplay.spamCheck = false;

			RconHandler.Password = file.RconPassword;
			RconHandler.ListenPort = file.RconPort;

			Utils.HashAlgo = file.HashAlgorithm;
		}
	}
}