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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Terraria;
using System.Text.RegularExpressions;

namespace TShockAPI
{
	public class Utils
	{
	    public static bool saving = false;

		public Utils()
		{
		}

		public Random Random = new Random();
		//private static List<Group> groups = new List<Group>();

		/// <summary>
		/// Provides the real IP address from a RemoteEndPoint string that contains a port and an IP
		/// </summary>
		/// <param name="mess">A string IPv4 address in IP:PORT form.</param>
		/// <returns>A string IPv4 address.</returns>
		public string GetRealIP(string mess)
		{
			return mess.Split(':')[0];
		}

		/// <summary>
		/// Used for some places where a list of players might be used.
		/// </summary>
		/// <returns>String of players seperated by commas.</returns>
		public string GetPlayers()
		{
			var sb = new StringBuilder();
			foreach (TSPlayer player in TShock.Players)
			{
				if (player != null && player.Active)
				{
					if (sb.Length != 0)
					{
						sb.Append(", ");
					}
					sb.Append(player.Name);
				}
			}
			return sb.ToString();
		}

        /// <summary>
        /// Used for some places where a list of players might be used.
        /// </summary>
        /// <returns>String of players and their id seperated by commas.</returns>
        public string GetPlayersWithIds()
        {
            var sb = new StringBuilder();
            foreach (TSPlayer player in TShock.Players)
            {
                if (player != null && player.Active)
                {
                    if (sb.Length != 0)
                    {
                        sb.Append(", ");
                    }
                    sb.Append(player.Name);
                    string id = "( " + Convert.ToString(TShock.Users.GetUserID(player.UserAccountName)) + " )";
                    sb.Append(id);
                }
            }
            return sb.ToString();
        }

		/// <summary>
		/// Finds a player and gets IP as string
		/// </summary>
		/// <param name="msg">Player name</param>
		public string GetPlayerIP(string playername)
		{
			foreach (TSPlayer player in TShock.Players)
			{
				if (player != null && player.Active)
				{
					if (playername.ToLower() == player.Name.ToLower())
					{
						return player.IP;
					}
				}
			}
			return null;
		}

		/// <summary>
		/// It's a clamp function
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value">Value to clamp</param>
		/// <param name="max">Maximum bounds of the clamp</param>
		/// <param name="min">Minimum bounds of the clamp</param>
		/// <returns></returns>
		public T Clamp<T>(T value, T max, T min)
			where T : IComparable<T>
		{
			T result = value;
			if (value.CompareTo(max) > 0)
				result = max;
			if (value.CompareTo(min) < 0)
				result = min;
			return result;
		}

		/// <summary>
		/// Saves the map data
		/// </summary>
		public void SaveWorld()
		{
            saving = true;
            WorldGen.realsaveWorld();
            foreach (TSPlayer player in TShock.Players)
            {
                if (player != null && player.Active && player.IsLoggedIn)
                {
                    if (TShock.Config.StoreInventory)
                        TShock.Inventory.UpdateInventory(player);
                    if (player.SavePlayer())
                        player.SendMessage("Your profile saved sucessfully", Color.Chartreuse);
                }
            }
            Broadcast("World saved.", Color.Yellow);
            Console.WriteLine("All profiles saved!");
            Log.Info(string.Format("World saved at ({0})", Main.worldPathName));
		}

        /// <summary>
        /// Encrypt *.plr file
        /// </summary>
        public void EncryptFile(string inputFile, string outputFile)
        {
            string s = "h3y_gUyZ";
            UnicodeEncoding unicodeEncoding = new UnicodeEncoding();
            byte[] bytes = unicodeEncoding.GetBytes(s);
            FileStream fileStream = new FileStream(outputFile, FileMode.Create);
            RijndaelManaged rijndaelManaged = new RijndaelManaged();
            CryptoStream cryptoStream = new CryptoStream(fileStream, rijndaelManaged.CreateEncryptor(bytes, bytes), CryptoStreamMode.Write);
            FileStream fileStream2 = new FileStream(inputFile, FileMode.Open);
            int num;
            while ((num = fileStream2.ReadByte()) != -1)
            {
                cryptoStream.WriteByte((byte)num);
            }
            fileStream2.Close();
            cryptoStream.Close();
            fileStream.Close();
        }

