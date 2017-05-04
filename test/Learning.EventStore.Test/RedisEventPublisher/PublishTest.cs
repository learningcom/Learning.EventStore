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
        private readonly IRedisClient _redis;
        private readonly string _serializedEvent;

        public PublishTest()
        {
            _redis = A.Fake<IRedisClient>();
            var publisher = new EventStore.RedisEventPublisher(_redis);
            var @event = new TestEvent();
            var subscriberList = new RedisValue[]
            {
                "Subscriber1",
                "Subscriber2"
            };
            _serializedEvent = JsonConvert.SerializeObject(@event);

            A.CallTo(() => _redis.SetMembersAsync("Subscribers:TestEvent")).Returns(Task.Run(() => subscriberList));

            publisher.Publish(@event).Wait();
        }

        [TestMethod]
        public void GetsAllSubscribers()
        {
            A.CallTo(() => _redis.SetMembersAsync("Subscribers:TestEvent")).MustHaveHappened();
        }

        [TestMethod]
        public void AddsMessagesToPublishedEventsListForEachSubscriber()
        {
            A.CallTo(() => _redis.ListRightPushAsync("{Subscriber1:TestEvent}:PublishedEvents", _serializedEvent))
                .MustHaveHappened();
            A.CallTo(() => _redis.ListRightPushAsync("{Subscriber2:TestEvent}:PublishedEvents", _serializedEvent))
                .MustHaveHappened();
        }

        [TestMethod]
        public void PublishesEvents()
        {
            A.CallTo(() => _redis.PublishAsync("TestEvent", true)).MustHaveHappened();
        }

    }
}
