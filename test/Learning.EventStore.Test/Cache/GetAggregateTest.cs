using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Learning.EventStore.Cache;
using Learning.EventStore.Domain;
using Learning.EventStore.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Learning.EventStore.Test.Cache
{
    [TestClass]
    public class GetAggregateTest
    {
        private readonly IRepository _cacheRepository;
        private readonly MemoryCache _memoryCache;
        private readonly TestAggregate _aggregate;

        public GetAggregateTest()
        {
            _memoryCache = new MemoryCache();
            IEventStore eventStore = new TestEventStore();
            _cacheRepository = new CacheRepository(new TestRepository(), eventStore, _memoryCache);
            _aggregate = _cacheRepository.GetAsync<TestAggregate>(Guid.NewGuid().ToString()).Result;
        }

        [TestMethod]
        public void ReleasesSemaphoreAfterGetIsComplete()
        {
            var property = _cacheRepository.GetType().GetField("SemaphoreSlim", BindingFlags.Static | BindingFlags.NonPublic);
            var semaphore = (SemaphoreSlim)property.GetValue(_cacheRepository);

            Assert.AreEqual(1, semaphore.CurrentCount);
        }

        [TestMethod]
        public async Task GetsCachedAggregate()
        {
            var aggregate = await _cacheRepository.GetAsync<TestAggregate>(_aggregate.Id);
            Assert.AreEqual(_aggregate, aggregate);
        }


        [TestMethod]
        public async Task UpdatesIfVersionChangedInEventStore()
        {
            await _cacheRepository.GetAsync<TestAggregate>(_aggregate.Id);
            Assert.AreEqual(3, _aggregate.Version);
        }

        [TestMethod]
        public async Task GetsSameAggregateFromDifferentCacheRespository()
        {
            var rep = new CacheRepository(new TestRepository(), new TestInMemoryEventStore(), _memoryCache);
            var aggregate = await rep.GetAsync<TestAggregate>(_aggregate.Id);

            Assert.AreEqual(_aggregate.DidSomethingCount, aggregate.DidSomethingCount);
            Assert.AreEqual(_aggregate.Id, aggregate.Id);
            Assert.AreEqual(_aggregate.Version, aggregate.Version);
        }
    }
}
