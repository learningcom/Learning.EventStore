using System;
using System.Threading.Tasks;
using Polly;
using StackExchange.Redis;

namespace Learning.EventStore.Common.Redis
{
    public class RedisClient : IRedisClient
    {
        private readonly Lazy<IConnectionMultiplexer> _redis;

        public IDatabase Database => _redis.Value.GetDatabase();
        private ISubscriber Subscriber => _redis.Value.GetSubscriber();
        private readonly int _retryCount;

        //private Policy RetryPolicyAsync =>
        //    Policy
        //        .Handle<Exception>()
        //        .WaitAndRetryAsync(
        //            _retryCount, // number of retries
        //            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) // exponential backoff
        //        );

        //private Policy RetryPolicy =>
        //    Policy
        //        .Handle<Exception>()
        //        .WaitAndRetry(
        //            _retryCount, // number of retries
        //            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) // exponential backoff
        //        );

        public RedisClient(Lazy<IConnectionMultiplexer> redis)
            : this(redis, 3)
        {
        }

        public RedisClient(Lazy<IConnectionMultiplexer> redis, int retryCount)
        {
            _redis = redis;
            _retryCount = retryCount;
        }

        public async Task<RedisValue[]> SetMembersAsync(RedisKey key)
        {
            var members = await Database.SetMembersAsync(key).ConfigureAwait(false);
            return members;
        }

        public async Task<long> ListRightPushAsync(RedisKey key, RedisValue value)
        {
            var result = await Database.ListRightPushAsync(key, value).ConfigureAwait(false);
            return result;
        }

        public async Task<long> ListLengthAsync(RedisKey key)
        {
            var length = await Database.ListLengthAsync(key);
            return length;
        }

        public async Task<RedisValue[]> ListRangeAsync(RedisKey key, long start , long stop)
        {
            var range = await Database.ListRangeAsync(key, start, stop);
            return range;
        }

        public async Task<RedisValue> HashGetAsync(RedisKey key, RedisValue value)
        {
            var hashValue = await Database.HashGetAsync(key, value).ConfigureAwait(false);
            return hashValue;
        }

        public RedisValue HashGet(RedisKey key, RedisValue value)
        {
            var hashValue = Database.HashGet(key, value);

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

        public RedisValue ListRightPopLeftPush(RedisKey source, RedisKey destination)
        {
            var result = Database.ListRightPopLeftPush(source, destination);
            return result;
        }

        public long ListRemove(RedisKey key, RedisValue value)
        {
            var result = Database.ListRemove(key, value);
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
            var result = await Database.StringGetAsync(key).ConfigureAwait(false);

            return result;
        }

        public async Task<bool> StringSetAsync(string key, string value, TimeSpan? expiry = null)
        {
            var result = await Database.StringSetAsync(key, value, expiry).ConfigureAwait(false);

            return result;
        }

        public async Task KeyDeleteAsync(string key)
        {
            await Database.KeyDeleteAsync(key).ConfigureAwait(false);
        }

        public async Task<bool> KeyExistsAsync(string key)
        {
            var result = await RDatabase.KeyExistsAsync(key).ConfigureAwait(false);

            return result;
        }

        public async Task KeyExpireAsync(string key, TimeSpan expiry)
        {
            await Database.KeyExpireAsync(key, expiry).ConfigureAwait(false);
        }

        public async Task<bool> HashSetAsync(string key, string field, string value)
        {
            var result = await Database.HashSetAsync(key, field, value).ConfigureAwait(false);

            return result;
        }

        public async Task<bool> HashDeleteAsync(string key, string field)
        {
            var result = await Database.HashDeleteAsync(key, field).ConfigureAwait(false);

            return result;
        }

        public async Task<bool> HashExistsAsync(string key, string field)
        {
            var result = await Database.HashExistsAsync(key, field).ConfigureAwait(false);

            return result;
        }

        public async Task<long> ListRemoveAsync(string key, string value, long count)
        {
            var result = await Database.ListRemoveAsync(key, value, count).ConfigureAwait(false);

            return result;
        }

    }
}
