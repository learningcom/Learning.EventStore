using System;
using System.Threading.Tasks;
using Learning.EventStore.Domain;
using Learning.EventStore.Domain.Exceptions;
using Learning.EventStore.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Learning.EventStore.Test.Domain
{
    [TestClass]
    public class GetEventsOutOfOrderTest
    {
        private readonly ISession _session;

        public GetEventsOutOfOrderTest()
        {
            var eventStore = new TestEventStoreWithBugs();
            var eventStoreSettings = new TestEventStoreSettings { SessionLockEnabled = false };
            _session = new Session(new Repository(eventStore), eventStoreSettings, null);
        }

        [TestMethod]
        public async Task ThrowsEventsOutOfOrderException()
        {
            var id = Guid.NewGuid().ToString();

            try
            {
                await _session.GetAsync<TestAggregate>(id, 3);
                Assert.Fail();
            }
            catch (EventsOutOfOrderException)
            {
                Assert.IsTrue(true);
            }
        }
    }
}

