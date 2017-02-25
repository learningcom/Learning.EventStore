using System;
using System.Threading.Tasks;
using Learning.EventStore.Domain;
using Learning.EventStore.Domain.Exceptions;
using Learning.EventStore.Test.Mocks;
using NUnit.Framework;

namespace Learning.EventStore.Test.Domain
{
    public class GetEventsOutOfOrderTest
    {
        private readonly ISession _session;

        public GetEventsOutOfOrderTest()
        {
            var eventStore = new TestEventStoreWithBugs();
            _session = new Session(new Repository(eventStore));
        }

        [Test]
        public async Task ThrowsEventsOutOfOrderException()
        {
            var id = Guid.NewGuid().ToString();

            try
            {
                await _session.Get<TestAggregate>(id, 3);
                Assert.Fail();
            }
            catch (EventsOutOfOrderException)
            {
                Assert.Pass();
            }
        }
    }
}

