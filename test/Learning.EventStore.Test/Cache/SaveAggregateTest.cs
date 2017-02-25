using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Learning.EventStore.Cache;
using Learning.EventStore.Test.Mocks;
using NUnit.Framework;

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
            _aggregate = _testRep.Get<TestAggregate>(Guid.NewGuid().ToString()).Result;
            _aggregate.DoSomething();
        }

        [Test]
        public async Task GetsSameAggregateOnGet()
        {
            await _rep.Save(_aggregate, -1);
            var aggregate = _rep.Get<TestAggregate>(_aggregate.Id).Result;
            Assert.AreEqual(_aggregate,aggregate);
        }

        [Test]
        public async Task SavesToRepository()
        {
            await _rep.Save(_aggregate, -1);
            Assert.AreEqual(_aggregate.Id, _testRep.Saved.Id);
        }

        [Test]
        public async Task DoesNotCacheEmptyId()
        {
            await _rep.Save(_aggregate, -1);
            var aggregate = new TestAggregate(Guid.Empty.ToString());
            await _rep.Save(aggregate);
            Assert.AreNotEqual(aggregate, _rep.Get<TestAggregate>(Guid.Empty.ToString()));
        }
    }
}
