using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using Learning.EventStore.Domain.Exceptions;
using Learning.EventStore.Infrastructure;
using Learning.EventStore.MessageQueue;
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
        private readonly IMessageQueue _messageQueue;
        private readonly EventStore.RedisEventStore _redisEventStore;

        public SaveTest()
        {
            _redis = A.Fake<IRedisClient>();
            _trans = A.Fake<ITransaction>();
            _eventList = new List<TestEvent> { new TestEvent {Id = "12345"} };
            _messageQueue = A.Fake<IMessageQueue>();
            var database = A.Fake<IDatabase>(); 
            _redisEventStore = new EventStore.RedisEventStore(_redis, "test", _messageQueue);

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

            A.CallTo(() => _redis.Database.CreateTransaction(null)).MustHaveHappened(Repeated.Exactly.Times(1));
        }

        [TestMethod]
        public async Task ExecutesTransaction()
        {
            A.CallTo(() => _trans.ExecuteAsync(CommandFlags.None)).Returns(Task.Run(() => true));
            await _redisEventStore.SaveAsync(_eventList);

            A.CallTo(() => _trans.ExecuteAsync(CommandFlags.None)).MustHaveHappened(Repeated.Exactly.Times(1));
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
        public async Task CallsMessageQueuePublish()
        {
            A.CallTo(() => _trans.ExecuteAsync(CommandFlags.None)).Returns(Task.Run(() => true));
            await _redisEventStore.SaveAsync(_eventList);

            A.CallTo(() => _messageQueue.PublishAsync(A<string>._, "12345", A<string>._ ))
                .MustHaveHappened();
        }

        [TestMethod]
        public async Task ThrowsExceptionIfSaveTransactionFails()
        {
            A.CallTo(() => _trans.ExecuteAsync(CommandFlags.None)).Returns(Task.Run(() => false));

            try
            {
                await _redisEventStore.SaveAsync(_eventList);
                Assert.Fail("Should have thrown ConcurrencyException");
            }
            catch(ConcurrencyException e)
            {
                Assert.AreEqual("A different version than expected was found in aggregate 12345", e.Message);
                A.CallTo(() => _trans.PublishAsync("test:TestEvent", true, CommandFlags.None)).MustNotHaveHappened();
                A.CallTo(() => _trans.ListRightPushAsync(A<RedisKey>.That.Matches(x => x.ToString().Contains(_eventList.First().Id)), A<RedisValue>._, When.Always, CommandFlags.None)).MustHaveHappened(Repeated.Exactly.Times(10));
                A.CallTo(() => _redis.HashDeleteAsync(A<string>.That.Contains("EventStore:test"), A<string>._)).MustHaveHappened();
            }
        }

        [TestMethod]
        public async Task DeletesFromEventStoreHashAndCommitListIfPublishThrowsException()
        {
            A.CallTo(() => _trans.ExecuteAsync(CommandFlags.None)).Returns(Task.Run(() => true));
            A.CallTo(() => _messageQueue.PublishAsync(A<string>._, A<string>._, A<string>._)).Throws(new MessagePublishFailedException("12345", 10));

            try
            {
                await _redisEventStore.SaveAsync(_eventList);
                Assert.Fail("Should have thrown MessagePublishFailedException");
            }
            catch (MessagePublishFailedException)
            {
                A.CallTo(() => _redis.HashDeleteAsync(A<string>.That.Contains("EventStore:test"), A<string>._)).MustHaveHappened();
                A.CallTo(() => _redis.ListRemoveAsync("{EventStore:test}:12345", A<string>._, -1)).MustHaveHappened();
            }
        }

        [TestMethod]
        public void ThrowsArgumentExceptionIfEventStoreSettingsConstructorIsUsedAndKeyPrefixIsNotSet()
        {
            var settings = new EventStoreSettings();

            try
            {
                new EventStore.RedisEventStore(_redis, settings, _messageQueue);
                Assert.Fail("Should have thrown ArgumentException");
            }
            catch (ArgumentException e)
            {
                Assert.AreEqual("KeyPrefix must be specified in EventStoreSettings", e.Message);
            }
        }
    }
}
