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
    public class SaveStaleDataTest
    {
        private readonly TestInMemoryEventStore _eventStore;
        private readonly TestAggregate _aggregate;
        private readonly Repository _rep;
        private readonly Session _session;

        public SaveStaleDataTest()
        {
            _eventStore = new TestInMemoryEventStore();
            _rep = new Repository(_eventStore);
            _session = new Session(_rep);

            _aggregate = new TestAggregate(Guid.NewGuid().ToString());
            _aggregate.DoSomething();
            _rep.SaveAsync(_aggregate).Wait();
        }

        [Test]
        public async Task ThrowConcurrencyExceptionFromRepository()
        {
            try
            {
                await _rep.SaveAsync(_aggregate, 0);
                Assert.Fail();
            }
            catch (ConcurrencyException)
            {
                Assert.Pass();
            }
        }

        [Test]
        public async Task ThrowConcurrencyExceptionFromSession()
        {
            _session.Add(_aggregate);
            _aggregate.DoSomething();
            await _rep.SaveAsync(_aggregate);

            try
            {
                await _session.CommitAsync();
                Assert.Fail();
            }
            catch (ConcurrencyException)
            {
                Assert.Pass();
            }
        }
    }
}
