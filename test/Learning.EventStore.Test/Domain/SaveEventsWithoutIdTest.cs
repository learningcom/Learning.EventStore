using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Learning.EventStore.Domain;
using Learning.EventStore.Domain.Exceptions;
using Learning.EventStore.Test.Mocks;
using NUnit.Framework;

namespace Learning.EventStore.Test.Domain
{
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

        [Test]
        public async Task ThrowAggregateOrEventMissingIdExceptionFromRepository()
        {
            try
            {
                await _rep.Save(_aggregate, 0);
                Assert.Fail();
            }
            catch (AggregateOrEventMissingIdException)
            {
                Assert.Pass();
            }
        }
    }
}