        /// <summary>
        /// Decrypt *.plr file
        /// </summary>
        public bool DecryptFile(string inputFile, string outputFile)
        {
            string s = "h3y_gUyZ";
            UnicodeEncoding unicodeEncoding = new UnicodeEncoding();
            byte[] bytes = unicodeEncoding.GetBytes(s);
            FileStream fileStream = new FileStream(inputFile, FileMode.Open);
            RijndaelManaged rijndaelManaged = new RijndaelManaged();
            CryptoStream cryptoStream = new CryptoStream(fileStream, rijndaelManaged.CreateDecryptor(bytes, bytes), CryptoStreamMode.Read);
            FileStream fileStream2 = new FileStream(outputFile, FileMode.Create);
            try
            {
                int num;
                while ((num = cryptoStream.ReadByte()) != -1)
                {
                    fileStream2.WriteByte((byte)num);
                }
                fileStream2.Close();
                cryptoStream.Close();
                fileStream.Close();
            }
            catch
            {
                fileStream2.Close();
                fileStream.Close();
                File.Delete(outputFile);
                return true;
            }
            return false;
        }

		/// <summary>
		/// Broadcasts a message to all players
		/// </summary>
		/// <param name="msg">string message</param>
		public void Broadcast(string msg)
		{
			Broadcast(msg, Color.Green);
		}

		public void Broadcast(string msg, byte red, byte green, byte blue)
		{
			TSPlayer.All.SendMessage(msg, red, green, blue);
			TSPlayer.Server.SendMessage(msg, red, green, blue);
			Log.Info(string.Format("Broadcast: {0}", msg));
		}

		public void Broadcast(string msg, Color color)
		{
			Broadcast(msg, color.R, color.G, color.B);
		}

		/// <summary>
		/// Sends message to all users with 'logs' permission.
		/// </summary>
		/// <param name="log">Message to send</param>
		/// <param name="color">Color of the message</param>
		public void SendLogs(string log, Color color)
		{
			Log.Info(log);
			TSPlayer.Server.SendMessage(log, color);
			foreach (TSPlayer player in TShock.Players)
			{
				if (player != null && player.Active && player.Group.HasPermission(Permissions.logs) && player.DisplayLogs &&
				    TShock.Config.DisableSpewLogs == false)
					player.SendMessage(log, color);
			}
		}

		/// <summary>
		/// The number of active players on the server.
		/// </summary>
		/// <returns>int playerCount</returns>
		public int ActivePlayers()
		{
			int num = 0;
			foreach (TSPlayer player in TShock.Players)
			{
				if (player != null && player.Active)
				{
					num++;
				}
			}
			return num;
		}

		/// <summary>
		/// Finds a player ID based on name
		/// </summary>
		/// <param name="ply">Player name</param>
		/// <returns></returns>
		public List<TSPlayer> FindPlayer(string ply)
		{
			var found = new List<TSPlayer>();
			ply = ply.ToLower();
			foreach (TSPlayer player in TShock.Players)
			{
				if (player == null)
					continue;

				string name = player.Name.ToLower();
				if (name.Equals(ply))
					return new List<TSPlayer> {player};
				if (name.Contains(ply))
					found.Add(player);
			}
			return found;
		}

		/// <summary>
		/// Gets a random clear tile in range
		/// </summary>
		/// <param name="startTileX">Bound X</param>
		/// <param name="startTileY">Bound Y</param>
		/// <param name="tileXRange">Range on the X axis</param>
		/// <param name="tileYRange">Range on the Y axis</param>
		/// <param name="tileX">X location</param>
		/// <param name="tileY">Y location</param>
		public void GetRandomClearTileWithInRange(int startTileX, int startTileY, int tileXRange, int tileYRange,
		                                          out int tileX, out int tileY)
		{
			int j = 0;
			do
			{
				if (j == 100)
				{
					tileX = startTileX;
					tileY = startTileY;
					break;
				}

				tileX = startTileX + Random.Next(tileXRange*-1, tileXRange);
				tileY = startTileY + Random.Next(tileYRange*-1, tileYRange);
				j++;
			} while (TileValid(tileX, tileY) && !TileClear(tileX, tileY));
		}

