using System;
using System.Data;
using MySql.Data.MySqlClient;

namespace TShockAPI.DB
{
        public class ChatManager
        {
            public IDbConnection database;

            public ChatManager(IDbConnection db)
            {
                database = db;

                var table = new SqlTable("chat",
                    new SqlColumn("ID", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
                    new SqlColumn("Username", MySqlDbType.VarChar, 32),
                    new SqlColumn("ToUsername", MySqlDbType.VarChar, 32),
                    new SqlColumn("Message", MySqlDbType.VarChar, 32)
                    );
                var creator = new SqlTableCreator(db, db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
                creator.EnsureExists(table);
            }

            public void AddMessage(string From, string To, string Message)
            {
                try
                {
                    database.Query("INSERT INTO Chat (Username, ToUsername, Message) VALUES (@0, @1, @2);", From, To, Message);
                
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
        }
    }