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

using MySql.Data.MySqlClient;
using Terraria;

namespace TShockAPI.DB
{
    public class HomeManager
    {
        public IDbConnection database;

        public HomeManager(IDbConnection db)
        {
            database = db;

            var table = new SqlTable("Home",
                new SqlColumn("ID", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
                new SqlColumn("Name", MySqlDbType.VarChar, 50),
                new SqlColumn("X", MySqlDbType.Int32),
                new SqlColumn("Y", MySqlDbType.Int32),
                new SqlColumn("WorldID", MySqlDbType.VarChar, 32)
            );
            var creator = new SqlTableCreator(db, db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
            creator.EnsureExists(table);
        }

        public Vector2 GetHome(string name)
        {
            try
            {
                using (var reader = database.QueryReader("SELECT * FROM Home WHERE Name=@0 AND WorldID = @1", name, Main.worldID.ToString()))
                {
                    if (reader.Read())
                    {
                        return new Vector2(reader.Get<int>("X"), reader.Get<int>("Y"));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }

            return new Vector2();
        }

        public void InsertHome(string name, int X, int Y)
        {
            if (GetHome(name) == Vector2.Zero)
            {
                try
                {
                    database.Query("INSERT INTO Home (Name, X, Y, WorldID) VALUES (@0, @1, @2, @3);", name, X, Y + 3, Main.worldID.ToString());
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
            else
            {
                try
                {
                    database.Query("UPDATE Home SET X = @0, Y = @1 WHERE Name = @2 AND WorldID = @3;", X, Y + 3, name, Main.worldID.ToString());
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
        }
    }
}
