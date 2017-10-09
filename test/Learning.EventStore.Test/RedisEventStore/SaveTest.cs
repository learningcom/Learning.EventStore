using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using Learning.EventStore.Infrastructure;
using Learning.EventStore.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Learning.EventStore.Test.RedisEventStore
{
    [TestClass]
    public class SaveTest
    {
        private readonly IRedisClient _redis;
        private readonly ITransaction _trans;
        private readonly IEventPublisher _publisher;
        private readonly List<TestEvent> _eventList;
        private readonly string _serializedEvent;
        private readonly EventStore.RedisEventStore _redisEventStore;

        public SaveTest()
        {
            _redis = A.Fake<IRedisClient>();
            _trans = A.Fake<ITransaction>();
            _publisher = A.Fake<IEventPublisher>();
            _eventList = new List<TestEvent> { new TestEvent() };
            var database = A.Fake<IDatabase>();
            _redisEventStore = new EventStore.RedisEventStore(_redis, _publisher, "test");

            A.CallTo(() => _redis.Database).Returns(database);
            A.CallTo(() => _redis.Database.CreateTransaction(null)).Returns(_trans);
            A.CallTo(() => _redis.HashLengthAsync("EventStore:test")).Returns(Task.Run(() => (long) 2));

            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            _serializedEvent = JsonConvert.SerializeObject(_eventList.First(), settings);
        }

        [TestMethod]
        public async Task CreatesTransaction()
        {
            A.CallTo(() => _trans.ExecuteAsync(CommandFlags.None)).Returns(Task.Run(() => true));
            await _redisEventStore.SaveAsync(_eventList);

            A.CallTo(() => _redis.Database.CreateTransaction(null)).MustHaveHappened(Repeated.Exactly.Once);
        }

        [TestMethod]
        public async Task ExecutesTransaction()
        {
            A.CallTo(() => _trans.ExecuteAsync(CommandFlags.None)).Returns(Task.Run(() => true));
            await _redisEventStore.SaveAsync(_eventList);

            A.CallTo(() => _trans.ExecuteAsync(CommandFlags.None)).MustHaveHappened(Repeated.Exactly.Once);
        }

        [TestMethod]
        public async Task SetsNewHashEntry()
        {
            A.CallTo(() => _trans.ExecuteAsync(CommandFlags.None)).Returns(Task.Run(() => true));
            await _redisEventStore.SaveAsync(_eventList);

            A.CallTo(() => _trans.HashSetAsync("EventStore:test", A<RedisValue>._, _serializedEvent, When.Always, CommandFlags.None)).MustHaveHappened(Repeated.Exactly.Once);
        }

        [TestMethod]
        public async Task AddsToCommitList()
        {
            A.CallTo(() => _trans.ExecuteAsync(CommandFlags.None)).Returns(Task.Run(() => true));
            await _redisEventStore.SaveAsync(_eventList);

            A.CallTo(() => _trans.ListRightPushAsync($"{{EventStore:test}}:{_eventList.First().Id}", A<RedisValue>._, When.Always, CommandFlags.None)).MustHaveHappened(Repeated.Exactly.Once);
        }

        [TestMethod]
        public async Task PublishesEvent()
        {
            A.CallTo(() => _trans.ExecuteAsync(CommandFlags.None)).Returns(Task.Run(() => true));
            await _redisEventStore.SaveAsync(_eventList);

            A.CallTo(() => _publisher.Publish(A<IEvent>.That.IsSameAs(_eventList.First()))).MustHaveHappened(Repeated.Exactly.Once);
        }

        [TestMethod]
        public async Task ThrowsExceptionIfSaveTransactionFails()
        {
            A.CallTo(() => _trans.ExecuteAsync(CommandFlags.None)).Returns(Task.Run(() => false));

            try
            {
                await _redisEventStore.SaveAsync(_eventList);
                Assert.Fail("Should have thrown InvalidOperationException");
            }
            catch(InvalidOperationException e)
            {
                Assert.AreEqual("Failed to save value in key EventStore:test after retrying 10 times", e.Message);
                A.CallTo(() => _publisher.Publish(A<IEvent>.That.IsSameAs(_eventList.First()))).MustHaveHappened(Repeated.Exactly.Times(10));
                A.CallTo(() => _trans.ListRightPushAsync($"{{EventStore:test}}:{_eventList.First().Id}", A<RedisValue>._, When.Always, CommandFlags.None)).MustHaveHappened(Repeated.Exactly.Times(10));
                A.CallTo(() => _trans.HashSetAsync("EventStore:test", A<RedisValue>._, _serializedEvent, When.Always, CommandFlags.None)).MustHaveHappened(Repeated.Exactly.Times(10));
            }
        }

        [TestMethod]
        public void ThrowsArgumentExceptionIfEventStoreSettingsConstructorIsUsedAndKeyPrefixIsNotSet()
        {
            var settings = new EventStoreSettings();

            try
            {
                new EventStore.RedisEventStore(_redis, _publisher, settings);
                Assert.Fail("Should have thrown ArgumentException");
            }
            catch (ArgumentException e)
            {
                Assert.AreEqual("KeyPrefix must be specified in EventStoreSettings", e.Message);
            }
        }
    }
}
