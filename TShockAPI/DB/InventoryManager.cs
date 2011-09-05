using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using MySql.Data.MySqlClient;

namespace TShockAPI.DB
{
    public class InventoryManager
    {
        private IDbConnection database;

        public InventoryManager(IDbConnection db)
        {
            database = db;

            var table = new SqlTable("Inventory",
                new SqlColumn("Username", MySqlDbType.VarChar, 32) { Unique = true },
                new SqlColumn("Slot0", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot1", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot2", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot3", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot4", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot5", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot6", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot7", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot8", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot9", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot10", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot11", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot12", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot13", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot14", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot15", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot16", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot17", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot18", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot19", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot20", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot21", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot22", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot23", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot24", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot25", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot26", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot27", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot28", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot29", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot30", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot31", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot32", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot33", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot34", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot35", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot36", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot37", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot38", MySqlDbType.VarChar, 32),
                new SqlColumn("Slot39", MySqlDbType.VarChar, 32)
                );
            var creator = new SqlTableCreator(db, db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
            creator.EnsureExists(table);
        }

        public void NewInventory(TSPlayer player)
        {
            string[] inv;
            inv = new string[41];
            for (int i = 0; i < 40; i++)
            {
                inv[i] = player.TPlayer.inventory[i].name + ":" + player.TPlayer.inventory[i].stack;
            }
            try
            {
                database.Query("INSERT INTO Inventory (Username, Slot0, Slot1, Slot2, Slot3, Slot4, " + 
                                                                 "Slot5, Slot6, Slot7, Slot8, Slot9, "+
                                                                 "Slot10, Slot11, Slot12, Slot13, Slot14, " + 
                                                                 "Slot15, Slot16, Slot17, Slot18, Slot19, "+
                                                                 "Slot20, Slot21, Slot22, Slot23, Slot24, " + 
                                                                 "Slot25, Slot26, Slot27, Slot28, Slot29, "+
                                                                 "Slot30, Slot31, Slot32, Slot33, Slot34, " + 
                                                                 "Slot35, Slot36, Slot37, Slot38, Slot39"+
                                                                 ") VALUES (@40, @0, @1, @2, @3, @4, @5, "+
                                                                 "@6, @7, @8, @9, @10, @11, @12, @13, "+
                                                                 "@14, @15, @16, @17, @18, @19, @20, "+
                                                                 "@21, @22, @23, @24, @25, @26, @27, "+
                                                                 "@28, @29, @30, @31, @32, @33, @34, @35, @36, "+
                                                                 "@37, @38, @39);", inv[0], inv[1], inv[2], inv[3], inv[4],
                                                                 inv[5], inv[6], inv[7], inv[8], inv[9], inv[10], inv[11],
                                                                 inv[12], inv[13], inv[14], inv[15], inv[16], inv[17],
                                                                 inv[18], inv[19], inv[20], inv[21], inv[22], inv[23],
                                                                 inv[24], inv[25], inv[26], inv[27], inv[28], inv[29],
                                                                 inv[30], inv[31], inv[32], inv[33], inv[34], inv[35],
                                                                 inv[36], inv[37], inv[38], inv[39], player.Name);
                                                                
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        public void UpdateInventory(TSPlayer player)
        {
            string[] inv;
            inv = new string[41];
            for (int i = 0; i < 40; i++)
            {
                inv[i] = player.TPlayer.inventory[i].name + ":" + player.TPlayer.inventory[i].stack;
            }
            try
            {
                database.Query("UPDATE Inventory SET Slot0 = @0, Slot1 = @1, Slot2 = @2, Slot3 = @3, Slot4 = @4, " +
                                                      "Slot5 = @5, Slot6 = @6, Slot7 = @7, Slot8 = @8, Slot9 = @9, " +
                                                      "Slot10 = @10, Slot11 = @11, Slot12 = @12, Slot13 = @13, Slot14 = @14, " +
                                                      "Slot15 = @15, Slot16 = @16, Slot17 = @17, Slot18 = @18, Slot19 = @19, " +
                                                      "Slot20 = @20, Slot21 = @21, Slot22 = @22, Slot23 = @23, Slot24 = @24, " +
                                                      "Slot25 = @25, Slot26 = @26, Slot27 = @27, Slot28 = @28, Slot29 = @29, " +
                                                      "Slot30 = @30, Slot31 = @31, Slot32 = @32, Slot33 = @33, Slot34 = @34, " +
                                                      "Slot35 = @35, Slot36 = @36, Slot37 = @37, Slot38 = @38, Slot39 = @39 " +
                                                      "WHERE LOWER (Username) = @40;", inv[0], inv[1], inv[2], inv[3], inv[4],
                                                                 inv[5], inv[6], inv[7], inv[8], inv[9], inv[10], inv[11],
                                                                 inv[12], inv[13], inv[14], inv[15], inv[16], inv[17],
                                                                 inv[18], inv[19], inv[20], inv[21], inv[22], inv[23],
                                                                 inv[24], inv[25], inv[26], inv[27], inv[28], inv[29],
                                                                 inv[30], inv[31], inv[32], inv[33], inv[34], inv[35],
                                                                 inv[36], inv[37], inv[38], inv[39], player.Name.ToLower());
                return;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        public bool CheckInventory(TSPlayer player)
        {
            string[] inv;
            inv = new string[41];
            QueryResult result;
            for (int i = 0; i < 40; i++)
            {
                inv[i] = player.TPlayer.inventory[i].name + ":" + player.TPlayer.inventory[i].stack;
            }
            try
            {
                result = database.QueryReader("SELECT * FROM Inventory WHERE LOWER (Username) = @0", player.Name.ToLower());
                using (var reader = result)
                {
                    if (reader.Read())
                    {
                        string slot0 = reader.Get<string>("Slot0"); string slot1 = reader.Get<string>("Slot1");
                        string slot2 = reader.Get<string>("Slot2"); string slot3 = reader.Get<string>("Slot3");
                        string slot4 = reader.Get<string>("Slot4"); string slot5 = reader.Get<string>("Slot5");
                        string slot6 = reader.Get<string>("Slot6"); string slot7 = reader.Get<string>("Slot7");
                        string slot8 = reader.Get<string>("Slot8"); string slot9 = reader.Get<string>("Slot9");
                        string slot10 = reader.Get<string>("Slot10"); string slot11 = reader.Get<string>("Slot11");
                        string slot12 = reader.Get<string>("Slot12"); string slot13 = reader.Get<string>("Slot13");
                        string slot14 = reader.Get<string>("Slot14"); string slot15 = reader.Get<string>("Slot15");
                        string slot16 = reader.Get<string>("Slot16"); string slot17 = reader.Get<string>("Slot17");
                        string slot18 = reader.Get<string>("Slot18"); string slot19 = reader.Get<string>("Slot19");
                        string slot20 = reader.Get<string>("Slot20"); string slot21 = reader.Get<string>("Slot21");
                        string slot22 = reader.Get<string>("Slot22"); string slot23 = reader.Get<string>("Slot23");
                        string slot24 = reader.Get<string>("Slot24"); string slot25 = reader.Get<string>("Slot25");
                        string slot26 = reader.Get<string>("Slot26"); string slot27 = reader.Get<string>("Slot27");
                        string slot28 = reader.Get<string>("Slot28"); string slot29 = reader.Get<string>("Slot29");
                        string slot30 = reader.Get<string>("Slot30"); string slot31 = reader.Get<string>("Slot31");
                        string slot32 = reader.Get<string>("Slot32"); string slot33 = reader.Get<string>("Slot33");
                        string slot34 = reader.Get<string>("Slot34"); string slot35 = reader.Get<string>("Slot35");
                        string slot36 = reader.Get<string>("Slot36"); string slot37 = reader.Get<string>("Slot37");
                        string slot38 = reader.Get<string>("Slot38"); string slot39 = reader.Get<string>("Slot39");

                        if (slot0 == inv[0] && slot1 == inv[1] && slot2 == inv[2] && slot3 == inv[3] && slot4 == inv[4] &&
                            slot5 == inv[5] && slot6 == inv[6] && slot7 == inv[7] && slot8 == inv[8] && slot9 == inv[9] &&
                            slot10 == inv[10] && slot11 == inv[11] && slot12 == inv[12] && slot13 == inv[13] && slot14 == inv[14] &&
                            slot15 == inv[15] && slot16 == inv[16] && slot17 == inv[17] && slot18 == inv[18] && slot19 == inv[19] &&
                            slot20 == inv[20] && slot21 == inv[21] && slot22 == inv[22] && slot23 == inv[23] && slot24 == inv[24] &&
                            slot25 == inv[25] && slot26 == inv[26] && slot27 == inv[27] && slot28 == inv[28] && slot29 == inv[29] &&
                            slot30 == inv[30] && slot31 == inv[31] && slot32 == inv[32] && slot33 == inv[33] && slot34 == inv[34] &&
                            slot35 == inv[35] && slot36 == inv[36] && slot37 == inv[37] && slot38 == inv[38] && slot39 == inv[39])
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            return false;
        }
        public bool UserExist(TSPlayer player)
        {
            try
            {
                QueryResult result;
                result = database.QueryReader("SELECT * FROM Inventory WHERE LOWER (Username) = @0", player.Name.ToLower());
                using (var reader = result)
                    {
                        if (reader.Read())
                            {
                                return true;
                            }
                    }
                return false;
            }
            catch
            {
                return false;
            }
        }
        public bool NewPlayer(TSPlayer player)
        {
            string[] inv;
            inv = new string[41];
            for (int i = 0; i < 40; i++)
            {
                inv[i] = player.TPlayer.inventory[i].name + ":" + player.TPlayer.inventory[i].stack;
            }
                        if ("Copper Shortsword:1" == inv[0] && "Copper Pickaxe:1" == inv[1] && "Copper Axe:1" == inv[2] && ":0" == inv[3] && ":0" == inv[4] &&
                            ":0" == inv[5] && ":0" == inv[6] && ":0" == inv[7] && ":0" == inv[8] && ":0" == inv[9] &&
                            ":0" == inv[10] && ":0" == inv[11] && ":0" == inv[12] && ":0" == inv[13] && ":0" == inv[14] &&
                            ":0" == inv[15] && ":0" == inv[16] && ":0" == inv[17] && ":0" == inv[18] && ":0" == inv[19] &&
                            ":0" == inv[20] && ":0" == inv[21] && ":0" == inv[22] && ":0" == inv[23] && ":0" == inv[24] &&
                            ":0" == inv[25] && ":0" == inv[26] && ":0" == inv[27] && ":0" == inv[28] && ":0" == inv[29] &&
                            ":0" == inv[30] && ":0" == inv[31] && ":0" == inv[32] && ":0" == inv[33] && ":0" == inv[34] &&
                            ":0" == inv[35] && ":0" == inv[36] && ":0" == inv[37] && ":0" == inv[38] && ":0" == inv[39])
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
          }
        public void DelInventory(string PlayerName)
        {
            database.Query("DELETE FROM Inventory WHERE LOWER (Username) = @0;", PlayerName.ToLower());
            Log.ConsoleInfo(string.Format("Player <{0}> inventory automatically deleted.", PlayerName));
        }
    }
}
