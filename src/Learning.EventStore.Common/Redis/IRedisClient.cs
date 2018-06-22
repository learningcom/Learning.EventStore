using System;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Learning.EventStore.Common.Redis
{
    public interface IRedisClient
    {
        IDatabase Database { get; }
        Task<RedisValue[]> SetMembersAsync(RedisKey key);
        Task<long> ListRightPushAsync(RedisKey key, RedisValue value);
        Task<long> ListLengthAsync(RedisKey key);
        Task<RedisValue[]> ListRangeAsync(RedisKey key, long start, long stop);
        Task<RedisValue> HashGetAsync(RedisKey key, RedisValue value);
        RedisValue HashGet(RedisKey key, RedisValue value);
        Task<long> HashLengthAsync(RedisKey key);
        Task HashSetAsync(RedisKey key, HashEntry[] value);
        Task<bool> SetAddAsync(RedisKey key, RedisValue value);
        RedisValue ListRightPopLeftPush(RedisKey source, RedisKey destination);
        long ListRemove(RedisKey key, RedisValue value);
        Task PublishAsync(RedisChannel channel, RedisValue value);
        Task SubscribeAsync(RedisChannel channel, Action<RedisChannel, RedisValue> handler);
        Task<string> StringGetAsync(string key);
        Task<bool> StringSetAsync(string key, string value, TimeSpan? expiry = null);
        Task<bool> KeyExistsAsync(string key);
        Task KeyDeleteAsync(string key);
        Task KeyExpireAsync(string key, TimeSpan expiry);
        Task<bool> HashSetAsync(string key, string field, string value);
        Task<bool> HashDeleteAsync(string key, string field);
        Task<bool> HashExistsAsync(string key, string field);
        Task<long> ListRemoveAsync(string key, string value, long count);
        void ListLeftPush(string key, string value);
        Task<string> ListGetByIndexAsync(string key, int index);
        Task ListRemoveAsync(string key, RedisValue value);
        Task<HashEntry[]> HashGetAllAsync(string key);
        ITransaction CreateTransaction();
        Task<bool> ExecuteTransactionAsync(ITransaction trans);
    }
}
