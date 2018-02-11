using System.Threading.Tasks;
using Learning.EventStore.Domain;
using Learning.EventStore.Snapshotting;
using Learning.EventStore.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Learning.EventStore.Test.Snapshotting
{
    [TestClass]
    public class WhenSavingASnapshotableAggregate
    {
        private readonly TestSnapshotStore _snapshotStore;

        public WhenSavingASnapshotableAggregate()
        {
            var eventStore = new TestInMemoryEventStore();
            _snapshotStore = new TestSnapshotStore();
            var snapshotStrategy = new DefaultSnapshotStrategy(_snapshotStore);
            var repository = new SnapshotRepository(_snapshotStore, snapshotStrategy, new Repository(eventStore),
                eventStore);
            var session = new Session(repository);
            var aggregate = new TestSnapshotAggregate();

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
        public void ShouldSaveSnapshot()
        {
            Assert.IsTrue(_snapshotStore.VerifySave);
        }

        [TestMethod]
        public void ShouldSaveLastVersionNumber()
        {
            Assert.AreEqual(200, _snapshotStore.SavedVersion);
        }
    }
}
