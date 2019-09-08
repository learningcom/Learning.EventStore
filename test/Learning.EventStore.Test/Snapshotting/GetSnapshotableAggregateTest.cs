using System;
using Learning.EventStore.Domain;
using Learning.EventStore.Snapshotting;
using Learning.EventStore.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Learning.EventStore.Test.Snapshotting
{
    [TestClass]
    public class GetSnapshotableAggregateTest
    {
        private readonly TestSnapshotStore _snapshotStore;
        private readonly TestSnapshotAggregate _aggregate;

        public GetSnapshotableAggregateTest()
        {
            var eventStore = new TestInMemoryEventStore();
            _snapshotStore = new TestSnapshotStore();
            var snapshotStrategy = new DefaultSnapshotStrategy(_snapshotStore);
            var repository = new SnapshotRepository(_snapshotStore, snapshotStrategy, new Repository(eventStore), eventStore);
            var session = new Session(repository);

            _aggregate = session.GetAsync<TestSnapshotAggregate>(Guid.NewGuid().ToString()).Result;
        }

        [TestMethod]
        public void ShouldAskForSnapshot()
        {
            Assert.IsTrue(_snapshotStore.VerifyGet);
        }

        [TestMethod]
        public void ShouldRunRestoreMethod()
        {
            Assert.IsTrue(_aggregate.Restored);
        }
    }
}