		/// <summary>
		/// Determines if a tile is valid
		/// </summary>
		/// <param name="tileX">Location X</param>
		/// <param name="tileY">Location Y</param>
		/// <returns>If the tile is valid</returns>
		private bool TileValid(int tileX, int tileY)
		{
			return tileX >= 0 && tileX <= Main.maxTilesX && tileY >= 0 && tileY <= Main.maxTilesY;
		}

		/// <summary>
		/// Clears a tile
		/// </summary>
		/// <param name="tileX">Location X</param>
		/// <param name="tileY">Location Y</param>
		/// <returns>The state of the tile</returns>
		private bool TileClear(int tileX, int tileY)
		{
			return !Main.tile[tileX, tileY].active;
		}

		/// <summary>
		/// Gets a list of items by ID or name
		/// </summary>
		/// <param name="idOrName">Item ID or name</param>
		/// <returns>List of Items</returns>
		public List<Item> GetItemByIdOrName(string idOrName)
		{
			int type = -1;
			if (int.TryParse(idOrName, out type))
			{
				return new List<Item> {GetItemById(type)};
			}
			return GetItemByName(idOrName);
		}

		/// <summary>
		/// Gets an item by ID
		/// </summary>
		/// <param name="id">ID</param>
		/// <returns>Item</returns>
		public Item GetItemById(int id)
		{
			Item item = new Item();
			item.netDefaults(id);
			return item;
		}

		/// <summary>
		/// Gets items by name
		/// </summary>
		/// <param name="name">name</param>
		/// <returns>List of Items</returns>
		public List<Item> GetItemByName(string name)
		{
			//Method #1 - must be exact match, allows support for different pickaxes/hammers/swords etc
			for (int i = 1; i < Main.maxItemTypes; i++)
			{
				Item item = new Item();
				item.SetDefaults(name);
				if (item.name == name)
					return new List<Item> {item};
			}
			//Method #2 - allows impartial matching
			var found = new List<Item>();
			for (int i = -24; i < Main.maxItemTypes; i++)
			{
				try
				{
					Item item = new Item();
					item.netDefaults(i);
					if (item.name.ToLower() == name.ToLower())
						return new List<Item> {item};
					if (item.name.ToLower().StartsWith(name.ToLower()))
						found.Add(item);
				}
				catch
				{
				}
			}
			return found;
		}

		/// <summary>
		/// Gets an NPC by ID or Name
		/// </summary>
		/// <param name="idOrName"></param>
		/// <returns>List of NPCs</returns>
		public List<NPC> GetNPCByIdOrName(string idOrName)
		{
			int type = -1;
			if (int.TryParse(idOrName, out type))
			{
				return new List<NPC> {GetNPCById(type)};
			}
			return GetNPCByName(idOrName);
		}

		/// <summary>
		/// Gets an NPC by ID
		/// </summary>
		/// <param name="id">ID</param>
		/// <returns>NPC</returns>
		public NPC GetNPCById(int id)
		{
			NPC npc = new NPC();
			npc.netDefaults(id);
			return npc;
		}

		/// <summary>
		/// Gets a NPC by name
		/// </summary>
		/// <param name="name">Name</param>
		/// <returns>List of matching NPCs</returns>
		public List<NPC> GetNPCByName(string name)
		{
			//Method #1 - must be exact match, allows support for different coloured slimes
			for (int i = -17; i < Main.maxNPCTypes; i++)
			{
				NPC npc = new NPC();
				npc.SetDefaults(name);
				if (npc.name == name)
					return new List<NPC> {npc};
			}
			//Method #2 - allows impartial matching
			var found = new List<NPC>();
			for (int i = 1; i < Main.maxNPCTypes; i++)
			{
				NPC npc = new NPC();
				npc.netDefaults(i);
				if (npc.name.ToLower() == name.ToLower())
					return new List<NPC> {npc};
				if (npc.name.ToLower().StartsWith(name.ToLower()))
					found.Add(npc);
			}
			return found;
		}

