using System;
using FakeItEasy;
using Learning.EventStore.Common.Redis;
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
        private TestEvent _callbackData;
        private readonly string _serializedEvent;

        public SubscribeTest()
        {
            _redis = A.Fake<IRedisClient>();
            var subscriber = new MessageQueue.RedisEventSubscriber(_redis, "TestPrefix", "Test");
            var testEvent = new TestEvent();
            _serializedEvent = JsonConvert.SerializeObject(testEvent);

            A.CallTo(() => _redis.ListLengthAsync("TestPrefix:{Test:TestEvent}:PublishedEvents")).Returns(2);
            A.CallTo(() => _redis.ListRangeAsync("TestPrefix:{Test:TestEvent}:PublishedEvents", 0, 2)).Returns(new RedisValue[] { "Test1", "Test2" });
            A.CallTo(() => _redis.ListRightPopLeftPush("TestPrefix:{Test:TestEvent}:PublishedEvents", "TestPrefix:{Test:TestEvent}:ProcessingEvents")).Returns(_serializedEvent);
            A.CallTo(() => _redis.SubscribeAsync("Test:TestEvent", A<Action<RedisChannel, RedisValue>>._))
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
            A.CallTo(() => _redis.SetAddAsync("Subscribers:{Test:TestEvent}", "TestPrefix")).MustHaveHappened();
        }

        [TestMethod]
        public void ExecutesCallbackWithCorrectData()
        {
            Assert.AreEqual(JsonConvert.SerializeObject(_callbackData), _serializedEvent);
        }

        [TestMethod]
        public void RemovesFromProcessingListUponCompletion()
        {
            A.CallTo(() => _redis.ListRemove("TestPrefix:{Test:TestEvent}:ProcessingEvents", _serializedEvent))
                .MustHaveHappened();
        }

        //[TestMethod]
        //public void ProcessesAllEventsInPublishedList()
        //{
        //    A.CallTo(() => _redis.ListRightPopLeftPush("TestPrefix:{Test:TestEvent}:PublishedEvents", "TestPrefix:{Test:TestEvent}:ProcessingEvents")).MustHaveHappened(Repeated.Exactly.Times(3));
        //}
    }
}
