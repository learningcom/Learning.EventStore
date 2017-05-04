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
    public class SaveTwoAggregatesInParallel
    {
        private readonly CacheRepository _rep1;
        private TestAggregate _aggregate1;
        private readonly TestInMemoryEventStore _testStore;
        private readonly TestAggregate _aggregate2;

        public SaveTwoAggregatesInParallel()
        {
            _testStore = new TestInMemoryEventStore();
            _rep1 = new CacheRepository(new Repository(_testStore), _testStore, new MemoryCache());

            _aggregate1 = new TestAggregate(Guid.NewGuid().ToString());
            _aggregate2 = new TestAggregate(Guid.NewGuid().ToString());

            _rep1.SaveAsync(_aggregate1).Wait();
            _rep1.SaveAsync(_aggregate2).Wait();

            var t1 = new Task(() =>
            {
                for (var i = 0; i < 100; i++)
                {
                    var aggregate = _rep1.GetAsync<TestAggregate>(_aggregate1.Id).Result;
                    aggregate.DoSomething();
                    _rep1.SaveAsync(aggregate).Wait();
                }
            });

            var t2 = new Task(() =>
            {
                for (var i = 0; i < 100; i++)
                {
                    var aggregate = _rep1.GetAsync<TestAggregate>(_aggregate2.Id).Result;
                    aggregate.DoSomething();
                    _rep1.SaveAsync(aggregate).Wait();
                }
            });
            t1.Start();
            t2.Start();

            Task.WaitAll(t1, t2);
        }

        [TestMethod]
        public void DoesNotGetMoreThanOneEventWithSameId()
        {
            Assert.AreEqual(_testStore.Events.Count, _testStore.Events.Select(x => x.Version).Count());
        }

        [TestMethod]
        public void SavesAllEvents()
        {
            Assert.AreEqual(202, _testStore.Events.Count);
        }

        [TestMethod]
        public async Task DistributesEventsCorrectly()
        {
            var aggregate1 = await _rep1.GetAsync<TestAggregate>(_aggregate2.Id);
            Assert.AreEqual(100, aggregate1.DidSomethingCount);

            var aggregate2 = await _rep1.GetAsync<TestAggregate>(_aggregate2.Id);
            Assert.AreEqual(100, aggregate2.DidSomethingCount);
        }
    }
}