		/// <summary>
		/// Gets a buff name by id
		/// </summary>
		/// <param name="id">ID</param>
		/// <returns>name</returns>
		public string GetBuffName(int id)
		{
			return (id > 0 && id < Main.maxBuffs) ? Main.buffName[id] : "null";
		}

		/// <summary>
		/// Gets the description of a buff
		/// </summary>
		/// <param name="id">ID</param>
		/// <returns>description</returns>
		public string GetBuffDescription(int id)
		{
			return (id > 0 && id < Main.maxBuffs) ? Main.buffTip[id] : "null";
		}

		/// <summary>
		/// Gets a list of buffs by name
		/// </summary>
		/// <param name="name">name</param>
		/// <returns>Matching list of buff ids</returns>
		public List<int> GetBuffByName(string name)
		{
			for (int i = 1; i < Main.maxBuffs; i++)
			{
				if (Main.buffName[i].ToLower() == name)
					return new List<int> {i};
			}
			var found = new List<int>();
			for (int i = 1; i < Main.maxBuffs; i++)
			{
				if (Main.buffName[i].ToLower().StartsWith(name.ToLower()))
					found.Add(i);
			}
			return found;
		}

		/// <summary>
		/// Gets a prefix based on its id
		/// </summary>
		/// <param name="id">ID</param>
		/// <returns>Prefix name</returns>
		public string GetPrefixById(int id)
		{
			var item = new Item();
			item.SetDefaults(0);
			item.prefix = (byte) id;
			item.AffixName();
			return item.name.Trim();
		}

		/// <summary>
		/// Gets a list of prefixes by name
		/// </summary>
		/// <param name="name">Name</param>
		/// <returns>List of prefix IDs</returns>
		public List<int> GetPrefixByName(string name)
		{
			Item item = new Item();
			item.SetDefaults(0);
			for (int i = 1; i < 83; i++)
			{
				item.prefix = (byte) i;
				if (item.AffixName().Trim() == name)
					return new List<int> {i};
			}
			var found = new List<int>();
			for (int i = 1; i < 83; i++)
			{
				try
				{
					item.prefix = (byte) i;
					if (item.AffixName().Trim().ToLower() == name.ToLower())
						return new List<int> {i};
					if (item.AffixName().Trim().ToLower().StartsWith(name.ToLower()))
						found.Add(i);
				}
				catch
				{
				}
			}
			return found;
		}

		/// <summary>
		/// Gets a prefix by ID or name
		/// </summary>
		/// <param name="idOrName">ID or name</param>
		/// <returns>List of prefix IDs</returns>
		public List<int> GetPrefixByIdOrName(string idOrName)
		{
			int type = -1;
			if (int.TryParse(idOrName, out type) && type > 0 && type < 84)
			{
				return new List<int> {type};
			}
			return GetPrefixByName(idOrName);
		}

		/// <summary>
		/// Kicks all player from the server without checking for immunetokick permission.
		/// </summary>
		/// <param name="ply">int player</param>
		/// <param name="reason">string reason</param>
		public void ForceKickAll(string reason)
		{
			foreach (TSPlayer player in TShock.Players)
			{
				if (player != null && player.Active)
				{
					ForceKick(player, reason);
				}
			}
		}

		/// <summary>
		/// Kicks a player from the server without checking for immunetokick permission.
		/// </summary>
		/// <param name="ply">int player</param>
		/// <param name="reason">string reason</param>
		public void ForceKick(TSPlayer player, string reason)
		{
			if (!player.ConnectionAlive)
				return;
			player.Disconnect(reason);
			Log.ConsoleInfo(string.Format("{0} was force kicked for : {1}", player.IP, reason));
		}

		public void ForceKick(TSPlayer player, string reason, bool silent)
		{
			player.SilentKickInProgress = true;
			if (!player.ConnectionAlive)
				return;
			player.Disconnect(reason);
			Log.ConsoleInfo(string.Format("{0} was force kicked for : {1}", player.IP, reason));
		}

