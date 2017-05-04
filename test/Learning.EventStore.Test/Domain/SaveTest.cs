using System;
using System.Linq;
using System.Threading.Tasks;
using Learning.EventStore.Domain;
using Learning.EventStore.Domain.Exceptions;
using Learning.EventStore.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Learning.EventStore.Test.Domain
{
    [TestClass]
    public class SaveTest
    {
        private readonly TestInMemoryEventStore _eventStore;
        private readonly TestAggregateNoParameterLessConstructor _aggregate;
        private readonly ISession _session;
        private readonly Repository _rep;

        public SaveTest()
        {
            _eventStore = new TestInMemoryEventStore();
            _rep = new Repository(_eventStore);
            _session = new Session(_rep);

            _aggregate = new TestAggregateNoParameterLessConstructor(2);
        }

        [TestInitialize]
        public void Setup()
        {
            _eventStore.Events.Clear();
        }

        [TestMethod]
        public async Task SavesUncommittedChanges()
        {
            _aggregate.DoSomething();
            _session.Add(_aggregate);
            await _session.CommitAsync();
            Assert.AreEqual(1, _eventStore.Events.Count);
        }

        [TestMethod]
        public async Task MarksCommittedAfterCommit()
        {
            _aggregate.DoSomething();
            _session.Add(_aggregate);
            await _session.CommitAsync();
            Assert.AreEqual(0, _aggregate.GetUncommittedChanges().Count());
        }

        [TestMethod]
        public async Task AddsNewAggregate()
        {
            var agg = new TestAggregateNoParameterLessConstructor(1);
            agg.DoSomething();
            _session.Add(agg);
            await _session.CommitAsync();
            Assert.AreEqual(1, _eventStore.Events.Count);
        }

        [TestMethod]
        public async Task SetsDate()
        {
            var agg = new TestAggregateNoParameterLessConstructor(1);
            agg.DoSomething();
            _session.Add(agg);
            await _session.CommitAsync();
            Assert.IsTrue(_eventStore.Events.First().TimeStamp >= DateTimeOffset.UtcNow.AddSeconds(-1));
            Assert.IsTrue(_eventStore.Events.First().TimeStamp <= DateTimeOffset.UtcNow.AddSeconds(1));
        }

        [TestMethod]
        public async Task SetsVersion()
        {
            var agg = new TestAggregateNoParameterLessConstructor(1);
            agg.DoSomething();
            agg.DoSomething();
            _session.Add(agg);
            await _session.CommitAsync();
            Assert.AreEqual(1, _eventStore.Events.First().Version);
            Assert.AreEqual(2, _eventStore.Events.Last().Version);
        }

        [TestMethod]
        public async Task SetsId()
        {
            var id = Guid.NewGuid().ToString();
            var agg = new TestAggregateNoParameterLessConstructor(1, id);
            agg.DoSomething();
            _session.Add(agg);
            await _session.CommitAsync();
            Assert.AreEqual(id, _eventStore.Events.First().Id);
        }

        [TestMethod]
        public async Task ClearsTrackedAggregates()
        {
            var agg = new TestAggregate(Guid.NewGuid().ToString());
            _session.Add(agg);
            agg.DoSomething();
            await _session.CommitAsync();
            _eventStore.Events.Clear();

            try
            {
                await _session.GetAsync<TestAggregate>(agg.Id);
                Assert.Fail();
            }
            catch (AggregateNotFoundException)
            {
                Assert.IsTrue(true);
            }
        }
    }
}

