using System.Threading.Tasks;
using Learning.EventStore.Domain;
using Learning.EventStore.Domain.Exceptions;
using Learning.EventStore.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Learning.EventStore.Test.Domain
{
    [TestClass]
    public class SaveEventsWithoutIdTest
    {
        private readonly TestInMemoryEventStore _eventStore;
        private readonly TestAggregate _aggregate;
        private readonly Repository _rep;

        public SaveEventsWithoutIdTest()
        {
            _eventStore = new TestInMemoryEventStore();
            _rep = new Repository(_eventStore);

            _aggregate = new TestAggregate("");
        }

        [TestMethod]
        public async Task ThrowAggregateOrEventMissingIdExceptionFromRepository()
        {
            try
            {
                await _rep.SaveAsync(_aggregate, 0);
                Assert.Fail();
            }
            catch (AggregateOrEventMissingIdException)
            {
                Assert.IsTrue(true);
            }
        }
    }
}
