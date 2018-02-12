using System;
using Learning.EventStore.Domain;
using Learning.EventStore.Snapshotting;
using Learning.EventStore.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Learning.EventStore.Test.Snapshotting
{
    [TestClass]
    public class GetAnAggregateWithASnapshotTest
    {
        private readonly TestSnapshotAggregate _aggregate;

        public GetAnAggregateWithASnapshotTest()
        {
            var eventStore = new TestInMemoryEventStore();
            var snapshotStore = new TestSnapshotStore();
            var snapshotStrategy = new DefaultSnapshotStrategy(snapshotStore);
            var snapshotRepository = new SnapshotRepository(snapshotStore, snapshotStrategy, new Repository(eventStore), eventStore);
            var session = new Session(snapshotRepository);

            _aggregate = session.GetAsync<TestSnapshotAggregate>(Guid.NewGuid().ToString()).Result;
        }

        [TestMethod]
        public void ShouldRestore()
        {
            Assert.IsTrue(_aggregate.Restored);
        }
    }
}
