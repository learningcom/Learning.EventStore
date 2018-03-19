using System;
using System.Threading.Tasks;
using Learning.EventStore.Cache;
using Learning.EventStore.DataStores;
using Learning.EventStore.Domain;
using Learning.EventStore.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Learning.EventStore.Test.Cache
{
    [TestClass]
    public class EvictCacheEntryTest
    {
        private readonly IRepository _cacheRepository;
        private readonly MemoryCache _memoryCache;
        private readonly TestAggregate _aggregate;

        public EvictCacheEntryTest()
        {
            _memoryCache = new MemoryCache();
            IEventStore eventStore = new TestEventStore();
            _cacheRepository = new CacheRepository(new TestRepository(), eventStore, _memoryCache);
            _aggregate = _cacheRepository.GetAsync<TestAggregate>(Guid.NewGuid().ToString().ToString()).Result;
        }

        [TestMethod]
        public async Task GetsNewAggregateOnNextGetAfterEviction()
        {
            _memoryCache.Remove(_aggregate.Id);

            var aggregate = await _cacheRepository.GetAsync<TestAggregate>(_aggregate.Id);
            Assert.AreNotEqual(_aggregate, aggregate);
        }
    }
}
