using System;
using System.Threading.Tasks;
using FakeItEasy;
using Learning.EventStore.Domain;
using Learning.EventStore.Domain.Exceptions;
using Learning.EventStore.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RedLockNet;

namespace Learning.EventStore.Test.Domain
{
    [TestClass]
    public class GetAggregateWithLockTest
    {
        private readonly ISession _session;
        private readonly IDistributedLockFactory _distributedLockFactory;

        public GetAggregateWithLockTest()
        {
            var eventStore = new TestEventStore();
            var eventStoreSettings = new TestEventStoreSettings { SessionLockEnabled = true };
            _distributedLockFactory = A.Fake<IDistributedLockFactory>();
            _session = new Session(new Repository(eventStore), eventStoreSettings, _distributedLockFactory);
        }

        [TestMethod]
        public void CreatesLock()
        {
            var aggregateId = Guid.NewGuid().ToString();;
            var distributedLock = A.Fake<IRedLock>();
            A.CallTo(() => distributedLock.IsAcquired).Returns(true);
            A.CallTo(() => distributedLock.Resource).Returns(aggregateId);
            A.CallTo(() => _distributedLockFactory.CreateLockAsync(aggregateId, A<TimeSpan>._, A<TimeSpan>._, A<TimeSpan>._, null)).Returns(distributedLock);

            var aggregate = _session.GetAsync<TestAggregate>(aggregateId);
            
            A.CallTo(() => _distributedLockFactory.CreateLockAsync(aggregateId, A<TimeSpan>._, A<TimeSpan>._, A<TimeSpan>._, null)).MustHaveHappenedOnceExactly();
        }
    }
}
