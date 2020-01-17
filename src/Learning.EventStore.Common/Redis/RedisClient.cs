using System;
using System.Threading.Tasks;
using Polly;
using StackExchange.Redis;

namespace Learning.EventStore.Common.Redis
{
    public class RedisClient : IRedisClient, IDisposable
    {
        private readonly Lazy<IConnectionMultiplexer> _redis;

        public IDatabase Database => _redis.Value.GetDatabase();

        private readonly int _retryCount;

        private IAsyncPolicy RetryPolicyAsync =>
            Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    _retryCount, // number of retries
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) // exponential backoff
                );

        private Policy RetryPolicy =>
            Policy
                .Handle<Exception>()
                .WaitAndRetry(
                    _retryCount, // number of retries
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) // exponential backoff
                );

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
            var members = await RetryPolicyAsync.ExecuteAsync(() => Database.SetMembersAsync(key)).ConfigureAwait(false);
            return members;
        }

        public async Task<long> ListRightPushAsync(RedisKey key, RedisValue value)
        {
            var result = await RetryPolicyAsync.ExecuteAsync(() => Database.ListRightPushAsync(key, value)).ConfigureAwait(false);
            return result;
        }

        public async Task<long> ListLengthAsync(RedisKey key)
        {
            var length = await RetryPolicyAsync.ExecuteAsync(() => Database.ListLengthAsync(key)).ConfigureAwait(false);
            return length;
        }

        public async Task<RedisValue[]> ListRangeAsync(RedisKey key, long start , long stop)
        {
            var range = await RetryPolicyAsync.ExecuteAsync(() => Database.ListRangeAsync(key, start, stop)).ConfigureAwait(false);
            return range;
        }

        public async Task<RedisValue> HashGetAsync(RedisKey key, RedisValue value)
        {
            var hashValue = await RetryPolicyAsync.ExecuteAsync(() => Database.HashGetAsync(key, value)).ConfigureAwait(false);
            return hashValue;
        }

        public RedisValue HashGet(RedisKey key, RedisValue value)
        {
            var hashValue = RetryPolicy.Execute(() => Database.HashGet(key, value));

            return hashValue;
        }

        public async Task<long> HashLengthAsync(RedisKey key)
        {
            var length = await RetryPolicyAsync.ExecuteAsync(() => Database.HashLengthAsync(key)).ConfigureAwait(false);
            return length;
        }

        public async Task HashSetAsync(RedisKey key, HashEntry[] value)
        {
            await RetryPolicyAsync.ExecuteAsync(() => Database.HashSetAsync(key, value)).ConfigureAwait(false);
        }

        public async Task<bool> SetAddAsync(RedisKey key, RedisValue value)
        {
            var result = await RetryPolicyAsync.ExecuteAsync(() => Database.SetAddAsync(key, value)).ConfigureAwait(false);
            return result;
        }

        public RedisValue ListRightPopLeftPush(RedisKey source, RedisKey destination)
        {
            var result = RetryPolicy.Execute(() => Database.ListRightPopLeftPush(source, destination));
            return result;
        }

        public long ListRemove(RedisKey key, RedisValue value)
        {
            var result = RetryPolicy.Execute(() => Database.ListRemove(key, value));
            return result;
        }

        public async Task PublishAsync(RedisChannel channel, RedisValue value)
        {
            await RetryPolicyAsync.ExecuteAsync(() => _redis.Value.GetSubscriber().PublishAsync(channel, value)).ConfigureAwait(false);
        }

        public async Task SubscribeAsync(RedisChannel channel, Action<RedisChannel, RedisValue> handler)
        {
            await RetryPolicyAsync.ExecuteAsync(() => _redis.Value.GetSubscriber().SubscribeAsync(channel, handler)).ConfigureAwait(false);
        }

        public async Task<string> StringGetAsync(string key)
        {
            var result = await RetryPolicyAsync.ExecuteAsync(() => Database.StringGetAsync(key)).ConfigureAwait(false);

            return result;
        }

        public async Task<bool> StringSetAsync(string key, string value, TimeSpan? expiry = null)
        {
            var result = await RetryPolicyAsync.ExecuteAsync(() => Database.StringSetAsync(key, value, expiry)).ConfigureAwait(false);

            return result;
        }

        public async Task KeyDeleteAsync(string key)
        {
            await RetryPolicyAsync.ExecuteAsync(() => Database.KeyDeleteAsync(key)).ConfigureAwait(false);
        }

        public async Task<bool> KeyExistsAsync(string key)
        {
            var result = await RetryPolicyAsync.ExecuteAsync(() => Database.KeyExistsAsync(key)).ConfigureAwait(false);

            return result;
        }

        public async Task KeyExpireAsync(string key, TimeSpan expiry)
        {
            await RetryPolicyAsync.ExecuteAsync(() => Database.KeyExpireAsync(key, expiry)).ConfigureAwait(false);
        }

        public async Task<bool> HashSetAsync(string key, string field, string value)
        {
            var result = await RetryPolicyAsync.ExecuteAsync(() => Database.HashSetAsync(key, field, value)).ConfigureAwait(false);

            return result;
        }

        public async Task<bool> HashDeleteAsync(string key, string field)
        {
            var result = await RetryPolicyAsync.ExecuteAsync(() => Database.HashDeleteAsync(key, field)).ConfigureAwait(false);

            return result;
        }

        public async Task<bool> HashExistsAsync(string key, string field)
        {
            var result = await RetryPolicyAsync.ExecuteAsync(() => Database.HashExistsAsync(key, field)).ConfigureAwait(false);

            return result;
        }

        public async Task<long> ListRemoveAsync(string key, string value, long count)
        {
            var result = await RetryPolicyAsync.ExecuteAsync(() => Database.ListRemoveAsync(key, value, count)).ConfigureAwait(false);

            return result;
        }

        public void ListLeftPush(string key, string value)
        {
            RetryPolicy.Execute(() => Database.ListLeftPush(key, value));
        }

        public async Task<string> ListGetByIndexAsync(string key, int index)
        {
            var result = await RetryPolicyAsync.ExecuteAsync(() => Database.ListGetByIndexAsync(key, index)).ConfigureAwait(false);

            return result;
        }

        public async Task ListRemoveAsync(string key, RedisValue value)
        {
            await RetryPolicyAsync.ExecuteAsync(() => Database.ListRemoveAsync(key, value)).ConfigureAwait(false);
        }

        public async Task HashIncrementAsync(string key, string field)
        {
            await RetryPolicyAsync.ExecuteAsync(() => Database.HashIncrementAsync(key, field)).ConfigureAwait(false);
        }

        public async Task<HashEntry[]> HashGetAllAsync(string key)
        {
            var result = await RetryPolicyAsync.ExecuteAsync(() => Database.HashGetAllAsync(key)).ConfigureAwait(false);

            return result;
        }

        public ITransaction CreateTransaction()
        {
            return Database.CreateTransaction();
        }

        public async Task<bool> ExecuteTransactionAsync(ITransaction trans)
        {
            var result = await RetryPolicyAsync.ExecuteAsync(() => trans.ExecuteAsync()).ConfigureAwait(false);

            return result;
        }

        public void Dispose()
        {
            _redis.Value.Dispose();
        }
    }
}
