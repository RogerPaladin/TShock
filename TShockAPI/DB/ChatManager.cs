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
                Message = Message.Replace("'", "`");
                try
                {
                    database.Query("INSERT INTO Chat (Username, ToUsername, Message) VALUES (@0, @1, @2);", From, To, Message);
                
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
            public bool ReadMessages(out string[] username, out string[] messages, string From = "", string To = "")
            {
                messages = new string[21];
                username = new string[21];
                int i = 20;
                try
                {
                    using (var reader = database.QueryReader("Select * FROM chat WHERE ToUsername = '' ORDER BY ID DESC LIMIT 20"))
                    {
                        while (reader.Read())
                        {
                            messages[i] = reader.Get<string>("Message");
                            username[i] = reader.Get<string>("Username");
                            i--;
                        }
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                return false;
            }
        }
    }