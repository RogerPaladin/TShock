
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
using System.Data;
using System.IO;
using MySql.Data.MySqlClient;
using Microsoft.Xna.Framework;

namespace TShockAPI.DB
{
    public class UserManager
    {
        private IDbConnection database;

        public UserManager(IDbConnection db)
        {
            database = db;

            var table = new SqlTable("Users",
                new SqlColumn("ID", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
                new SqlColumn("Username", MySqlDbType.VarChar, 32) { Unique = true },
                new SqlColumn("Password", MySqlDbType.VarChar, 128),
                new SqlColumn("Usergroup", MySqlDbType.Text),
                new SqlColumn("IP", MySqlDbType.VarChar, 16),
                new SqlColumn("LastLogin", MySqlDbType.VarChar, 32),
                new SqlColumn("PlayingTime", MySqlDbType.Int32),
                new SqlColumn("RCoins", MySqlDbType.Double)
                );
            
            var creator = new SqlTableCreator(db, db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
            creator.EnsureExists(table);

            String file = Path.Combine(TShock.SavePath, "users.txt");
            if (File.Exists(file))
            {
                using (StreamReader sr = new StreamReader(file))
                {
                    String line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Equals("") || line.Substring(0, 1).Equals("#"))
                            continue;
                        String[] info = line.Split(' ');
                        String username = "";
                        String sha = "";
                        String group = "";
                        String ip = "";
                        String lastlogin = "0";
                        int playingtime = 0;
                        
                        String[] nameSha = info[0].Split(':');

                        if (nameSha.Length < 2)
                        {
                            username = nameSha[0];
                            ip = nameSha[0];
                            group = info[1];
                        }
                        else
                        {
                            username = nameSha[0];
                            sha = nameSha[1];
                            group = info[1];
                        }

                        string query = (TShock.Config.StorageType.ToLower() == "sqlite") ?
                            "INSERT OR IGNORE INTO Users (Username, Password, Usergroup, IP, LastLogin, PlayingTime) VALUES (@0, @1, @2, @3, @4, @5)" :
                            "INSERT IGNORE INTO Users SET Username=@0, Password=@1, Usergroup=@2, IP=@3, Lastlogin=@4, PlayingTime=@5";

                        database.Query(query, username.Trim(), sha.Trim(), group.Trim(), ip.Trim(), lastlogin.Trim(), playingtime);
                    }
                }
                String path = Path.Combine(TShock.SavePath, "old_configs");
                String file2 = Path.Combine(path, "users.txt");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                if (File.Exists(file2))
                    File.Delete(file2);
                File.Move(file, file2);
            }

        }

        /// <summary>
        /// Adds a given username to the database
        /// </summary>
        /// <param name="user">User user</param>
        public void AddUser(User user)
        {
            try
            {
                if (!TShock.Groups.GroupExists(user.Group))
                    throw new GroupNotExistsException(user.Group);

                if (database.Query("INSERT INTO Users (Username, Password, UserGroup, IP, LastLogin, PlayingTime, RCoins) VALUES (@0, @1, @2, @3, @4, @5, @6);", user.Name, Tools.HashPassword(user.Password), user.Group, user.Address, Convert.ToString(DateTime.Now.ToFileTime()), 0, 0) < 1)
                    throw new UserExistsException(user.Name);
            }
            catch (Exception ex)
            {
                throw new UserManagerException("AddUser SQL returned an error", ex);
            }
        }

        /// <summary>
        /// Removes a given username from the database
        /// </summary>
        /// <param name="user">User user</param>
        public void RemoveUser(User user)
        {
            try
            {
                int affected = -1;
                if (!string.IsNullOrEmpty(user.Address))
                {
                    affected = database.Query("DELETE FROM Users WHERE IP=@0", user.Address);
                }
                else
                {
                    affected = database.Query("DELETE FROM Users WHERE LOWER (Username)=@0", user.Name.ToLower());
                }

                if (affected < 1)
                    throw new UserNotExistException(string.IsNullOrEmpty(user.Address) ? user.Name : user.Address);
            }
            catch (Exception ex)
            {
                throw new UserManagerException("RemoveUser SQL returned an error", ex);
            }
        }

