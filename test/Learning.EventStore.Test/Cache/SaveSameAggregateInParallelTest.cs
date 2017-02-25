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
    public class SaveSameAggregateInParallelTest
    {
        private readonly CacheRepository _rep1;
        private readonly CacheRepository _rep2;
        private readonly TestAggregate _aggregate;
        private readonly TestInMemoryEventStore _testStore;

        public SaveSameAggregateInParallelTest()
        {
            var memoryCache = new MemoryCache();

            _testStore = new TestInMemoryEventStore();
            _rep1 = new CacheRepository(new Repository(_testStore), _testStore, memoryCache);
            _rep2 = new CacheRepository(new Repository(_testStore), _testStore, memoryCache);

            _aggregate = new TestAggregate(Guid.NewGuid().ToString());

            _rep1.Save(_aggregate).Wait();

            var t1 = new Task(() =>
            {
                for (var i = 0; i < 100; i++)
                {
                    var aggregate = _rep1.Get<TestAggregate>(_aggregate.Id).Result;
                    aggregate.DoSomething();
                    _rep1.Save(aggregate).Wait();
                }
            });

            var t2 = new Task(() =>
            {
                for (var i = 0; i < 100; i++)
                {
                    var aggregate = _rep2.Get<TestAggregate>(_aggregate.Id).Result;
                    aggregate.DoSomething();
                    _rep2.Save(aggregate).Wait();
                }
            });
            var t3 = new Task(() =>
            {
                for (var i = 0; i < 100; i++)
                {
                    var aggregate = _rep2.Get<TestAggregate>(_aggregate.Id).Result;
                    aggregate.DoSomething();
                    _rep2.Save(aggregate).Wait();
                }
            });
            t1.Start();
            t2.Start();
            t3.Start();

            Task.WaitAll(t1, t2, t3);

        }

        [Test]
        public void GetsMoreThanOneEventWithSameId()
        {
            Assert.AreEqual(_testStore.Events.Count, _testStore.Events.Select(x => x.Version).Distinct().Count());
        }

        [Test]
        public void SavesAllEvents()
        {
            Assert.AreEqual(301, _testStore.Events.Count);
        }
    }
}
