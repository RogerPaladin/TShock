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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Terraria;
using TShockAPI.DB;

namespace TShockAPI
{
	public delegate void CommandDelegate(CommandArgs args);

	public class CommandArgs : EventArgs
	{
		public string Message { get; private set; }
		public TSPlayer Player { get; private set; }

		/// <summary>
		/// Parameters passed to the arguement. Does not include the command name.
		/// IE '/kick "jerk face"' will only have 1 argument
		/// </summary>
		public List<string> Parameters { get; private set; }

		public Player TPlayer
		{
			get { return Player.TPlayer; }
		}

		public CommandArgs(string message, TSPlayer ply, List<string> args)
		{
			Message = message;
			Player = ply;
			Parameters = args;
		}
	}

	public class Command
	{
		public string Name
		{
			get { return Names[0]; }
		}

		public List<string> Names { get; protected set; }
		public bool DoLog { get; set; }
		public string Permission { get; protected set; }
		private CommandDelegate command;

		public Command(string permissionneeded, CommandDelegate cmd, params string[] names)
			: this(cmd, names)
		{
			Permission = permissionneeded;
		}

		public Command(CommandDelegate cmd, params string[] names)
		{
			if (names == null || names.Length < 1)
				throw new NotSupportedException();
			Permission = null;
			Names = new List<string>(names);
			command = cmd;
			DoLog = true;
		}

		public bool Run(string msg, TSPlayer ply, List<string> parms)
		{
            if (!ply.Group.HasPermission(Permission))
				return false;

			try
			{
				command(new CommandArgs(msg, ply, parms));
			}
			catch (Exception e)
			{
				ply.SendMessage("Command failed, check logs for more details.");
				Log.Error(e.ToString());
			}

			return true;
		}

		public bool HasAlias(string name)
		{
			return Names.Contains(name);
		}

		public bool CanRun(TSPlayer ply)
		{
			return ply.Group.HasPermission(Permission);
		}
	}

	public static class Commands
	{
		public static List<Command> ChatCommands = new List<Command>();

		private delegate void AddChatCommand(string permission, CommandDelegate command, params string[] names);

		public static void InitCommands()
		{
			//When adding new perm in here, add new perm to CommandList in DBEditor
			AddChatCommand add = (p, c, n) => ChatCommands.Add(new Command(p, c, n));
			add(Permissions.kick, Kick, "kick");
			add(Permissions.ban, Ban, "ban");
			add(Permissions.ban, BanIP, "banip");
			add(Permissions.ban, UnBan, "unban");
			add(Permissions.ban, UnBanIP, "unbanip");
			add(Permissions.maintenance, ClearBans, "clearbans");
			add(Permissions.whitelist, Whitelist, "whitelist");
			add(Permissions.maintenance, Off, "off", "exit");
			add(Permissions.maintenance, OffNoSave, "off-nosave", "exit-nosave");
			add(Permissions.maintenance, CheckUpdates, "checkupdates");
			add(Permissions.causeevents, DropMeteor, "dropmeteor");
			add(Permissions.causeevents, Star, "star");
			add(Permissions.causeevents, Fullmoon, "fullmoon");
			add(Permissions.causeevents, Bloodmoon, "bloodmoon");
			add(Permissions.causeevents, Invade, "invade");
			add(Permissions.spawnboss, Eater, "eater");
			add(Permissions.spawnboss, Eye, "eye");
			add(Permissions.spawnboss, King, "king");
			add(Permissions.spawnboss, Skeletron, "skeletron");
			add(Permissions.spawnboss, WoF, "wof", "wallofflesh");
			add(Permissions.spawnboss, Twins, "twins");
			add(Permissions.spawnboss, Destroyer, "destroyer");
			add(Permissions.spawnboss, SkeletronPrime, "skeletronp", "prime");
			add(Permissions.spawnboss, Hardcore, "hardcore");
			add(Permissions.spawnmob, SpawnMob, "spawnmob", "sm");
			add(Permissions.tp, Home, "home");
			add(Permissions.tp, Spawn, "spawn");
			add(Permissions.tp, TP, "tp");
			add(Permissions.tphere, TPHere, "tphere", "th");
			add(Permissions.tphere, SendWarp, "sendwarp", "sw");
			add(Permissions.tpallow, TPAllow, "tpallow");
			add(Permissions.warp, UseWarp, "warp");
			add(Permissions.managewarp, SetWarp, "setwarp");
			add(Permissions.managewarp, DeleteWarp, "delwarp");
			add(Permissions.managewarp, HideWarp, "hidewarp");
			add(Permissions.managegroup, AddGroup, "addgroup");
			add(Permissions.managegroup, DeleteGroup, "delgroup");
			add(Permissions.managegroup, ModifyGroup, "modgroup");
			add(Permissions.manageitem, AddItem, "additem");
			add(Permissions.manageitem, DeleteItem, "delitem");
			add(Permissions.cfg, SetSpawn, "setspawn");
			add(Permissions.cfg, Reload, "reload");
			add(Permissions.cfg, ServerPassword, "serverpassword");
			add(Permissions.cfg, Save, "save");
			add(Permissions.cfg, Settle, "settle");
			add(Permissions.cfg, MaxSpawns, "maxspawns");
			add(Permissions.cfg, SpawnRate, "spawnrate");
			add(Permissions.time, Time, "time");
			add(Permissions.pvpfun, Slap, "slap");
			add(Permissions.editspawn, ToggleAntiBuild, "antibuild");
			add(Permissions.editspawn, ProtectSpawn, "protectspawn");
			add(Permissions.manageregion, Region, "region");
			add(Permissions.manageregion, DebugRegions, "debugreg");
			add(null, Help, "help");
			add(null, Playing, "playing", "online", "who", "version");
			add(null, AuthToken, "auth");
			add(Permissions.cantalkinthird, ThirdPerson, "me");
			add(Permissions.canpartychat, PartyChat, "p");
			add(null, Motd, "motd");
			add(null, Rules, "rules");
			add(Permissions.mute, Mute, "mute", "unmute");
			add(Permissions.logs, DisplayLogs, "displaylogs");
			ChatCommands.Add(new Command(Permissions.canchangepassword, PasswordUser, "password") {DoLog = false});
			ChatCommands.Add(new Command(Permissions.canregister, RegisterUser, "register", "reg") {DoLog = false});
			ChatCommands.Add(new Command(Permissions.rootonly, ManageUsers, "user") {DoLog = false});
			add(Permissions.rootonly, GrabUserUserInfo, "userinfo", "ui");
			add(Permissions.rootonly, AuthVerify, "auth-verify");
			ChatCommands.Add(new Command(Permissions.canlogin, AttemptLogin, "login", "log") {DoLog = false});
			add(Permissions.cfg, Broadcast, "broadcast", "bc", "say");
			add(Permissions.whisper, Whisper, "whisper", "w", "tell");
			add(Permissions.whisper, Reply, "reply", "r");
			add(Permissions.annoy, Annoy, "annoy");
			add(Permissions.kill, Kill, "kill");
			add(Permissions.butcher, Butcher, "butcher");
			add(Permissions.item, Item, "item", "i");
			add(Permissions.item, Give, "give");
			add(Permissions.clearitems, ClearItems, "clear", "clearitems");
			add(Permissions.heal, Heal, "heal", "h");
			add(Permissions.buff, Buff, "buff");
			add(Permissions.buffplayer, GBuff, "gbuff", "buffplayer");
			add(Permissions.grow, Grow, "grow");
            add(Permissions.grow, GrowTree, "gt");
			add(Permissions.hardmode, StartHardMode, "hardmode");
			add(Permissions.hardmode, DisableHardMode, "stophardmode", "disablehardmode");
			add(Permissions.cfg, ServerInfo, "stats");
			add(Permissions.converthardmode, ConvertCorruption, "convertcorruption");
			add(Permissions.converthardmode, ConvertHallow, "converthallow");
            add(null, SetHome, "sethome");
            add(Permissions.adminchat, AdminChat, "a", "@");
            add(Permissions.time, AltarTimer, "timer");
            add(Permissions.manageregion, AltarEdit, "edit");
            add(null, Location, "location", "loc");
            add(null, Status, "rc", "rcoins", "status", "check");
            add(null, TopTime, "toptime");
            add(null, TopRC, "toprc");
            add(null, PayRC, "pay");
            add(null, Shop, "shop", "buy", "b");
            add(Permissions.tradechat, TradeChat, "t", "tc");
            add(null, Shout, "!");
            add(null, GroupChat, "g");
            add(null, Question, "?");
            add(null, ItemList, "items", "itemlist");
            add(Permissions.converthardmode, ConvertAll, "convertall");
            add(Permissions.manageregion, RegionSet1, "r1");
            add(Permissions.manageregion, RegionSet2, "r2");
            add(Permissions.manageregion, RegionDefine, "rd");
            add(Permissions.manageregion, RegionAllow, "ra");
            add(Permissions.manageregion, RegionDelCoOwner, "rdeluser");
            add(Permissions.manageregion, RegionDelete, "rdel");
            add(Permissions.manageregion, RegionInfo, "ri");
            add(null, HomeSet1, "h1");
            add(null, HomeSet2, "h2");
            add(null, HomeDefine, "hd");
            add(null, HomeAllow, "ha");
            add(null, HomeDelCoOwner, "hdeluser");
            add(null, HomeDelete, "hdel");
            add(null, HomeInfo, "hi");
            add(Permissions.managetown, TownSet1, "t1");
            add(Permissions.managetown, TownSet2, "t2");
            add(Permissions.managetown, TownDefine, "td");
            add(Permissions.managetown, TownChangeMayor, "tcm");
            add(Permissions.managetown, TownDelete, "tdel");
            add(null, TownInfo, "ti");
            add(null, TownTell, "tt");
		}

		public static bool HandleCommand(TSPlayer player, string text)
		{
			string cmdText = text.Remove(0, 1);

			var args = ParseParameters(cmdText);
			if (args.Count < 1)
				return false;

			string cmdName = args[0];
			args.RemoveAt(0);

			Command cmd = ChatCommands.FirstOrDefault(c => c.HasAlias(cmdName));

			if (cmd == null)
			{
				player.SendMessage("Invalid Command Entered. Type /help for a list of valid Commands.", Color.Red);
				return true;
			}

			if (!cmd.CanRun(player))
			{
				TShock.Utils.SendLogs(string.Format("{0} tried to execute {1}", player.Name, cmd.Name), Color.Red);
				player.SendMessage("You do not have access to that command.", Color.Red);
			}
			else
			{
				if (cmd.DoLog)
					TShock.Utils.SendLogs(string.Format("{0} executed: /{1}", player.Name, cmdText), Color.Red);
				cmd.Run(cmdText, player, args);
			}
			return true;
		}

		/// <summary>
		/// Parses a string of parameters into a list. Handles quotes.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		private static List<String> ParseParameters(string str)
		{
			var ret = new List<string>();
			var sb = new StringBuilder();
			bool instr = false;
			for (int i = 0; i < str.Length; i++)
			{
				char c = str[i];

				if (instr)
				{
					if (c == '\\')
					{
						if (i + 1 >= str.Length)
							break;
						c = GetEscape(str[++i]);
					}
					else if (c == '"')
					{
						ret.Add(sb.ToString());
						sb.Clear();
						instr = false;
						continue;
					}
					sb.Append(c);
				}
				else
				{
					if (IsWhiteSpace(c))
					{
						if (sb.Length > 0)
						{
							ret.Add(sb.ToString());
							sb.Clear();
						}
					}
					else if (c == '"')
					{
						if (sb.Length > 0)
						{
							ret.Add(sb.ToString());
							sb.Clear();
						}
						instr = true;
					}
					else
					{
						sb.Append(c);
					}
				}
			}
			if (sb.Length > 0)
				ret.Add(sb.ToString());

			return ret;
		}

		private static char GetEscape(char c)
		{
			switch (c)
			{
				case '\\':
					return '\\';
				case '"':
					return '"';
				case 't':
					return '\t';
				default:
					return c;
			}
		}

		private static bool IsWhiteSpace(char c)
		{
			return c == ' ' || c == '\t' || c == '\n';
		}

		#region Account commands

		public static void AttemptLogin(CommandArgs args)
		{
			if (args.Player.LoginAttempts > TShock.Config.MaximumLoginAttempts && (TShock.Config.MaximumLoginAttempts != -1))
			{
				Log.Warn(args.Player.IP + "(" + args.Player.Name + ") had " + TShock.Config.MaximumLoginAttempts +
						 " or more invalid login attempts and was kicked automatically.");
				TShock.Utils.Kick(args.Player, "Too many invalid login attempts.");
			}

			var user = TShock.Users.GetUserByName(args.Player.Name);
			string encrPass = "";

			if (args.Parameters.Count == 1)
			{
				user = TShock.Users.GetUserByName(args.Player.Name);
				encrPass = TShock.Utils.HashPassword(args.Parameters[0]);
			}
			else if (args.Parameters.Count == 2 && TShock.Config.AllowLoginAnyUsername)
			{
				user = TShock.Users.GetUserByName(args.Parameters[0]);
				encrPass = TShock.Utils.HashPassword(args.Parameters[1]);
			}
			else
			{
				args.Player.SendMessage("Syntax: /login [password]");
				args.Player.SendMessage("If you forgot your password, there is no way to recover it.");
				return;
			}
			try
			{
				if (user == null)
				{
					args.Player.SendMessage("User by that name does not exist");
				}
				else if (user.Password.ToUpper() == encrPass.ToUpper())
				{
					//args.Player.PlayerData = TShock.InventoryDB.GetPlayerData(args.Player, TShock.Users.GetUserID(user.Name));

					var group = TShock.Utils.GetGroup(user.Group);

					if (group.HasPermission(Permissions.ignorestackhackdetection))
						args.Player.IgnoreActionsForCheating = "none";

					if (group.HasPermission(Permissions.usebanneditem))
						args.Player.IgnoreActionsForDisabledArmor = "none";

                    args.Player.Group = TShock.Utils.GetGroup(user.Group);
                    args.Player.UserAccountName = args.Player.Name;
                    args.Player.UserID = TShock.Users.GetUserID(args.Player.UserAccountName);
                    args.Player.IsLoggedIn = true;
                    args.Player.SendMessage("Authenticated successfully.", Color.LimeGreen);
                    args.Player.SendMessage(string.Format("Hello {0}. Your last login is {1}.", args.Player.Name, Convert.ToDateTime(user.LastLogin)));
                    TShock.Users.Login(args.Player);
                    args.Player.SavePlayer();
                    Log.ConsoleInfo(args.Player.Name + " authenticated successfully.");
				}
				else
				{
					args.Player.SendMessage("Incorrect password", Color.Red);
					Log.Warn(args.Player.IP + " failed to authenticate as user: " + user.Name);
					args.Player.LoginAttempts++;
				}
			}
			catch (Exception ex)
			{
				args.Player.SendMessage("There was an error processing your request.", Color.Red);
				Log.Error(ex.ToString());
			}
		}

		private static void PasswordUser(CommandArgs args)
		{
			try
			{
				if (args.Player.IsLoggedIn && args.Parameters.Count == 2)
				{
					var user = TShock.Users.GetUserByName(args.Player.UserAccountName);
					string encrPass = TShock.Utils.HashPassword(args.Parameters[0]);
					if (user.Password.ToUpper() == encrPass.ToUpper())
					{
						args.Player.SendMessage("You changed your password!", Color.Green);
						TShock.Users.SetUserPassword(user, args.Parameters[1]); // SetUserPassword will hash it for you.
						Log.ConsoleInfo(args.Player.IP + " named " + args.Player.Name + " changed the password of Account " + user.Name);
					}
					else
					{
						args.Player.SendMessage("You failed to change your password!", Color.Red);
						Log.ConsoleError(args.Player.IP + " named " + args.Player.Name + " failed to change password for Account: " +
										 user.Name);
					}
				}
				else
				{
					args.Player.SendMessage("Not Logged in or Invalid syntax! Proper syntax: /password <oldpassword> <newpassword>",
											Color.Red);
				}
			}
			catch (UserManagerException ex)
			{
				args.Player.SendMessage("Sorry, an error occured: " + ex.Message, Color.Green);
				Log.ConsoleError("RegisterUser returned an error: " + ex);
			}
		}

		private static void RegisterUser(CommandArgs args)
		{
			try
			{
				var user = new User();

				if (args.Parameters.Count == 1)
				{
					user.Name = args.Player.Name;
					user.Password = args.Parameters[0];
				}
				else if (args.Parameters.Count == 2 && TShock.Config.AllowRegisterAnyUsername)
				{
					user.Name = args.Parameters[0];
					user.Password = args.Parameters[1];
				}
				else
				{
					args.Player.SendMessage("Invalid syntax! Proper syntax: /register <password>", Color.Red);
					return;
				}

				user.Group = TShock.Config.DefaultRegistrationGroupName; // FIXME -- we should get this from the DB.

				if (TShock.Users.GetUserByName(user.Name) == null) // Cheap way of checking for existance of a user
				{
					args.Player.SendMessage("Account " + user.Name + " has been registered.", Color.Green);
					args.Player.SendMessage("Your password is " + user.Password);
					TShock.Users.AddUser(user);
					Log.ConsoleInfo(args.Player.Name + " registered an Account: " + user.Name);
				}
				else
				{
					args.Player.SendMessage("Account " + user.Name + " has already been registered.", Color.Green);
					Log.ConsoleInfo(args.Player.Name + " failed to register an existing Account: " + user.Name);
				}
			}
			catch (UserManagerException ex)
			{
				args.Player.SendMessage("Sorry, an error occured: " + ex.Message, Color.Green);
				Log.ConsoleError("RegisterUser returned an error: " + ex);
			}
		}

		//Todo: Add separate help text for '/user add' and '/user del'. Also add '/user addip' and '/user delip'

