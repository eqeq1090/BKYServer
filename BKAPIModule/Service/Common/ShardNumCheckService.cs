using Dapper;
using MySql.Data.MySqlClient;
using MySqlConnector;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using BKServerBase.Config;
using BKServerBase.ConstEnum;
using BKServerBase.Logger;
using BKServerBase.Util;
using BKNetwork.Discovery;
using BKWebAPIComponent.ConstEnum;

namespace BKWebAPIComponent.Service.Initialize
{
    public class ShardNumCheckService
    {
        private ConcurrentDictionary<long/*playerUID*/, (int shardNum, long timestamp)> m_ShardNumDict = new ConcurrentDictionary<long, (int shardNum, long timestamp)>();
        private System.Threading.Timer m_ShardCheckTimer;


        public ShardNumCheckService()
        {
            m_ShardCheckTimer = new Timer(CheckShardNumExpire, null, 0, 1000);
        }

        public int GetShardNum(long playerUID)
        {
            if (m_ShardNumDict.TryGetValue(playerUID, out var pair) == false)
            {
                return -1;
            }
            return pair.shardNum;
        }

        public void UpdateShardNum(long playerUID, int shardNum)
        {
            if (m_ShardNumDict.TryGetValue(playerUID, out var pair) == false)
            {
                pair = (shardNum, TimeUtil.GetCurrentTickMilliSec());
                m_ShardNumDict.TryAdd(playerUID, pair);
            }
            else
            {
                pair.timestamp = TimeUtil.GetCurrentTickMilliSec();
            }
        }

        private void CheckShardNumExpire(Object? state)
        {
            var toRemove = new List<long>();
            var currentTime = TimeUtil.GetCurrentTickMilliSec();
            foreach (var item in m_ShardNumDict)
            {
                if (item.Value.timestamp + Consts.SHARD_NUM_EXPIRE_LIMIT < currentTime)
                {
                    toRemove.Add(item.Value.timestamp);
                }
            }
            foreach (var item in toRemove)
            {
                m_ShardNumDict.TryRemove(item, out _);
            }
        }
    }
}
