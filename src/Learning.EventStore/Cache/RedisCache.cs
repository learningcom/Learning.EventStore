using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Learning.EventStore.Common.Redis;
using Learning.EventStore.Domain;
using Newtonsoft.Json;

namespace Learning.EventStore.Cache
{
    public class RedisCache : ICache
    {
        private readonly IRedisClient _redis;
        private readonly ICache _memoryCache;
        private readonly int _expiry;
        private readonly string _keyPrefix;
        private readonly string _environment;

        private static readonly JsonSerializerSettings JsonSerializerSettings =
            new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.All};

        public RedisCache(ICache memoryCache, IRedisClient redis, string environment, string keyPrefix)
            : this(memoryCache, redis, environment, keyPrefix, 30)
        {
        }

        public RedisCache(ICache memoryCache, IRedisClient redis, string environment, string keyPrefix, int expiry)
        {
            _memoryCache = memoryCache;
            _redis = redis;
            _expiry = expiry;
            _keyPrefix = keyPrefix;
            _environment = environment;
        }

        public async Task<bool> IsTracked(string id)
        {
            var cacheKey = $"{_keyPrefix}:{_environment}:{id}";
            var inMemoryCache = await _memoryCache.IsTracked(cacheKey).ConfigureAwait(false);
            var inRedisCache = await _redis.KeyExistsAsync(cacheKey).ConfigureAwait(false);
            return inMemoryCache || inRedisCache;
        }

        public async Task Set(string id, AggregateRoot aggregate)
        {
            if (aggregate != null)
            {
                var cacheKey = $"{_keyPrefix}:{_environment}:{id}";
                var serializedAggregateRoot = JsonConvert.SerializeObject(aggregate, JsonSerializerSettings);
                var tasks = new List<Task> 
                {
                    _redis.StringSetAsync(cacheKey, serializedAggregateRoot, TimeSpan.FromMinutes(_expiry)),
                    _memoryCache.Set(cacheKey, aggregate)
                };

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }

        public async Task<AggregateRoot> Get(string id)
        {
            var cacheKey = $"{_keyPrefix}:{_environment}:{id}";
            var memoryCacheValue = await _memoryCache.Get(cacheKey).ConfigureAwait(false);
            if (memoryCacheValue != null)
            {
                return memoryCacheValue;
            }

            var serializedAggregateRoot = await _redis.StringGetAsync(cacheKey).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(serializedAggregateRoot))
            {
                var deserializedAggregateRoot =
                    JsonConvert.DeserializeObject(serializedAggregateRoot, JsonSerializerSettings) as AggregateRoot;

                var tasks = new List<Task> 
                {
                    _redis.KeyExpireAsync(cacheKey, TimeSpan.FromMinutes(_expiry)),
                    _memoryCache.Set(cacheKey, deserializedAggregateRoot)
                };

                await Task.WhenAll(tasks).ConfigureAwait(false);

                return deserializedAggregateRoot;
            }

            return null;
        }

        public async Task Remove(string id)
        {
            var cacheKey = $"{_keyPrefix}:{_environment}:{id}";
            var tasks = new List<Task>
            {
                _memoryCache.Remove(cacheKey),
                _redis.KeyDeleteAsync(cacheKey)
            };

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        public void RegisterEvictionCallback(Action<string> action)
        {
        }
    }
}
