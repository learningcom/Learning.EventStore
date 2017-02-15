using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Learning.EventStore.Cache;
using Learning.EventStore.Domain;
using Learning.EventStore.Test.Mocks;
using NUnit.Framework;

namespace Learning.EventStore.Test.Cache
{
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
            _aggregate = _cacheRepository.Get<TestAggregate>(Guid.NewGuid()).Result;
        }

        [Test]
        public void ReleasesSemaphoreAfterGetIsComplete()
        {
            var property = _cacheRepository.GetType().GetField("SemaphoreSlim", BindingFlags.Static | BindingFlags.NonPublic);
            var semaphore = (SemaphoreSlim)property.GetValue(_cacheRepository);

            Assert.AreEqual(1, semaphore.CurrentCount);
        }

        [Test]
        public async Task GetsCachedAggregate()
        {
            var aggregate = await _cacheRepository.Get<TestAggregate>(_aggregate.Id);
            Assert.AreEqual(_aggregate, aggregate);
        }


        [Test]
        public async Task UpdatesIfVersionChangedInEventStore()
        {
            await _cacheRepository.Get<TestAggregate>(_aggregate.Id);
            Assert.AreEqual(3, _aggregate.Version);
        }

        [Test]
        public async Task GetsSameAggregateFromDifferentCacheRespository()
        {
            var rep = new CacheRepository(new TestRepository(), new TestInMemoryEventStore(), _memoryCache);
            var aggregate = await rep.Get<TestAggregate>(_aggregate.Id);

            Assert.AreEqual(_aggregate.DidSomethingCount, aggregate.DidSomethingCount);
            Assert.AreEqual(_aggregate.Id, aggregate.Id);
            Assert.AreEqual(_aggregate.Version, aggregate.Version);
        }
    }
}
