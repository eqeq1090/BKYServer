using StackExchange.Redis;
using BKServerBase.ConstEnum;
using BKServerBase.Logger;
using BKServerBase.Messaging.Detail;
using BKServerBase.Threading;
using BKCommonComponent.Detail;
using BKCommonComponent.Redis;
using BKCommonComponent.Redis.Detail;
using BKGameServerComponent.Actor;
using BKGameServerComponent.Actor.Detail;
using BKProtocol;

namespace BKGameServerComponent
{
    public partial class GameServerComponent
    {
        internal CustomTask<bool> InvokePubsubMessage(BKRedisDataType dataType, IPubsubMsg message, IActor taskOwner)
        {
            var resultTask = new CustomTask<bool>(string.Empty);

            var connector = RedisComponent.GetPubsubClient(dataType);
            connector.Publish(message)
                .ContinueWith(task =>
                {
                    var result = task.Result;
                    taskOwner.Post(() =>
                    {
                        resultTask.SetResult(result);
                    });
                });

            return resultTask;
        }
        
        internal CustomTask<bool> InvokeSaveRedisInfo<T>(BKRedisDataType dataType, RedisKey redisKey, T data, TimeSpan? expiry, IActor taskOwner, CommandFlags flags = CommandFlags.None)
            where T : class
        {
            var resultTask = new CustomTask<bool>(string.Empty);

            var redisOperator = new RedisOperator();
            redisOperator.AddAsync(dataType, redisKey, data, expiry, flags)
                .ContinueWith(task =>
                {
                    var result = task.Result;
                    taskOwner.Post(() =>
                    {
                        resultTask.SetResult(result);
                    });
                });

            return resultTask;
        }

        internal CustomTask<T?> InvokeGetRedisInfo<T>(BKRedisDataType dataType, RedisKey redisKey, TimeSpan? expiry, IActor taskOwner)
            where T : class
        {
            var resultTask = new CustomTask<T?>(string.Empty);

            var redisOperator = new RedisOperator();
            redisOperator.GetAsync<T>(dataType, redisKey, expiry)
                .ContinueWith(task =>
                {
                    var result = task.Result;
                    taskOwner.Post(() =>
                    {
                        resultTask.SetResult(result);
                    });
                });

            return resultTask;
        }

        internal CustomTask<bool> InvokeRemoveRedisKey(BKRedisDataType dataType, RedisKey redisKey, IActor taskOwner)
        {
            var resultTask = new CustomTask<bool>(string.Empty);

            var redisOperator = new RedisOperator();
            redisOperator.RemoveKeyAsync(dataType, redisKey, CommandFlags.FireAndForget)
                .ContinueWith(task =>
                {
                    var result = task.Result;
                    taskOwner.Post(() =>
                    {
                        resultTask.SetResult(result);
                    });
                });

            return resultTask;
        }
    }
}
