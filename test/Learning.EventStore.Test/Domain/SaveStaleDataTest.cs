using System;
using System.Threading.Tasks;
using Learning.EventStore.Domain;
using Learning.EventStore.Domain.Exceptions;
using Learning.EventStore.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Learning.EventStore.Test.Domain
{
    [TestClass]
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
            var eventStoreSettings = new TestEventStoreSettings { SessionLockEnabled = false };
            _session = new Session(_rep, eventStoreSettings, null);

            _aggregate = new TestAggregate(Guid.NewGuid().ToString());
            _aggregate.DoSomething();
            _rep.SaveAsync(_aggregate).Wait();
        }

        [TestMethod]
        public async Task ThrowConcurrencyExceptionFromRepository()
        {
            try
            {
                await _rep.SaveAsync(_aggregate, 0);
                Assert.Fail();
            }
            catch (ConcurrencyException)
            {
                Assert.IsTrue(true);
            }
        }

        [TestMethod]
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
                Assert.IsTrue(true);
            }
        }
    }
}
