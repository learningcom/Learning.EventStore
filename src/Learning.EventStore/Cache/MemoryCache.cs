using System;
using System.Threading.Tasks;
using Learning.EventStore.Domain;
using Microsoft.Extensions.Caching.Memory;

namespace Learning.EventStore.Cache
{
    public class MemoryCache : ICache
    {
        private readonly MemoryCacheEntryOptions _cacheOptions;
        private readonly IMemoryCache _cache;
        public MemoryCache()
            :this(15)
        {
        }

        public MemoryCache(int expiry)
        {
            _cacheOptions = new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(expiry)
            };
            _cache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new MemoryCacheOptions());
        }

        public Task<bool> IsTracked(string id)
        {
            return Task.FromResult(_cache.TryGetValue(id, out var o));
        }

        public Task Set(string id, AggregateRoot aggregate)
        {
            _cache.Set(id, aggregate, _cacheOptions);

            return Task.FromResult(0);
        }

        public Task<AggregateRoot> Get(string id)
        {
            return Task.FromResult((AggregateRoot)_cache.Get(id));
        }

        public Task Remove(string id)
        {
            _cache.Remove(id);

            return Task.FromResult(0);
        }

        public void RegisterEvictionCallback(Action<string> action)
        {
            _cacheOptions.RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                action.Invoke((string)key);
            });
        }
    }
}
