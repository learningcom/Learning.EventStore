using System;
using FakeItEasy;
using Learning.EventStore.Common;
using Learning.EventStore.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StackExchange.Redis;

namespace Learning.EventStore.Test.RedisEventSubscriber
{
    [TestClass]
    public class NoValueTest
    {
        [TestMethod]
        public void DoesNotThrowAndDoesNotCallCallBackIfEventIsAlreadyProcessed()
        {
            var redis = A.Fake<IRedisClient>();
            var subscriber = new MessageQueue.RedisEventSubscriber(redis, "TestPrefix", "Test");
            TestEvent callbackData = null;

            A.CallTo(() => redis.ListRightPopLeftPush("TestPrefix:{Test:TestEvent}:PublishedEvents", "TestPrefix:{Test:TestEvent}:ProcessingEvents")).Returns(RedisValue.Null);
            A.CallTo(() => redis.SubscribeAsync("Test:TestEvent", A<Action<RedisChannel, RedisValue>>._))
                .Invokes(callObject =>
                {
                    var action = callObject.Arguments[1] as Action<RedisChannel, RedisValue>;
                    action(callObject.Arguments[0].ToString(), RedisValue.Null);
                });

            Action<TestEvent> cb = (data) =>
            {
                callbackData = null;
            };

            subscriber.SubscribeAsync(cb).Wait();

            A.CallTo(() => redis.ListRightPopLeftPush("TestPrefix:{Test:TestEvent}:PublishedEvents", "TestPrefix:{Test:TestEvent}:ProcessingEvents")).MustHaveHappened();
            Assert.IsNull(callbackData);
        }
    }
}
