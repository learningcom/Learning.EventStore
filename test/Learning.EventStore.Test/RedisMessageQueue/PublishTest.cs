using System;
using System.Threading.Tasks;
using FakeItEasy;
using Learning.EventStore.Common;
using Learning.EventStore.Test.Mocks;
using Learning.MessageQueue;
using Learning.MessageQueue.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Learning.EventStore.Test.RedisMessageQueue
{
    [TestClass]
    public class PublishTest
    {
        private readonly IRedisClient _redis;
        private readonly ITransaction _trans;
        private readonly TestEvent _event;
        private readonly string _serializedEvent;
        private readonly IMessageQueue _messageQueue;

        public PublishTest()
        {
            _redis = A.Fake<IRedisClient>();
            _trans = A.Fake<ITransaction>();
            _messageQueue = A.Fake<IMessageQueue>();
            _event = new TestEvent { Id = "12345", TimeStamp = DateTimeOffset.UtcNow};
            var database = A.Fake<IDatabase>();
            _messageQueue = new MessageQueue.RedisMessageQueue(_redis, "test", "test");

            A.CallTo(() => _redis.Database).Returns(database);
            A.CallTo(() => _redis.Database.CreateTransaction(null)).Returns(_trans);

            var subscriberList = new RedisValue[]
            {
                "Subscriber1",
                "Subscriber2"
            };
            A.CallTo(() => _redis.SetMembersAsync("Subscribers:{test:TestEvent}")).Returns(Task.Run(() => subscriberList));


            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            _serializedEvent = JsonConvert.SerializeObject(_event, settings);
        }

        [TestMethod]
        public async Task CreatesTransaction()
        {
            A.CallTo(() => _trans.ExecuteAsync(CommandFlags.None)).Returns(Task.Run(() => true));

            await _messageQueue.PublishAsync(_event);

            A.CallTo(() => _redis.Database.CreateTransaction(null)).MustHaveHappened(Repeated.Exactly.Once);
        }

        [TestMethod]
        public async Task ExecutesTransaction()
        {
            A.CallTo(() => _trans.ExecuteAsync(CommandFlags.None)).Returns(Task.Run(() => true));

            await _messageQueue.PublishAsync(_event);

            A.CallTo(() => _trans.ExecuteAsync(CommandFlags.None)).MustHaveHappened();
        }


        [TestMethod]
        public async Task AddsMessagesToPublishedEventsListForEachSubscriber()
        {
            A.CallTo(() => _trans.ExecuteAsync(CommandFlags.None)).Returns(Task.Run(() => true));

            await _messageQueue.PublishAsync(_event);

            A.CallTo(() => _trans.ListRightPushAsync("Subscriber1:{test:TestEvent}:PublishedEvents", _serializedEvent, When.Always, CommandFlags.None))
                .MustHaveHappened();
            A.CallTo(() => _trans.ListRightPushAsync("Subscriber2:{test:TestEvent}:PublishedEvents", _serializedEvent, When.Always, CommandFlags.None))
                .MustHaveHappened();
        }

        [TestMethod]
        public async Task PublishesEvent()
        {
            A.CallTo(() => _trans.ExecuteAsync(CommandFlags.None)).Returns(Task.Run(() => true));

            await _messageQueue.PublishAsync(_event);

            A.CallTo(() => _trans.PublishAsync("test:TestEvent", true, CommandFlags.None)).MustHaveHappened(Repeated.Exactly.Once);
        }

        [TestMethod]
        public async Task ThrowsExeptionIfPublishTransactionFails()
        {
            A.CallTo(() => _trans.ExecuteAsync(CommandFlags.None)).Returns(Task.Run(() => false));

            try
            {
                await _messageQueue.PublishAsync(_event);
                Assert.Fail("Should have thrown MessagePublishFailedException");
            }
            catch (MessagePublishFailedException)
            {
                A.CallTo(() => _trans.ExecuteAsync(CommandFlags.None)).MustHaveHappened(Repeated.Exactly.Times(10));
                A.CallTo(() => _trans.ListRightPushAsync("Subscriber1:{test:TestEvent}:PublishedEvents", _serializedEvent, When.Always, CommandFlags.None))
                    .MustHaveHappened(Repeated.Exactly.Times(10));
                A.CallTo(() => _trans.ListRightPushAsync("Subscriber2:{test:TestEvent}:PublishedEvents", _serializedEvent, When.Always, CommandFlags.None))
                    .MustHaveHappened(Repeated.Exactly.Times(10));
                A.CallTo(() => _trans.PublishAsync("test:TestEvent", true, CommandFlags.None)).MustHaveHappened(Repeated.Exactly.Times(10));
            }
        }
    }
}
