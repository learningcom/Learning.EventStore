using System;
using System.Linq;
using System.Threading.Tasks;
using Learning.EventStore.Cache;
using Learning.EventStore.Domain;
using Learning.EventStore.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Learning.EventStore.Test.Cache
{
    [TestClass]
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

            _rep1.SaveAsync(_aggregate).Wait();

            var t1 = new Task(() =>
            {
                for (var i = 0; i < 100; i++)
                {
                    var aggregate = _rep1.GetAsync<TestAggregate>(_aggregate.Id).Result;
                    aggregate.DoSomething();
                    _rep1.SaveAsync(aggregate).Wait();
                }
            });

            var t2 = new Task(() =>
            {
                for (var i = 0; i < 100; i++)
                {
                    var aggregate = _rep2.GetAsync<TestAggregate>(_aggregate.Id).Result;
                    aggregate.DoSomething();
                    _rep2.SaveAsync(aggregate).Wait();
                }
            });
            var t3 = new Task(() =>
            {
                for (var i = 0; i < 100; i++)
                {
                    var aggregate = _rep2.GetAsync<TestAggregate>(_aggregate.Id).Result;
                    aggregate.DoSomething();
                    _rep2.SaveAsync(aggregate).Wait();
                }
            });
            t1.Start();
            t2.Start();
            t3.Start();

            Task.WaitAll(t1, t2, t3);

        }

        [TestMethod]
        public void GetsMoreThanOneEventWithSameId()
        {
            Assert.AreEqual(_testStore.Events.Count, _testStore.Events.Select(x => x.Version).Distinct().Count());
        }

        [TestMethod]
        public void SavesAllEvents()
        {
            Assert.AreEqual(301, _testStore.Events.Count);
        }
    }
}