        /// <summary>
        /// Removes a user from database if last login time bigger than x days (x*24*60)
        /// </summary>
        /// <param name="time">int time</param>
        public void DeletePlayersAfterMinutes(int time)
        {
            string MergedIDs = string.Empty;
            string PlayerName = string.Empty;
            string PlayerGroup = string.Empty;
            DateTime LastLogin = DateTime.Now;
            
            using (var reader = database.QueryReader("SELECT * FROM Users WHERE LastLogin < @0;", DateTime.Now.AddMinutes(-time).ToFileTime()))
           {
                while (reader.Read())
                {
                    MergedIDs = reader.Get<string>("ID");
                    PlayerName = reader.Get<string>("Username");
                    PlayerGroup = reader.Get<string>("Usergroup");
                    LastLogin = DateTime.FromFileTime(reader.Get<long>("LastLogin"));
                    if (!PlayerGroup.Equals("admin") && !PlayerGroup.Equals("trustedadmin") && !PlayerGroup.Equals("superadmin"))
                    {
                    TShock.Regions.DeleteRegionAfterMinutes(PlayerName);
                    TShock.Regions.DeleteOwnersAfterMinutes(PlayerName);
                    database.Query("DELETE FROM Users WHERE LOWER (Username) = @0;", PlayerName.ToLower());
                    Log.ConsoleInfo(string.Format("Player {0}:{1} [{2}] deleted - lastlogin {3}", MergedIDs, PlayerName, PlayerGroup, LastLogin));
                    }
                 }
            }
        }
        
        /// <summary>
        /// Sets the login time for a given username
        /// </summary>
        /// <param name="user">TSplayer user</param>
        public void Login(TSPlayer user)
        {
            try
            {
                if (database.Query("UPDATE Users SET LastLogin = @0, IP = @1 WHERE LOWER (Username) = @2;", Convert.ToString(DateTime.Now.ToFileTime()), user.IP, user.Name.ToLower()) == 0)
                    throw new UserNotExistException(user.Name);
            }
            catch (Exception ex)
            {
                throw new UserManagerException("Login SQL returned an error", ex);
            }
        }

        /// <summary>
        /// Sets the total played time for a given username
        /// </summary>
        /// <param name="Name">String Name</param>
        /// <param name="PlayingTime">integer PlayingTime</param>
        public void PlayingTime(string Name, int PlayingTime)
        {
            try
            {
                var user = TShock.Users.GetUserByName(Name);
                if (database.Query("UPDATE Users SET PlayingTime = @0 WHERE LOWER (Username) = @1;", (Convert.ToInt32(user.PlayingTime) + PlayingTime), user.Name.ToLower()) == 0)
                    throw new UserNotExistException(user.Name);
            }
            catch (Exception ex)
            {
                throw new UserManagerException("PlayingTime SQL returned an error", ex);
            }
        }

