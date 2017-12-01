using System;
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

        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

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

        public bool IsTracked(string id)
        {
            var cacheKey = $"{_keyPrefix}:{_environment}:{id}";
            return _memoryCache.IsTracked(cacheKey) || _redis.KeyExistsAsync(cacheKey).Result;
        }

        public void Set(string id, AggregateRoot aggregate)
        {
            var cacheKey = $"{_keyPrefix}:{_environment}:{id}";
            var serializedAggregateRoot = JsonConvert.SerializeObject(aggregate, JsonSerializerSettings);
            _redis.StringSetAsync(cacheKey, serializedAggregateRoot, TimeSpan.FromMinutes(_expiry)).Wait();
            _memoryCache.Set(cacheKey, aggregate);
        }

        public AggregateRoot Get(string id)
        {
            var cacheKey = $"{_keyPrefix}:{_environment}:{id}";
            var memoryCacheValue = _memoryCache.Get(cacheKey);
            if (memoryCacheValue != null)
            {
                return memoryCacheValue;
            }

            var serializedAggregateRoot = _redis.StringGetAsync(cacheKey).Result;
            if (!string.IsNullOrEmpty(serializedAggregateRoot))
            {
                _redis.KeyExpireAsync(cacheKey, TimeSpan.FromMinutes(_expiry)).Wait();

                var deserializedAggregateRoot = JsonConvert.DeserializeObject(serializedAggregateRoot, JsonSerializerSettings) as AggregateRoot;

                _memoryCache.Set(cacheKey, deserializedAggregateRoot);

                return deserializedAggregateRoot;
            }

            return null;
        }

        public void Remove(string id)
        {
            var cacheKey = $"{_keyPrefix}:{_environment}:{id}";
            _memoryCache.Remove(cacheKey);
            _redis.KeyDeleteAsync(cacheKey).Wait();
        }
    }
}
