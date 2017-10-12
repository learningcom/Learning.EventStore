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
        private readonly List<TestEvent> _eventList;
        private readonly string _serializedEvent;
        private readonly EventStore.RedisEventStore _redisEventStore;

        public SaveTest()
        {
            _redis = A.Fake<IRedisClient>();
            _trans = A.Fake<ITransaction>();
            _eventList = new List<TestEvent> { new TestEvent {Id = "12345"} };
            var database = A.Fake<IDatabase>();
            _redisEventStore = new EventStore.RedisEventStore(_redis, "test", "test");

            A.CallTo(() => _redis.Database).Returns(database);
            A.CallTo(() => _redis.Database.CreateTransaction(null)).Returns(_trans);
            A.CallTo(() => _redis.HashLengthAsync("EventStore:test")).Returns(Task.Run(() => (long) 2));

            var subscriberList = new RedisValue[]
                {
                    "Subscriber1",
                    "Subscriber2"
                };
            A.CallTo(() => _redis.SetMembersAsync("Subscribers:test:TestEvent")).Returns(Task.Run(() => subscriberList));


            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            _serializedEvent = JsonConvert.SerializeObject(_eventList.First(), settings);
        }

        [TestMethod]
        public async Task CreatesTransactions()
        {
            A.CallTo(() => _trans.ExecuteAsync(CommandFlags.None)).Returns(Task.Run(() => true));
            await _redisEventStore.SaveAsync(_eventList);

            A.CallTo(() => _redis.Database.CreateTransaction(null)).MustHaveHappened(Repeated.Exactly.Times(2));
        }

        [TestMethod]
        public async Task ExecutesTransactions()
        {
            A.CallTo(() => _trans.ExecuteAsync(CommandFlags.None)).Returns(Task.Run(() => true));
            await _redisEventStore.SaveAsync(_eventList);

            A.CallTo(() => _trans.ExecuteAsync(CommandFlags.None)).MustHaveHappened(Repeated.Exactly.Times(2));
        }

        [TestMethod]
        public async Task SetsNewHashEntry()
        {
            A.CallTo(() => _trans.ExecuteAsync(CommandFlags.None)).Returns(Task.Run(() => true));
            await _redisEventStore.SaveAsync(_eventList);

            A.CallTo(() => _redis.HashSetAsync(A<string>.That.Contains("EventStore:test"), A<string>._, _serializedEvent)).MustHaveHappened(Repeated.Exactly.Once);
        }

        [TestMethod]
        public async Task AddsToCommitList()
        {
            A.CallTo(() => _trans.ExecuteAsync(CommandFlags.None)).Returns(Task.Run(() => true));
            await _redisEventStore.SaveAsync(_eventList);

            A.CallTo(() => _trans.ListRightPushAsync($"{{EventStore:test}}:{_eventList.First().Id}", A<RedisValue>._, When.Always, CommandFlags.None)).MustHaveHappened(Repeated.Exactly.Once);
        }


        [TestMethod]
        public async Task AddsMessagesToPublishedEventsListForEachSubscriber()
        {
            A.CallTo(() => _trans.ExecuteAsync(CommandFlags.None)).Returns(Task.Run(() => true));
            await _redisEventStore.SaveAsync(_eventList);

            A.CallTo(() => _trans.ListRightPushAsync("{Subscriber1:test:TestEvent}:PublishedEvents", _serializedEvent, When.Always, CommandFlags.None))
                .MustHaveHappened();
            A.CallTo(() => _trans.ListRightPushAsync("{Subscriber2:test:TestEvent}:PublishedEvents", _serializedEvent, When.Always, CommandFlags.None))
                .MustHaveHappened();
        }

        [TestMethod]
        public async Task PublishesEvent()
        {
            A.CallTo(() => _trans.ExecuteAsync(CommandFlags.None)).Returns(Task.Run(() => true));
            await _redisEventStore.SaveAsync(_eventList);

            A.CallTo(() => _trans.PublishAsync("test:TestEvent", true, CommandFlags.None)).MustHaveHappened(Repeated.Exactly.Once);
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
                Assert.IsTrue(e.Message.Contains("Failed to save value in key EventStore:test:"));
                A.CallTo(() => _trans.PublishAsync("test:TestEvent", true, CommandFlags.None)).MustNotHaveHappened();
                A.CallTo(() => _trans.ListRightPushAsync(A<RedisKey>.That.Matches(x => x.ToString().Contains(_eventList.First().Id)), A<RedisValue>._, When.Always, CommandFlags.None)).MustHaveHappened(Repeated.Exactly.Times(10));
                A.CallTo(() => _redis.HashDeleteAsync(A<string>.That.Contains("EventStore:test"), A<string>._)).MustHaveHappened();
            }
        }

        [TestMethod]
        public void ThrowsArgumentExceptionIfEventStoreSettingsConstructorIsUsedAndKeyPrefixIsNotSet()
        {
            var settings = new EventStoreSettings();

            try
            {
                new EventStore.RedisEventStore(_redis, settings, "test");
                Assert.Fail("Should have thrown ArgumentException");
            }
            catch (ArgumentException e)
            {
                Assert.AreEqual("KeyPrefix must be specified in EventStoreSettings", e.Message);
            }
        }
    }
}
