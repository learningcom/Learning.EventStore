using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Learning.EventStore.Cache;
using Learning.EventStore.Test.Mocks;
using NUnit.Framework;

namespace Learning.EventStore.Test.Cache
{
    public class SaveFailsTest
    {
        private readonly CacheRepository _rep;
        private readonly TestAggregate _aggregate;
        private readonly TestRepository _testRep;
        private readonly ICache _memoryCache;

        public SaveFailsTest()
        {
            _memoryCache = new MemoryCache();
            _testRep = new TestRepository();
            _rep = new CacheRepository(_testRep, new TestInMemoryEventStore(), _memoryCache);
            _aggregate = _testRep.Get<TestAggregate>(Guid.NewGuid()).Result;
            _aggregate.DoSomething();
            
        }

        [Test]
        public async Task EvictsOldObjectFromCache()
        {
            try
            {
                await _rep.Save(_aggregate, 100);
            }
            catch (Exception) { }

            var aggregate = _memoryCache.Get(_aggregate.Id);
            Assert.Null(aggregate);
        }
    }
}
