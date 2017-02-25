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
    public class GetAggregateTest
    {
        private readonly ISession _session;

        public GetAggregateTest()
        {
            var eventStore = new TestEventStore();
            _session = new Session(new Repository(eventStore));
        }

        [Test]
        public void Should_get_aggregate_from_eventstore()
        {
            var aggregate = _session.Get<TestAggregate>(Guid.NewGuid().ToString());
            Assert.NotNull(aggregate);
        }

        [Test]
        public async Task AppliesEvents()
        {
            var aggregate = await _session.Get<TestAggregate>(Guid.NewGuid().ToString());
            Assert.AreEqual(2, aggregate.DidSomethingCount);
        }

        [Test]
        public async Task FailsIfAggregateDoesNotExist()
        {
            try
            {
                await _session.Get<TestAggregate>("");
                Assert.Fail();
            }
            catch (AggregateNotFoundException)
            {
                Assert.Pass();
            }
        }

        [Test]
        public async Task TracksChanges()
        {
            var agg = new TestAggregate(Guid.NewGuid().ToString());
            _session.Add(agg);
            var aggregate = await _session.Get<TestAggregate>(agg.Id);
            Assert.AreEqual(agg, aggregate);
        }

        [Test]
        public async Task GetsFromSessionIfTracked()
        {
            var id = Guid.NewGuid().ToString();
            var aggregate = await _session.Get<TestAggregate>(id);
            var aggregate2 = await _session.Get<TestAggregate>(id);

            Assert.AreEqual(aggregate, aggregate2);
        }

        [Test]
        public async Task ThrowsConcurrencyExceptionIfTracked()
        {
            var id = Guid.NewGuid().ToString();
            await _session.Get<TestAggregate>(id);

            try
            {
                await _session.Get<TestAggregate>(id, 100);
                Assert.Fail();
            }
            catch (ConcurrencyException)
            {
                Assert.Pass();
            }
        }

        [Test]
        public async Task Should_get_correct_version()
        {
            var id = Guid.NewGuid().ToString();
            var aggregate = await _session.Get<TestAggregate>(id);

            Assert.AreEqual(3, aggregate.Version);
        }

        [Test]
        public async Task ThrowsConcurrencyException()
        {
            var id = Guid.NewGuid().ToString();

            try
            {
                await _session.Get<TestAggregate>(id, 1);
                Assert.Fail();
            }
            catch (ConcurrencyException)
            {
                Assert.Pass();
            }
        }
    }
}