        /// <summary>
        /// Check the RCoins for a given username
        /// </summary>
        /// <param name="Name">String Name</param>
        /// <param name="RCoins">double RCoins</param>
        public bool Buy(string Name, double RCoins, bool check = false)
        {
            double rcoins = 0;
            try
            {
                var user = TShock.Users.GetUserByName(Name);
                var player = Tools.FindPlayer(Name);
                var plr = player[0];
                
                using (var reader = database.QueryReader("SELECT * FROM Users WHERE LOWER (Username) = @0;", Name.ToLower()))
                {
                    if (reader.Read())
                    {
                        rcoins = reader.Get<double>("RCoins");
                        
                        if (plr.Group.HasPermission("rich"))
                        {
                            return true;
                        }

                        if (rcoins >= RCoins && check == false)
                        {
                            TShock.Users.SetRCoins(Name, -RCoins);
                            return true;
                        }
                        if (rcoins >= RCoins && check == true)
                        {
                            return true;
                        }
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new UserManagerException("CheckRCoins SQL returned an error", ex);
            }
            return false;
        }
        
        /// <summary>
        /// Sets the RCoins for a given username
        /// </summary>
        /// <param name="Name">String Name</param>
        /// <param name="RCoins">double RCoins</param>
        public void SetRCoins(string Name, double RCoins)
        {
            try
            {
                var user = TShock.Users.GetUserByName(Name);
                if (RCoins < 0)
                {
                    Log.ConsoleInfo(string.Format("[RCoins] Player <{0}> spent {1} RCoins. Before {2} - After {3}", Name, Math.Abs(RCoins), Math.Round(user.RCoins, 2), (Math.Round(user.RCoins, 2) + Math.Round(RCoins, 2))));
                }
                else
                {
                    Log.ConsoleInfo(string.Format("[RCoins] Player <{0}> gained {1} RCoins. Before {2} - After {3}", Name, RCoins, Math.Round(user.RCoins, 2), (Math.Round(user.RCoins, 2) + Math.Round(RCoins, 2))));
                }
                if (database.Query("UPDATE Users SET RCoins = @0 WHERE LOWER (Username) = @1;", ((user.RCoins) + RCoins), user.Name.ToLower()) == 0)
                    throw new UserNotExistException(user.Name);
            }
            catch (Exception ex)
            {
                throw new UserManagerException("RCoins SQL returned an error", ex);
            }
        }

        /// <summary>
        /// Show the RCoins for a given username
        /// </summary>
        /// <param name="player">TSPlayer player</param>
        public void Status(TSPlayer player, string name = "")
        {
            string playername = string.Empty;
            int playingtime = 0;
            double rcoins = 0;
            var user = TShock.Users.GetUserByName(player.Name);
            if (name != "")
            {
                using (var reader = database.QueryReader("SELECT * FROM Users WHERE LOWER (Username) = @0;", name.ToLower()))
                {
                    if (reader.Read())
                    {
                        playername = reader.Get<string>("Username");
                        playingtime = reader.Get<int>("PlayingTime");
                        rcoins = reader.Get<double>("RCoins");
                        player.SendMessage(string.Format("Player <{0}> have {1} RCoins. Total played time is {2} minutes.", playername, rcoins, playingtime), Color.LightGreen);
                        return;
                    }
                    else
                    {
                        player.SendMessage("No players found", Color.Red);
                        return;
                    }
                }
            }
            player.SendMessage("You have " + user.RCoins + " RCoins.", Color.LightPink);
            player.SendMessage("Your total played time is " + user.PlayingTime + " minutes.", Color.LightGreen);
        }
        
        /// <summary>
        /// Show the top of players
        /// </summary>
        /// <param name="player">Tsplayer player</param>
        public void Top(TSPlayer player, bool rc = true)
        {
            string playername = string.Empty;
            int playingtime = 0;
            int count = 0;
            double rcoins = 0;
            var user = TShock.Users.GetUserByName(player.Name);
            try
            {
                if (rc == true)
                {
                    using (var reader = database.QueryReader("SELECT * FROM Users ORDER BY RCoins DESC LIMIT 3"))
                            while (reader.Read())
                        {
                            count++;
                            playername = reader.Get<string>("Username");
                            playingtime = reader.Get<int>("PlayingTime");
                            rcoins = reader.Get<double>("RCoins");
                            
                                if (count == 1)
                                player.SendMessage(string.Format("{0} place - <{1}>. Have {2} RCoins. Total played time is {3} minutes.", count, playername, Math.Round(rcoins, 2), playingtime), Color.LightPink);
                            if (count == 2)
                                player.SendMessage(string.Format("{0} place - <{1}>. Have {2} RCoins. Total played time is {3} minutes.", count, playername, Math.Round(rcoins, 2), playingtime), Color.LightGreen);
                            if (count == 3)
                                player.SendMessage(string.Format("{0} place - <{1}>. Have {2} RCoins. Total played time is {3} minutes.", count, playername, Math.Round(rcoins, 2), playingtime), Color.LightBlue);
                        }
                }
                else
                {
                    using (var reader = database.QueryReader("SELECT * FROM Users ORDER BY PlayingTime DESC LIMIT 3"))
                        while (reader.Read())
                        {
                            count++;
                            playername = reader.Get<string>("Username");
                            playingtime = reader.Get<int>("PlayingTime");
                            rcoins = reader.Get<double>("RCoins");

                            if (count == 1)
                                player.SendMessage(string.Format("{0} place - <{1}>. Have {2} RCoins. Total played time is {3} minutes.", count, playername, Math.Round(rcoins, 2), playingtime), Color.LightPink);
                            if (count == 2)
                                player.SendMessage(string.Format("{0} place - <{1}>. Have {2} RCoins. Total played time is {3} minutes.", count, playername, Math.Round(rcoins, 2), playingtime), Color.LightGreen);
                            if (count == 3)
                                player.SendMessage(string.Format("{0} place - <{1}>. Have {2} RCoins. Total played time is {3} minutes.", count, playername, Math.Round(rcoins, 2), playingtime), Color.LightBlue);
                        }
                }

            }
            catch (Exception ex)
            {
                throw new UserManagerException("Top SQL returned an error", ex);
            }
            player.SendMessage("You have " + Math.Round(user.RCoins, 2) + " Rcoins. Total played time is " + user.PlayingTime + " minutes", Color.Yellow);
        }

        /// <summary>
        /// Automatically sets vip group for users who played 1500 minutes.
        /// </summary>
        /// <param name="viptime">int viptime</param>
        public void AutoVip(int viptime)
        {
            string playername = string.Empty;
            try
            {
                using (var reader = database.QueryReader("SELECT * FROM Users WHERE PlayingTime > @0 AND Usergroup = @1;", viptime, "registered"))
                {
                    while (reader.Read())
                    {
                        playername = reader.Get<string>("Username");
                        var user = GetUserByName(playername);
                        SetUserGroup(user, "vip");
                        Log.ConsoleInfo(string.Format("Player <{0}> vipped.", user.Name));
                    }
                }
            }
            catch (Exception ex)
            {
                throw new UserManagerException("AutoVip SQL returned an error", ex);
            }
        }
        
        /// <summary>
        /// Sets the Hashed Password for a given username
        /// </summary>
        /// <param name="user">User user</param>
        /// <param name="group">string password</param>
        public void SetUserPassword(User user, string password)
        {
            try
            {
                if (database.Query("UPDATE Users SET Password = @0 WHERE LOWER (Username) = @1;", Tools.HashPassword(password), user.Name.ToLower()) == 0)
                    throw new UserNotExistException(user.Name);
            }
            catch (Exception ex)
            {
                throw new UserManagerException("SetUserPassword SQL returned an error", ex);
            }
        }

        /// <summary>
        /// Sets the group for a given username
        /// </summary>
        /// <param name="user">User user</param>
        /// <param name="group">string group</param>
        public void SetUserGroup(User user, string group)
        {
            try
            {
                if (!TShock.Groups.GroupExists(group))
                    throw new GroupNotExistsException(group);

                if (database.Query("UPDATE Users SET Usergroup = @0 WHERE LOWER (Username) = @1;", group, user.Name.ToLower()) == 0)
                    throw new UserNotExistException(user.Name);
            }
            catch (Exception ex)
            {
                throw new UserManagerException("SetUserGroup SQL returned an error", ex);
            }
        }

        public int GetUserID(string username)
        {
            try
            {
                using (var reader = database.QueryReader("SELECT * FROM Users WHERE LOWER (Username) = @0", username.ToLower()))
                {
                    if (reader.Read())
                    {
                        return reader.Get<int>("ID");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.ConsoleError("FetchHashedPasswordAndGroup SQL returned an error: " + ex);
            }
            return -1;
        }

        /// <summary>
        /// Returns a Name for a ID from the database
        /// </summary>
        /// <param name="ply">int ID</param>
        public string GetNameForID(int ID)
        {
            try
            {
                using (var reader = database.QueryReader("SELECT * FROM Users WHERE ID=@0", ID))
                {
                    if (reader.Read())
                    {
                        return reader.Get<string>("Username");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.ConsoleError("GetNameForID SQL returned an error: " + ex);
            }
            return "";
        }
        
        /// <summary>
        /// Returns a Group for a ip from the database
        /// </summary>
        /// <param name="ply">string ip</param>
        public Group GetGroupForIP(string ip)
        {
            try
            {
                using (var reader = database.QueryReader("SELECT * FROM Users WHERE IP=@0", ip))
                {
                    if (reader.Read())
                    {
                        string group = reader.Get<string>("UserGroup");
                        return Tools.GetGroup(group);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.ConsoleError("GetGroupForIP SQL returned an error: " + ex);
            }
            return Tools.GetGroup("default");
        }

        public Group GetGroupForIPExpensive(string ip)
        {
            try
            {
                using (var reader = database.QueryReader("SELECT IP, UserGroup FROM Users"))
                {
                    while (reader.Read())
                    {
                        if (Tools.GetIPv4Address(reader.Get<string>("IP")) == ip)
                        {
                            return Tools.GetGroup(reader.Get<string>("UserGroup"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.ConsoleError("GetGroupForIP SQL returned an error: " + ex);
            }
            return Tools.GetGroup("default");
        }


        public User GetUserByName(string name)
        {
            try
            {
                return GetUser(new User { Name = name });
            }
            catch (UserManagerException)
            {
                return null;
            }
        }
        public User GetUserByID(int id)
        {
            try
            {
                return GetUser(new User { ID = id });
            }
            catch (UserManagerException)
            {
                return null;
            }
        }
        public User GetUserByIP(string ip)
        {
            try
            {
                return GetUser(new User { Address = ip });
            }
            catch (UserManagerException)
            {
                return null;
            }
        }
        public User GetUser(User user)
        {
            try
            {
                QueryResult result;
                if (string.IsNullOrEmpty(user.Address))
                {
                    result = database.QueryReader("SELECT * FROM Users WHERE LOWER (Username) = @0", user.Name.ToLower());
                }
                else
                {
                    result = database.QueryReader("SELECT * FROM Users WHERE IP=@0", user.Address);
                }

                using (var reader = result)
                {
                    if (reader.Read())
                    {
                        user.ID = reader.Get<int>("ID");
                        user.Group = reader.Get<string>("Usergroup");
                        user.Password = reader.Get<string>("Password");
                        user.Name = reader.Get<string>("Username");
                        user.Address = reader.Get<string>("IP");
                        user.LastLogin = DateTime.FromFileTime(reader.Get<long>("LastLogin"));
                        user.PlayingTime = reader.Get<int>("PlayingTime");
                        user.RCoins = reader.Get<double>("RCoins");
                        return user;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new UserManagerException("GetUserID SQL returned an error", ex);
            }
            throw new UserNotExistException(string.IsNullOrEmpty(user.Address) ? user.Name : user.Address);
        }
    }

    public class User
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public string Group { get; set; }
        public string Address { get; set; }
        public DateTime LastLogin { get; set; }
        public int PlayingTime { get; set; }
        public double RCoins { get; set; }

        public User(string ip, string name, string pass, string group, DateTime lastlogin, int playingtime, double rcoins)
        {
            Address = ip;
            Name = name;
            Password = pass;
            Group = group;
            LastLogin = lastlogin;
            PlayingTime = playingtime;
            RCoins = rcoins;
        }
        public User()
        {
            Address = "";
            Name = "";
            Password = "";
            Group = "";
            LastLogin = DateTime.Now;
            PlayingTime = 0;
            RCoins = 0;
        }
    }

    [Serializable]
    public class UserManagerException : Exception
    {
        public UserManagerException(string message)
            : base(message)
        {

        }
        public UserManagerException(string message, Exception inner)
            : base(message, inner)
        {

        }
    }
    [Serializable]
    public class UserExistsException : UserManagerException
    {
        public UserExistsException(string name)
            : base("User '" + name + "' already exists")
        {
        }
    }
    [Serializable]
    public class UserNotExistException : UserManagerException
    {
        public UserNotExistException(string name)
            : base("User '" + name + "' does not exist")
        {
        }
    }
    [Serializable]
    public class GroupNotExistsException : UserManagerException
    {
        public GroupNotExistsException(string group)
            : base("Group '" + group + "' does not exist")
        {
        }
    }
}