		private static void ManageUsers(CommandArgs args)
		{
			// This guy needs to go away for the help later on to take effect.

			//if (args.Parameters.Count < 2)
			//{
			//    args.Player.SendMessage("Syntax: /user <add/del> <ip/user:pass> [group]");
			//    args.Player.SendMessage("Note: Passwords are stored with SHA512 hashing. To reset a user's password, remove and re-add them.");
			//    return;
			//}

			// This guy needs to be here so that people don't get exceptions when they type /user
			if (args.Parameters.Count < 1)
			{
				args.Player.SendMessage("Invalid user syntax. Try /user help.", Color.Red);
				return;
			}

			string subcmd = args.Parameters[0];

			// Add requires a username:password pair/ip address and a group specified.
			if (subcmd == "add")
			{
				var namepass = args.Parameters[1].Split(':');
				var user = new User();

				try
				{
					if (args.Parameters.Count > 2)
					{
						if (namepass.Length == 2)
						{
							user.Name = namepass[0];
							user.Password = namepass[1];
							user.Group = args.Parameters[2];
						}
						else if (namepass.Length == 1)
						{
							user.Address = namepass[0];
							user.Group = args.Parameters[2];
							user.Name = user.Address;
						}
						if (!string.IsNullOrEmpty(user.Address))
						{
							args.Player.SendMessage("IP address admin added. If they're logged in, tell them to rejoin.", Color.Green);
							args.Player.SendMessage("WARNING: This is insecure! It would be better to use a user account instead.", Color.Red);
							TShock.Users.AddUser(user);
							Log.ConsoleInfo(args.Player.Name + " added IP " + user.Address + " to group " + user.Group);
						}
						else
						{
							args.Player.SendMessage("Account " + user.Name + " has been added to group " + user.Group + "!", Color.Green);
							TShock.Users.AddUser(user);
							Log.ConsoleInfo(args.Player.Name + " added Account " + user.Name + " to group " + user.Group);
						}
					}
					else
					{
						args.Player.SendMessage("Invalid syntax. Try /user help.", Color.Red);
					}
				}
				catch (UserManagerException ex)
				{
					args.Player.SendMessage(ex.Message, Color.Green);
					Log.ConsoleError(ex.ToString());
				}
			}
				// User deletion requires a username
			else if (subcmd == "del" && args.Parameters.Count == 2)
			{
				var user = new User();
				if (args.Parameters[1].Contains("."))
					user.Address = args.Parameters[1];
				else
					user.Name = args.Parameters[1];

				try
				{
					TShock.Users.RemoveUser(user);
					args.Player.SendMessage("Account removed successfully.", Color.Green);
					Log.ConsoleInfo(args.Player.Name + " successfully deleted account: " + args.Parameters[1]);
				}
				catch (UserManagerException ex)
				{
					args.Player.SendMessage(ex.Message, Color.Red);
					Log.ConsoleError(ex.ToString());
				}
			}
				// Password changing requires a username, and a new password to set
			else if (subcmd == "password")
			{
				var user = new User();
				user.Name = args.Parameters[1];

				try
				{
					if (args.Parameters.Count == 3)
					{
						args.Player.SendMessage("Changed the password of " + user.Name + "!", Color.Green);
						TShock.Users.SetUserPassword(user, args.Parameters[2]);
						Log.ConsoleInfo(args.Player.Name + " changed the password of Account " + user.Name);
					}
					else
					{
						args.Player.SendMessage("Invalid user password syntax. Try /user help.", Color.Red);
					}
				}
				catch (UserManagerException ex)
				{
					args.Player.SendMessage(ex.Message, Color.Green);
					Log.ConsoleError(ex.ToString());
				}
			}
				// Group changing requires a username or IP address, and a new group to set
			else if (subcmd == "group")
			{
				var user = new User();
				if (args.Parameters[1].Contains("."))
					user.Address = args.Parameters[1];
				else
					user.Name = args.Parameters[1];

				try
				{
					if (args.Parameters.Count == 3)
					{
						if (!string.IsNullOrEmpty(user.Address))
						{
							args.Player.SendMessage("IP Address " + user.Address + " has been changed to group " + args.Parameters[2] + "!",
													Color.Green);
							TShock.Users.SetUserGroup(user, args.Parameters[2]);
							Log.ConsoleInfo(args.Player.Name + " changed IP Address " + user.Address + " to group " + args.Parameters[2]);
						}
						else
						{
							args.Player.SendMessage("Account " + user.Name + " has been changed to group " + args.Parameters[2] + "!",
													Color.Green);
							TShock.Users.SetUserGroup(user, args.Parameters[2]);
							Log.ConsoleInfo(args.Player.Name + " changed Account " + user.Name + " to group " + args.Parameters[2]);
						}
					}
					else
					{
						args.Player.SendMessage("Invalid user group syntax. Try /user help.", Color.Red);
					}
				}
				catch (UserManagerException ex)
				{
					args.Player.SendMessage(ex.Message, Color.Green);
					Log.ConsoleError(ex.ToString());
				}
			}
			else if (subcmd == "help")
			{
				args.Player.SendMessage("Help for user subcommands:");
				args.Player.SendMessage("/user add username:password group   -- Adds a specified user");
				args.Player.SendMessage("/user del username                  -- Removes a specified user");
				args.Player.SendMessage("/user password username newpassword -- Changes a user's password");
				args.Player.SendMessage("/user group username newgroup       -- Changes a user's group");
			}
			else
			{
				args.Player.SendMessage("Invalid user syntax. Try /user help.", Color.Red);
			}
		}

		#endregion

		#region Stupid commands

		public static void ServerInfo(CommandArgs args)
		{
			args.Player.SendMessage("Memory usage: " + Process.GetCurrentProcess().WorkingSet64);
			args.Player.SendMessage("Allocated memory: " + Process.GetCurrentProcess().VirtualMemorySize64);
			args.Player.SendMessage("Total processor time: " + Process.GetCurrentProcess().TotalProcessorTime);
			args.Player.SendMessage("Ver: " + Environment.OSVersion);
			args.Player.SendMessage("Proc count: " + Environment.ProcessorCount);
			args.Player.SendMessage("Machine name: " + Environment.MachineName);
		}

		#endregion

		#region Player Management Commands

		private static void GrabUserUserInfo(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /userinfo <player>", Color.Red);
				return;
			}

			var players = TShock.Utils.FindPlayer(args.Parameters[0]);
			if (players.Count > 1)
			{
				args.Player.SendMessage("More than one player matched your query.", Color.Red);
				return;
			}
			try
			{
				args.Player.SendMessage("IP Address: " + players[0].IP + " Logged In As: " + players[0].UserAccountName, Color.Green);
			}
			catch (Exception)
			{
				args.Player.SendMessage("Invalid player.", Color.Red);
			}
		}

		private static void Kick(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /kick <player> [reason]", Color.Red);
				return;
			}
			if (args.Parameters[0].Length == 0)
			{
				args.Player.SendMessage("Missing player name", Color.Red);
				return;
			}
            
            if (args.Parameters[0].ToLower() == "all")
            {
                foreach (TSPlayer player in TShock.Players)
                {
                    if (player != null && player.Active && player.IsLoggedIn)
                    {
                        if (TShock.Config.StoreInventory)
                            TShock.Inventory.UpdateInventory(player);
                        if (player.SavePlayer())
                            player.SendMessage("Your profile saved successfully", Color.Green);
                    }
                }
                Console.WriteLine("All profiles saved!");
                TShock.Utils.ForceKickAll("Reboot!");
                return;
            }