		/// <summary>
		/// Kicks a player from the server.
		/// </summary>
		/// <param name="ply">int player</param>
		/// <param name="reason">string reason</param>
		public bool Kick(TSPlayer player, string reason, string adminUserName = "")
		{
			if (!player.ConnectionAlive)
				return true;
			if (!player.Group.HasPermission(Permissions.immunetokick))
			{
				string playerName = player.Name;
				player.Disconnect(string.Format("Kicked: {0}", reason));
				Log.ConsoleInfo(string.Format("Kicked {0} for : {1}", playerName, reason));
				if (adminUserName.Length == 0)
					Broadcast(string.Format("{0} was kicked for {1}", playerName, reason.ToLower()));
				else
					Broadcast(string.Format("{0} kicked {1} for {2}", adminUserName, playerName, reason.ToLower()));
				return true;
			}
			return false;
		}

		/// <summary>
		/// Bans and kicks a player from the server.
		/// </summary>
		/// <param name="ply">int player</param>
		/// <param name="reason">string reason</param>
		public bool Ban(TSPlayer player, string reason, string adminUserName = "Server")
		{
			if (!player.ConnectionAlive)
				return true;
			if (!player.Group.HasPermission(Permissions.immunetoban))
			{
				string ip = player.IP;
				string playerName = player.Name;
                TShock.Bans.AddBan(ip, playerName, reason, adminUserName);
				player.Disconnect(string.Format("Banned: {0}", reason));
				Log.ConsoleInfo(string.Format("Banned {0} for : {1}", playerName, reason));
				if (adminUserName.Length == 0)
					Broadcast(string.Format("{0} was banned for {1}", playerName, reason.ToLower()));
				else
					Broadcast(string.Format("{0} banned {1} for {2}", adminUserName, playerName, reason.ToLower()));
				return true;
			}
			return false;
		}

		/// <summary>
		/// Shows a file to the user.
		/// </summary>
		/// <param name="ply">int player</param>
		/// <param name="file">string filename reletave to savedir</param>
		//Todo: Fix this
		public void ShowFileToUser(TSPlayer player, string file)
		{
			string foo = "";
			using (var tr = new StreamReader(Path.Combine(TShock.SavePath, file)))
			{
				while ((foo = tr.ReadLine()) != null)
				{
					foo = foo.Replace("%map%", Main.worldName);
					foo = foo.Replace("%players%", GetPlayers());
					//foo = SanitizeString(foo);
					if (foo.Substring(0, 1) == "%" && foo.Substring(12, 1) == "%") //Look for a beginning color code.
					{
						string possibleColor = foo.Substring(0, 13);
						foo = foo.Remove(0, 13);
						float[] pC = {0, 0, 0};
						possibleColor = possibleColor.Replace("%", "");
						string[] pCc = possibleColor.Split(',');
						if (pCc.Length == 3)
						{
							try
							{
								player.SendMessage(foo, (byte) Convert.ToInt32(pCc[0]), (byte) Convert.ToInt32(pCc[1]),
								                   (byte) Convert.ToInt32(pCc[2]));
								continue;
							}
							catch (Exception e)
							{
								Log.Error(e.ToString());
							}
						}
					}
					player.SendMessage(foo);
				}
			}
		}

		/// <summary>
		/// Returns a Group from the name of the group
		/// </summary>
		/// <param name="ply">string groupName</param>
		public Group GetGroup(string groupName)
		{
			//first attempt on cached groups
			for (int i = 0; i < TShock.Groups.groups.Count; i++)
			{
				if (TShock.Groups.groups[i].Name.Equals(groupName))
				{
					return TShock.Groups.groups[i];
				}
			}
			return new Group(TShock.Config.DefaultGuestGroupName);
		}

		/// <summary>
		/// Returns an IPv4 address from a DNS query
		/// </summary>
		/// <param name="hostname">string ip</param>
		public string GetIPv4Address(string hostname)
		{
			try
			{
				//Get the ipv4 address from GetHostAddresses, if an ip is passed it will return that ip
				var ip = Dns.GetHostAddresses(hostname).FirstOrDefault(i => i.AddressFamily == AddressFamily.InterNetwork);
				//if the dns query was successful then return it, otherwise return an empty string
				return ip != null ? ip.ToString() : "";
			}
			catch (SocketException)
			{
			}
			return "";
		}

