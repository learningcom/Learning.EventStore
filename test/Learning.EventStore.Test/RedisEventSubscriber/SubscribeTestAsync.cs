using System;
using System.Threading.Tasks;
using FakeItEasy;
using Learning.EventStore.Common.Redis;
using Learning.EventStore.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Learning.EventStore.Test.RedisEventSubscriber
{
    [TestClass]
    public class SubscribeTestAsync
    {
        private IRedisClient _redis;
        private MessageQueue.RedisEventSubscriber _subscriber;
        private TestEvent _callbackData;
        private int _callBackExecutedCount = 0;
        private string _serializedEvent;

        [TestInitialize]
        public async Task Setup()
        {
            _redis = A.Fake<IRedisClient>();
            _subscriber = new MessageQueue.RedisEventSubscriber(_redis, "TestPrefix", "Test");
            var testEvent = new TestEvent();
            _serializedEvent = JsonConvert.SerializeObject(testEvent);

            A.CallTo(() => _redis.ListLengthAsync("TestPrefix:{Test:TestEvent}:PublishedEvents")).Returns(2);
            A.CallTo(() => _redis.ListRangeAsync("TestPrefix:{Test:TestEvent}:PublishedEvents", 0, 2)).Returns(new RedisValue[] { "Test1", "Test2" });
            A.CallTo(() => _redis.ListRightPopLeftPush("TestPrefix:{Test:TestEvent}:PublishedEvents", "TestPrefix:{Test:TestEvent}:ProcessingEvents")).Returns(_serializedEvent);
            A.CallTo(() => _redis.SubscribeAsync("Test:TestEvent", A<Action<RedisChannel, RedisValue>>._))
                .Invokes(callObject =>
                {
                    var action = callObject.Arguments[1] as Action<RedisChannel, RedisValue>;
                    action(callObject.Arguments[0].ToString(), _serializedEvent);
                });

            Func<TestEvent, Task> cb = async (data) =>
            {
                await Task.Delay(20);

                _callbackData = data;
                _callBackExecutedCount++;
            };

            await _subscriber.SubscribeAsync(cb);
        }

        [TestMethod]
        public void RegistersAsSubscriber()
        {
            A.CallTo(() => _redis.SetAddAsync("Subscribers:{Test:TestEvent}", "TestPrefix")).MustHaveHappened(Repeated.Exactly.Once);
        }

        [TestMethod]
        public void ExecutesCallbackWithCorrectData()
        {
            Assert.AreEqual(JsonConvert.SerializeObject(_callbackData), _serializedEvent);
        }

        [TestMethod]
        public void RemovesFromProcessingListUponCompletion()
        {
            A.CallTo(() => _redis.ListRemoveAsync("TestPrefix:{Test:TestEvent}:ProcessingEvents", _serializedEvent))
                .MustHaveHappened(Repeated.Exactly.Times(3));
        }

        [TestMethod]
        public void ProcessesExistingItemsInPublishedQueue()
        {
            Assert.AreEqual(3, _callBackExecutedCount);
            A.CallTo(() => _redis.ListRightPopLeftPush("TestPrefix:{Test:TestEvent}:PublishedEvents", "TestPrefix:{Test:TestEvent}:ProcessingEvents"))
                .MustHaveHappened(Repeated.Exactly.Times(3));
        }
    }
}
