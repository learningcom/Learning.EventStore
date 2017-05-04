using System;
using System.Threading.Tasks;
using Learning.EventStore.Cache;
using Learning.EventStore.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Learning.EventStore.Test.Cache
{
    [TestClass]
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
            _aggregate = _testRep.GetAsync<TestAggregate>(Guid.NewGuid().ToString()).Result;
            _aggregate.DoSomething();
            
        }

        [TestMethod]
        public async Task EvictsOldObjectFromCache()
        {
            try
            {
                await _rep.SaveAsync(_aggregate, 100);
            }
            catch (Exception) { }

            var aggregate = _memoryCache.Get(_aggregate.Id);
            Assert.IsNull(aggregate);
        }
    }
}
