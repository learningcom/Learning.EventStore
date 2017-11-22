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

        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

        public RedisCache(ICache memoryCache, IRedisClient redis)
            : this(memoryCache, redis, 30)
        {
        }

        public RedisCache(ICache memoryCache, IRedisClient redis, int expiry)
        {
            _memoryCache = memoryCache;
            _redis = redis;
            _expiry = expiry;
        }

        public bool IsTracked(string id)
        {
            return _memoryCache.IsTracked(id) || _redis.KeyExistsAsync(id).Result;
        }

        public void Set(string id, AggregateRoot aggregate)
        {
            var serializedAggregateRoot = JsonConvert.SerializeObject(aggregate, JsonSerializerSettings);
            _redis.StringSetAsync(id, serializedAggregateRoot, TimeSpan.FromMinutes(_expiry)).Wait();
            _memoryCache.Set(id, aggregate);
        }

        public AggregateRoot Get(string id)
        {
            var memoryCacheValue = _memoryCache.Get(id);
            if (memoryCacheValue != null)
            {
                return memoryCacheValue;
            }

            var serializedAggregateRoot = _redis.StringGetAsync(id).Result;
            if (!string.IsNullOrEmpty(serializedAggregateRoot))
            {
                _redis.KeyExpireAsync(id, TimeSpan.FromMinutes(_expiry)).Wait();

                var deserializedAggregateRoot = JsonConvert.DeserializeObject(serializedAggregateRoot, JsonSerializerSettings) as AggregateRoot;

                _memoryCache.Set(id, deserializedAggregateRoot);

                return deserializedAggregateRoot;
            }

            return null;
        }

        public void Remove(string id)
        {
            _memoryCache.Remove(id);
            _redis.KeyDeleteAsync(id).Wait();
        }
    }
}
