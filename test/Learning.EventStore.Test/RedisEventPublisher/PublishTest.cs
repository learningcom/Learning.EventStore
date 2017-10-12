using System.Threading.Tasks;
using FakeItEasy;
using Learning.EventStore.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Learning.EventStore.Test.RedisEventPublisher
{
    [TestClass]
    public class PublishTest
    {
        //private readonly IRedisClient _redis;
        //private readonly string _serializedEvent;

        //public PublishTest()
        //{
        //    _redis = A.Fake<IRedisClient>();
        //    var publisher = new EventStore.RedisEventPublisher(_redis, "TestPrefix");
        //    var @event = new TestEvent();
        //    var subscriberList = new RedisValue[]
        //    {
        //        "Subscriber1",
        //        "Subscriber2"
        //    };
        //    _serializedEvent = JsonConvert.SerializeObject(@event);

        //    A.CallTo(() => _redis.SetMembersAsync("Subscribers:TestPrefix:TestEvent")).Returns(Task.Run(() => subscriberList));

        //    publisher.Publish(@event).Wait();
        //}

        //[TestMethod]
        //public void GetsAllSubscribers()
        //{
        //    A.CallTo(() => _redis.SetMembersAsync("Subscribers:TestPrefix:TestEvent")).MustHaveHappened();
        //}

        //[TestMethod]
        //public void AddsMessagesToPublishedEventsListForEachSubscriber()
        //{
        //    A.CallTo(() => _redis.ListRightPushAsync("{Subscriber1:TestPrefix:TestEvent}:PublishedEvents", _serializedEvent))
        //        .MustHaveHappened();
        //    A.CallTo(() => _redis.ListRightPushAsync("{Subscriber2:TestPrefix:TestEvent}:PublishedEvents", _serializedEvent))
        //        .MustHaveHappened();
        //}

        //[TestMethod]
        //public void PublishesEvents()
        //{
        //    A.CallTo(() => _redis.PublishAsync("TestPrefix:TestEvent", true)).MustHaveHappened();
        //}

    }
}
