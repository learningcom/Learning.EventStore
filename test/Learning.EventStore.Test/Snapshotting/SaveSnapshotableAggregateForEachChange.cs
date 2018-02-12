using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Learning.EventStore.Domain;
using Learning.EventStore.Snapshotting;
using Learning.EventStore.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Learning.EventStore.Test.Snapshotting
{
    [TestClass]
    public class SaveSnapshotableAggregateForEachChange
    {
        private readonly TestInMemorySnapshotStore _snapshotStore;
        private readonly ISession _session;
        private readonly TestSnapshotAggregate _aggregate;

        public SaveSnapshotableAggregateForEachChange()
        {
            IEventStore eventStore = new TestInMemoryEventStore();
            _snapshotStore = new TestInMemorySnapshotStore();
            var snapshotStrategy = new DefaultSnapshotStrategy(_snapshotStore);
            var repository = new SnapshotRepository(_snapshotStore, snapshotStrategy, new Repository(eventStore), eventStore);
            _session = new Session(repository);
            _aggregate = new TestSnapshotAggregate();

            for (var i = 0; i < 150; i++)
            {
                _session.Add(_aggregate);
                _aggregate.DoSomething();
                _session.CommitAsync().Wait();
            }
        }

        [TestMethod]
        public void ShouldSnapshot100thChange()
        {
            Assert.AreEqual(100, _snapshotStore.SavedVersion);
        }

        [TestMethod]
        public void ShouldNotSnapshotFirstEvent()
        {
            Assert.IsFalse(_snapshotStore.FirstSaved);
        }

        [TestMethod]
        public async Task ShouldGetAggregateBackCorrect()
        {
            Assert.AreEqual(150, (await _session.GetAsync<TestSnapshotAggregate>(_aggregate.Id)).Number);
        }
    }
}
