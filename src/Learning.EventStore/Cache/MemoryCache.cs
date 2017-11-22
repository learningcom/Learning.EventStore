using System;
using Learning.EventStore.Domain;
using Microsoft.Extensions.Caching.Memory;
#if NET451
using System.Runtime.Caching;
#else

#endif

namespace Learning.EventStore.Cache
{
    public class MemoryCache : ICache
    {

#if NET451
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
#if NET451
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

        public bool IsTracked(string id)
        {
            object o;
#if NET451
            return _cache.Contains(id);
#else
            return _cache.TryGetValue(id, out o);
#endif
        }

        public void Set(string id, AggregateRoot aggregate)
        {
#if NET451
            _cache.Add(id, aggregate, _policyFactory.Invoke());
#else
            _cache.Set(id, aggregate, _cacheOptions);
#endif
        }

        public AggregateRoot Get(string id)
        {
#if NET451
            return (AggregateRoot)_cache.Get(id);
#else
            return (AggregateRoot) _cache.Get(id);
#endif
        }

        public void Remove(string id)
        {
#if NET451
            _cache.Remove(id);
#else
            _cache.Remove(id);
#endif
        }
    }
}
