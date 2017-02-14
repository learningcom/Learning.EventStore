using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Learning.EventStore.Cache;
using Learning.EventStore.Domain;
using Learning.EventStore.Test.Mocks;
using NUnit.Framework;

namespace Learning.EventStore.Test
{
    public class CacheTest
    {
        private readonly IRepository _cacheRepository;
        private readonly IEventStore _eventStore;
        private readonly MemoryCache _memoryCache;

        public CacheTest()
        {
            _memoryCache = new MemoryCache();
            _eventStore = A.Fake<IEventStore>();
            _cacheRepository = new CacheRepository(new TestRepository(), _eventStore, _memoryCache);       
        }

        [Test]
        public void ReleasesSemaphoreAfterGetIsComplete()
        {
            var aggregate = _cacheRepository.Get<TestAggregate>(Guid.NewGuid()).Result;
            A.CallTo(() => _eventStore.Get<TestAggregate>(aggregate.Id, aggregate.Version)).Returns(new List<IEvent>
                {
                    new TestAggregateDidSomething {Id = aggregate.Id, Version = 1},
                    new TestAggregateDidSomeethingElse {Id = aggregate.Id, Version = 2},
                    new TestAggregateDidSomething {Id = aggregate.Id, Version = 3},
                }.Where(x => x.Version > aggregate.Version));

            var property = _cacheRepository.GetType().GetField("SemaphoreSlim", BindingFlags.Static | BindingFlags.NonPublic);
            var semaphore = (SemaphoreSlim)property.GetValue(_cacheRepository);
            Assert.AreEqual(1, semaphore.CurrentCount);
        }
    }
}
