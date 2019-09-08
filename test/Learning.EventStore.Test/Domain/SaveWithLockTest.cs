using System;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using Learning.EventStore.Common;
using Learning.EventStore.Common.Exceptions;
using Learning.EventStore.Domain;
using Learning.EventStore.Domain.Exceptions;
using Learning.EventStore.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RedLockNet;

namespace Learning.EventStore.Test.Domain
{
    [TestClass]
    public class SaveWithLockTest
    {
        private readonly TestInMemoryEventStore _eventStore;
        private readonly TestAggregateNoParameterLessConstructor _aggregate;
        private readonly ISession _session;
        private readonly Repository _rep;
        private readonly IDistributedLockFactory _distributedLockFactory;
        private readonly DistributedLockSettings _distributedLockSettings;

        public SaveWithLockTest()
        {
            _eventStore = new TestInMemoryEventStore();
            _rep = new Repository(_eventStore);
            _distributedLockFactory = A.Fake<IDistributedLockFactory>();
            _session = new Session(_rep, _distributedLockFactory, true);
            _aggregate = new TestAggregateNoParameterLessConstructor(2);
            _distributedLockSettings = new DistributedLockSettings();
        }

        [TestInitialize]
        public void Setup()
        {
            _eventStore.Events.Clear();
        }

        [TestMethod]
        public async Task CreatesLockAndSavesUncommittedChangesAndDisposesLock()
        {
            var distributedLock = A.Fake<IRedLock>();
            A.CallTo(() => distributedLock.IsAcquired).Returns(true);
            A.CallTo(() => distributedLock.Resource).Returns(_aggregate.Id);
            A.CallTo(() => _distributedLockFactory.CreateLockAsync(_aggregate.Id, A<TimeSpan>._, A<TimeSpan>._, A<TimeSpan>._, null)).Returns(distributedLock);

            _aggregate.DoSomething();
            _session.Add(_aggregate);
            await _session.CommitAsync();

            Assert.AreEqual(1, _eventStore.Events.Count);
            A.CallTo(() => _distributedLockFactory.CreateLockAsync(_aggregate.Id, A<TimeSpan>._, A<TimeSpan>._, A<TimeSpan>._, null)).MustHaveHappenedOnceExactly();
            A.CallTo(() => distributedLock.Dispose()).MustHaveHappened();
        }

        [TestMethod]
        public async Task ThrowsDistributedLockExceptionIfLockIsExpiredDuringCommit()
        {
            var distributedLock = A.Fake<IRedLock>();
            A.CallTo(() => distributedLock.IsAcquired).Returns(true);
            A.CallTo(() => distributedLock.Resource).Returns(_aggregate.Id);
            A.CallTo(() => distributedLock.Status).Returns(RedLockStatus.Expired);
            A.CallTo(() => _distributedLockFactory.CreateLockAsync(_aggregate.Id, A<TimeSpan>._, A<TimeSpan>._, A<TimeSpan>._, null)).Returns(distributedLock);

            _aggregate.DoSomething();
            _session.Add(_aggregate);

            try
            {
                await _session.CommitAsync();
                Assert.Fail("Should have thrown DistributedLockException");
            }
            catch (DistributedLockException e)
            {
                Assert.AreEqual($"Session lock expired for aggregate '{_aggregate.Id} after {_distributedLockSettings.ExpirySeconds} seconds. Aborting session commit.", e.Message);
            }
        }

        [TestMethod]
        public async Task ThrowsDistributedLockExceptionIfExistingLockIsExpiredDuringAdd()
        {
            var distributedLock = A.Fake<IRedLock>();
            A.CallTo(() => distributedLock.IsAcquired).Returns(true);
            A.CallTo(() => distributedLock.Resource).Returns(_aggregate.Id);
            A.CallTo(() => _distributedLockFactory.CreateLockAsync(_aggregate.Id, A<TimeSpan>._, A<TimeSpan>._, A<TimeSpan>._, null)).Returns(distributedLock);
            await _session.GetAsync<TestAggregateNoParameterLessConstructor>(_aggregate.Id);
            
            try
            {
                A.CallTo(() => distributedLock.Status).Returns(RedLockStatus.Expired);
                await _session.AddAsync(_aggregate);
                Assert.Fail("Should have thrown DistributedLockException");
            }
            catch (DistributedLockException e)
            {
                Assert.AreEqual($"Existing session lock expired for aggregate '{_aggregate.Id} after {_distributedLockSettings.ExpirySeconds} seconds.", e.Message);
                Assert.AreEqual(0, _eventStore.Events.Count);
                A.CallTo(() => distributedLock.Dispose()).MustHaveHappened();
            }
        }

        [TestMethod]
        public async Task ThrowsDistributedLockExceptionIfNewLockCannotBeAcquired()
        {
            var distributedLock = A.Fake<IRedLock>();
            A.CallTo(() => distributedLock.IsAcquired).Returns(false);
            A.CallTo(() => distributedLock.Status).Returns(RedLockStatus.NoQuorum);
            A.CallTo(() => distributedLock.Resource).Returns(_aggregate.Id);
            A.CallTo(() => _distributedLockFactory.CreateLockAsync(_aggregate.Id, A<TimeSpan>._, A<TimeSpan>._, A<TimeSpan>._, null)).Returns(distributedLock);
            
            try
            {
                await _session.AddAsync(_aggregate);
                Assert.Fail("Should have thrown DistributedLockException");
            }
            catch (DistributedLockException e)
            {
                Assert.AreEqual($"Failed to get lock for Aggregate '{_aggregate.Id}' within {_distributedLockSettings.WaitSeconds} seconds with status: NoQuorum", e.Message);
            }
        }
    }
}

