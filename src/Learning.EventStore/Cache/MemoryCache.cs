using System;
using System.Threading.Tasks;
using Learning.EventStore.Domain;
#if NET452
using System.Runtime.Caching;
#else
using Microsoft.Extensions.Caching.Memory;
#endif

namespace Learning.EventStore.Cache
{
    public class MemoryCache : ICache
    {

#if NET452
        private readonly System.Runtime.Caching.MemoryCache _cache;
        private Func<CacheItemPolicy> _policyFactory;
#else
        private readonly MemoryCacheEntryOptions _cacheOptions;
        private readonly IMemoryCache _cache;
#endif
        public MemoryCache()
            :this(15)
        {
        }

        public MemoryCache(int expiry)
        {
#if NET452
            _cache = System.Runtime.Caching.MemoryCache.Default;
            _policyFactory = () => new CacheItemPolicy();
#else
            _cacheOptions = new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(expiry)
            };
            _cache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new MemoryCacheOptions());
#endif

        }

        public Task<bool> IsTracked(string id)
        {
#if NET452
            return Task.FromResult(_cache.Contains(id));
#else
            return Task.FromResult(_cache.TryGetValue(id, out var o));
#endif
        }

        public Task Set(string id, AggregateRoot aggregate)
        {
#if NET452
            _cache.Add(id, aggregate, _policyFactory.Invoke());
#else
            _cache.Set(id, aggregate, _cacheOptions);
#endif
            return Task.FromResult(0);
        }

        public Task<AggregateRoot> Get(string id)
        {
#if NET452
            return Task.FromResult((AggregateRoot)_cache.Get(id));
#else
            return Task.FromResult((AggregateRoot)_cache.Get(id));
#endif
        }

        public Task Remove(string id)
        {
#if NET451
            _cache.Remove(id);
#else
            _cache.Remove(id);
#endif
            return Task.FromResult(0);
        }

        public void RegisterEvictionCallback(Action<string> action)
        {
#if NET452
            _policyFactory = () => new CacheItemPolicy
            {
                SlidingExpiration = TimeSpan.FromMinutes(15),
                RemovedCallback = x =>
                {
                    action.Invoke(x.CacheItem.Key);
                }
            };
#else
            _cacheOptions.RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                action.Invoke((string)key);
            });
#endif
        }
    }
}
