using System;
using System.Threading.Tasks;
using Learning.EventStore.Cache;
using Learning.EventStore.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Learning.EventStore.Test.Cache
{
    public class SaveAggregateTest
    {
        private readonly CacheRepository _rep;
        private readonly TestAggregate _aggregate;
        private readonly TestRepository _testRep;

        public SaveAggregateTest()
        {
            _testRep = new TestRepository();
            _rep = new CacheRepository(_testRep, new TestInMemoryEventStore(), new MemoryCache());
            _aggregate = _testRep.GetAsync<TestAggregate>(Guid.NewGuid().ToString()).Result;
            _aggregate.DoSomething();
        }

        [TestMethod]
        public async Task GetsSameAggregateOnGet()
        {
            await _rep.SaveAsync(_aggregate, -1);
            var aggregate = _rep.GetAsync<TestAggregate>(_aggregate.Id).Result;
            Assert.AreEqual(_aggregate,aggregate);
        }

        [TestMethod]
        public async Task SavesToRepository()
        {
            await _rep.SaveAsync(_aggregate, -1);
            Assert.AreEqual(_aggregate.Id, _testRep.Saved.Id);
        }

        [TestMethod]
        public async Task DoesNotCacheEmptyId()
        {
            await _rep.SaveAsync(_aggregate, -1);
            var aggregate = new TestAggregate(Guid.Empty.ToString());
            await _rep.SaveAsync(aggregate);
            Assert.AreNotEqual(aggregate, _rep.GetAsync<TestAggregate>(Guid.Empty.ToString()));
        }
    }
}
