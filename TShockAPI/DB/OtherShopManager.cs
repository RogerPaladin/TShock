using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using MySql.Data.MySqlClient;

namespace TShockAPI.DB
{
    public class OtherShopManager
    {
        public IDbConnection database;

        public OtherShopManager(IDbConnection db)
        {
            database = db;

            var table = new SqlTable("Other",
                new SqlColumn("ID", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
                new SqlColumn("Name", MySqlDbType.VarChar, 32),
                new SqlColumn("InGameName", MySqlDbType.VarChar, 128),
                new SqlColumn("Contains", MySqlDbType.VarChar, 128),
                new SqlColumn("Price", MySqlDbType.Int32));
            var creator = new SqlTableCreator(db, db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
            creator.EnsureExists(table);
        }

        public List<string> InGameNames()
        {
            try
            {
                QueryResult result;
                var Other = new List<String>();
                result = database.QueryReader("SELECT * FROM Other ORDER BY Price");
                using (var reader = result)
                {
                    while (reader.Read())
                    {
                        Other.Add(reader.Get<string>("InGameName") + "(" + reader.Get<int>("Price") + ")");
                    }
                }
                return Other;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return null;
            }

        }

        public bool GetOther(string ReqName, out string Name, out string Contains, out double Price)
        {
            Price = 0;
            Name = string.Empty;
            Contains = string.Empty;
            try
            {
                using (var reader = database.QueryReader("SELECT * FROM Other WHERE InGameName=@0", ReqName))
                {
                    if (reader.Read())
                    {
                        Name = reader.Get<string>("Name");
                        Contains = reader.Get<string>("Contains");
                        Price = reader.Get<double>("Price");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }

            return false;
        }
    }
}

