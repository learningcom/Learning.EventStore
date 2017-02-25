using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Learning.EventStore.Cache;
using Learning.EventStore.Domain;
using Learning.EventStore.Test.Mocks;
using NUnit.Framework;

namespace Learning.EventStore.Test.Cache
{
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
            _aggregate = _cacheRepository.Get<TestAggregate>(Guid.NewGuid().ToString()).Result;
        }

        [Test]
        public async Task EvictsOldObjectFromCache()
        {
            await _cacheRepository.Get<TestAggregate>(_aggregate.Id);
            var newAggregate = _memoryCache.Get(_aggregate.Id);
            Assert.AreNotEqual(_aggregate, newAggregate);
        }

        [Test]
        public async Task GetsEventsFromStart()
        {
            var aggregate = await _cacheRepository.Get<TestAggregate>(_aggregate.Id);
            Assert.AreEqual(1, aggregate.Version);
        }
    }
}
