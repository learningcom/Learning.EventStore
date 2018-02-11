using System;
using System.Threading.Tasks;
using Learning.EventStore.Domain;
using Learning.EventStore.Snapshotting;
using Learning.EventStore.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Learning.EventStore.Test.Snapshotting
{
    [TestClass]
    public class SaveNotSnapshotableAggregate
    {
        private readonly TestSnapshotStore _snapshotStore;
        private readonly TestInMemoryEventStore _eventStore;

        public SaveNotSnapshotableAggregate()
        {
            _eventStore = new TestInMemoryEventStore();
            _snapshotStore = new TestSnapshotStore();
            var snapshotStrategy = new DefaultSnapshotStrategy(_snapshotStore);
            var repository = new SnapshotRepository(_snapshotStore, snapshotStrategy, new Repository(_eventStore), _eventStore);
            var session = new Session(repository);
            var aggregate = new TestAggregate(Guid.NewGuid().ToString());

            for (var i = 0; i < 200; i++)
            {
                aggregate.DoSomething();
            }
            Task.Run(async () =>
            {
                session.Add(aggregate);
                await session.CommitAsync();
            }).Wait();
        }

        [TestMethod]
        public void ShouldNotSaveSnapshot()
        {
            Assert.IsFalse(_snapshotStore.VerifySave);
        }

        [TestMethod]
        public void ShouldSaveEvents()
        {
            Assert.AreEqual(201, _eventStore.Events.Count);
        }
    }
}