			string plStr = args.Parameters[0];
			var players = TShock.Utils.FindPlayer(plStr);
			if (players.Count == 0)
			{
				args.Player.SendMessage("Invalid player!", Color.Red);
			}
			else if (players.Count > 1)
			{
				args.Player.SendMessage("More than one player matched!", Color.Red);
			}
			else
			{
				string reason = args.Parameters.Count > 1
									? String.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1))
									: "Misbehaviour.";
				if (!TShock.Utils.Kick(players[0], reason))
				{
					args.Player.SendMessage("You can't kick another admin!", Color.Red);
				}
			}
		}

		private static void Ban(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /ban <player> [reason]", Color.Red);
				return;
			}
			if (args.Parameters[0].Length == 0)
			{
				args.Player.SendMessage("Missing player name", Color.Red);
				return;
			}

			string plStr = args.Parameters[0];
			var players = TShock.Utils.FindPlayer(plStr);
			if (players.Count == 0)
			{
				args.Player.SendMessage("Invalid player!", Color.Red);
			}
			else if (players.Count > 1)
			{
				args.Player.SendMessage("More than one player matched!", Color.Red);
			}
			else
			{
				string reason = args.Parameters.Count > 1
									? String.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1))
									: "Misbehaviour.";
				if (!TShock.Utils.Ban(players[0], reason, args.Player.Name))
				{
					args.Player.SendMessage("You can't ban another admin!", Color.Red);
				}
			}
		}

		private static void BanIP(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendMessage("Syntax: /banip <ip> [reason]", Color.Red);
				return;
			}
			if (args.Parameters[0].Length == 0)
			{
				args.Player.SendMessage("Missing IP address", Color.Red);
				return;
			}

			string ip = args.Parameters[0];
			string reason = args.Parameters.Count > 1
								? String.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1))
								: "Manually added IP address ban.";
			TShock.Bans.AddBan(ip, "", reason, args.Player.Name);
		}

		private static void UnBan(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /unban <player>", Color.Red);
				return;
			}
			if (args.Parameters[0].Length == 0)
			{
				args.Player.SendMessage("Missing player name", Color.Red);
				return;
			}

			string plStr = args.Parameters[0];
			var ban = TShock.Bans.GetBanByName(plStr);
			if (ban != null)
			{
				if (TShock.Bans.RemoveBan(ban.IP))
					args.Player.SendMessage(string.Format("Unbanned {0} ({1})!", ban.Name, ban.IP), Color.Red);
				else
					args.Player.SendMessage(string.Format("Failed to unban {0} ({1})!", ban.Name, ban.IP), Color.Red);
			}
			else if (!TShock.Config.EnableBanOnUsernames)
			{
				ban = TShock.Bans.GetBanByIp(plStr);

				if (ban == null)
					args.Player.SendMessage(string.Format("Failed to unban {0}, not found.", args.Parameters[0]), Color.Red);
				else if (TShock.Bans.RemoveBan(ban.IP))
					args.Player.SendMessage(string.Format("Unbanned {0} ({1})!", ban.Name, ban.IP), Color.Red);
				else
					args.Player.SendMessage(string.Format("Failed to unban {0} ({1})!", ban.Name, ban.IP), Color.Red);
			}
			else
			{
				args.Player.SendMessage("Invalid player!", Color.Red);
			}
		}

		private static int ClearBansCode = -1;

		private static void ClearBans(CommandArgs args)
		{
			if (args.Parameters.Count < 1 && ClearBansCode == -1)
			{
				ClearBansCode = new Random().Next(0, short.MaxValue);
				args.Player.SendMessage("ClearBans Code: " + ClearBansCode, Color.Red);
				return;
			}
			if (args.Parameters.Count < 1)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /clearbans <code>");
				return;
			}

			int num;
			if (!int.TryParse(args.Parameters[0], out num))
			{
				args.Player.SendMessage("Invalid syntax! Expecting number");
				return;
			}

			if (num == ClearBansCode)
			{
				ClearBansCode = -1;
				if (TShock.Bans.ClearBans())
				{
					Log.ConsoleInfo("Bans cleared");
					args.Player.SendMessage("Bans cleared");
				}
				else
				{
					args.Player.SendMessage("Failed to clear bans");
				}
			}
			else
			{
				args.Player.SendMessage("Incorrect clear code");
			}
		}

		private static void UnBanIP(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /unbanip <ip>", Color.Red);
				return;
			}
			if (args.Parameters[0].Length == 0)
			{
				args.Player.SendMessage("Missing ip", Color.Red);
				return;
			}

			string plStr = args.Parameters[0];
			var ban = TShock.Bans.GetBanByIp(plStr);
			if (ban != null)
			{
				if (TShock.Bans.RemoveBan(ban.IP))
					args.Player.SendMessage(string.Format("Unbanned {0} ({1})!", ban.Name, ban.IP), Color.Red);
				else
					args.Player.SendMessage(string.Format("Failed to unban {0} ({1})!", ban.Name, ban.IP), Color.Red);
			}
			else
			{
				args.Player.SendMessage("Invalid player!", Color.Red);
			}
		}

		public static void Whitelist(CommandArgs args)
		{
			if (args.Parameters.Count == 1)
			{
				using (var tw = new StreamWriter(FileTools.WhitelistPath, true))
				{
					tw.WriteLine(args.Parameters[0]);
				}
				args.Player.SendMessage("Added " + args.Parameters[0] + " to the whitelist.");
			}
		}

		public static void DisplayLogs(CommandArgs args)
		{
			args.Player.DisplayLogs = (!args.Player.DisplayLogs);
			args.Player.SendMessage("You now " + (args.Player.DisplayLogs ? "receive" : "stopped receiving") + " logs");
		}

		#endregion Player Management Commands

		#region Server Maintenence Commands

		private static void Broadcast(CommandArgs args)
		{
			string message = "";

			for (int i = 0; i < args.Parameters.Count; i++)
			{
				message += " " + args.Parameters[i];
			}

			TShock.Utils.Broadcast("(Server)" + message, Color.Yellow);
			return;
		}

		private static void Off(CommandArgs args)
		{
			TShock.Utils.ForceKickAll("Server shutting down!");
			WorldGen.saveWorld();
			Netplay.disconnect = true;
		}

		private static void OffNoSave(CommandArgs args)
		{
			TShock.Utils.ForceKickAll("Server shutting down!");
			Netplay.disconnect = true;
		}

		private static void CheckUpdates(CommandArgs args)
		{
			ThreadPool.QueueUserWorkItem(UpdateManager.CheckUpdate);
		}

		#endregion Server Maintenence Commands

		#region Cause Events and Spawn Monsters Commands

		private static void DropMeteor(CommandArgs args)
		{
			WorldGen.spawnMeteor = false;
			WorldGen.dropMeteor();
		}

		private static void Star(CommandArgs args)
		{
			int penis56 = 12;
			int penis57 = Main.rand.Next(Main.maxTilesX - 50) + 100;
			penis57 *= 0x10;
			int penis58 = Main.rand.Next((int) (Main.maxTilesY*0.05))*0x10;
			Vector2 vector = new Vector2(penis57, penis58);
			float speedX = Main.rand.Next(-100, 0x65);
			float speedY = Main.rand.Next(200) + 100;
			float penis61 = (float) Math.Sqrt(((speedX*speedX) + (speedY*speedY)));
			penis61 = (penis56)/penis61;
			speedX *= penis61;
			speedY *= penis61;
			Projectile.NewProjectile(vector.X, vector.Y, speedX, speedY, 12, 0x3e8, 10f, Main.myPlayer);
		}

		private static void Fullmoon(CommandArgs args)
		{
			TSPlayer.Server.SetFullMoon(true);
			TShock.Utils.Broadcast(string.Format("{0} turned on full moon.", args.Player.Name));
		}

		private static void Bloodmoon(CommandArgs args)
		{
			TSPlayer.Server.SetBloodMoon(true);
			TShock.Utils.Broadcast(string.Format("{0} turned on blood moon.", args.Player.Name));
		}

		private static void Invade(CommandArgs args)
		{
			if (Main.invasionSize <= 0)
			{
				TShock.Utils.Broadcast(string.Format("{0} has started an invasion.", args.Player.Name));
				TShock.StartInvasion();
			}
			else
			{
				TShock.Utils.Broadcast(string.Format("{0} has ended an invasion.", args.Player.Name));
				Main.invasionSize = 0;
			}
		}

		[Obsolete("This specific command for spawning mobs will replaced soon.")]
		private static void Eater(CommandArgs args)
		{
			if (args.Parameters.Count > 1)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /eater [amount]", Color.Red);
				return;
			}
			int amount = 1;
			if (args.Parameters.Count == 1 && !int.TryParse(args.Parameters[0], out amount))
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /eater [amount]", Color.Red);
				return;
			}
			amount = Math.Min(amount, Main.maxNPCs);
			NPC eater = TShock.Utils.GetNPCById(13);
			TSPlayer.Server.SpawnNPC(eater.type, eater.name, amount, args.Player.TileX, args.Player.TileY);
			TShock.Utils.Broadcast(string.Format("{0} has spawned eater of worlds {1} times!", args.Player.Name, amount));
		}

		[Obsolete("This specific command for spawning mobs will replaced soon.")]
		private static void Eye(CommandArgs args)
		{
			if (args.Parameters.Count > 1)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /eye [amount]", Color.Red);
				return;
			}
			int amount = 1;
			if (args.Parameters.Count == 1 && !int.TryParse(args.Parameters[0], out amount))
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /eye [amount]", Color.Red);
				return;
			}
			amount = Math.Min(amount, Main.maxNPCs);
			NPC eye = TShock.Utils.GetNPCById(4);
			TSPlayer.Server.SetTime(false, 0.0);
			TSPlayer.Server.SpawnNPC(eye.type, eye.name, amount, args.Player.TileX, args.Player.TileY);
			TShock.Utils.Broadcast(string.Format("{0} has spawned eye {1} times!", args.Player.Name, amount));
		}

		[Obsolete("This specific command for spawning mobs will replaced soon.")]
		private static void King(CommandArgs args)
		{
			if (args.Parameters.Count > 1)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /king [amount]", Color.Red);
				return;
			}
			int amount = 1;
			if (args.Parameters.Count == 1 && !int.TryParse(args.Parameters[0], out amount))
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /king [amount]", Color.Red);
				return;
			}
			amount = Math.Min(amount, Main.maxNPCs);
			NPC king = TShock.Utils.GetNPCById(50);
			TSPlayer.Server.SpawnNPC(king.type, king.name, amount, args.Player.TileX, args.Player.TileY);
			TShock.Utils.Broadcast(string.Format("{0} has spawned king slime {1} times!", args.Player.Name, amount));
		}

		[Obsolete("This specific command for spawning mobs will replaced soon.")]
		private static void Skeletron(CommandArgs args)
		{
			if (args.Parameters.Count > 1)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /skeletron [amount]", Color.Red);
				return;
			}
			int amount = 1;
			if (args.Parameters.Count == 1 && !int.TryParse(args.Parameters[0], out amount))
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /skeletron [amount]", Color.Red);
				return;
			}
			amount = Math.Min(amount, Main.maxNPCs);
			NPC skeletron = TShock.Utils.GetNPCById(35);
			TSPlayer.Server.SetTime(false, 0.0);
			TSPlayer.Server.SpawnNPC(skeletron.type, skeletron.name, amount, args.Player.TileX, args.Player.TileY);
			TShock.Utils.Broadcast(string.Format("{0} has spawned skeletron {1} times!", args.Player.Name, amount));
		}

		[Obsolete("This specific command for spawning mobs will replaced soon.")]
		private static void WoF(CommandArgs args)
		{
			if (Main.wof >= 0 || (args.Player.Y/16f < (Main.maxTilesY - 205)))
			{
				args.Player.SendMessage("Can't spawn Wall of Flesh!", Color.Red);
				return;
			}
			NPC.SpawnWOF(new Vector2(args.Player.X, args.Player.Y));
			TShock.Utils.Broadcast(string.Format("{0} has spawned Wall of Flesh!", args.Player.Name));
		}

		[Obsolete("This specific command for spawning mobs will replaced soon.")]
		private static void Twins(CommandArgs args)
		{
			if (args.Parameters.Count > 1)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /twins [amount]", Color.Red);
				return;
			}
			int amount = 1;
			if (args.Parameters.Count == 1 && !int.TryParse(args.Parameters[0], out amount))
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /twins [amount]", Color.Red);
				return;
			}
			amount = Math.Min(amount, Main.maxNPCs);
			NPC retinazer = TShock.Utils.GetNPCById(125);
			NPC spaz = TShock.Utils.GetNPCById(126);
			TSPlayer.Server.SetTime(false, 0.0);
			TSPlayer.Server.SpawnNPC(retinazer.type, retinazer.name, amount, args.Player.TileX, args.Player.TileY);
			TSPlayer.Server.SpawnNPC(spaz.type, spaz.name, amount, args.Player.TileX, args.Player.TileY);
			TShock.Utils.Broadcast(string.Format("{0} has spawned the twins {1} times!", args.Player.Name, amount));
		}

		[Obsolete("This specific command for spawning mobs will replaced soon.")]
		private static void Destroyer(CommandArgs args)
		{
			if (args.Parameters.Count > 1)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /destroyer [amount]", Color.Red);
				return;
			}
			int amount = 1;
			if (args.Parameters.Count == 1 && !int.TryParse(args.Parameters[0], out amount))
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /destroyer [amount]", Color.Red);
				return;
			}
			amount = Math.Min(amount, Main.maxNPCs);
			NPC destroyer = TShock.Utils.GetNPCById(134);
			TSPlayer.Server.SetTime(false, 0.0);
			TSPlayer.Server.SpawnNPC(destroyer.type, destroyer.name, amount, args.Player.TileX, args.Player.TileY);
			TShock.Utils.Broadcast(string.Format("{0} has spawned the destroyer {1} times!", args.Player.Name, amount));
		}

		[Obsolete("This specific command for spawning mobs will replaced soon.")]
		private static void SkeletronPrime(CommandArgs args)
		{
			if (args.Parameters.Count > 1)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /prime [amount]", Color.Red);
				return;
			}
			int amount = 1;
			if (args.Parameters.Count == 1 && !int.TryParse(args.Parameters[0], out amount))
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /prime [amount]", Color.Red);
				return;
			}
			amount = Math.Min(amount, Main.maxNPCs);
			NPC prime = TShock.Utils.GetNPCById(127);
			TSPlayer.Server.SetTime(false, 0.0);
			TSPlayer.Server.SpawnNPC(prime.type, prime.name, amount, args.Player.TileX, args.Player.TileY);
			TShock.Utils.Broadcast(string.Format("{0} has spawned skeletron prime {1} times!", args.Player.Name, amount));
		}

		[Obsolete("This specific command for spawning mobs will replaced soon.")]
		private static void Hardcore(CommandArgs args) // TODO: Add all 8 bosses
		{
			if (args.Parameters.Count > 1)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /hardcore [amount]", Color.Red);
				return;
			}
			int amount = 1;
			if (args.Parameters.Count == 1 && !int.TryParse(args.Parameters[0], out amount))
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /hardcore [amount]", Color.Red);
				return;
			}
			amount = Math.Min(amount, Main.maxNPCs/4);
			NPC retinazer = TShock.Utils.GetNPCById(125);
			NPC spaz = TShock.Utils.GetNPCById(126);
			NPC destroyer = TShock.Utils.GetNPCById(134);
			NPC prime = TShock.Utils.GetNPCById(127);
			NPC eater = TShock.Utils.GetNPCById(13);
			NPC eye = TShock.Utils.GetNPCById(4);
			NPC king = TShock.Utils.GetNPCById(50);
			NPC skeletron = TShock.Utils.GetNPCById(35);
			TSPlayer.Server.SetTime(false, 0.0);
			TSPlayer.Server.SpawnNPC(retinazer.type, retinazer.name, amount, args.Player.TileX, args.Player.TileY);
			TSPlayer.Server.SpawnNPC(spaz.type, spaz.name, amount, args.Player.TileX, args.Player.TileY);
			TSPlayer.Server.SpawnNPC(destroyer.type, destroyer.name, amount, args.Player.TileX, args.Player.TileY);
			TSPlayer.Server.SpawnNPC(prime.type, prime.name, amount, args.Player.TileX, args.Player.TileY);
			TSPlayer.Server.SpawnNPC(eater.type, eater.name, amount, args.Player.TileX, args.Player.TileY);
			TSPlayer.Server.SpawnNPC(eye.type, eye.name, amount, args.Player.TileX, args.Player.TileY);
			TSPlayer.Server.SpawnNPC(king.type, king.name, amount, args.Player.TileX, args.Player.TileY);
			TSPlayer.Server.SpawnNPC(skeletron.type, skeletron.name, amount, args.Player.TileX, args.Player.TileY);
			TShock.Utils.Broadcast(string.Format("{0} has spawned all bosses {1} times!", args.Player.Name, amount));
		}

		private static void SpawnMob(CommandArgs args)
		{
			if (args.Parameters.Count < 1 || args.Parameters.Count > 2)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /spawnmob <mob name/id> [amount]", Color.Red);
				return;
			}
			if (args.Parameters[0].Length == 0)
			{
				args.Player.SendMessage("Missing mob name/id", Color.Red);
				return;
			}
			int amount = 1;
			if (args.Parameters.Count == 2 && !int.TryParse(args.Parameters[1], out amount))
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /spawnmob <mob name/id> [amount]", Color.Red);
				return;
			}

			amount = Math.Min(amount, Main.maxNPCs);

			var npcs = TShock.Utils.GetNPCByIdOrName(args.Parameters[0]);
			if (npcs.Count == 0)
			{
				args.Player.SendMessage("Invalid mob type!", Color.Red);
			}
			else if (npcs.Count > 1)
			{
				args.Player.SendMessage(string.Format("More than one ({0}) mob matched!", npcs.Count), Color.Red);
			}
			else
			{
				var npc = npcs[0];
				if (npc.type >= 1 && npc.type < Main.maxNPCTypes && npc.type != 113)
					//Do not allow WoF to spawn, in certain conditions may cause loops in client
				{
					TSPlayer.Server.SpawnNPC(npc.type, npc.name, amount, args.Player.TileX, args.Player.TileY, 50, 20);
					TShock.Utils.Broadcast(string.Format("{0} was spawned {1} time(s).", npc.name, amount));
				}
				else if (npc.type == 113)
					args.Player.SendMessage("Sorry, you can't spawn Wall of Flesh! Try /wof instead.");
						// Maybe perhaps do something with WorldGen.SpawnWoF?
				else
					args.Player.SendMessage("Invalid mob type!", Color.Red);
			}
		}

        	private static void StartHardMode(CommandArgs args)
        	{
        		if (!TShock.Config.DisableHardmode)
            			WorldGen.StartHardmode();
            		else
            			args.Player.SendMessage("Hardmode is disabled via config", Color.Red);
        	}

        	private static void DisableHardMode(CommandArgs args)
        	{
            		Main.hardMode = false;
            		args.Player.SendMessage("Hardmode is now disabled", Color.Green);
        	}

		private static void ConvertCorruption(CommandArgs args)
		{
			TShock.Utils.Broadcast("Server is might lag for a moment.", Color.Red);
			for (int x = 0; x < Main.maxTilesX; x++)
			{
				for (int y = 0; y < Main.maxTilesY; y++)
				{
					switch (Main.tile[x, y].type)
					{
						case 22:
						case 25:
							Main.tile[x, y].type = 117;
							break;
						case 23:
							Main.tile[x, y].type = 109;
							break;
						case 32:
							Main.tile[x, y].type = 0;
							Main.tile[x, y].active = false;
							break;
						case 24:
							Main.tile[x, y].type = 110;
							break;
						case 112:
							Main.tile[x, y].type = 116;
							break;
						default:
							continue;
					}
				}
			}
			WorldGen.CountTiles(0);
			TSPlayer.All.SendData(PacketTypes.UpdateGoodEvil);
			Netplay.ResetSections();
			TShock.Utils.Broadcast("Corruption conversion done.");
		}

		private static void ConvertHallow(CommandArgs args)
		{
			TShock.Utils.Broadcast("Server is might lag for a moment.", Color.Red);
			for (int x = 0; x < Main.maxTilesX; x++)
			{
				for (int y = 0; y < Main.maxTilesY; y++)
				{
					switch (Main.tile[x, y].type)
					{
						case 117:
							Main.tile[x, y].type = 25;
							break;
						case 109:
							Main.tile[x, y].type = 23;
							break;
						case 116:
							Main.tile[x, y].type = 112;
							break;
						default:
							continue;
					}
				}
			}
			WorldGen.CountTiles(0);
			TSPlayer.All.SendData(PacketTypes.UpdateGoodEvil);
			Netplay.ResetSections();
			TShock.Utils.Broadcast("Hallow conversion done.");
		}

        private static void ConvertAll(CommandArgs args)
        {
            TShock.Utils.Broadcast("Server is might lag for a moment.", Color.Red);
            for (int x = 0; x < Main.maxTilesX; x++)
            {
                for (int y = 0; y < Main.maxTilesY; y++)
                {
                    switch (Main.tile[x, y].type)
                    {
                        case 22:
                        case 25:
                            Main.tile[x, y].type = 1;
                            break;
                        case 23:
                            Main.tile[x, y].type = 2;
                            break;
                        case 32:
                            Main.tile[x, y].type = 0;
                            Main.tile[x, y].active = false;
                            break;
                        case 24:
                            Main.tile[x, y].type = 3;
                            break;
                        case 112:
                            Main.tile[x, y].type = 53;
                            break;
                        default:
                            continue;
                    }
                }
            }
            WorldGen.CountTiles(0);
            TSPlayer.All.SendData(PacketTypes.UpdateGoodEvil);
            Netplay.ResetSections();
            TShock.Utils.Broadcast("Now ground is clean.");
        }

		#endregion Cause Events and Spawn Monsters Commands

		#region Teleport Commands

		private static void Home(CommandArgs args)
		{
			if (!args.Player.RealPlayer)
			{
				args.Player.SendMessage("You cannot use teleport commands!");
				return;
			}

            if (TShock.HomeManager.GetHome(args.Player.Name) != Vector2.Zero)
            {
                var pos = TShock.HomeManager.GetHome(args.Player.Name);
                args.Player.Teleport((int)pos.X, (int)pos.Y);
                args.Player.SendTileSquare((int)pos.X, (int)pos.Y);
                args.Player.SendMessage("Teleported to your Home.");
            }
            else
            {
                args.Player.SendMessage("Home not placed yet.");
            }
		}

		private static void Spawn(CommandArgs args)
		{
			if (!args.Player.RealPlayer)
			{
				args.Player.SendMessage("You cannot use teleport commands!");
				return;
			}

			if (args.Player.Teleport(Main.spawnTileX, Main.spawnTileY))
				args.Player.SendMessage("Teleported to the map's spawnpoint.");
		}

		private static void TP(CommandArgs args)
        {
            if (TShock.Users.Buy(args.Player.Name, 3, true) || args.Player.Group.HasPermission("rich") || args.Player.Group.HasPermission("vipstatus"))
             {
                int result = 0;
                if (!args.Player.RealPlayer)
                {
                    args.Player.SendMessage("You cannot use teleport commands!");
                    return;
                }

                if (args.Parameters.Count < 1)
                {
                    args.Player.SendMessage("Invalid syntax! Proper syntax: /tp <player/x y> ", Color.Red);
                    return;
                }
                if (Int32.TryParse(args.Parameters[args.Parameters.Count - 1], out result))
                {
                    if (args.Player.Teleport(Convert.ToInt32(args.Parameters[0]), Convert.ToInt32(args.Parameters[1]) + 3))
                        args.Player.SendMessage(string.Format("Teleported to X= {0}; Y= {1}", args.Parameters[0], args.Parameters[1]));
                }
                else
                {
                    string plStr = String.Join(" ", args.Parameters);
                    var players = TShock.Utils.FindPlayer(plStr);
                    if (players.Count == 0)
                        args.Player.SendMessage("Invalid player!", Color.Red);
                    else if (players.Count > 1)
                        args.Player.SendMessage("More than one player matched!", Color.Red);
                    else
                    {
                        var plr = players[0];
                        if (plr.TPAllow || args.Player.Group.HasPermission(Permissions.adminstatus))
                        {
                            if (args.Player.Teleport(plr.TileX, plr.TileY + 3))
                            {
                                if (args.Player.Group.HasPermission("vipstatus"))
                                {
                                    args.Player.SendMessage(string.Format("Teleported to {0}", plr.Name));
                                    return;
                                }

                                if (TShock.Users.Buy(args.Player.Name, 3))
                                {
                                    args.Player.SendMessage("You spent 3 RCoins.", Color.BlanchedAlmond);
                                    args.Player.SendMessage(string.Format("Teleported to {0}", plr.Name));
                                }
                            }
                        }
                        else
                        {
                            args.Player.SendMessage("Player <" + plr.Name + "> is hidden from the teleport ", Color.Red);
                        }
                    }
                }
            }
            else
            {
                args.Player.SendMessage("You need 3 RCoins to use teleport!", Color.Red);
            }
        }

		private static void TPHere(CommandArgs args)
		{
			if (!args.Player.RealPlayer)
			{
				args.Player.SendMessage("You cannot use teleport commands!");
				return;
			}

			if (args.Parameters.Count < 1)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /tphere <player> ", Color.Red);
				return;
			}

			string plStr = String.Join(" ", args.Parameters);

			if (plStr == "all" || plStr == "*")
			{
				args.Player.SendMessage(string.Format("You brought all players here."));
				for (int i = 0; i < Main.maxPlayers; i++)
				{
					if (Main.player[i].active && (Main.player[i] != args.TPlayer))
					{
						if (TShock.Players[i].Teleport(args.Player.TileX, args.Player.TileY + 3))
							TShock.Players[i].SendMessage(string.Format("You were teleported to {0}.", args.Player.Name));
					}
				}
				return;
			}

			var players = TShock.Utils.FindPlayer(plStr);
			if (players.Count == 0)
			{
				args.Player.SendMessage("Invalid player!", Color.Red);
			}
			else if (players.Count > 1)
			{
				args.Player.SendMessage("More than one player matched!", Color.Red);
			}
			else
			{
				var plr = players[0];
				if (plr.Teleport(args.Player.TileX, args.Player.TileY + 3))
				{
					plr.SendMessage(string.Format("You were teleported to {0}.", args.Player.Name));
					args.Player.SendMessage(string.Format("You brought {0} here.", plr.Name));
				}
			}
		}

		private static void TPAllow(CommandArgs args)
		{
            if (!args.Player.TPAllow)
            {
                args.Player.SendMessage("Other Players Can Now Teleport To You");
                args.Player.TPAllow = true;
            }
            else
            {
                args.Player.SendMessage("Other Players Can No Longer Teleport To You");
                args.Player.TPAllow = false;
            }
		}

		private static void SendWarp(CommandArgs args)
		{
			if (args.Parameters.Count < 2)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /sendwarp [player] [warpname]", Color.Red);
				return;
			}

			var foundplr = TShock.Utils.FindPlayer(args.Parameters[0]);
			if (foundplr.Count == 0)
			{
				args.Player.SendMessage("Invalid player!", Color.Red);
				return;
			}
			else if (foundplr.Count > 1)
			{
				args.Player.SendMessage(string.Format("More than one ({0}) player matched!", args.Parameters.Count), Color.Red);
				return;
			}
			string warpName = String.Join(" ", args.Parameters[1]);
			var warp = TShock.Warps.FindWarp(warpName);
			var plr = foundplr[0];
			if (warp.WarpPos != Vector2.Zero)
			{
				if (plr.Teleport((int) warp.WarpPos.X, (int) warp.WarpPos.Y + 3))
				{
					plr.SendMessage(string.Format("{0} Warped you to {1}", args.Player.Name, warpName), Color.Yellow);
					args.Player.SendMessage(string.Format("You warped {0} to {1}.", plr.Name, warpName), Color.Yellow);
				}
			}
			else
			{
				args.Player.SendMessage("Specified warp not found", Color.Red);
			}
		}

		private static void SetWarp(CommandArgs args)
		{
			if (args.Parameters.Count > 0)
			{
				string warpName = String.Join(" ", args.Parameters);
				if (warpName.Equals("list"))
				{
					args.Player.SendMessage("Name reserved, use a different name", Color.Red);
				}
				else if (TShock.Warps.AddWarp(args.Player.TileX, args.Player.TileY, warpName, Main.worldID.ToString()))
				{
					args.Player.SendMessage("Set warp " + warpName, Color.Yellow);
				}
				else
				{
					args.Player.SendMessage("Warp " + warpName + " already exists", Color.Red);
				}
			}
			else
				args.Player.SendMessage("Invalid syntax! Proper syntax: /setwarp [name]", Color.Red);
		}

		private static void DeleteWarp(CommandArgs args)
		{
			if (args.Parameters.Count > 0)
			{
				string warpName = String.Join(" ", args.Parameters);
				if (TShock.Warps.RemoveWarp(warpName))
					args.Player.SendMessage("Deleted warp " + warpName, Color.Yellow);
				else
					args.Player.SendMessage("Could not find specified warp", Color.Red);
			}
			else
				args.Player.SendMessage("Invalid syntax! Proper syntax: /delwarp [name]", Color.Red);
		}

		private static void HideWarp(CommandArgs args)
		{
			if (args.Parameters.Count > 1)
			{
				string warpName = String.Join(" ", args.Parameters);
				bool state = false;
				if (Boolean.TryParse(args.Parameters[1], out state))
				{
					if (TShock.Warps.HideWarp(args.Parameters[0], state))
					{
						if (state)
							args.Player.SendMessage("Made warp " + warpName + " private", Color.Yellow);
						else
							args.Player.SendMessage("Made warp " + warpName + " public", Color.Yellow);
					}
					else
						args.Player.SendMessage("Could not find specified warp", Color.Red);
				}
				else
					args.Player.SendMessage("Invalid syntax! Proper syntax: /hidewarp [name] <true/false>", Color.Red);
			}
			else
				args.Player.SendMessage("Invalid syntax! Proper syntax: /hidewarp [name] <true/false>", Color.Red);
		}

		private static void UseWarp(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /warp [name] or /warp list <page>", Color.Red);
				return;
			}

			if (args.Parameters[0].Equals("list"))
			{
				//How many warps per page
				const int pagelimit = 15;
				//How many warps per line
				const int perline = 5;
				//Pages start at 0 but are displayed and parsed at 1
				int page = 0;


				if (args.Parameters.Count > 1)
				{
					if (!int.TryParse(args.Parameters[1], out page) || page < 1)
					{
						args.Player.SendMessage(string.Format("Invalid page number ({0})", page), Color.Red);
						return;
					}
					page--; //Substract 1 as pages are parsed starting at 1 and not 0
				}

				var warps = TShock.Warps.ListAllPublicWarps(Main.worldID.ToString());

				//Check if they are trying to access a page that doesn't exist.
				int pagecount = warps.Count/pagelimit;
				if (page > pagecount)
				{
					args.Player.SendMessage(string.Format("Page number exceeds pages ({0}/{1})", page + 1, pagecount + 1), Color.Red);
					return;
				}

				//Display the current page and the number of pages.
				args.Player.SendMessage(string.Format("Current Warps ({0}/{1}):", page + 1, pagecount + 1), Color.Green);

				//Add up to pagelimit names to a list
				var nameslist = new List<string>();
				for (int i = (page*pagelimit); (i < ((page*pagelimit) + pagelimit)) && i < warps.Count; i++)
				{
					nameslist.Add(warps[i].WarpName);
				}

				//convert the list to an array for joining
				var names = nameslist.ToArray();
				for (int i = 0; i < names.Length; i += perline)
				{
					args.Player.SendMessage(string.Join(", ", names, i, Math.Min(names.Length - i, perline)), Color.Yellow);
				}

				if (page < pagecount)
				{
					args.Player.SendMessage(string.Format("Type /warp list {0} for more warps.", (page + 2)), Color.Yellow);
				}
			}
			else
			{
				string warpName = String.Join(" ", args.Parameters);
				var warp = TShock.Warps.FindWarp(warpName);
				if (warp.WarpPos != Vector2.Zero)
				{
                    if (warpName.ToLower().Contains("pvp"))
                        TShock.Utils.Broadcast(string.Format("{0} teleported to PVP arena and wants to kick your ass!!!", args.Player.Name), Color.SkyBlue);
                    if (args.Player.Teleport((int) warp.WarpPos.X, (int) warp.WarpPos.Y + 3))
						args.Player.SendMessage("Warped to " + warpName, Color.Yellow);
				}
				else
				{
					args.Player.SendMessage("Specified warp not found", Color.Red);
				}
			}
		}

        private static void SetHome(CommandArgs args)
        {
            if (args.Parameters.Count < 0)
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /sethome ", Color.Red);
                return;
            }

            TShock.HomeManager.InsertHome(args.Player.Name, args.Player.TileX, args.Player.TileY);
            args.Player.SendMessage("Home placed successfully!", Color.Yellow);
        }

		#endregion Teleport Commands

		#region Group Management

		private static void AddGroup(CommandArgs args)
		{
			if (args.Parameters.Count > 0)
			{
				String groupname = args.Parameters[0];
				args.Parameters.RemoveAt(0);
				String permissions = String.Join(",", args.Parameters);

				String response = TShock.Groups.AddGroup(groupname, permissions);
				if (response.Length > 0)
					args.Player.SendMessage(response, Color.Green);
			}
			else
			{
				args.Player.SendMessage("Incorrect format: /addGroup <group name> [optional permissions]", Color.Red);
			}
		}

		private static void DeleteGroup(CommandArgs args)
		{
			if (args.Parameters.Count > 0)
			{
				String groupname = args.Parameters[0];

				String response = TShock.Groups.DeleteGroup(groupname);
				if (response.Length > 0)
					args.Player.SendMessage(response, Color.Green);
			}
			else
			{
				args.Player.SendMessage("Incorrect format: /delGroup <group name>", Color.Red);
			}
		}

		private static void ModifyGroup(CommandArgs args)
		{
			if (args.Parameters.Count > 2)
			{
				String com = args.Parameters[0];
				args.Parameters.RemoveAt(0);

				String groupname = args.Parameters[0];
				args.Parameters.RemoveAt(0);

				if (com.Equals("add"))
				{
					String response = TShock.Groups.AddPermissions(groupname, args.Parameters);
					if (response.Length > 0)
						args.Player.SendMessage(response, Color.Green);
					return;
				}
				else if (com.Equals("del") || com.Equals("delete"))
				{
					String response = TShock.Groups.DeletePermissions(groupname, args.Parameters);
					if (response.Length > 0)
						args.Player.SendMessage(response, Color.Green);
					return;
				}
			}
			args.Player.SendMessage("Incorrect format: /modGroup add|del <group name> <permission to add or remove>", Color.Red);
		}

		#endregion Group Management

		#region Item Management

		private static void AddItem(CommandArgs args)
		{
			if (args.Parameters.Count > 0)
			{
				var items = TShock.Utils.GetItemByIdOrName(args.Parameters[0]);
				if (items.Count == 0)
				{
					args.Player.SendMessage("Invalid item type!", Color.Red);
				}
				else if (items.Count > 1)
				{
					args.Player.SendMessage(string.Format("More than one ({0}) item matched!", items.Count), Color.Red);
				}
				else
				{
					var item = items[0];
					if (item.type >= 1)
					{
						TShock.Itembans.AddNewBan(item.name);
						args.Player.SendMessage(item.name + " has been banned.", Color.Green);
					}
					else
					{
						args.Player.SendMessage("Invalid item type!", Color.Red);
					}
				}
			}
			else
			{
				args.Player.SendMessage("Invalid use: /addItem \"item name\" or /addItem ##", Color.Red);
			}
		}

		private static void DeleteItem(CommandArgs args)
		{
			if (args.Parameters.Count > 0)
			{
				var items = TShock.Utils.GetItemByIdOrName(args.Parameters[0]);
				if (items.Count == 0)
				{
					args.Player.SendMessage("Invalid item type!", Color.Red);
				}
				else if (items.Count > 1)
				{
					args.Player.SendMessage(string.Format("More than one ({0}) item matched!", items.Count), Color.Red);
				}
				else
				{
					var item = items[0];
					if (item.type >= 1)
					{
						TShock.Itembans.RemoveBan(item.name);
						args.Player.SendMessage(item.name + " has been unbanned.", Color.Green);
					}
					else
					{
						args.Player.SendMessage("Invalid item type!", Color.Red);
					}
				}
			}
			else
			{
				args.Player.SendMessage("Invalid use: /delItem \"item name\" or /delItem ##", Color.Red);
			}
		}

		#endregion Item Management

		#region Server Config Commands

		private static void SetSpawn(CommandArgs args)
		{
			Main.spawnTileX = args.Player.TileX + 1;
			Main.spawnTileY = args.Player.TileY + 3;

			TShock.Utils.Broadcast("Server map saving, potential lag spike");
			Thread SaveWorld = new Thread(TShock.Utils.SaveWorld);
			SaveWorld.Start();
		}

		private static void Reload(CommandArgs args)
		{
			FileTools.SetupConfig();
			TShock.Groups.LoadPermisions();
			TShock.Regions.ReloadAllRegions();
            TShock.Towns.ReloadAllTowns();
			args.Player.SendMessage(
				"Configuration, Permissions, and Regions reload complete. Some changes may require server restart.");
		}

		private static void ServerPassword(CommandArgs args)
		{
			if (args.Parameters.Count != 1)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /password \"<new password>\"", Color.Red);
				return;
			}
			string passwd = args.Parameters[0];
			TShock.Config.ServerPassword = passwd;
			args.Player.SendMessage(string.Format("Server password changed to: {0}", passwd));
		}

		private static void Save(CommandArgs args)
		{
			TShock.Utils.Broadcast("Server map saving, potential lag spike");
			Thread SaveWorld = new Thread(TShock.Utils.SaveWorld);
			SaveWorld.Start();
		}

		private static void Settle(CommandArgs args)
		{
			if (Liquid.panicMode)
			{
				args.Player.SendMessage("Liquid is already settling!", Color.Red);
				return;
			}
			Liquid.StartPanic();
			TShock.Utils.Broadcast("Settling all liquids...");
		}

		private static void MaxSpawns(CommandArgs args)
		{
			if (args.Parameters.Count != 1)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /maxspawns <maxspawns>", Color.Red);
				return;
			}

			int amount = Convert.ToInt32(args.Parameters[0]);
			int.TryParse(args.Parameters[0], out amount);
			NPC.defaultMaxSpawns = amount;
			TShock.Config.DefaultMaximumSpawns = amount;
			TShock.Utils.Broadcast(string.Format("{0} changed the maximum spawns to: {1}", args.Player.Name, amount));
		}

		private static void SpawnRate(CommandArgs args)
		{
			if (args.Parameters.Count != 1)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /spawnrate <spawnrate>", Color.Red);
				return;
			}

			int amount = Convert.ToInt32(args.Parameters[0]);
			int.TryParse(args.Parameters[0], out amount);
			NPC.defaultSpawnRate = amount;
			TShock.Config.DefaultSpawnRate = amount;
			TShock.Utils.Broadcast(string.Format("{0} changed the spawn rate to: {1}", args.Player.Name, amount));
		}

		#endregion Server Config Commands

		#region Time/PvpFun Commands

		private static void Time(CommandArgs args)
		{
			if (args.Parameters.Count != 1)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /time <day/night/dusk/noon/midnight>", Color.Red);
				return;
			}

			switch (args.Parameters[0])
			{
				case "day":
					TSPlayer.Server.SetTime(true, 150.0);
					TShock.Utils.Broadcast(string.Format("{0} set time to day.", args.Player.Name));
					break;
				case "night":
					TSPlayer.Server.SetTime(false, 0.0);
					TShock.Utils.Broadcast(string.Format("{0} set time to night.", args.Player.Name));
					break;
				case "dusk":
					TSPlayer.Server.SetTime(false, 0.0);
					TShock.Utils.Broadcast(string.Format("{0} set time to dusk.", args.Player.Name));
					break;
				case "noon":
					TSPlayer.Server.SetTime(true, 27000.0);
					TShock.Utils.Broadcast(string.Format("{0} set time to noon.", args.Player.Name));
					break;
				case "midnight":
					TSPlayer.Server.SetTime(false, 16200.0);
					TShock.Utils.Broadcast(string.Format("{0} set time to midnight.", args.Player.Name));
					break;
				default:
					args.Player.SendMessage("Invalid syntax! Proper syntax: /time <day/night/dusk/noon/midnight>", Color.Red);
					break;
			}
		}

		private static void Slap(CommandArgs args)
		{
			if (args.Parameters.Count < 1 || args.Parameters.Count > 2)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /slap <player> [dmg]", Color.Red);
				return;
			}
			if (args.Parameters[0].Length == 0)
			{
				args.Player.SendMessage("Missing player name", Color.Red);
				return;
			}

			string plStr = args.Parameters[0];
			var players = TShock.Utils.FindPlayer(plStr);
			if (players.Count == 0)
			{
				args.Player.SendMessage("Invalid player!", Color.Red);
			}
			else if (players.Count > 1)
			{
				args.Player.SendMessage("More than one player matched!", Color.Red);
			}
			else
			{
				var plr = players[0];
				int damage = 5;
				if (args.Parameters.Count == 2)
				{
					int.TryParse(args.Parameters[1], out damage);
				}
				if (!args.Player.Group.HasPermission(Permissions.kill))
				{
					damage = TShock.Utils.Clamp(damage, 15, 0);
				}
				plr.DamagePlayer(damage);
				TShock.Utils.Broadcast(string.Format("{0} slapped {1} for {2} damage.",
													 args.Player.Name, plr.Name, damage));
				Log.Info(args.Player.Name + " slapped " + plr.Name + " with " + damage + " damage.");
			}
		}

		#endregion Time/PvpFun Commands

		#region World Protection Commands

		private static void ToggleAntiBuild(CommandArgs args)
		{
			TShock.Config.DisableBuild = (TShock.Config.DisableBuild == false);
			TShock.Utils.Broadcast(string.Format("Anti-build is now {0}.", (TShock.Config.DisableBuild ? "on" : "off")));
		}

		private static void ProtectSpawn(CommandArgs args)
		{
			TShock.Config.SpawnProtection = (TShock.Config.SpawnProtection == false);
			TShock.Utils.Broadcast(string.Format("Spawn is now {0}.", (TShock.Config.SpawnProtection ? "protected" : "open")));
		}

		private static void DebugRegions(CommandArgs args)
		{
			foreach (Region r in TShock.Regions.Regions)
			{
				args.Player.SendMessage(r.Name + ": P: " + r.DisableBuild + " X: " + r.Area.X + " Y: " + r.Area.Y + " W: " +
										r.Area.Width + " H: " + r.Area.Height);
				foreach (int s in r.AllowedIDs)
				{
					args.Player.SendMessage(r.Name + ": " + s);
				}
			}
		}

		private static void Region(CommandArgs args)
		{
			string cmd = "help";
            string Owner = string.Empty;
            string RegionName = string.Empty;

			if (args.Parameters.Count > 0)
			{
				cmd = args.Parameters[0].ToLower();
			}
			switch (cmd)
			{
				case "name":
					{
						{
							args.Player.SendMessage("Hit a block to get the name of the region", Color.Yellow);
							args.Player.AwaitingName = true;
						}
						break;
					}
				case "set":
					{
						int choice = 0;
						if (args.Parameters.Count == 2 &&
							int.TryParse(args.Parameters[1], out choice) &&
							choice >= 1 && choice <= 2)
						{
							args.Player.SendMessage("Hit a block to Set Point " + choice, Color.Yellow);
							args.Player.AwaitingTempPoint = choice;
						}
						else
						{
							args.Player.SendMessage("Invalid syntax! Proper syntax: /region set [1/2]", Color.Red);
						}
						break;
					}
				case "define":
					{
						if (args.Parameters.Count > 1)
						{
							if (!args.Player.TempPoints.Any(p => p == Point.Zero))
							{
								string regionName = String.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1));
								var x = Math.Min(args.Player.TempPoints[0].X, args.Player.TempPoints[1].X);
								var y = Math.Min(args.Player.TempPoints[0].Y, args.Player.TempPoints[1].Y);
								var width = Math.Abs(args.Player.TempPoints[0].X - args.Player.TempPoints[1].X);
								var height = Math.Abs(args.Player.TempPoints[0].Y - args.Player.TempPoints[1].Y);

								if (TShock.Regions.AddRegion(x, y, width, height, regionName, args.Player.UserAccountName,
															 Main.worldID.ToString()))
								{
									args.Player.TempPoints[0] = Point.Zero;
									args.Player.TempPoints[1] = Point.Zero;
									args.Player.SendMessage("Set region " + regionName, Color.Yellow);
								}
								else
								{
									args.Player.SendMessage("Region " + regionName + " already exists", Color.Red);
								}
							}
							else
							{
								args.Player.SendMessage("Points not set up yet", Color.Red);
							}
						}
						else
							args.Player.SendMessage("Invalid syntax! Proper syntax: /region define [name]", Color.Red);
						break;
					}
				case "protect":
					{
						if (args.Parameters.Count == 3)
						{
							string regionName = args.Parameters[1];
							if (args.Parameters[2].ToLower() == "true")
							{
								if (TShock.Regions.SetRegionState(regionName, true))
									args.Player.SendMessage("Protected region " + regionName, Color.Yellow);
								else
									args.Player.SendMessage("Could not find specified region", Color.Red);
							}
							else if (args.Parameters[2].ToLower() == "false")
							{
								if (TShock.Regions.SetRegionState(regionName, false))
									args.Player.SendMessage("Unprotected region " + regionName, Color.Yellow);
								else
									args.Player.SendMessage("Could not find specified region", Color.Red);
							}
							else
								args.Player.SendMessage("Invalid syntax! Proper syntax: /region protect [name] [true/false]", Color.Red);
						}
						else
							args.Player.SendMessage("Invalid syntax! Proper syntax: /region protect [name] [true/false]", Color.Red);
						break;
					}
				case "delete":
					{
						if (args.Parameters.Count > 1)
						{
							string regionName = String.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1));
							if (TShock.Regions.DeleteRegion(regionName))
								args.Player.SendMessage("Deleted region " + regionName, Color.Yellow);
							else
								args.Player.SendMessage("Could not find specified region", Color.Red);
						}
						else
							args.Player.SendMessage("Invalid syntax! Proper syntax: /region delete [name]", Color.Red);
						break;
					}
				case "clear":
					{
						args.Player.TempPoints[0] = Point.Zero;
						args.Player.TempPoints[1] = Point.Zero;
						args.Player.SendMessage("Cleared temp area", Color.Yellow);
						args.Player.AwaitingTempPoint = 0;
						break;
					}
				case "allow":
					{
						if (args.Parameters.Count > 2)
						{
							string playerName = args.Parameters[1];
							string regionName = "";

							for (int i = 2; i < args.Parameters.Count; i++)
							{
								if (regionName == "")
								{
									regionName = args.Parameters[2];
								}
								else
								{
									regionName = regionName + " " + args.Parameters[i];
								}
							}
							if (TShock.Users.GetUserByName(playerName) != null)
							{
								if (TShock.Regions.AddNewUser(regionName, playerName))
								{
									args.Player.SendMessage("Added user " + playerName + " to " + regionName, Color.Yellow);
								}
								else
									args.Player.SendMessage("Region " + regionName + " not found", Color.Red);
							}
							else
							{
								args.Player.SendMessage("Player " + playerName + " not found", Color.Red);
							}
						}
						else
							args.Player.SendMessage("Invalid syntax! Proper syntax: /region allow [name] [region]", Color.Red);
						break;
					}
				case "remove":
					if (args.Parameters.Count > 2)
					{
						string playerName = args.Parameters[1];
						string regionName = "";

						for (int i = 2; i < args.Parameters.Count; i++)
						{
							if (regionName == "")
							{
								regionName = args.Parameters[2];
							}
							else
							{
								regionName = regionName + " " + args.Parameters[i];
							}
						}
						if (TShock.Users.GetUserByName(playerName) != null)
						{
							if (TShock.Regions.RemoveUser(regionName, playerName))
							{
								args.Player.SendMessage("Removed user " + playerName + " from " + regionName, Color.Yellow);
							}
							else
								args.Player.SendMessage("Region " + regionName + " not found", Color.Red);
						}
						else
						{
							args.Player.SendMessage("Player " + playerName + " not found", Color.Red);
						}
					}
					else
						args.Player.SendMessage("Invalid syntax! Proper syntax: /region remove [name] [region]", Color.Red);
					break;
				case "allowg":
					{
						if (args.Parameters.Count > 2)
						{
							string group = args.Parameters[1];
							string regionName = "";

							for (int i = 2; i < args.Parameters.Count; i++)
							{
								if (regionName == "")
								{
									regionName = args.Parameters[2];
								}
								else
								{
									regionName = regionName + " " + args.Parameters[i];
								}
							}
							if (TShock.Groups.GroupExists(group))
							{
								if (TShock.Regions.AllowGroup(regionName, group))
								{
									args.Player.SendMessage("Added group " + group + " to " + regionName, Color.Yellow);
								}
								else
									args.Player.SendMessage("Region " + regionName + " not found", Color.Red);
							}
							else
							{
								args.Player.SendMessage("Group " + group + " not found", Color.Red);
							}
						}
						else
							args.Player.SendMessage("Invalid syntax! Proper syntax: /region allow [group] [region]", Color.Red);
						break;
					}
				case "removeg":
					if (args.Parameters.Count > 2)
					{
						string group = args.Parameters[1];
						string regionName = "";

						for (int i = 2; i < args.Parameters.Count; i++)
						{
							if (regionName == "")
							{
								regionName = args.Parameters[2];
							}
							else
							{
								regionName = regionName + " " + args.Parameters[i];
							}
						}
						if (TShock.Groups.GroupExists(group))
						{
							if (TShock.Regions.RemoveGroup(regionName, group))
							{
								args.Player.SendMessage("Removed group " + group + " from " + regionName, Color.Yellow);
							}
							else
								args.Player.SendMessage("Region " + regionName + " not found", Color.Red);
						}
						else
						{
							args.Player.SendMessage("Group " + group + " not found", Color.Red);
						}
					}
					else
						args.Player.SendMessage("Invalid syntax! Proper syntax: /region removeg [group] [region]", Color.Red);
					break;
				case "list":
					{
						//How many regions per page
						const int pagelimit = 15;
						//How many regions per line
						const int perline = 5;
						//Pages start at 0 but are displayed and parsed at 1
						int page = 0;


						if (args.Parameters.Count > 1)
						{
							if (!int.TryParse(args.Parameters[1], out page) || page < 1)
							{
								args.Player.SendMessage(string.Format("Invalid page number ({0})", page), Color.Red);
								return;
							}
							page--; //Substract 1 as pages are parsed starting at 1 and not 0
						}

						var regions = TShock.Regions.ListAllRegions(Main.worldID.ToString());

						// Are there even any regions to display?
						if (regions.Count == 0)
						{
							args.Player.SendMessage("There are currently no regions defined.", Color.Red);
							return;
						}

						//Check if they are trying to access a page that doesn't exist.
						int pagecount = regions.Count/pagelimit;
						if (page > pagecount)
						{
							args.Player.SendMessage(string.Format("Page number exceeds pages ({0}/{1})", page + 1, pagecount + 1), Color.Red);
							return;
						}

						//Display the current page and the number of pages.
						args.Player.SendMessage(string.Format("Current Regions ({0}/{1}):", page + 1, pagecount + 1), Color.Green);

						//Add up to pagelimit names to a list
						var nameslist = new List<string>();
						for (int i = (page*pagelimit); (i < ((page*pagelimit) + pagelimit)) && i < regions.Count; i++)
						{
							nameslist.Add(regions[i].Name);
						}

						//convert the list to an array for joining
						var names = nameslist.ToArray();
						for (int i = 0; i < names.Length; i += perline)
						{
							args.Player.SendMessage(string.Join(", ", names, i, Math.Min(names.Length - i, perline)), Color.Yellow);
						}

						if (page < pagecount)
						{
							args.Player.SendMessage(string.Format("Type /region list {0} for more regions.", (page + 2)), Color.Yellow);
						}

						break;
					}
                case "delowner":
                    {
                        if (args.Parameters.Count > 2)
                        {
                            string playerName = args.Parameters[1];
                            string regionName = "";

                            for (int i = 2; i < args.Parameters.Count; i++)
                            {
                                if (regionName == "")
                                {
                                    regionName = args.Parameters[2];
                                }
                                else
                                {
                                    regionName = regionName + " " + args.Parameters[i];
                                }
                            }
                            if (TShock.Regions.DelCoOwner(regionName, playerName))
                            {
                                args.Player.SendMessage(playerName + " deleted from " + regionName, Color.Yellow);
                            }
                            else
                                args.Player.SendMessage("Region " + regionName + " or owner " + playerName + " not found", Color.Red);
                        }
                        else
                            args.Player.SendMessage("Invalid syntax! Proper syntax: /region delowner [name] [region]", Color.Red);
                        break;
                    }
                case "info":
                    {
                        if (args.Parameters.Count < 2)
                        {
                            if (TShock.Regions.InArea(args.Player.TileX, args.Player.TileY, out RegionName) && TShock.Regions.CanBuild(args.Player.TileX, args.Player.TileY, args.Player, out Owner) || !TShock.Regions.CanBuild(args.Player.TileX, args.Player.TileY, args.Player, out Owner))
                            {
                                args.Player.SendMessage("This region <" + RegionName + "> is protected by " + Owner, Color.Yellow);
                            }
                            else
                                args.Player.SendMessage("Region is not protected", Color.Yellow);
                        }

                        if (args.Parameters.Count >= 2)
                        {
                            args.Player.SendMessage("Invalid syntax! Proper syntax: /region info {region name}", Color.Red);
                        }
                        break;
                    }
				case "resize":
				case "expand":
					{
						if (args.Parameters.Count == 4)
						{
							int direction;
							switch (args.Parameters[2])
							{
								case "u":
								case "up":
									{
										direction = 0;
										break;
									}
								case "r":
								case "right":
									{
										direction = 1;
										break;
									}
								case "d":
								case "down":
									{
										direction = 2;
										break;
									}
								case "l":
								case "left":
									{
										direction = 3;
										break;
									}
								default:
									{
										direction = -1;
										break;
									}
							}
							int addAmount;
							int.TryParse(args.Parameters[3], out addAmount);
							if (TShock.Regions.resizeRegion(args.Parameters[1], addAmount, direction))
							{
								args.Player.SendMessage("Region Resized Successfully!", Color.Yellow);
								TShock.Regions.ReloadAllRegions();
							}
							else
							{
								args.Player.SendMessage("Invalid syntax! Proper syntax: /region resize [regionname] [u/d/l/r] [amount]",
														Color.Red);
							}
						}
						else
						{
							args.Player.SendMessage("Invalid syntax! Proper syntax: /region resize [regionname] [u/d/l/r] [amount]1",
													Color.Red);
						}
						break;
					}
				case "help":
				default:
					{
						args.Player.SendMessage("Avialable region commands:", Color.Green);
						args.Player.SendMessage("/region set [1/2] /region define [name] /region protect [name] [true/false]",
												Color.Yellow);
						args.Player.SendMessage("/region name (provides region name)", Color.Yellow);
						args.Player.SendMessage("/region delete [name] /region clear (temporary region)", Color.Yellow);
						args.Player.SendMessage("/region allow [name] [regionname]", Color.Yellow);
						args.Player.SendMessage("/region resize [regionname] [u/d/l/r] [amount]", Color.Yellow);
						break;
					}
			}
		}

        private static void RegionSet1(CommandArgs args)
        {
            args.Player.SendMessage("Hit a block to Set Point 1", Color.Yellow);
            args.Player.AwaitingTempPoint = 1;
        }

        private static void RegionSet2(CommandArgs args)
        {
            args.Player.SendMessage("Hit a block to Set Point 2", Color.Yellow);
            args.Player.AwaitingTempPoint = 2;
        }

        private static void RegionDefine(CommandArgs args)
        {
            if (args.Parameters.Count > 0)
            {
                string regionName = "";
                if (!args.Player.TempPoints.Any(p => p == Point.Zero))
                {
                    for (int i = 0; i < args.Parameters.Count; i++)
                    {
                        if (regionName == "")
                        {
                            regionName = args.Parameters[0];
                        }
                        else
                        {
                            regionName = regionName + " " + args.Parameters[i];
                        }
                    }
                    var x = Math.Min(args.Player.TempPoints[0].X, args.Player.TempPoints[1].X);
                    var y = Math.Min(args.Player.TempPoints[0].Y, args.Player.TempPoints[1].Y);
                    var width = Math.Abs(args.Player.TempPoints[0].X - args.Player.TempPoints[1].X);
                    var height = Math.Abs(args.Player.TempPoints[0].Y - args.Player.TempPoints[1].Y);

                    if (TShock.Regions.AddRegion(x, y, width, height, regionName, args.Player.UserAccountName,
                                                 Main.worldID.ToString()))
                    {
                        args.Player.TempPoints[0] = Point.Zero;
                        args.Player.TempPoints[1] = Point.Zero;
                        args.Player.SendMessage("Set region " + regionName, Color.Yellow);
                    }
                    else
                    {
                        args.Player.SendMessage("Region " + regionName + " already exists", Color.Red);
                    }
                }
                else
                {
                    args.Player.SendMessage("Points not set up yet", Color.Red);
                }
            }
            else
                args.Player.SendMessage("Invalid syntax! Proper syntax: /rd [name]", Color.Red);

        }

        private static void RegionAllow(CommandArgs args)
        {
            if (args.Parameters.Count > 1)
            {
                string playerName = args.Parameters[0];
                string regionName = "";

                for (int i = 1; i < args.Parameters.Count; i++)
                {
                    if (regionName == "")
                    {
                        regionName = args.Parameters[1];
                    }
                    else
                    {
                        regionName = regionName + " " + args.Parameters[i];
                    }
                }
                if (TShock.Users.GetUserByName(playerName) != null)
                {
                    if (TShock.Regions.AddNewUser(regionName, playerName))
                    {
                        args.Player.SendMessage("Added user " + playerName + " to " + regionName + " region.", Color.Yellow);
                    }
                    else
                        args.Player.SendMessage("Region " + regionName + " not found", Color.Red);
                }
                else
                {
                    args.Player.SendMessage("Player " + playerName + " not found", Color.Red);
                }
            }
            else
                args.Player.SendMessage("Invalid syntax! Proper syntax: /ra [name] [region]", Color.Red);
        }

        private static void RegionDelCoOwner(CommandArgs args)
        {
            if (args.Parameters.Count > 1)
            {
                string playerName = args.Parameters[0];
                string regionName = "";

                for (int i = 1; i < args.Parameters.Count; i++)
                {
                    if (regionName == "")
                    {
                        regionName = args.Parameters[1];
                    }
                    else
                    {
                        regionName = regionName + " " + args.Parameters[i];
                    }
                }
                    if (TShock.Regions.DelCoOwner(regionName, playerName))
                    {
                        args.Player.SendMessage(playerName + " deleted from " + regionName, Color.Yellow);
                    }
                    else
                        args.Player.SendMessage("Region " + regionName + " or user " + playerName + " not found", Color.Red);
            }
            else
                args.Player.SendMessage("Invalid syntax! Proper syntax: /rdeluser [name] [region]", Color.Red);
        }

        private static void RegionDelete(CommandArgs args)
        {
            string regionName = string.Empty;
            if (args.Parameters.Count > 0)
            {
                for (int i = 0; i < args.Parameters.Count; i++)
                {
                    if (regionName == "")
                    {
                        regionName = args.Parameters[0];
                    }
                    else
                    {
                        regionName = regionName + " " + args.Parameters[i];
                    }
                }
                    if (TShock.Regions.DeleteRegion(regionName))
                        args.Player.SendMessage("Deleted region " + regionName, Color.Yellow);
                    else
                        args.Player.SendMessage("Could not find specified region", Color.Red);
            }
            else
                args.Player.SendMessage("Invalid syntax! Proper syntax: /rdel [name]", Color.Red);
        }

        private static void RegionInfo(CommandArgs args)
        {
            string CoOwner = string.Empty;
            string RegionName = string.Empty;

            if (TShock.Regions.InArea(args.Player.TileX, args.Player.TileY, out RegionName) && TShock.Regions.CanBuild(args.Player.TileX, args.Player.TileY, args.Player, out CoOwner) || !TShock.Regions.CanBuild(args.Player.TileX, args.Player.TileY, args.Player, out CoOwner))
            {
                args.Player.SendMessage("This region <" + RegionName + "> is protected by " + CoOwner, Color.Yellow);
            }
            else
                args.Player.SendMessage("Region is not protected", Color.Yellow);
        }

        
        private static void HomeSet1(CommandArgs args)
        {
            if (!args.Player.Group.HasPermission(Permissions.manageregion) && !TShock.Towns.MayorCheck(args.Player))
            {
                args.Player.SendMessage("You are not the mayor!", Color.Red);
                return;
            }
            args.Player.SendMessage("Hit a block to Set Point 1", Color.Yellow);
            args.Player.AwaitingTempPoint = 1;
        }

        private static void HomeSet2(CommandArgs args)
        {
            if (!args.Player.Group.HasPermission(Permissions.manageregion) && !TShock.Towns.MayorCheck(args.Player))
            {
                args.Player.SendMessage("You are not the mayor!", Color.Red);
                return;
            }
            args.Player.SendMessage("Hit a block to Set Point 2", Color.Yellow);
            args.Player.AwaitingTempPoint = 2;
        }

        private static void HomeDefine(CommandArgs args)
        {
            string TownName = string.Empty;

            if (!args.Player.Group.HasPermission(Permissions.manageregion) && !TShock.Towns.MayorCheck(args.Player))
            {
                args.Player.SendMessage("You are not the mayor!", Color.Red);
                return;
            }

            if (args.Parameters.Count > 0)
            {
                string regionName = "";
                if (!args.Player.TempPoints.Any(p => p == Point.Zero))
                {
                    for (int i = 0; i < args.Parameters.Count; i++)
                    {
                        if (regionName == "")
                        {
                            regionName = args.Parameters[0];
                        }
                        else
                        {
                            regionName = regionName + " " + args.Parameters[i];
                        }
                    }
                    var x = Math.Min(args.Player.TempPoints[0].X, args.Player.TempPoints[1].X);
                    var y = Math.Min(args.Player.TempPoints[0].Y, args.Player.TempPoints[1].Y);
                    var width = Math.Abs(args.Player.TempPoints[0].X - args.Player.TempPoints[1].X);
                    var height = Math.Abs(args.Player.TempPoints[0].Y - args.Player.TempPoints[1].Y);
                    if (TShock.Towns.InArea(x, y, out TownName))
                    {
                        var town = TShock.Towns.GetTownByName(TownName);
                        if (TShock.Regions.AddRegion(x, y, width, height, regionName, town.Mayor,
                                                     Main.worldID.ToString()))
                        {
                            args.Player.TempPoints[0] = Point.Zero;
                            args.Player.TempPoints[1] = Point.Zero;
                            args.Player.SendMessage("Set region " + regionName, Color.Yellow);
                        }
                        else
                        {
                            args.Player.SendMessage("Region " + regionName + " already exists", Color.Red);
                        }
                    }
                    else
                    {
                        args.Player.SendMessage("You can select regions only within the town.", Color.Red);
                    }
                }
                else
                {
                    args.Player.SendMessage("Points not set up yet", Color.Red);
                }
            }
            else
                args.Player.SendMessage("Invalid syntax! Proper syntax: /hd [name]", Color.Red);

        }

        private static void HomeAllow(CommandArgs args)
        {
            if (!args.Player.Group.HasPermission(Permissions.manageregion) && !TShock.Towns.MayorCheck(args.Player))
            {
                args.Player.SendMessage("You are not the mayor!", Color.Red);
                return;
            }

            if (args.Parameters.Count > 1)
            {
                string playerName = args.Parameters[0];
                string regionName = "";
                string TownName = string.Empty;

                for (int i = 1; i < args.Parameters.Count; i++)
                {
                    if (regionName == "")
                    {
                        regionName = args.Parameters[1];
                    }
                    else
                    {
                        regionName = regionName + " " + args.Parameters[i];
                    }
                }
                var Region = TShock.Regions.GetRegionByName(regionName);

                if (playerName.ToLower().Equals("all") || playerName.ToLower().Equals("*"))
                {
                    if (Region.Owner.Equals(args.Player.Name) || args.Player.Group.HasPermission(Permissions.manageregion))
                    {
                        if (TShock.Towns.InArea(Region.Area.X, Region.Area.Y, out TownName))
                        {
                            foreach (string s in TShock.Towns.GetTownsPeople(TownName))
                            {
                                if (TShock.Regions.AddNewUser(regionName, s))
                                {
                                }
                                else
                                {
                                    args.Player.SendMessage("Region " + regionName + " not found", Color.Red);
                                    return;
                                }
                            }
                            args.Player.SendMessage("Added users to " + regionName + " region successfully!", Color.Green);
                        }
                        else
                        {
                            args.Player.SendMessage("Region " + regionName + " is not in the city!", Color.Red);
                        }
                    }
                    else
                    {
                        args.Player.SendMessage("Only the mayor " + Region.Owner + " can manage this region.", Color.Red);
                    }
                    return;
                }
                else
                {
                    if (TShock.Users.GetUserByName(playerName) != null)
                    {
                        if (Region.Owner.Equals(args.Player.Name) || args.Player.Group.HasPermission(Permissions.manageregion))
                        {
                            if (TShock.Regions.AddNewUser(regionName, playerName))
                            {
                                args.Player.SendMessage("Added user " + playerName + " to " + regionName + " region.", Color.Yellow);
                            }
                            else
                                args.Player.SendMessage("Region " + regionName + " not found", Color.Red);
                        }
                        else
                        {
                            args.Player.SendMessage("Only the mayor " + Region.Owner + " can manage this region.", Color.Red);
                        }
                    }
                    else
                    {
                        args.Player.SendMessage("Player " + playerName + " not found", Color.Red);
                    }
                }
            }
            else
                args.Player.SendMessage("Invalid syntax! Proper syntax: /ha [name] [region]", Color.Red);
        }
        
		private static void HomeDelCoOwner(CommandArgs args)
        {
            string Mayor = string.Empty;
            string TownName = string.Empty;

            if (!args.Player.Group.HasPermission(Permissions.manageregion) && !TShock.Towns.MayorCheck(args.Player))
            {
                args.Player.SendMessage("You are not the mayor!", Color.Red);
                return;
            }

            if (args.Parameters.Count > 1)
            {
                string playerName = args.Parameters[0];
                string regionName = "";

                for (int i = 1; i < args.Parameters.Count; i++)
                {
                    if (regionName == "")
                    {
                        regionName = args.Parameters[1];
                    }
                    else
                    {
                        regionName = regionName + " " + args.Parameters[i];
                    }
                }
                var Region = TShock.Regions.GetRegionByName(regionName);
                if (Region.Owner.Equals(args.Player.Name) || args.Player.Group.HasPermission(Permissions.manageregion))
                {
                    if (playerName.ToLower().Equals("all") || playerName.ToLower().Equals("*"))
                    {
                        if (TShock.Regions.DelAllCoOwners(regionName))
                        {
                            args.Player.SendMessage("All players deleted from " + regionName + " region.", Color.Yellow);
                        }
                        else
                            args.Player.SendMessage("Region " + regionName + " not found", Color.Red);
                    }
                    else
                    {
                        if (TShock.Regions.DelCoOwner(regionName, playerName))
                        {
                            args.Player.SendMessage(playerName + " deleted from " + regionName + " region.", Color.Yellow);
                        }
                        else
                            args.Player.SendMessage("Region " + regionName + " or user " + playerName + " not found", Color.Red);
                    }
                }
                else
                {
                    args.Player.SendMessage("Only the mayor " + Region.Owner + " can manage this region.", Color.Red);
                }
            }
            else
                args.Player.SendMessage("Invalid syntax! Proper syntax: /hdeluser [name] [region]", Color.Red);
        }
        
		private static void HomeDelete(CommandArgs args)
        {
            string Mayor = string.Empty;
            string TownName = string.Empty;
            string regionName = string.Empty;

            if (!args.Player.Group.HasPermission(Permissions.manageregion) && !TShock.Towns.MayorCheck(args.Player))
            {
                args.Player.SendMessage("You are not the mayor!", Color.Red);
                return;
            }

            if (args.Parameters.Count > 0)
            {
                for (int i = 0; i < args.Parameters.Count; i++)
                {
                    if (regionName == "")
                    {
                        regionName = args.Parameters[0];
                    }
                    else
                    {
                        regionName = regionName + " " + args.Parameters[i];
                    }
                }

                var Region = TShock.Regions.GetRegionByName(regionName);
                if (Region.Owner.Equals(args.Player.Name) || args.Player.Group.HasPermission(Permissions.manageregion))
                {
                    if (TShock.Regions.DeleteRegion(regionName))
                        args.Player.SendMessage("Deleted region " + regionName, Color.Yellow);
                    else
                        args.Player.SendMessage("Could not find specified region", Color.Red);
                }
                else
                    args.Player.SendMessage("Only the mayor " + Region.Owner + " can manage this region.", Color.Red);
            }
            else
                args.Player.SendMessage("Invalid syntax! Proper syntax: /hdel [name]", Color.Red);
        }
		
		private static void HomeInfo(CommandArgs args)
        {
            string CoOwner = string.Empty;
            string RegionName = string.Empty;

            if (TShock.Regions.InArea(args.Player.TileX, args.Player.TileY, out RegionName) && TShock.Regions.CanBuild(args.Player.TileX, args.Player.TileY, args.Player, out CoOwner) || !TShock.Regions.CanBuild(args.Player.TileX, args.Player.TileY, args.Player, out CoOwner))
            {
                args.Player.SendMessage("This region <" + RegionName + "> is protected by " + CoOwner, Color.Yellow);
            }
            else
                args.Player.SendMessage("Region is not protected", Color.Yellow);
        }


        private static void TownSet1(CommandArgs args)
        {
            args.Player.SendMessage("[Town] Hit a block to Set Point 1", Color.Yellow);
            args.Player.AwaitingTempPoint = 1;
        }

        private static void TownSet2(CommandArgs args)
        {
            args.Player.SendMessage("[Town] Hit a block to Set Point 2", Color.Yellow);
            args.Player.AwaitingTempPoint = 2;
        }

        private static void TownDefine(CommandArgs args)
        {
            if (args.Parameters.Count > 0)
            {
                string townname = "";
                if (!args.Player.TempPoints.Any(p => p == Point.Zero))
                {
                    for (int i = 0; i < args.Parameters.Count; i++)
                    {
                        if (townname == "")
                        {
                            townname = args.Parameters[0];
                        }
                        else
                        {
                            townname = townname + " " + args.Parameters[i];
                        }
                    }
                    var x = Math.Min(args.Player.TempPoints[0].X, args.Player.TempPoints[1].X);
                    var y = Math.Min(args.Player.TempPoints[0].Y, args.Player.TempPoints[1].Y);
                    var width = Math.Abs(args.Player.TempPoints[0].X - args.Player.TempPoints[1].X);
                    var height = Math.Abs(args.Player.TempPoints[0].Y - args.Player.TempPoints[1].Y);

                    if (TShock.Towns.AddTown(x, y, width, height, townname, args.Player.Name,
                                                 Main.worldID.ToString()))
                    {
                        args.Player.TempPoints[0] = Point.Zero;
                        args.Player.TempPoints[1] = Point.Zero;
                        args.Player.SendMessage("Set town " + townname, Color.Yellow);
                    }
                    else
                    {
                        args.Player.SendMessage("Town " + townname + " already exists", Color.Red);
                    }
                }
                else
                {
                    args.Player.SendMessage("Points not set up yet", Color.Red);
                }
            }
            else
                args.Player.SendMessage("Invalid syntax! Proper syntax: /td [name]", Color.Red);

        }

        private static void TownChangeMayor(CommandArgs args)
        {
            if (args.Parameters.Count > 1)
            {
                string playerName = args.Parameters[0];
                string townname = "";

                for (int i = 1; i < args.Parameters.Count; i++)
                {
                    if (townname == "")
                    {
                        townname = args.Parameters[1];
                    }
                    else
                    {
                        townname = townname + " " + args.Parameters[i];
                    }
                }
                if (TShock.Users.GetUserByName(playerName) != null)
                {
                    if (TShock.Towns.ChangeMayor(townname, playerName))
                    {
                        var town = TShock.Towns.GetTownByName(townname);
                        args.Player.SendMessage("Added new mayor " + playerName + " in " + townname, Color.Yellow);
                        foreach (Region r in TShock.Regions.Regions)
                        {
                            if (town.InArea(r.Area))
                            {
                                TShock.Regions.ChangeOwner(r.Name, playerName);
                            }
                        }
                        TShock.Regions.ReloadAllRegions();
                    }
                    else
                        args.Player.SendMessage("Town " + townname + " not found", Color.Red);
                }
                else
                {
                    args.Player.SendMessage("Player " + playerName + " not found", Color.Red);
                }
            }
            else
                args.Player.SendMessage("Invalid syntax! Proper syntax: /tcm [name] [town]", Color.Red);
        }

        private static void TownDelete(CommandArgs args)
        {
            string Mayor = string.Empty;
            string townname = string.Empty;
            if (args.Parameters.Count > 0)
            {
                for (int i = 0; i < args.Parameters.Count; i++)
                {
                    if (townname == "")
                    {
                        townname = args.Parameters[0];
                    }
                    else
                    {
                        townname = townname + " " + args.Parameters[i];
                    }
                }
                if (TShock.Towns.DeleteTown(townname))
                {
                    args.Player.SendMessage("Deleted town " + townname, Color.Yellow);
                    TShock.Towns.ReloadAllTowns();
                }
                else
                    args.Player.SendMessage("Could not find specified town", Color.Red);
            }
            else
                args.Player.SendMessage("Invalid syntax! Proper syntax: /tdel [name]", Color.Red);
        }

        private static void TownTell(CommandArgs args)
        {
            string TownName = string.Empty;
            string TownsPeople = string.Empty;
            string Message = string.Empty;
            TSPlayer plr;

            if (!args.Player.Group.HasPermission(Permissions.manageregion) && !TShock.Towns.MayorCheck(args.Player))
            {
                args.Player.SendMessage("You are not the mayor!", Color.Red);
                return;
            }

            if (args.Parameters.Count > 0)
            {
                for (int i = 0; i < args.Parameters.Count; i++)
                {
                    if (Message == "")
                    {
                        Message = args.Parameters[0];
                    }
                    else
                    {
                        Message = Message + " " + args.Parameters[i];
                    }
                }
                var Town = TShock.Towns.GetTownByMayorName(args.Player.Name);
                
                foreach (string s in TShock.Towns.GetTownsPeople(Town.Name))
                {
                        var players = TShock.Utils.FindPlayer(s);
                        if (players.Count > 0)
                        {
                            plr = players[0];
                            plr.SendMessage("<Mayor " + args.Player.Name + "> " + Message, Color.MediumSlateBlue);
                        }
                }
                args.Player.SendMessage("<Mayor " + args.Player.Name + "> " + Message, Color.MediumSlateBlue);
            }
            else
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /tt [text]", Color.Red);
            }
        }

        private static void TownInfo(CommandArgs args)
        {
            string Mayor = string.Empty;
            string TownName = string.Empty;
            string TownsPeople = string.Empty;

            if (TShock.Towns.InArea(args.Player.TileX, args.Player.TileY, out TownName))
            {
                args.Player.SendMessage("This town <" + TownName + "> is protected by mayor <" + TShock.Towns.GetMayor(TownName) + ">", Color.MediumSlateBlue);
                foreach (string s in TShock.Towns.GetTownsPeople(TownName))
                {
                    TownsPeople = TownsPeople + " " + s;
                }
                TownsPeople = TownsPeople.Remove(0, 1);
                args.Player.SendMessage("Residents of the town: " + TownsPeople, Color.MediumSlateBlue);
            }
            else
                args.Player.SendMessage("There are no towns here!", Color.Yellow);
        }



        private static void AltarEdit(CommandArgs args)
        {
            if (!args.Player.Group.HasPermission(Permissions.altaredit))
            {
                args.Player.Group = TShock.Utils.GetGroup("editor");
                args.Player.SendMessage("Now you can destroy altars", Color.Yellow);
            }
            else
            {
                args.Player.Group = TShock.Utils.GetGroup("trustedadmin");
                args.Player.SendMessage("Now you can't destroy altars", Color.Green);
            }
        }

        private static void AltarTimer(CommandArgs args)
        {
            TShock.DispenserTime.Remove(args.Player.Name + ";" + Convert.ToString(TShock.Utils.DispencerTime(args.Player.Name)));
            TShock.DispenserTime.Add(args.Player.Name + ";" + Convert.ToString(DateTime.UtcNow.AddMilliseconds(-TShock.disptime)));
            TShock.Spawner = DateTime.UtcNow.AddMinutes(-30);
            args.Player.SendMessage("Altar timers reset successfull.", Color.Green);
        }

        private static void Location(CommandArgs args)
        {
            args.Player.SendMessage("X = " + args.Player.TileX + "; Y = " + args.Player.TileY, Color.Yellow);
            foreach (TSPlayer Player in TShock.Players)
            {
                if (Player != null && Player.Active && Player.Group.HasPermission("adminstatus"))
                {
                    Player.SendMessage("(Location)<{0}> {1}".SFormat(args.Player.Name, "X = " + args.Player.TileX + "; Y = " + args.Player.TileY), Color.PaleGreen);
                }
            }
        }

		#endregion World Protection Commands

		#region General Commands

		private static void Help(CommandArgs args)
		{
			args.Player.SendMessage("TShock Commands:");
			int page = 1;
			if (args.Parameters.Count > 0)
				int.TryParse(args.Parameters[0], out page);
			var cmdlist = new List<Command>();
			for (int j = 0; j < ChatCommands.Count; j++)
			{
				if (ChatCommands[j].CanRun(args.Player))
				{
					cmdlist.Add(ChatCommands[j]);
				}
			}
			var sb = new StringBuilder();
			if (cmdlist.Count > (15*(page - 1)))
			{
				for (int j = (15*(page - 1)); j < (15*page); j++)
				{
					if (sb.Length != 0)
						sb.Append(", ");
					sb.Append("/").Append(cmdlist[j].Name);
					if (j == cmdlist.Count - 1)
					{
						args.Player.SendMessage(sb.ToString(), Color.Yellow);
						break;
					}
					if ((j + 1)%5 == 0)
					{
						args.Player.SendMessage(sb.ToString(), Color.Yellow);
						sb.Clear();
					}
				}
			}
			if (cmdlist.Count > (15*page))
			{
				args.Player.SendMessage(string.Format("Type /help {0} for more commands.", (page + 1)), Color.Yellow);
			}
		}

		private static void Playing(CommandArgs args)
		{
            {
                int count = 0;
                string Admins = String.Empty;
                string Players = String.Empty;
                string Vips = String.Empty;
                foreach (TSPlayer player in TShock.Players)
                {
                    if (player != null && player.Active)
                    {
                        count++;
                        if (player.Group.HasPermission(Permissions.adminstatus))
                        {
                            Admins = string.Format("{0}, {1}", Admins, player.Name);
                        }
                        else
                        {
                            if (player.Group.HasPermission(Permissions.vipstatus))
                            {
                                Vips = string.Format("{0}, {1}", Vips, player.Name);
                            }
                            else
                                Players = string.Format("{0}, {1}", Players, player.Name);
                        }
                    }
                }
                if (Players.Length > 1)
                    args.Player.SendMessage(string.Format("Current players: {0}.", Players.Remove(0, 1)), 255, 240, 20);
                if (Vips.Length > 1)
                    args.Player.SendMessage(string.Format("Current vips: {0}.", Vips.Remove(0, 1)), Color.LightGreen);
                if (Admins.Length > 1)
                    args.Player.SendMessage(string.Format("Current admins: {0}.", Admins.Remove(0, 1)), 0, 192, 255);
                args.Player.SendMessage(string.Format("Total online players: {0}.", count), 255, 240, 20);
            }
		}

		private static void AuthToken(CommandArgs args)
		{
			if (TShock.AuthToken == 0)
			{
				args.Player.SendMessage("Auth is disabled. This incident has been logged.", Color.Red);
				Log.Warn(args.Player.IP + " attempted to use /auth even though it's disabled.");
				return;
			}
			int givenCode = Convert.ToInt32(args.Parameters[0]);
			if (givenCode == TShock.AuthToken && args.Player.Group.Name != "superadmin")
			{
				try
				{
					TShock.Users.AddUser(new User(args.Player.IP, "", "", "superadmin", DateTime.Now, 0, 0));
					args.Player.Group = TShock.Utils.GetGroup("superadmin");
					args.Player.SendMessage("This IP address is now superadmin. Please perform the following command:");
					args.Player.SendMessage("/user add <username>:<password> superadmin");
					args.Player.SendMessage("Creates: <username> with the password <password> as part of the superadmin group.");
					args.Player.SendMessage("Please use /login <username> <password> to login from now on.");
					args.Player.SendMessage("If you understand, please /login <username> <password> now, and type /auth-verify");
				}
				catch (UserManagerException ex)
				{
					Log.ConsoleError(ex.ToString());
					args.Player.SendMessage(ex.Message);
				}
				return;
			}

			if (args.Player.Group.Name == "superadmin")
			{
				args.Player.SendMessage("Please disable the auth system! If you need help, consult the forums. http://tshock.co/");
				args.Player.SendMessage("This IP address is now superadmin. Please perform the following command:");
				args.Player.SendMessage("/user add <username>:<password> superadmin");
				args.Player.SendMessage("Creates: <username> with the password <password> as part of the superadmin group.");
				args.Player.SendMessage("Please use /login <username> <password> to login from now on.");
				args.Player.SendMessage("If you understand, please /login <username> <password> now, and type /auth-verify");
				return;
			}

			args.Player.SendMessage("Incorrect auth code. This incident has been logged.");
			Log.Warn(args.Player.IP + " attempted to use an incorrect auth code.");
		}

		private static void AuthVerify(CommandArgs args)
		{
			if (TShock.AuthToken == 0)
			{
				args.Player.SendMessage("It appears that you have already turned off the auth token.");
				args.Player.SendMessage("If this is a mistake, delete auth.lck.");
				return;
			}

			if (!args.Player.IsLoggedIn)
			{
				args.Player.SendMessage("You must be logged in to disable the auth system.");
				args.Player.SendMessage("This is a security measure designed to prevent insecure administration setups.");
				args.Player.SendMessage("Please re-run /auth and read the instructions!");
				args.Player.SendMessage("If you're still confused, consult the forums. http://tshock.co/");
				return;
			}

			args.Player.SendMessage("Your new account has been verified, and the /auth system has been turned off.");
			args.Player.SendMessage("You can always use the /user command to manage players. Don't just delete the auth.lck.");
			args.Player.SendMessage("Thankyou for using TShock! http://tshock.co/ & http://github.com/TShock/TShock");
			FileTools.CreateFile(Path.Combine(TShock.SavePath, "auth.lck"));
			File.Delete(Path.Combine(TShock.SavePath, "authcode.txt"));
			TShock.AuthToken = 0;
		}

		private static void ThirdPerson(CommandArgs args)
		{
			if (args.Parameters.Count == 0)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /me <text>", Color.Red);
				return;
			}
			if (args.Player.mute)
				args.Player.SendMessage("You are muted.");
			else
				TShock.Utils.Broadcast(string.Format("*{0} {1}", args.Player.Name, String.Join(" ", args.Parameters)), 205, 133, 63);
		}

		private static void PartyChat(CommandArgs args)
		{
			if (args.Parameters.Count == 0)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /p <team chat text>", Color.Red);
				return;
			}
			int playerTeam = args.Player.Team;

			if (args.Player.mute)
				args.Player.SendMessage("You are muted.");
			else if (playerTeam != 0)
			{
				string msg = string.Format("<{0}> {1}", args.Player.Name, String.Join(" ", args.Parameters));
				foreach (TSPlayer player in TShock.Players)
				{
					if (player != null && player.Active && player.Team == playerTeam)
						player.SendMessage(msg, Main.teamColor[playerTeam].R, Main.teamColor[playerTeam].G, Main.teamColor[playerTeam].B);
				}
			}
			else
				args.Player.SendMessage("You are not in a party!", 255, 240, 20);
		}

        private static void AdminChat(CommandArgs args)
        {
            string message = "";

            for (int i = 0; i < args.Parameters.Count; i++)
            {
                message += " " + args.Parameters[i];
            }

            TShock.Utils.Broadcast(TShock.Config.SuperAdminChatPrefix + "<" + args.Player.Name + ">" + message,
                 (byte)TShock.Config.SuperAdminChatRGB[0], (byte)TShock.Config.SuperAdminChatRGB[1], (byte)TShock.Config.SuperAdminChatRGB[2]);
            TShock.Chat.AddMessage(args.Player.Name, "/Admin", message);
            return;
        }

        private static void TradeChat(CommandArgs args)
        {
            string message = "";

            if (args.Player.mute)
            {
                args.Player.SendMessage("You are muted!");
                return;
            }

            for (int i = 0; i < args.Parameters.Count; i++)
            {
                message += " " + args.Parameters[i];
            }

            TShock.Utils.Broadcast("(Trade)<" + args.Player.Name + ">" + message, Color.PaleGoldenrod);
            TShock.Chat.AddMessage(args.Player.Name, "/Trade", message);
            return;
        }

        private static void Shout(CommandArgs args)
        {
            string message = "";
            if (args.Player.mute)
            {
                args.Player.SendMessage("You are muted!");
                return;
            }
            for (int i = 0; i < args.Parameters.Count; i++)
            {
                message += " " + args.Parameters[i];
            }
            if (TShock.Users.Buy(args.Player.Name, 0.5))
            {
                TShock.Utils.Broadcast("(ToAll)<" + args.Player.Name + ">" + message, Color.Gold);
                TShock.Chat.AddMessage(args.Player.Name, "/all", message);
                return;
            }
            else
            {
                args.Player.SendMessage("You need 0,5 RCoins to shout.", Color.Red);
                return;
            }
        }

        private static void GroupChat(CommandArgs args)
        {
            string message = "";

            for (int i = 0; i < args.Parameters.Count; i++)
            {
                message += " " + args.Parameters[i];
            }
            foreach (TSPlayer Player in TShock.Players)
            {
                if (Player != null && Player.Active && args.Player.Group.Name == Player.Group.Name)
                {
                    Player.SendMessage("(To {2})<{0}> {1}".SFormat(args.Player.Name, message, args.Player.Group.Name),
                                                            args.Player.Group.R, args.Player.Group.G,
                                                                    args.Player.Group.B);
                }
            }
            TShock.Chat.AddMessage(args.Player.Name, "/" + args.Player.Group.Name, message);
        }

        private static void Question(CommandArgs args)
        {
            string message = "";
            int count = 0;
            for (int i = 0; i < args.Parameters.Count; i++)
            {
                message += " " + args.Parameters[i];
            }
            foreach (TSPlayer Player in TShock.Players)
            {
                if (Player != null && Player.Active && Player.Group.HasPermission("adminstatus"))
                {
                    count++;
                    Player.SendMessage("(To admin)<{0}> {1}".SFormat(args.Player.Name, message, args.Player.Group.Name),
                                                            Color.PaleGreen);
                    Player.LastWhisper = args.Player;
                }
            }
            TShock.Chat.AddMessage(args.Player.Name, "/question", message);
            Log.ConsoleInfo(string.Format("[Question]<{0}> {1}".SFormat(args.Player.Name, message)));
            if (count == 0)
            {
                args.Player.SendMessage("There are no operators online.");
            }
            return;
        }

        private static void TopRC(CommandArgs args)
        {
            if (TShock.Users.GetUserByName(args.Player.Name) == null)
            {
                args.Player.SendMessage("To see a top you need to register.", Color.Red);
            }
            if (args.Parameters.Count == 0)
            {
                TShock.Users.Top(args.Player, true);
            }
            else
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /toprc", Color.Red);
            }
        }

        private static void TopTime(CommandArgs args)
        {
            if (TShock.Users.GetUserByName(args.Player.Name) == null)
            {
                args.Player.SendMessage("To see a top you need to register.", Color.Red);
            }
            if (args.Parameters.Count >= 0)
            {
                TShock.Users.Top(args.Player, false);
            }
            else
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /toptime", Color.Red);
            }
        }

        private static void Status(CommandArgs args)
        {
            if (TShock.Users.GetUserByName(args.Player.Name) == null)
            {
                args.Player.SendMessage("To see stats you need to register.", Color.Red);
            }
            if (args.Parameters.Count >= 0)
            {
                string plStr = String.Join(" ", args.Parameters);
                TShock.Users.Status(args.Player, plStr);
            }
            else
            {
                TShock.Users.Status(args.Player);
            }
        }

		private static void Mute(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /mute <player> ", Color.Red);
				return;
			}

			string plStr = String.Join(" ", args.Parameters);
			var players = TShock.Utils.FindPlayer(plStr);
            if (players.Count == 0)
            {
                args.Player.SendMessage("Invalid player!", Color.Red);
            }
            else
            {
                if (players.Count > 1)
                {
                    args.Player.SendMessage("More than one player matched!", Color.Red);
                }
                else
                {
                    var plr = players[0];
                    if (TShock.MutedPlayers.Contains(plr.Name) && !players[0].Group.HasPermission(Permissions.mute))
                    {
                        plr.mute = false;
                        TShock.MutedPlayers.Remove(plr.Name);
                        plr.SendMessage("You have been unmuted.");
                        TShock.Utils.Broadcast(plr.Name + " has been unmuted by " + args.Player.Name, Color.Yellow);
                    }
                    else
                    {
                        if (!players[0].Group.HasPermission(Permissions.mute))
                        {
                            plr.mute = true;
                            TShock.MutedPlayers.Add(plr.Name);
                            plr.SendMessage("You have been muted.");
                            TShock.Utils.Broadcast(plr.Name + " has been muted by " + args.Player.Name, Color.Yellow);
                        }
                        else
                            args.Player.SendMessage("You cannot mute this player.");
                    }
                }
            }
		}

		private static void Motd(CommandArgs args)
		{
			TShock.Utils.ShowFileToUser(args.Player, "motd.txt");
		}

		private static void Rules(CommandArgs args)
		{
			TShock.Utils.ShowFileToUser(args.Player, "rules.txt");
		}

		private static void Whisper(CommandArgs args)
		{
			if (args.Parameters.Count < 2)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /whisper <player> <text>", Color.Red);
				return;
			}

			var players = TShock.Utils.FindPlayer(args.Parameters[0]);
			if (players.Count == 0)
			{
				args.Player.SendMessage("Invalid player!", Color.Red);
			}
			else if (players.Count > 1)
			{
				args.Player.SendMessage("More than one player matched!", Color.Red);
			}
			else if (args.Player.mute)
				args.Player.SendMessage("You are muted.");
			else
			{
				var plr = players[0];
				var msg = string.Join(" ", args.Parameters.ToArray(), 1, args.Parameters.Count - 1);
				plr.SendMessage("(Whisper From)" + "<" + args.Player.Name + "> " + msg, Color.MediumPurple);
				args.Player.SendMessage("(Whisper To)" + "<" + plr.Name + "> " + msg, Color.MediumPurple);
				plr.LastWhisper = args.Player;
				args.Player.LastWhisper = plr;
			}
		}

		private static void Reply(CommandArgs args)
		{
			if (args.Player.mute)
				args.Player.SendMessage("You are muted.");
			else if (args.Player.LastWhisper != null)
			{
				var msg = string.Join(" ", args.Parameters);
				args.Player.LastWhisper.SendMessage("(Whisper From)" + "<" + args.Player.Name + ">" + msg, Color.MediumPurple);
				args.Player.SendMessage("(Whisper To)" + "<" + args.Player.LastWhisper.Name + ">" + msg, Color.MediumPurple);
			}
			else
				args.Player.SendMessage(
					"You haven't previously received any whispers. Please use /whisper to whisper to other people.", Color.Red);
		}

		private static void Annoy(CommandArgs args)
		{
			if (args.Parameters.Count != 2)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /annoy <player> <seconds to annoy>", Color.Red);
				return;
			}
			int annoy = 5;
			int.TryParse(args.Parameters[1], out annoy);

			var players = TShock.Utils.FindPlayer(args.Parameters[0]);
			if (players.Count == 0)
				args.Player.SendMessage("Invalid player!", Color.Red);
			else if (players.Count > 1)
				args.Player.SendMessage("More than one player matched!", Color.Red);
			else
			{
				var ply = players[0];
				args.Player.SendMessage("Annoying " + ply.Name + " for " + annoy + " seconds.");
				(new Thread(ply.Whoopie)).Start(annoy);
			}
		}

        private static void PayRC(CommandArgs args)
        {
            if (args.Parameters.Count >= 1)
            {
                double rcoins = 0;
                if (TShock.Users.GetUserByName(args.Player.Name) == null)
                {
                    args.Player.SendMessage("To pay RCoins you need to register.", Color.Red);
                    return;
                }
                if (TShock.Users.GetUserByName(args.Parameters[1]) == null)
                {
                    args.Player.SendMessage("No players found.", Color.Red);
                    return;
                }
                if (args.Parameters[0].Length == 0)
                {
                    args.Player.SendMessage("You must write the amount of RCoins.", Color.Red);
                    return;
                }
                if (args.Parameters[0].Contains("."))
                {
                    string v = args.Parameters[0];
                    string r = v.Replace(".", ",");
                    rcoins = Convert.ToDouble(r);
                }
                else
                {
                    rcoins = Convert.ToDouble(args.Parameters[0]);
                }
                if (rcoins < 0 && !args.Player.Group.HasPermission("rich"))
                {
                    args.Player.SendMessage("Ammount need to be bigger than 0", Color.Red);
                    return;
                }

                if (TShock.Users.Buy(args.Player.Name, rcoins))
                {
                    TShock.Users.SetRCoins(args.Parameters[1], rcoins);

                    args.Player.SendMessage("You payed " + rcoins + " Rcoins to <" + args.Parameters[1] + "> successfully", Color.LightGreen);
                    var players = TShock.Utils.FindPlayer(args.Parameters[1]);
                    if (players.Count == 0)
                    {
                        //args.Player.SendMessage("Invalid player!", Color.Red);
                    }
                    else if (players.Count > 1)
                    {
                        args.Player.SendMessage("More than one player matched!", Color.Red);
                    }
                    else
                    {
                        var plr = players[0];
                        if (rcoins > 0)
                        {
                            plr.SendMessage("Player <" + args.Player.Name + "> give you " + rcoins + " RCoins.", Color.LightGreen);
                        }
                        else
                        {
                            plr.SendMessage("Player <" + args.Player.Name + "> was fined you for " + rcoins + " RCoins.", Color.Yellow);
                        }
                    }
                    Log.ConsoleInfo("[RCoins] " + args.Player.Name + " payed " + rcoins + " Rcoins to " + args.Parameters[1] + " successfully");
                }
                else
                {
                    args.Player.SendMessage("Not enough RCoins.", Color.Red);
                    Log.ConsoleInfo("[RCoins] " + args.Player.Name + " - not enough RCoins.");
                }
            }
            else
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /pay <amount> <player>", Color.Red);
                return;
            }
        }

        private static void Shop(CommandArgs args)
        {
            string Name = string.Empty;
            string Contains = string.Empty;
            string[] Items = new string[3];
            double Price = 0;
            int quantity = 1;
            int Ammount = 0;
            double cost;
            if (args.Parameters.Count == 0)
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /buy [item/buff/warp(50)/vip(500)]", Color.Red);
                return;
            }

            switch (args.Parameters[0].ToLower())
            {
                #region item
                case "i":
                case "item":
                    if (args.Parameters.Count < 2)
                    {
                        args.Player.SendMessage("Invalid syntax! Proper syntax: /buy item [items]", Color.Red);
                        args.Player.SendMessage("To see list of items type /itemlist [armor/weapon/item/block/other]", Color.Red);
                        return;
                    }
                    if (args.Parameters.Count == 3)
                        int.TryParse(args.Parameters[2], out quantity);
                    if (TShock.ArmorShopManager.GetArmor(args.Parameters[1].ToLower(), out Name, out Contains, out Price))
                    {
                        if (TShock.Users.Buy(args.Player.Name, Price))
                        {
                            Items = Contains.Split(';');
                            foreach (string s in Items)
                            {
                                var items = TShock.Utils.GetItemByIdOrName(s);
                                var item = items[0];
                                args.Player.GiveItem(item.type, item.name, item.width, item.height, 1);
                            }

                            args.Player.SendMessage("You spent " + Price + " RCoins.", Color.BlanchedAlmond);
                            args.Player.SendMessage("You buy " + Name + " successfully.", Color.Green);
                            return;
                        }
                        else
                        {
                            args.Player.SendMessage("You need " + Price + " RCoins to buy " + Name + ".", Color.Red);
                            return;
                        }
                    }
                    if (TShock.WeaponShopManager.GetWeapon(args.Parameters[1].ToLower(), out Name, out Contains, out Price))
                    {
                        if (TShock.Users.Buy(args.Player.Name, Price))
                        {
                            var items = TShock.Utils.GetItemByIdOrName(Contains);
                            var item = items[0];
                            args.Player.GiveItem(item.type, item.name, item.width, item.height, 1);
                            args.Player.SendMessage("You spent " + Price + " RCoins.", Color.BlanchedAlmond);
                            args.Player.SendMessage("You buy " + Name + " successfully.", Color.Green);
                            return;
                        }
                        else
                        {
                            args.Player.SendMessage("You need " + Price + " RCoins to buy " + Name + ".", Color.Red);
                            return;
                        }
                    }
                    if (TShock.ItemShopManager.GetItem(args.Parameters[1].ToLower(), out Name, out Contains, out Price))
                    {
                        if (TShock.Users.Buy(args.Player.Name, Price))
                        {
                            var items = TShock.Utils.GetItemByIdOrName(Contains);
                            var item = items[0];
                            args.Player.GiveItem(item.type, item.name, item.width, item.height, 1);
                            args.Player.SendMessage("You spent " + Price + " RCoins.", Color.BlanchedAlmond);
                            args.Player.SendMessage("You buy " + Name + " successfully.", Color.Green);
                            return;
                        }
                        else
                        {
                            args.Player.SendMessage("You need " + Price + " RCoins to buy " + Name + ".", Color.Red);
                            return;
                        }
                    }
                    if (TShock.BlockShopManager.GetBlock(args.Parameters[1].ToLower(), out Name, out Contains, out Price))
                    {
                        Ammount = Convert.ToInt32(Name.Split(':')[1]);
                        Name = Name.Split(':')[0];
                        cost = ((double)Price / Ammount) * quantity;
                        if (TShock.Users.Buy(args.Player.Name, cost))
                        {
                            var items = TShock.Utils.GetItemByIdOrName(Contains);
                            var item = items[0];
                            args.Player.GiveItem(item.type, item.name, item.width, item.height, quantity);
                            args.Player.SendMessage("You spent " + cost + " RCoins.", Color.BlanchedAlmond);
                            args.Player.SendMessage("You buy " + quantity + " " + Name + " successfully.", Color.Green);
                            return;
                        }
                        else
                        {
                            args.Player.SendMessage("You need " + cost + " RCoins to buy " + quantity + " " + Name + ".", Color.Red);
                            return;
                        }
                    }
                    if (TShock.OtherShopManager.GetOther(args.Parameters[1].ToLower(), out Name, out Contains, out Price))
                    {
                        Ammount = Convert.ToInt32(Name.Split(':')[1]);
                        Name = Name.Split(':')[0];
                        cost = ((double)Price / Ammount) * quantity;
                        if (TShock.Users.Buy(args.Player.Name, cost))
                        {
                            var items = TShock.Utils.GetItemByIdOrName(Contains);
                            var item = items[0];
                            args.Player.GiveItem(item.type, item.name, item.width, item.height, quantity);
                            args.Player.SendMessage("You spent " + cost + " RCoins.", Color.BlanchedAlmond);
                            args.Player.SendMessage("You buy " + quantity + " " + Name + " successfully.", Color.Green);
                            return;
                        }
                        else
                        {
                            args.Player.SendMessage("You need " + cost + " RCoins to buy " + quantity + " " + Name + ".", Color.Red);
                            return;
                        }
                    }
                    return;
                default:
                    args.Player.SendMessage("Invalid item name.", Color.Red);
                    return;
                #endregion
                #region buff
                case "b":
                case "buff":
                    if (args.Parameters.Count < 2)
                    {
                        args.Player.SendMessage("Invalid syntax! Proper syntax: /buy buff [fighter(15)/explorer(7)]", Color.Red);
                        return;
                    }
                    switch (args.Parameters[1].ToLower())
                    {
                        case "f":
                        case "fighter":
                            if (TShock.Users.Buy(args.Player.Name, 15, true) || args.Player.Group.HasPermission("vipstatus"))
                            {
                                //Regeneration
                                args.Player.SetBuff(2, 60 * 60 * 5);
                                //Swiftness
                                args.Player.SetBuff(3, 60 * 60 * 4);
                                //Ironskin
                                args.Player.SetBuff(5, 60 * 60 * 5);
                                //Mana Regeneration
                                args.Player.SetBuff(6, 60 * 60 * 2);
                                //Magic Power
                                args.Player.SetBuff(7, 60 * 60 * 2);
                                //Battle
                                args.Player.SetBuff(13, 60 * 60 * 7);
                                //Thorns
                                args.Player.SetBuff(14, 60 * 60 * 2);
                                //Archery
                                args.Player.SetBuff(16, 60 * 60 * 4);
                                //Hunter
                                args.Player.SetBuff(17, 60 * 60 * 5);
                                //Well Fed
                                args.Player.SetBuff(26, 60 * 60 * 9);

                                if (args.Player.Group.HasPermission("vipstatus"))
                                {
                                    args.Player.SendMessage("You get fighter buff successfully.", Color.Green);
                                    return;
                                }

                                TShock.Users.Buy(args.Player.Name, 15);
                                args.Player.SendMessage("You spent 15 RCoins.", Color.BlanchedAlmond);
                                args.Player.SendMessage("You buy fighter buff successfully.", Color.Green);
                                return;
                            }
                            else
                            {
                                args.Player.SendMessage("You need 15 RCoins to buy fighter buff.", Color.Red);
                                return;
                            }
                        case "e":
                        case "explorer":
                            if (TShock.Users.Buy(args.Player.Name, 7, true) || args.Player.Group.HasPermission("vipstatus"))
                            {
                                //Obsidian Skin
                                args.Player.SetBuff(1, 60 * 60 * 4);
                                //Swiftness
                                args.Player.SetBuff(3, 60 * 60 * 4);
                                //Featherfall
                                args.Player.SetBuff(8, 60 * 60 * 5);
                                //Spelunker
                                args.Player.SetBuff(9, 60 * 60 * 5);
                                //Night Owl
                                args.Player.SetBuff(12, 60 * 60 * 4);
                                //Water Walking
                                args.Player.SetBuff(15, 60 * 60 * 5);
                                //Hunter
                                args.Player.SetBuff(17, 60 * 60 * 5);
                                //Gravitation
                                args.Player.SetBuff(18, 60 * 60 * 3);
                                //Orb of Light
                                args.Player.SetBuff(19, 60 * 60 * 5);
                                //Well Fed
                                args.Player.SetBuff(26, 60 * 60 * 9);

                                if (args.Player.Group.HasPermission("vipstatus"))
                                {
                                    args.Player.SendMessage("You get explorer buff successfully.", Color.Green);
                                    return;
                                }

                                TShock.Users.Buy(args.Player.Name, 7);
                                args.Player.SendMessage("You spent 7 RCoins.", Color.BlanchedAlmond);
                                args.Player.SendMessage("You buy explorer buff successfully.", Color.Green);
                                return;
                            }
                            else
                            {
                                args.Player.SendMessage("You need 7 RCoins to buy explorer buff.", Color.Red);
                                return;
                            }
                        default:
                            args.Player.SendMessage("Invalid buff name.", Color.Red);
                            return;
                    }
                #endregion
                #region warp
                case "warp":
                    if (args.Parameters.Count > 1)
                    {
                        if (TShock.Users.Buy(args.Player.Name, 50))
                        {
                            string warpName = args.Parameters[1];
                            if (warpName.Equals("list"))
                            {
                                args.Player.SendMessage("Name reserved, use a different name", Color.Red);
                                return;
                            }
                            else if (TShock.Warps.AddWarp(args.Player.TileX, args.Player.TileY, warpName, Main.worldID.ToString()))
                            {
                                args.Player.SendMessage("You spent 50 RCoins.", Color.BlanchedAlmond);
                                args.Player.SendMessage("Set warp " + warpName, Color.Green);
                                return;
                            }
                            else
                            {
                                args.Player.SendMessage("Warp " + warpName + " already exists", Color.Red);
                            }
                        }
                        else
                        {
                            args.Player.SendMessage("You need 50 RCoins to buy warp.", Color.Red);
                            return;
                        }
                    }
                    else
                    {
                        args.Player.SendMessage("Need to write warp name.", Color.Red);
                        return;
                    }
                    return;
                #endregion
                #region vip
                case "vip":
                    if (TShock.Users.Buy(args.Player.Name, 500))
                    {
                        var user = new User();
                        user.Name = args.Player.Name;
                        TShock.Users.SetUserGroup(user, "vip");
                        args.Player.SendMessage("You spent 500 RCoins.", Color.BlanchedAlmond);
                        args.Player.SendMessage("You buy vip status successfully.", Color.Green);
                        args.Player.SendMessage("To use vip account reconnect to server.", Color.Green);
                    }
                    else
                    {
                        args.Player.SendMessage("You need 500 RCoins to buy vip.", Color.Red);
                    }
                    return;
                #endregion
            }
        }

        private static void ItemList(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /itemlist [armor/weapon/item/block/other]", Color.Red);
                return;
            }

            switch (args.Parameters[0].ToLower())
            {
                #region armor
                case "a":
                case "armor":
                    int page = 1;
                    if (args.Parameters.Count == 2)
                        int.TryParse(args.Parameters[1], out page);
                    var sb = new StringBuilder();
                    var ArmorSet = TShock.ArmorShopManager.InGameNames();

                    if (ArmorSet.Count > (15 * (page - 1)))
                    {
                        for (int j = (15 * (page - 1)); j < (15 * page); j++)
                        {
                            if (sb.Length != 0)
                                sb.Append(", ");
                            sb.Append(ArmorSet[j]);
                            if (j == ArmorSet.Count - 1)
                            {
                                args.Player.SendMessage(sb.ToString(), Color.Yellow);
                                break;
                            }
                            if ((j + 1) % 5 == 0)
                            {
                                args.Player.SendMessage(sb.ToString(), Color.Yellow);
                                sb.Clear();
                            }
                        }
                    }
                    if (ArmorSet.Count > (15 * page))
                    {
                        args.Player.SendMessage(string.Format("Type /itemlist armor {0} for more items.", (page + 1)), Color.Yellow);
                    }
                    return;
                #endregion
                #region weapon
                case "w":
                case "weapon":
                    page = 1;
                    if (args.Parameters.Count == 2)
                        int.TryParse(args.Parameters[1], out page);
                    sb = new StringBuilder();
                    var WeaponSet = TShock.WeaponShopManager.InGameNames();
                    if (WeaponSet.Count > (15 * (page - 1)))
                    {
                        for (int j = (15 * (page - 1)); j < (15 * page); j++)
                        {
                            if (sb.Length != 0)
                                sb.Append(", ");
                            sb.Append(WeaponSet[j]);
                            if (j == WeaponSet.Count - 1)
                            {
                                args.Player.SendMessage(sb.ToString(), Color.Yellow);
                                break;
                            }
                            if ((j + 1) % 5 == 0)
                            {
                                args.Player.SendMessage(sb.ToString(), Color.Yellow);
                                sb.Clear();
                            }
                        }
                    }
                    if (WeaponSet.Count > (15 * page))
                    {
                        args.Player.SendMessage(string.Format("Type /itemlist weapon {0} for more items.", (page + 1)), Color.Yellow);
                    }
                    return;
                #endregion
                #region item
                case "i":
                case "item":
                    page = 1;
                    if (args.Parameters.Count == 2)
                        int.TryParse(args.Parameters[1], out page);
                    sb = new StringBuilder();
                    var ItemSet = TShock.ItemShopManager.InGameNames();

                    if (ItemSet.Count > (15 * (page - 1)))
                    {
                        for (int j = (15 * (page - 1)); j < (15 * page); j++)
                        {
                            if (sb.Length != 0)
                                sb.Append(", ");
                            sb.Append(ItemSet[j]);
                            if (j == ItemSet.Count - 1)
                            {
                                args.Player.SendMessage(sb.ToString(), Color.Yellow);
                                break;
                            }
                            if ((j + 1) % 5 == 0)
                            {
                                args.Player.SendMessage(sb.ToString(), Color.Yellow);
                                sb.Clear();
                            }
                        }
                    }
                    if (ItemSet.Count > (15 * page))
                    {
                        args.Player.SendMessage(string.Format("Type /itemlist item {0} for more items.", (page + 1)), Color.Yellow);
                    }
                    return;
                #endregion
                #region block
                case "b":
                case "block":
                    page = 1;
                    if (args.Parameters.Count == 2)
                        int.TryParse(args.Parameters[1], out page);
                    sb = new StringBuilder();
                    var BlockSet = TShock.BlockShopManager.InGameNames();

                    if (BlockSet.Count > (15 * (page - 1)))
                    {
                        for (int j = (15 * (page - 1)); j < (15 * page); j++)
                        {
                            if (sb.Length != 0)
                                sb.Append(", ");
                            sb.Append(BlockSet[j]);
                            if (j == BlockSet.Count - 1)
                            {
                                args.Player.SendMessage(sb.ToString(), Color.Yellow);
                                break;
                            }
                            if ((j + 1) % 5 == 0)
                            {
                                args.Player.SendMessage(sb.ToString(), Color.Yellow);
                                sb.Clear();
                            }
                        }
                    }
                    if (BlockSet.Count > (15 * page))
                    {
                        args.Player.SendMessage(string.Format("Type /itemlist block {0} for more items.", (page + 1)), Color.Yellow);
                    }
                    return;
                #endregion
                #region other
                case "o":
                case "other":
                    page = 1;
                    if (args.Parameters.Count == 2)
                        int.TryParse(args.Parameters[1], out page);
                    sb = new StringBuilder();
                    var other = TShock.OtherShopManager.InGameNames();

                    if (other.Count > (15 * (page - 1)))
                    {
                        for (int j = (15 * (page - 1)); j < (15 * page); j++)
                        {
                            if (sb.Length != 0)
                                sb.Append(", ");
                            sb.Append(other[j]);
                            if (j == other.Count - 1)
                            {
                                args.Player.SendMessage(sb.ToString(), Color.Yellow);
                                break;
                            }
                            if ((j + 1) % 5 == 0)
                            {
                                args.Player.SendMessage(sb.ToString(), Color.Yellow);
                                sb.Clear();
                            }
                        }
                    }
                    if (other.Count > (15 * page))
                    {
                        args.Player.SendMessage(string.Format("Type /itemlist other {0} for more items.", (page + 1)), Color.Yellow);
                    }
                    return;
                #endregion
            }
        }

		#endregion General Commands

		#region Cheat Commands

		private static void Kill(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /kill <player>", Color.Red);
				return;
			}

			string plStr = String.Join(" ", args.Parameters);
			var players = TShock.Utils.FindPlayer(plStr);
			if (players.Count == 0)
			{
				args.Player.SendMessage("Invalid player!", Color.Red);
			}
			else if (players.Count > 1)
			{
				args.Player.SendMessage("More than one player matched!", Color.Red);
			}
			else
			{
				var plr = players[0];
				plr.DamagePlayer(999999);
				args.Player.SendMessage(string.Format("You just killed {0}!", plr.Name));
				plr.SendMessage(string.Format("{0} just killed you!", args.Player.Name));
			}
		}

		private static void Butcher(CommandArgs args)
		{
			if (args.Parameters.Count > 1)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /butcher [killFriendly(true/false)]", Color.Red);
				return;
			}

			bool killFriendly = true;
			if (args.Parameters.Count == 1)
				bool.TryParse(args.Parameters[0], out killFriendly);

			int killcount = 0;
			for (int i = 0; i < Main.npc.Length; i++)
			{
				if (Main.npc[i].active && Main.npc[i].type != 0 && !Main.npc[i].townNPC && (!Main.npc[i].friendly || killFriendly))
				{
					TSPlayer.Server.StrikeNPC(i, 99999, 90f, 1);
					killcount++;
				}
			}
			TShock.Utils.Broadcast(string.Format("Killed {0} NPCs.", killcount));
		}

		private static void Item(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /item <item name/id> [item amount] [prefix id/name]",
										Color.Red);
				return;
			}
			if (args.Parameters[0].Length == 0)
			{
				args.Player.SendMessage("Missing item name/id", Color.Red);
				return;
			}
			int itemAmount = 0;
			int prefix = 0;
			if (args.Parameters.Count == 2)
				int.TryParse(args.Parameters[1], out itemAmount);
			else if (args.Parameters.Count == 3)
			{
				int.TryParse(args.Parameters[1], out itemAmount);
				var found = TShock.Utils.GetPrefixByIdOrName(args.Parameters[2]);
				if (found.Count == 1)
					prefix = found[0];
			}
			var items = TShock.Utils.GetItemByIdOrName(args.Parameters[0]);
			if (items.Count == 0)
			{
				args.Player.SendMessage("Invalid item type!", Color.Red);
			}
			else if (items.Count > 1)
			{
				args.Player.SendMessage(string.Format("More than one ({0}) item matched!", items.Count), Color.Red);
			}
			else
			{
				var item = items[0];
				if (item.type >= 1 && item.type < Main.maxItemTypes)
				{
					if (args.Player.InventorySlotAvailable || item.name.Contains("Coin"))
					{
						if (itemAmount == 0 || itemAmount > item.maxStack)
							itemAmount = item.maxStack;
						args.Player.GiveItem(item.type, item.name, item.width, item.height, itemAmount, prefix);
						args.Player.SendMessage(string.Format("Gave {0} {1}(s).", itemAmount, item.name));
					}
					else
					{
						args.Player.SendMessage("You don't have free slots!", Color.Red);
					}
				}
				else
				{
					args.Player.SendMessage("Invalid item type!", Color.Red);
				}
			}
		}

		private static void Give(CommandArgs args)
		{
			if (args.Parameters.Count < 2)
			{
				args.Player.SendMessage(
					"Invalid syntax! Proper syntax: /give <item type/id> <player> [item amount] [prefix id/name]", Color.Red);
				return;
			}
			if (args.Parameters[0].Length == 0)
			{
				args.Player.SendMessage("Missing item name/id", Color.Red);
				return;
			}
			if (args.Parameters[1].Length == 0)
			{
				args.Player.SendMessage("Missing player name", Color.Red);
				return;
			}
			int itemAmount = 0;
			int prefix = 0;
			var items = TShock.Utils.GetItemByIdOrName(args.Parameters[0]);
			args.Parameters.RemoveAt(0);
			string plStr = args.Parameters[0];
			args.Parameters.RemoveAt(0);
			if (args.Parameters.Count == 1)
				int.TryParse(args.Parameters[0], out itemAmount);
			else if (args.Parameters.Count == 2)
			{
				int.TryParse(args.Parameters[0], out itemAmount);
				var found = TShock.Utils.GetPrefixByIdOrName(args.Parameters[1]);
				if (found.Count == 1)
					prefix = found[0];
			}

			if (items.Count == 0)
			{
				args.Player.SendMessage("Invalid item type!", Color.Red);
			}
			else if (items.Count > 1)
			{
				args.Player.SendMessage(string.Format("More than one ({0}) item matched!", items.Count), Color.Red);
			}
			else
			{
				var item = items[0];
				if (item.type >= 1 && item.type < Main.maxItemTypes)
				{
					var players = TShock.Utils.FindPlayer(plStr);
					if (players.Count == 0)
					{
						args.Player.SendMessage("Invalid player!", Color.Red);
					}
					else if (players.Count > 1)
					{
						args.Player.SendMessage("More than one player matched!", Color.Red);
					}
					else
					{
						var plr = players[0];
						if (plr.InventorySlotAvailable || item.name.Contains("Coin"))
						{
							if (itemAmount == 0 || itemAmount > item.maxStack)
								itemAmount = item.maxStack;
							plr.GiveItem(item.type, item.name, item.width, item.height, itemAmount, prefix);
							args.Player.SendMessage(string.Format("Gave {0} {1} {2}(s).", plr.Name, itemAmount, item.name));
							plr.SendMessage(string.Format("{0} gave you {1} {2}(s).", args.Player.Name, itemAmount, item.name));
						}
						else
						{
							args.Player.SendMessage("Player does not have free slots!", Color.Red);
						}
					}
				}
				else
				{
					args.Player.SendMessage("Invalid item type!", Color.Red);
				}
			}
		}

		public static void ClearItems(CommandArgs args)
		{
			int radius = 50;
			if (args.Parameters.Count > 0)
			{
				if (args.Parameters[0].ToLower() == "all")
				{
					radius = Int32.MaxValue/16;
				}
				else
				{
					try
					{
						radius = Convert.ToInt32(args.Parameters[0]);
					}
					catch (Exception)
					{
						args.Player.SendMessage(
							"Please either enter the keyword \"all\", or the block radius you wish to delete all items from.", Color.Red);
						return;
					}
				}
			}
			int count = 0;
			for (int i = 0; i < 200; i++)
			{
				if (
					(Math.Sqrt(Math.Pow(Main.item[i].position.X - args.Player.X, 2) +
							   Math.Pow(Main.item[i].position.Y - args.Player.Y, 2)) < radius*16) && (Main.item[i].active))
				{
					Main.item[i].active = false;
					NetMessage.SendData(0x15, -1, -1, "", i, 0f, 0f, 0f, 0);
					count++;
				}
			}
			args.Player.SendMessage("All " + count + " items within a radius of " + radius + " have been deleted.");
		}

		private static void Heal(CommandArgs args)
		{
			TSPlayer playerToHeal;
            Item heart = TShock.Utils.GetItemById(58);
            Item star = TShock.Utils.GetItemById(184);

			if (args.Parameters.Count > 0)
			{
                if (args.Parameters[0] == "all")
                {
                    if (args.Player.Group.HasPermission("canhealall"))
                    {
                        foreach (TSPlayer player in TShock.Players)
                        {
                            if (player != null && player.Active)
                            {
                                for (int i = 0; i < 20; i++)
                                    player.GiveItem(heart.type, heart.name, heart.width, heart.height, heart.maxStack);
                                for (int i = 0; i < 10; i++)
                                    player.GiveItem(star.type, star.name, star.width, star.height, star.maxStack);
                                player.SendMessage(string.Format("{0} just healed you!", args.Player.Name));
                            }
                        }
                        args.Player.SendMessage("You heal all players");
                        return;
                    }
                    else
                    {
                        args.Player.SendMessage("You do not have permission to use heall all command.");
                        return;
                    }
                }

                string plStr = String.Join(" ", args.Parameters);
				var players = TShock.Utils.FindPlayer(plStr);
				if (players.Count == 0)
				{
					args.Player.SendMessage("Invalid player!", Color.Red);
					return;
				}
				else if (players.Count > 1)
				{
					args.Player.SendMessage("More than one player matched!", Color.Red);
					return;
				}
				else
				{
					playerToHeal = players[0];
				}
			}
			else if (!args.Player.RealPlayer)
			{
				args.Player.SendMessage("You cant heal yourself!");
				return;
			}
			else
			{
				playerToHeal = args.Player;
			}

			for (int i = 0; i < 20; i++)
				playerToHeal.GiveItem(heart.type, heart.name, heart.width, heart.height, heart.maxStack);
			for (int i = 0; i < 10; i++)
				playerToHeal.GiveItem(star.type, star.name, star.width, star.height, star.maxStack);
			if (playerToHeal == args.Player)
			{
				args.Player.SendMessage("You just got healed!");
			}
			else
			{
				args.Player.SendMessage(string.Format("You just healed {0}", playerToHeal.Name));
				playerToHeal.SendMessage(string.Format("{0} just healed you!", args.Player.Name));
			}
		}

		private static void Buff(CommandArgs args)
		{
			if (args.Parameters.Count < 1 || args.Parameters.Count > 2)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /buff <buff id/name> [time(seconds)]", Color.Red);
				return;
			}
			int id = 0;
			int time = 60;
			if (!int.TryParse(args.Parameters[0], out id))
			{
				var found = TShock.Utils.GetBuffByName(args.Parameters[0]);
				if (found.Count == 0)
				{
					args.Player.SendMessage("Invalid buff name!", Color.Red);
					return;
				}
				else if (found.Count > 1)
				{
					args.Player.SendMessage(string.Format("More than one ({0}) buff matched!", found.Count), Color.Red);
					return;
				}
				id = found[0];
			}
			if (args.Parameters.Count == 2)
				int.TryParse(args.Parameters[1], out time);
			if (id > 0 && id < Main.maxBuffs)
			{
				if (time < 0 || time > short.MaxValue)
					time = 60;
				args.Player.SetBuff(id, time*60);
				args.Player.SendMessage(string.Format("You have buffed yourself with {0}({1}) for {2} seconds!",
													  TShock.Utils.GetBuffName(id), TShock.Utils.GetBuffDescription(id), (time)),
										Color.Green);
			}
			else
				args.Player.SendMessage("Invalid buff ID!", Color.Red);
		}

		private static void GBuff(CommandArgs args)
		{
			if (args.Parameters.Count < 2 || args.Parameters.Count > 3)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /gbuff <player> <buff id/name> [time(seconds)]", Color.Red);
				return;
			}
			int id = 0;
			int time = 60;
			var foundplr = TShock.Utils.FindPlayer(args.Parameters[0]);
			if (foundplr.Count == 0)
			{
				args.Player.SendMessage("Invalid player!", Color.Red);
				return;
			}
			else if (foundplr.Count > 1)
			{
				args.Player.SendMessage(string.Format("More than one ({0}) player matched!", args.Parameters.Count), Color.Red);
				return;
			}
			else
			{
				if (!int.TryParse(args.Parameters[1], out id))
				{
					var found = TShock.Utils.GetBuffByName(args.Parameters[1]);
					if (found.Count == 0)
					{
						args.Player.SendMessage("Invalid buff name!", Color.Red);
						return;
					}
					else if (found.Count > 1)
					{
						args.Player.SendMessage(string.Format("More than one ({0}) buff matched!", found.Count), Color.Red);
						return;
					}
					id = found[0];
				}
				if (args.Parameters.Count == 3)
					int.TryParse(args.Parameters[2], out time);
				if (id > 0 && id < Main.maxBuffs)
				{
					if (time < 0 || time > short.MaxValue)
						time = 60;
					foundplr[0].SetBuff(id, time*60);
					args.Player.SendMessage(string.Format("You have buffed {0} with {1}({2}) for {3} seconds!",
														  foundplr[0].Name, TShock.Utils.GetBuffName(id),
														  TShock.Utils.GetBuffDescription(id), (time)), Color.Green);
					foundplr[0].SendMessage(string.Format("{0} has buffed you with {1}({2}) for {3} seconds!",
														  args.Player.Name, TShock.Utils.GetBuffName(id),
														  TShock.Utils.GetBuffDescription(id), (time)), Color.Green);
				}
				else
					args.Player.SendMessage("Invalid buff ID!", Color.Red);
			}
		}

		private static void Grow(CommandArgs args)
		{
			if (args.Parameters.Count != 1)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /grow [tree/epictree/mushroom/cactus/herb]", Color.Red);
				return;
			}
			var name = "Fail";
			var x = args.Player.TileX;
			var y = args.Player.TileY + 3;
			switch (args.Parameters[0].ToLower())
			{
				case "tree":
					for (int i = x - 1; i < x + 2; i++)
					{
						Main.tile[i, y].active = true;
						Main.tile[i, y].type = 2;
						Main.tile[i, y].wall = 0;
					}
					Main.tile[x, y - 1].wall = 0;
					WorldGen.GrowTree(x, y);
					name = "Tree";
					break;
				case "epictree":
					for (int i = x - 1; i < x + 2; i++)
					{
						Main.tile[i, y].active = true;
						Main.tile[i, y].type = 2;
						Main.tile[i, y].wall = 0;
					}
					Main.tile[x, y - 1].wall = 0;
					Main.tile[x, y - 1].liquid = 0;
					Main.tile[x, y - 1].active = true;
					WorldGen.GrowEpicTree(x, y);
					name = "Epic Tree";
					break;
				case "mushroom":
					for (int i = x - 1; i < x + 2; i++)
					{
						Main.tile[i, y].active = true;
						Main.tile[i, y].type = 70;
						Main.tile[i, y].wall = 0;
					}
					Main.tile[x, y - 1].wall = 0;
					WorldGen.GrowShroom(x, y);
					name = "Mushroom";
					break;
				case "cactus":
					Main.tile[x, y].type = 53;
					WorldGen.GrowCactus(x, y);
					name = "Cactus";
					break;
				case "herb":
					Main.tile[x, y].active = true;
					Main.tile[x, y].frameX = 36;
					Main.tile[x, y].type = 83;
					WorldGen.GrowAlch(x, y);
					name = "Herb";
					break;
				default:
					args.Player.SendMessage("Unknown plant!", Color.Red);
					return;
			}
			args.Player.SendTileSquare(x, y);
			args.Player.SendMessage("Tried to grow a " + name, Color.Green);
		}

        private static void GrowTree(CommandArgs args)
        {
            if (args.Parameters.Count > 0)
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /gt", Color.Red);
                return;
            }
            
            var name = "Fail";
            var x = args.Player.TileX;
            var y = args.Player.TileY + 3;
            for (int i = x - 1; i < x + 2; i++)
            {
                Main.tile[i, y].active = true;
                Main.tile[i, y].type = 2;
                Main.tile[i, y].wall = 0;
            }
            Main.tile[x, y - 1].wall = 0;
            WorldGen.GrowTree(x, y);
            name = "Tree";
            args.Player.SendTileSquare(x, y);
            args.Player.SendMessage("Tried to grow a " + name, Color.Green);
        }

		#endregion Cheat Comamnds
	}
}