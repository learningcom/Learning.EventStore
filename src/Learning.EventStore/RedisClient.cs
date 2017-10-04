using System;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Learning.EventStore
{
    public class RedisClient : IRedisClient
    {
        private readonly Lazy<IConnectionMultiplexer> _redis;

        public IDatabase Database => _redis.Value.GetDatabase();
        private ISubscriber Subscriber => _redis.Value.GetSubscriber();

        public RedisClient(Lazy<IConnectionMultiplexer> redis)
        {
            _redis = redis;
        }

        public async Task<RedisValue[]> SetMembersAsync(RedisKey key)
        {
            var members = await Database.SetMembersAsync(key);
            return members;
        }

        public async Task<long> ListRightPushAsync(RedisKey key, RedisValue value)
        {
            var result = await Database.ListRightPushAsync(key, value).ConfigureAwait(false);
            return result;
        }

        public async Task<long> ListLengthAsync(RedisKey key)
        {
            var length = await Database.ListLengthAsync(key).ConfigureAwait(false);
            return length;
        }

        public async Task<RedisValue[]> ListRangeAsync(RedisKey key, long start , long stop)
        {
            var range = await Database.ListRangeAsync(key, start, stop).ConfigureAwait(false);
            return range;
        }

        public async Task<RedisValue> HashGetAsync(RedisKey key, RedisValue value)
        {
            var hashValue = await Database.HashGetAsync(key, value).ConfigureAwait(false);
            return hashValue;
        }

        public async Task<long> HashLengthAsync(RedisKey key)
        {
            var length = await Database.HashLengthAsync(key).ConfigureAwait(false);
            return length;
        }

        public async Task HashSetAsync(RedisKey key, HashEntry[] value)
        {
            await Database.HashSetAsync(key, value).ConfigureAwait(false);
        }

        public async Task<bool> SetAddAsync(RedisKey key, RedisValue value)
        {
            var result = await Database.SetAddAsync(key, value).ConfigureAwait(false);
            return result;
        }

        public async Task<RedisValue> ListRightPopLeftPushAsync(RedisKey source, RedisKey destination)
        {
            var result = await Database.ListRightPopLeftPushAsync(source, destination).ConfigureAwait(false);
            return result;
        }

        public async Task<long> ListRemoveAsync(RedisKey key, RedisValue value)
        {
            var result = await Database.ListRemoveAsync(key, value).ConfigureAwait(false);
            return result;
        }

        public async Task PublishAsync(RedisChannel channel, RedisValue value)
        {
            await Subscriber.PublishAsync(channel, value).ConfigureAwait(false);
        }

        public async Task SubscribeAsync(RedisChannel channel, Action<RedisChannel, RedisValue> handler)
        {
            await Subscriber.SubscribeAsync(channel, handler).ConfigureAwait(false);
        }

        public async Task<string> StringGetAsync(string key)
        {
            var result = await Database.StringGetAsync(key);

            return result;
        }

        public async Task<long> StringIncrementAsync(string key)
        {
            var result = await Database.StringIncrementAsync(key);

            return result;
        }

        public async Task<bool> StringSetAsync(string key, string value)
        {
            var result = await Database.StringSetAsync(key, value);

            return result;
        }
    }
}
