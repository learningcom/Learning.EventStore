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
    public class CallbackErrorTest
    {
        [TestMethod]
        public void MovesToDeadLetterQueue()
        {
            var redis = A.Fake<IRedisClient>();
            var subscriber = new MessageQueue.RedisEventSubscriber(redis, "TestPrefix", "Test");
            var eventData = new TestEvent();
            var serializedEvent = JsonConvert.SerializeObject(eventData);
            var transaction = A.Fake<ITransaction>();
            TestEvent callbackData = null;

            A.CallTo(() => redis.ListRightPopLeftPush("TestPrefix:{Test:TestEvent}:PublishedEvents", "TestPrefix:{Test:TestEvent}:ProcessingEvents")).Returns(serializedEvent);
            A.CallTo(() => redis.SubscribeAsync("Test:TestEvent", A<Action<RedisChannel, RedisValue>>._))
                .Invokes(callObject =>
                {
                    var action = callObject.Arguments[1] as Action<RedisChannel, RedisValue>;
                    action(callObject.Arguments[0].ToString(), serializedEvent);
                });
            A.CallTo(() => redis.CreateTransaction()).Returns(transaction);
            A.CallTo(() => redis.ExecuteTransactionAsync(transaction)).Returns(true);
            

            Action<TestEvent> cb = (data) =>
            {
                callbackData = data;
                throw new Exception("Oh No!");
            };

            try
            {
                subscriber.SubscribeAsync(cb).Wait();
                Assert.Fail("Should have thrown exception");
            }
            catch(Exception e)
            {
                Assert.AreEqual("Oh No!", e.InnerException.Message);
                A.CallTo(() => redis.ListRightPopLeftPush("TestPrefix:{Test:TestEvent}:PublishedEvents", "TestPrefix:{Test:TestEvent}:ProcessingEvents")).MustHaveHappened();
                A.CallTo(() => transaction.ListLeftPushAsync("TestPrefix:{Test:TestEvent}:DeadLetters", serializedEvent, When.Always, CommandFlags.None)).MustHaveHappened();
                A.CallTo(() => redis.ListRemove("TestPrefix:{Test:TestEvent}:ProcessingEvents", serializedEvent)).MustHaveHappened();
            }
        }
    }
}