        public string HashAlgo = "sha512";

		public readonly Dictionary<string, Func<HashAlgorithm>> HashTypes = new Dictionary<string, Func<HashAlgorithm>>
		                                                                    	{
		                                                                    		{"sha512", () => new SHA512Managed()},
		                                                                    		{"sha256", () => new SHA256Managed()},
		                                                                    		{"md5", () => new MD5Cng()},
		                                                                    		{"sha512-xp", () => SHA512.Create()},
		                                                                    		{"sha256-xp", () => SHA256.Create()},
		                                                                    		{"md5-xp", () => MD5.Create()},
		                                                                    	};

		/// <summary>
		/// Returns a Sha256 string for a given string
		/// </summary>
		/// <param name="bytes">bytes to hash</param>
		/// <returns>string sha256</returns>
		public string HashPassword(byte[] bytes)
		{
			if (bytes == null)
				throw new NullReferenceException("bytes");
			Func<HashAlgorithm> func;
			if (!HashTypes.TryGetValue(HashAlgo.ToLower(), out func))
				throw new NotSupportedException("Hashing algorithm {0} is not supported".SFormat(HashAlgo.ToLower()));

			using (var hash = func())
			{
				var ret = hash.ComputeHash(bytes);
				return ret.Aggregate("", (s, b) => s + b.ToString("X2"));
			}
		}

		/// <summary>
		/// Returns a Sha256 string for a given string
		/// </summary>
		/// <param name="bytes">bytes to hash</param>
		/// <returns>string sha256</returns>
		public string HashPassword(string password)
		{
			if (string.IsNullOrEmpty(password) || password == "non-existant password")
				return "non-existant password";
			return HashPassword(Encoding.UTF8.GetBytes(password));
		}

