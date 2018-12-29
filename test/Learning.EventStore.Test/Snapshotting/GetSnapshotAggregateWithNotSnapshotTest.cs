using System;
using System.Threading.Tasks;
using Learning.EventStore.Domain;
using Learning.EventStore.Snapshotting;
using Learning.EventStore.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Learning.EventStore.Test.Snapshotting
{
    [TestClass]
    public class GetSnapshotAggregateWithNoSnapshotTest
    {
        private readonly TestSnapshotAggregate _aggregate;

        public GetSnapshotAggregateWithNoSnapshotTest()
        {
            var eventStore = new TestEventStore();
            var snapshotStore = new NullSnapshotStore();
            var snapshotStrategy = new DefaultSnapshotStrategy(snapshotStore);
            var repository = new SnapshotRepository(snapshotStore, snapshotStrategy, new Repository(eventStore), eventStore);
            var eventStoreSettings = new TestEventStoreSettings { SessionLockEnabled = false };
            var session = new Session(repository, eventStoreSettings, null);
            _aggregate = session.GetAsync<TestSnapshotAggregate>(Guid.NewGuid().ToString()).Result;
        }

        private class NullSnapshotStore : ISnapshotStore
        {
            public Task<bool> ExistsAsync(string id)
            {
                return Task.FromResult(true);
            }

            public Task<Snapshot> GetAsync(string id)
            {
                return Task.FromResult<Snapshot>(null);
            }
            public Task SaveAsync(Snapshot snapshot)
            {
                return Task.CompletedTask;
            }
        }

        [TestMethod]
        public void Should_load_events()
        {
            Assert.IsTrue(_aggregate.Loaded);
        }

        [TestMethod]
        public void Should_not_load_snapshot()
        {
            Assert.IsFalse(_aggregate.Restored);
        }
    }
}
