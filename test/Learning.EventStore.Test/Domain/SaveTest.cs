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

        [SetUp]
        public void Setup()
        {
            _eventStore.Events.Clear();
        }

        [Test]
        public async Task SavesUncommittedChanges()
        {
            _aggregate.DoSomething();
            _session.Add(_aggregate);
            await _session.Commit();
            Assert.AreEqual(1, _eventStore.Events.Count);
        }

        [Test]
        public async Task MarksCommittedAfterCommit()
        {
            _aggregate.DoSomething();
            _session.Add(_aggregate);
            await _session.Commit();
            Assert.AreEqual(0, _aggregate.GetUncommittedChanges().Count());
        }

        [Test]
        public async Task AddsNewAggregate()
        {
            var agg = new TestAggregateNoParameterLessConstructor(1);
            agg.DoSomething();
            _session.Add(agg);
            await _session.Commit();
            Assert.AreEqual(1, _eventStore.Events.Count);
        }

        [Test]
        public async Task SetsDate()
        {
            var agg = new TestAggregateNoParameterLessConstructor(1);
            agg.DoSomething();
            _session.Add(agg);
            await _session.Commit();
            Assert.That(_eventStore.Events.First().TimeStamp,
                Is.InRange(DateTimeOffset.UtcNow.AddSeconds(-1), DateTimeOffset.UtcNow.AddSeconds(1)));
        }

        [Test]
        public async Task SetsVersion()
        {
            var agg = new TestAggregateNoParameterLessConstructor(1);
            agg.DoSomething();
            agg.DoSomething();
            _session.Add(agg);
            await _session.Commit();
            Assert.AreEqual(1, _eventStore.Events.First().Version);
            Assert.AreEqual(2, _eventStore.Events.Last().Version);
        }

        [Test]
        public async Task SetsId()
        {
            var id = Guid.NewGuid();
            var agg = new TestAggregateNoParameterLessConstructor(1, id);
            agg.DoSomething();
            _session.Add(agg);
            await _session.Commit();
            Assert.AreEqual(id, _eventStore.Events.First().Id);
        }

        [Test]
        public async Task ClearsTrackedAggregates()
        {
            var agg = new TestAggregate(Guid.NewGuid());
            _session.Add(agg);
            agg.DoSomething();
            await _session.Commit();
            _eventStore.Events.Clear();

            try
            {
                await _session.Get<TestAggregate>(agg.Id);
                Assert.Fail();
            }
            catch (AggregateNotFoundException)
            {
                Assert.Pass();
            }
        }
    }
}

