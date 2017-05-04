using System;
using System.Threading.Tasks;
using Learning.EventStore.Cache;
using Learning.EventStore.Domain;
using Learning.EventStore.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Learning.EventStore.Test.Cache
{
    [TestClass]
    public class GetEarlierThanExpectedEventsFromEventStoreTest
    {
        private readonly IRepository _cacheRepository;
        private readonly MemoryCache _memoryCache;
        private readonly TestAggregate _aggregate;

        public GetEarlierThanExpectedEventsFromEventStoreTest()
        {
            _memoryCache = new MemoryCache();
            var eventStore = new TestEventStoreWithBugs();
            _cacheRepository = new CacheRepository(new TestRepository(), eventStore, _memoryCache);
            _aggregate = _cacheRepository.GetAsync<TestAggregate>(Guid.NewGuid().ToString()).Result;
        }

        [TestMethod]
        public async Task EvictsOldObjectFromCache()
        {
            await _cacheRepository.GetAsync<TestAggregate>(_aggregate.Id);
            var newAggregate = _memoryCache.Get(_aggregate.Id);
            Assert.AreNotEqual(_aggregate, newAggregate);
        }

        [TestMethod]
        public async Task GetsEventsFromStart()
        {
            var aggregate = await _cacheRepository.GetAsync<TestAggregate>(_aggregate.Id);
            Assert.AreEqual(1, aggregate.Version);
        }
    }
}
