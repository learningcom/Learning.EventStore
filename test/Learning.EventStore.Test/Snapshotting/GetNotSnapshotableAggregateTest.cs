using System;
using Learning.EventStore.Domain;
using Learning.EventStore.Snapshotting;
using Learning.EventStore.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Learning.EventStore.Test.Snapshotting
{
    [TestClass]
    public class GetNotSnapshotableAggregateTest
    {
        private readonly TestSnapshotStore _snapshotStore;
        private readonly TestAggregate _aggregate;

        public GetNotSnapshotableAggregateTest()
        {
            var eventStore = new TestEventStore();
            _snapshotStore = new TestSnapshotStore();
            var snapshotStrategy = new DefaultSnapshotStrategy(_snapshotStore);
            var repository = new SnapshotRepository(_snapshotStore, snapshotStrategy, new Repository(eventStore), eventStore);
            var eventStoreSettings = new TestEventStoreSettings { SessionLockEnabled = false };
            var session = new Session(repository, eventStoreSettings, null);

            _aggregate = session.GetAsync<TestAggregate>(Guid.NewGuid().ToString()).Result;
        }

        [TestMethod]
        public void ShouldNotAskForSnapshot()
        {
            Assert.IsFalse(_snapshotStore.VerifyGet);
        }

        [TestMethod]
        public void ShouldRestoreFromEvents()
        {
            Assert.AreEqual(3, _aggregate.Version);
        }
    }
}

