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
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml;
using MySql.Data.MySqlClient;
using Terraria;

namespace TShockAPI.DB
{
    public class TownManager
    {
        public List<Town> Towns = new List<Town>();

        private IDbConnection database;

        public TownManager(IDbConnection db)
        {
            database = db;
            var table = new SqlTable("Towns",
                                     new SqlColumn("ID", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
                                     new SqlColumn("X1", MySqlDbType.Int32),
                                     new SqlColumn("Y1", MySqlDbType.Int32),
                                     new SqlColumn("width", MySqlDbType.Int32),
                                     new SqlColumn("height", MySqlDbType.Int32),
                                     new SqlColumn("TownName", MySqlDbType.VarChar, 50),
                                     new SqlColumn("Mayor", MySqlDbType.VarChar, 50),
                                     new SqlColumn("WorldID", MySqlDbType.VarChar, 32)
                );
            var creator = new SqlTableCreator(db,
                                              db.GetSqlType() == SqlType.Sqlite
                                                ? (IQueryBuilder)new SqliteQueryCreator()
                                                : new MysqlQueryCreator());
            creator.EnsureExists(table);

            ReloadAllTowns();
        }

        public void ReloadAllTowns()
        {
            try
            {
                using (var reader = database.QueryReader("SELECT * FROM Towns WHERE WorldID=@0", Main.worldID.ToString()))
                {
                    Towns.Clear();
                    while (reader.Read())
                    {
                        int X1 = reader.Get<int>("X1");
                        int Y1 = reader.Get<int>("Y1");
                        int height = reader.Get<int>("height");
                        int width = reader.Get<int>("width");
                        string name = reader.Get<string>("TownName");
                        string mayor = reader.Get<string>("Mayor");

                        Town r = new Town(new Rectangle(X1, Y1, width, height), name, mayor, Main.worldID.ToString());

                        Towns.Add(r);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        public bool AddTown(int tx, int ty, int width, int height, string townname, string mayor, string worldid)
        {
            if (GetTownByName(townname) != null)
            {
                return false;
            }
            try
            {
                database.Query(
                    "INSERT INTO Towns (X1, Y1, width, height, TownName, Mayor, WorldID) VALUES (@0, @1, @2, @3, @4, @5, @6);",
                    tx, ty, width, height, townname, mayor, worldid);
                Towns.Add(new Town(new Rectangle(tx, ty, width, height), townname, mayor, worldid));
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            return false;
        }

        public bool DeleteTown(string name)
        {
            try
            {
                database.Query("DELETE FROM Towns WHERE LOWER (TownName) = @0 AND WorldID=@1", name.ToLower(), Main.worldID.ToString());
                var worldid = Main.worldID.ToString();
                Towns.RemoveAll(r => r.Name.ToLower() == name.ToLower() && r.WorldID == worldid);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            return false;
        }

        public bool CanBuild(int x, int y, TSPlayer ply, out string Mayor)
        {
            Mayor = string.Empty;
            if (!ply.Group.HasPermission(Permissions.canbuild))
            {
                return false;
            }
            for (int i = 0; i < Towns.Count; i++)
            {
                if (Towns[i].InArea(new Rectangle(x, y, 0, 0)) && !Towns[i].HasPermissionToBuildInRegion(ply, out Mayor))
                {
                    return false;
                }
            }
            return true;
        }

        public bool InArea(int x, int y, out string TownName)
        {
            TownName = string.Empty;
            foreach (Town town in Towns)
            {
                if (x > town.Area.Left && x < town.Area.Right &&
                    y > town.Area.Top && y < town.Area.Bottom)
                {
                    TownName = town.Name;
                    return true;
                }
            }
            return false;
        }

        public bool resizeTown(string TownName, int addAmount, int direction)
        {
            //0 = up
            //1 = right
            //2 = down
            //3 = left
            int X = 0;
            int Y = 0;
            int height = 0;
            int width = 0;
            try
            {
                using (
                    var reader = database.QueryReader("SELECT X1, Y1, height, width FROM Towns WHERE LOWER (TownName) = @0 AND WorldID=@1",
                                                      TownName.ToLower(), Main.worldID.ToString()))
                {
                    if (reader.Read())
                        X = reader.Get<int>("X1");
                    width = reader.Get<int>("width");
                    Y = reader.Get<int>("Y1");
                    height = reader.Get<int>("height");
                }
                if (!(direction == 0))
                {
                    if (!(direction == 1))
                    {
                        if (!(direction == 2))
                        {
                            if (!(direction == 3))
                            {
                                return false;
                            }
                            else
                            {
                                X -= addAmount;
                                width += addAmount;
                            }
                        }
                        else
                        {
                            height += addAmount;
                        }
                    }
                    else
                    {
                        width += addAmount;
                    }
                }
                else
                {
                    Y -= addAmount;
                    height += addAmount;
                }
                int q =
                    database.Query(
                        "UPDATE Towns SET X1 = @0, Y1 = @1, width = @2, height = @3 WHERE LOWER (TownName) = @4 AND WorldID=@5", X, Y, width,
                        height, TownName.ToLower(), Main.worldID.ToString());
                if (q > 0)
                {
                    foreach (var t in Towns)
                    {
                        if (t.Name.ToLower() == TownName.ToLower() && t.WorldID == Main.worldID.ToString())
                        {
                            t.Area = new Rectangle(X, Y, width, height);
                        }
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            return false;
        }

        /// <summary>
        /// Gets all the town names from world
        /// </summary>
        /// <param name="worldid">World name to get towns from</param>
        /// <returns>List of towns with only their names</returns>
        public List<Town> ListAllTowns(string worldid)
        {
            var towns = new List<Town>();
            try
            {
                using (var reader = database.QueryReader("SELECT TownName FROM Towns WHERE WorldID=@0", worldid))
                {
                    while (reader.Read())
                        towns.Add(new Town { Name = reader.Get<string>("TownName") });
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            return towns;
        }

        public Town GetTownByName(string name)
        {
            return Towns.FirstOrDefault(r => r.Name.ToLower().Equals(name.ToLower()) && r.WorldID == Main.worldID.ToString());
        }

        public Town GetTownByMayorName(string MayorName)
        {
            return Towns.FirstOrDefault(r => r.Mayor.ToLower().Equals(MayorName.ToLower()) && r.WorldID == Main.worldID.ToString());
        }

        public bool ChangeMayor(string TownName, string newMayor)
        {
            var town = GetTownByName(TownName);
            if (town != null)
            {
                town.Mayor = newMayor;
                int q = database.Query("UPDATE Towns SET Mayor=@0 WHERE LOWER (TownName) = @1 AND WorldID=@2", newMayor,
                                       TownName.ToLower(), Main.worldID.ToString());
                if (q > 0)
                {
                    foreach (var t in Towns)
                    {
                        if (t.Name.ToLower() == TownName.ToLower() && t.WorldID == Main.worldID.ToString())
                            t.Mayor = newMayor;
                    }
                    return true;
                }
            }
            return false;
        }

        public bool MayorCheck(TSPlayer Player)
        {
            foreach (var t in Towns)
            {
                if (t.WorldID == Main.worldID.ToString())
                    if (t.Mayor.Equals(Player.Name))
                        return true;
            }
            return false;
        }

        public string GetMayor(string TownName)
        {
            string Mayor = string.Empty;
            foreach (var t in Towns)
            {
                if (t.Name.ToLower() == TownName.ToLower() && t.WorldID == Main.worldID.ToString())
                    Mayor = t.Mayor;
            }
            return Mayor;
        }

        public List<string> GetTownsPeople(string TownName)
        {
            List<string> TownsPeople = new List<string>();
            try
            {
                var town = GetTownByName(TownName);
                foreach (Region r in TShock.Regions.Regions)
                {
                    if (town.InArea(r.Area))
                    {
                        for (int i = 0; i < r.AllowedIDs.Count; i++)
                        {
                            if (!TownsPeople.Contains(TShock.Users.GetNameForID((int)r.AllowedIDs[i])))
                                TownsPeople.Add(TShock.Users.GetNameForID((int)r.AllowedIDs[i]));
                        }
                    }
                }
                return TownsPeople;
            }
            catch
            {
                return null;
            }
        }

        public class Town
        {
            public Rectangle Area { get; set; }
            public string Name { get; set; }
            public string Mayor { get; set; }
            public string WorldID { get; set; }

            public Town(Rectangle Town, string name, string mayor, string RegionWorldIDz)
                : this()
            {
                Area = Town;
                Name = name;
                Mayor = mayor;
                WorldID = RegionWorldIDz;
            }

            public Town()
            {
                Area = Rectangle.Empty;
                Name = string.Empty;
                Mayor = string.Empty;
                WorldID = string.Empty;
            }

            public bool InArea(Rectangle point)
            {
                if (Area.Contains(point.X, point.Y))
                {
                    return true;
                }
                return false;
            }

            public bool HasPermissionToBuildInRegion(TSPlayer ply, out string _Mayor)
            {
                _Mayor = string.Empty;
                if (!ply.IsLoggedIn)
                {
                    ply.SendMessage("You must be logged in to take advantage of protected regions.", Color.Red);
                    return false;
                }
                if (ply.Name.Equals(Mayor))
                {
                    _Mayor = Mayor;
                    return true;
                }

                _Mayor = Mayor;
                return false;
            }
        }
    }
}