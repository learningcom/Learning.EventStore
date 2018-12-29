using System;
using Learning.EventStore.Domain;
using Learning.EventStore.Domain.Exceptions;
using Learning.EventStore.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Learning.EventStore.Test.Domain
{
    [TestClass]
    public class AddAggregateToRepositoryTest
    {
        private readonly Session _session;

        public AddAggregateToRepositoryTest()
        {
            var eventStore = new TestInMemoryEventStore();
            var eventStoreSettings = new TestEventStoreSettings { SessionLockEnabled = false };
            _session = new Session(new Repository(eventStore), eventStoreSettings, null);
        }

        [TestMethod]
        public void ThrowsIfDifferentObjectWithTrackedGuidIsAdded()
        {
            var aggregate = new TestAggregate(Guid.NewGuid().ToString());
            var aggregate2 = new TestAggregate(aggregate.Id);
            _session.Add(aggregate);
            try
            {
                _session.Add(aggregate2);
                Assert.Fail("should have thrown ConcurrencyException");
            }
            catch (ConcurrencyException)
            {
                Assert.IsTrue(true);
            }
        }

        [TestMethod]
        public void DoesNotThrowIfObjectAlreadyTracked()
        {
            var aggregate = new TestAggregate(Guid.NewGuid().ToString());
            _session.Add(aggregate);
            _session.Add(aggregate);
        }
    }
}