		/// <summary>
		/// Checks if the string contains any unprintable characters
		/// </summary>
		/// <param name="str">String to check</param>
		/// <returns>True if the string only contains printable characters</returns>
		public bool ValidString(string str)
		{
			foreach (var c in str)
			{
				if (c < 0x20 || c > 0xA9)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Checks if world has hit the max number of chests
		/// </summary>
		/// <returns>True if the entire chest array is used</returns>
		public bool MaxChests()
		{
			for (int i = 0; i < Main.chest.Length; i++)
			{
				if (Main.chest[i] == null)
					return false;
			}
			return true;
		}

        public bool Altar(int x, int y, int center, int cross, int diagonal)
        {
            int[] X = new int[9] { x - 1, x, x + 1, x - 1, x, x + 1, x - 1, x, x + 1 };
            int[] Y = new int[9] { y - 1, y - 1, y - 1, y, y, y, y + 1, y + 1, y + 1 };
            for (int i = 0; i <= 8; i++)
            {
                if (Main.tile[X[i], Y[i]].type == center && Main.tile[X[i], Y[i] - 1].type == cross && Main.tile[X[i], Y[i] + 1].type == cross && Main.tile[X[i] - 1, Y[i]].type == cross && Main.tile[X[i] + 1, Y[i]].type == cross && Main.tile[X[i] - 1, Y[i] - 1].type == diagonal && Main.tile[X[i] + 1, Y[i] - 1].type == diagonal && Main.tile[X[i] - 1, Y[i] + 1].type == diagonal && Main.tile[X[i] + 1, Y[i] + 1].type == diagonal)
                {
                    return true;
                }
                //Altar Debug       
                //Console.WriteLine(Main.tile[X[i], Y[i]].type);
            }

            return false;
        }

        public bool SignName(int x, int y, string name)
        {
            string[] split;
            if (x != 0 && y != 0)
            {
                try
                {
                    split = Main.sign[Sign.ReadSign(x, y)].text.Split('\n');

                    if (split[1].Equals(name) && split.Count() < 8)
                    {
                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        public bool Signs(int x, int y, out string PlayerName, out string[] ItemName, out double Price)
        {
            string[] split;
            PlayerName = string.Empty;
            ItemName = new string[5];
            Price = 0;
            int j = 0;
            double result = 0;
            int[] X = new int[9] { x - 1, x, x + 1, x - 1, x, x + 1, x - 1, x, x + 1 };
            int[] Y = new int[9] { y - 1, y - 1, y - 1, y, y, y, y + 1, y + 1, y + 1 };
            for (int i = 0; i <= 8; i++)
            {
                if (Main.tile[X[i], Y[i]].type == 55)
                {
                    split = Main.sign[Sign.ReadSign(X[i], Y[i])].text.Split('\n');

                    if (split[0].Equals("[Sell]") && split.Count() < 8)
                    {
                        ItemName = new string[split.Count() - 3];

                        PlayerName = split[1];
                        for (int t = 2; t < split.Count() - 1; t++)
                        {
                            ItemName[j] = split[t];
                            j++;
                        }
                        if (double.TryParse(split[split.Count() - 1], out result))
                            Price = Math.Abs(result);
                        else
                        {
                            return false;
                        }
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return false;
        }

        public bool ActiveBlockCheck(int x, int y)
        {
            int[] X = new int[2] { x, x + 1};
            int[] Y = new int[2] { y + 1, y + 1};
            for (int i = 0; i <= 1; i++)
            {
                if (Main.tile[X[i], Y[i]].type == 130)
                {
                        return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Return a time for a Player
        /// </summary>
        /// <param name="name">string name</param>
        public string DispencerTime(string name)
        {
            string result = TShock.DispenserTime.Find(item => item.Contains(name));
            string[] time = result.Split(';');
            return time[1];
        }

        public string UTF8toWin1251Converter(byte[] bytes)
        {
            Encoding win1251 = Encoding.GetEncoding("Windows-1251");
            char[] chars = win1251.GetChars(bytes);
            return new string(chars);
        }

        public string Win1251ToUTF8(string source)
        {

            Encoding utf8 = Encoding.GetEncoding("UTF-8");
            Encoding win1251 = Encoding.GetEncoding("Windows-1251");

            byte[] utf8Bytes = win1251.GetBytes(source);
            byte[] win1251Bytes = Encoding.Convert(utf8, win1251, utf8Bytes);
            source = win1251.GetString(win1251Bytes);
            return source;

        }

        public bool DeclineTrade(TSPlayer player)
        {
            if (player.TradeItem != null && player.TradeItemStack > 0)
            {
                Item item = player.TradeItem;
                byte stack = player.TradeItemStack;
                byte prefix = player.TradeItemPrefix;
                player.GiveItem(item.type, item.name, item.width, item.height, stack, prefix);
                Log.ConsoleInfo(string.Format("[Trade] {0} recieved back {1} x {2}", player.Name, item.name, stack));
                player.TradeAccept = false;
                player.TradeItem = null;
                player.TradeItemStack = 0;
                player.TradeItemPrefix = 0;
            }
            if (player.TradeRequestedByMan != null)
            {
                if (player.TradeRequestedByMan.TradeItem != null && player.TradeRequestedByMan.TradeItemStack > 0)
                {
                    Item item = player.TradeRequestedByMan.TradeItem;
                    byte stack = player.TradeRequestedByMan.TradeItemStack;
                    byte prefix = player.TradeRequestedByMan.TradeItemPrefix;
                    player.TradeRequestedByMan.GiveItem(item.type, item.name, item.width, item.height, stack, prefix);
                    Log.ConsoleInfo(string.Format("[Trade] {0} recieved back {1} x {2}", player.TradeRequestedByMan.Name, item.name, stack));
                    player.TradeRequestedByMan.TradeAccept = false;
                    player.TradeItem = null;
                    player.TradeItemStack = 0;
                    player.TradeItemPrefix = 0;
                }
            }
            else
            {
                if (player.TradeMan.TradeItem != null && player.TradeMan.TradeItemStack > 0)
                {
                    Item item = player.TradeMan.TradeItem;
                    byte stack = player.TradeMan.TradeItemStack;
                    byte prefix = player.TradeMan.TradeItemPrefix;
                    player.TradeMan.GiveItem(item.type, item.name, item.width, item.height, stack, prefix);
                    Log.ConsoleInfo(string.Format("[Trade] {0} recieved back {1} x {2}", player.TradeMan.Name, item.name, stack));
                    player.TradeMan.TradeAccept = false;
                    player.TradeItem = null;
                    player.TradeItemStack = 0;
                    player.TradeItemPrefix = 0;
                }
            }
            if (player.TradeRequestedByMan != null)
            {
                player.TradeRequestedByMan.InTrade = false;
                player.TradeRequestedByMan.TradeMan = null;
                player.TradeRequestedByMan.TradeRequestedByMan = null;
                player.TradeRequestedByMan.TradeRC = 0;
            }
            else
            {
                player.TradeMan.InTrade = false;
                player.TradeMan.TradeMan = null;
                player.TradeMan.TradeRequestedByMan = null;
                player.TradeMan.TradeRC = 0;
            }
            player.InTrade = false;
            player.TradeMan = null;
            player.TradeRequestedByMan = null;
            player.TradeRC = 0;
            return true;
        }

        public int GetDistanceToSpawn(int x, int y)
        {
            int Distance = 0;
            Point Spawn = new Point(Main.spawnTileX, Main.spawnTileY);
            Distance = Math.Abs(x - Spawn.X) + Math.Abs(y - Spawn.Y);
            return Distance;
        }

        public bool Cyrillic(string name)
        {
            Regex reg = new Regex(@"^([^�-��-�]+)$");
            if (!reg.IsMatch(name))
                return true;
            else
                return false;
        }

        public bool CheckPlayerOnOtherServer(string name)
        {
            string text = new WebClient().DownloadString("http://rogerpaladin.dyndns.org:" + TShock.Config.SecondServerRestApiPort + "/status/");
            string[] split1 = text.Remove(0 , 1).Split('/');
            if (split1[0].Contains(","))
            {
                string[] split2 = split1[0].Split(',');
                foreach (string s in split2)
                {
                    if (name.Equals(s.Remove(0, 1)))
                        return true;
                }
            }
            if (split1[0] != "")
            {
                if (name.Equals(split1[0].Remove(0, 1)))
                    return true; 
            }

            return false;
        }

        public double RegionPrice(int RegionX, int RegionY, int RegionWidth, int RegionHeight, out double tilecost)
        {
            double price = 0;
            double modifier = 1;
            int tiles = 0;
            Point RegionCenter = new Point(RegionX + RegionWidth / 2, RegionY + RegionHeight / 2);
            int Distance = TShock.Utils.GetDistanceToSpawn(RegionCenter.X, RegionCenter.Y);
            tiles = (RegionHeight * RegionWidth) - TShock.Config.MaximumSquarePerRegion;
            if (Distance < 100)
            {
                if (tiles < 0)
                    tiles = Math.Abs(tiles);
                modifier = 1;
            }
            else
            {
                modifier = (double)100 / (double)Distance;
            }

            if (tiles < 0)
            {
                tiles = 0;
            }
            price = tiles * 2 * modifier;
            tilecost = Math.Round(2 * modifier, 2);
            return Math.Round(price, 2);
        }
		/// <summary>
		/// Searches for a projectile by identity and owner
		/// </summary>
		/// <param name="identity">identity</param>
		/// <param name="owner">owner</param>
		/// <returns>projectile ID</returns>
		public int SearchProjectile(short identity, int owner)
		{
			for (int i = 0; i < Main.maxProjectiles; i++)
			{
				if (Main.projectile[i].identity == identity && Main.projectile[i].owner == owner)
					return i;
			}
			return 1000;
		}

		/// <summary>
		/// Sanitizes input strings
		/// </summary>
		/// <param name="str">string</param>
		/// <returns>sanitized string</returns>
		public string SanitizeString(string str)
		{
			var returnstr = str.ToCharArray();
			for (int i = 0; i < str.Length; i++)
			{
				if (!ValidString(str[i].ToString()))
					returnstr[i] = ' ';
			}
			return new string(returnstr);
		}
	}
}