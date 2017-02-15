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
    public class AddAggregateToRepositoryTest
    {
        private readonly Session _session;

        public AddAggregateToRepositoryTest()
        {
            var eventStore = new TestInMemoryEventStore();
            _session = new Session(new Repository(eventStore));
        }

        [Test]
        public void ThrowsIfDifferentObjectWithTrackedGuidIsAdded()
        {
            var aggregate = new TestAggregate(Guid.NewGuid());
            var aggregate2 = new TestAggregate(aggregate.Id);
            _session.Add(aggregate);
            Assert.Throws<ConcurrencyException>(() => _session.Add(aggregate2));
        }

        [Test]
        public void DoesNotThrowIfObjectAlreadyTracked()
        {
            var aggregate = new TestAggregate(Guid.NewGuid());
            _session.Add(aggregate);
            _session.Add(aggregate);
        }
    }
}
