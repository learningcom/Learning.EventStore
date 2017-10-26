using System;
using System.Threading.Tasks;
using Learning.EventStore.Domain;
using Learning.EventStore.Domain.Exceptions;
using Learning.EventStore.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Learning.EventStore.Test.Domain
{
    [TestClass]
    public class GetAggregateTest
    {
        private readonly ISession _session;

        public GetAggregateTest()
        {
            var eventStore = new TestEventStore();
            _session = new Session(new Repository(eventStore));
        }

        [TestMethod]
        public void Should_get_aggregate_from_eventstore()
        {
            var aggregate = _session.GetAsync<TestAggregate>(Guid.NewGuid().ToString());
            Assert.IsNotNull(aggregate);
        }

        [TestMethod]
        public async Task AppliesEvents()
        {
            var aggregate = await _session.GetAsync<TestAggregate>(Guid.NewGuid().ToString());
            Assert.AreEqual(2, aggregate.DidSomethingCount);
        }

        [TestMethod]
        public async Task ReturnsNullIfAggregateDoesNotExist()
        {
            var aggregate = await _session.GetAsync<TestAggregate>("");
            Assert.IsNull(aggregate);
        }

        [TestMethod]
        public async Task TracksChanges()
        {
            var agg = new TestAggregate(Guid.NewGuid().ToString());
            _session.Add(agg);
            var aggregate = await _session.GetAsync<TestAggregate>(agg.Id);
            Assert.AreEqual(agg, aggregate);
        }

        [TestMethod]
        public async Task GetsFromSessionIfTracked()
        {
            var id = Guid.NewGuid().ToString();
            var aggregate = await _session.GetAsync<TestAggregate>(id);
            var aggregate2 = await _session.GetAsync<TestAggregate>(id);

            Assert.AreEqual(aggregate, aggregate2);
        }

        [TestMethod]
        public async Task ThrowsConcurrencyExceptionIfTracked()
        {
            var id = Guid.NewGuid().ToString();
            await _session.GetAsync<TestAggregate>(id);

            try
            {
                await _session.GetAsync<TestAggregate>(id, 100);
                Assert.Fail();
            }
            catch (ConcurrencyException)
            {
                Assert.IsTrue(true);
            }
        }

        [TestMethod]
        public async Task Should_get_correct_version()
        {
            var id = Guid.NewGuid().ToString();
            var aggregate = await _session.GetAsync<TestAggregate>(id);

            Assert.AreEqual(3, aggregate.Version);
        }

        [TestMethod]
        public async Task ThrowsConcurrencyException()
        {
            var id = Guid.NewGuid().ToString();

            try
            {
                await _session.GetAsync<TestAggregate>(id, 1);
                Assert.Fail();
            }
            catch (ConcurrencyException)
            {
                Assert.IsTrue(true);
            }
        }
    }
}
