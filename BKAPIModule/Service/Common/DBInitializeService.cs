using Dapper;
using MySql.Data.MySqlClient;
using MySqlConnector;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using System.Timers;
using BKServerBase.Config;
using BKServerBase.ConstEnum;
using BKServerBase.Logger;
using BKServerBase.Util;
using BKDataLoader.MasterData;
using BKNetwork.Discovery;
using BKWebAPIComponent.Model.Entity.Master;

namespace BKWebAPIComponent.Service.Initialize
{
    public class DBInitializeService
    {
        private System.Timers.Timer m_SeasonHandler;

        public DBInitializeService()
        {
            m_SeasonHandler = new System.Timers.Timer();
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
        }

        public async Task<bool> CheckSchemaChanged()
        {
            try
            {
                var connectionString = ConfigManager.Instance.DBConnectionConf?.GetShardConnectionString(BKSchemaType.bk_global_master, 0);
                var playerShardInfo = ConfigManager.Instance.DBConnectionConf?.GetDBShardInfos(BKSchemaType.bk_player_shard);
                if (playerShardInfo == null)
                {
                    //ERROR
                    return false;
                }
                var path = ConfigManager.Instance.FindDefaultResourceDDLPath();
                var hashCode = CommonUtil.CreateDirectoryMd5(path);
                CoreLog.Critical.LogWarning($"DDL PATH : {path}");
                var connection = new MySqlConnector.MySqlConnection(connectionString);
                connection.Open();
                var sql = "CREATE TABLE IF NOT EXISTS `maintenance_info` (" +
                            "`status` tinyint NOT NULL," +
                            "`hashcode` varchar(48) NOT NULL DEFAULT 'no_hash_code'," +
                            "PRIMARY KEY(`status`)" +
                            ") ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;";
                await connection.ExecuteAsync(sql);
                var queryResult = await connection.QueryAsync("SELECT `hashcode` FROM `maintenance_info`");
                var prevHashCode = queryResult.FirstOrDefault();
                if (prevHashCode?.hashcode != hashCode)
                {
                    CoreLog.Critical.LogError($"HashCode Changed. {prevHashCode?.hashcode} // {hashCode}");
                    if (ConfigManager.Instance.ServerProfile == ServerProfile.Dev ||
                        ConfigManager.Instance.ServerProfile == ServerProfile.Local)
                    {
                        //MASTER
                        {
                            var result = await connection.QueryAsync<dynamic>($"SELECT TABLE_NAME FROM information_schema.tables WHERE TABLE_SCHEMA = '{BKSchemaType.bk_global_master.ToString()}';");
                            foreach (var item in result)
                            {
                                await connection.ExecuteAsync($"DROP TABLE IF EXISTS {item.TABLE_NAME}");
                            }

                            var oldDBConnection = new MySql.Data.MySqlClient.MySqlConnection(connectionString);
                            oldDBConnection.Open();
                            var masterDBSqlPath = Path.Combine(path, $"{BKSchemaType.bk_global_master}.sql");

                            MySqlScript script = new MySqlScript(oldDBConnection, File.ReadAllText(masterDBSqlPath));
                            await script.ExecuteAsync();

                            await connection.ExecuteAsync($"INSERT INTO `maintenance_info` (`status`, `hashcode`) VALUES (0,'{hashCode}')");

                            await connection.ExecuteAsync("INSERT INTO email_list (email) Values \n('sunoki@birdletter.com'),\n('jhye@birdletter.com'),\n('palmblad@birdletter.com'),\n('ds89_kim@birdletter.com'),\n('kym9313@birdletter.com'),\n('myclimax@birdletter.com'),\n('hara@birdletter.com'),\n('powergo7@birdletter.com'),\n('ljwon@birdletter.com'),\n('jenner@birdletter.com'),\n('woosub@birdletter.com'),\n('jside@birdletter.com'),\n('trey@birdletter.com'),\n('jsluchio@birdletter.com'),\n('angelika@birdletter.com'),\n('will@birdletter.com'),\n('yoonah12@birdletter.com'),\n('looseyh@birdletter.com'),\n('max036@birdletter.com'),\n('checker@birdletter.com'),\n('greatpanda89@birdletter.com'),\n('kyungim@birdletter.com'),\n('hyokyeong@birdletter.com'),\n('ckdbqls01@birdletter.com'),\n('clover@birdletter.com'),\n('gorogoro@birdletter.com'),\n('kasier48@birdletter.com'),\n('eqeq1090@birdletter.com'),\n('kratia@birdletter.com'),\n('zinik172@birdletter.com'),\n('young@birdletter.com'),\n('kimyj@birdletter.com'),\n('ky.yang@birdletter.com'),\n('seungyup@birdletter.com'),\n('cukim@birdletter.com'),\n('sixtail5789@birdletter.com'),\n('yache0120@birdletter.com'),\n('chaud@birdletter.com'),\n('QA01'),\n('QA02'),\n('QA03'),\n('QA04'),\n('QA05'),\n('QA06'),\n('QA07'),\n('QA08'),\n('QA09'),\n('QA10')");
                        }
                        //PLAYER_SHARD
                        foreach (var shardConnectionString in playerShardInfo.Connections)
                        {
                            using (var newShardConnection = new MySqlConnector.MySqlConnection(shardConnectionString.GetConnectionString()))
                            {
                                newShardConnection.Open();
                                var result = await connection.QueryAsync<dynamic>($"SELECT TABLE_NAME FROM information_schema.tables WHERE TABLE_SCHEMA = '{BKSchemaType.bk_player_shard.ToString()}';");
                                foreach (var item in result)
                                {
                                    await connection.ExecuteAsync($"DROP TABLE IF EXISTS {item.TABLE_NAME}");
                                }

                                var oldDBConnection = new MySql.Data.MySqlClient.MySqlConnection(shardConnectionString.GetConnectionString());
                                oldDBConnection.Open();

                                var shardDBPath = Path.Combine(path, $"{BKSchemaType.bk_player_shard.ToString()}.sql");

                                MySqlScript script = new MySqlScript(oldDBConnection, File.ReadAllText(shardDBPath));
                                await script.ExecuteAsync();
                            }
                        }
                        return true;
                    }
                    else
                    {
                        //에러 띄우고 서버 중단
                        return false;
                    }
                }
            }
            catch(Exception ex)
            {
                CoreLog.Critical.LogError(ex);
            }
            return true;
            //
            /*
            
            */
            //접근해서 hashcode를 가져온다.

            //테이블을 만들기를 시도한다.

        }
    }
}
