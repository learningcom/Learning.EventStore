using System;
using FakeItEasy;
using Learning.EventStore.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Learning.EventStore.Test.RedisEventSubscriber
{
    [TestClass]
    public class SubscribeTest
    {
        private readonly IRedisClient _redis;
        private readonly TestEvent _testEvent;
        private TestEvent _callbackData;
        private readonly string _serializedEvent;

        public SubscribeTest()
        {
            _redis = A.Fake<IRedisClient>();
            var subscriber = new EventStore.RedisEventSubscriber(_redis, "TestPrefix");
            _testEvent = new TestEvent();
            _serializedEvent = JsonConvert.SerializeObject(_testEvent);

            A.CallTo(() => _redis.ListRightPopLeftPushAsync("{TestPrefix:TestEvent}:PublishedEvents", "{TestPrefix:TestEvent}:ProcessingEvents")).Returns(_serializedEvent);
            A.CallTo(() => _redis.SubscribeAsync("TestEvent", A<Action<RedisChannel, RedisValue>>._))
                .Invokes(callObject =>
                {
                    var action = callObject.Arguments[1] as Action<RedisChannel,RedisValue>;
                    action(callObject.Arguments[0].ToString(), _serializedEvent);
                });

            Action<TestEvent> cb = (data) =>
            {
                _callbackData = data;
            };

            subscriber.SubscribeAsync(cb).Wait();
        }

        [TestMethod]
        public void RegistersAsSubscriber()
        {
            A.CallTo(() => _redis.SetAddAsync("Subscribers:TestEvent", "TestPrefix")).MustHaveHappened();
        }

        [TestMethod]
        public void ExecutesCallbackWithCorrectData()
        {
            Assert.AreEqual(JsonConvert.SerializeObject(_callbackData), _serializedEvent);
        }

        [TestMethod]
        public void PopsEventFromPublishedListAndPushesIntoProcessingList()
        {
            A.CallTo(() => _redis.ListRightPopLeftPushAsync("{TestPrefix:TestEvent}:PublishedEvents", "{TestPrefix:TestEvent}:ProcessingEvents")).MustHaveHappened();
        }

        [TestMethod]
        public void RemovesFromProcessingListUponCompletion()
        {
            A.CallTo(() => _redis.ListRemoveAsync("{TestPrefix:TestEvent}:ProcessingEvents", _serializedEvent))
                .MustHaveHappened();
        }
    }
}
