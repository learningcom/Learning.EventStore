using System;
using System.Threading.Tasks;
using FakeItEasy;
using Learning.EventStore.Test.Mocks;
using Newtonsoft.Json;
using NUnit.Framework;
using StackExchange.Redis;

namespace Learning.EventStore.Test.RedisEventPublisher
{
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

        [Test]
        public void GetsAllSubscribers()
        {
            A.CallTo(() => _redis.SetMembersAsync("Subscribers:TestEvent")).MustHaveHappened();
        }

        [Test]
        public void AddsMessagesToPublishedEventsListForEachSubscriber()
        {
            A.CallTo(() => _redis.ListRightPushAsync("{Subscriber1:TestEvent}:PublishedEvents", _serializedEvent))
                .MustHaveHappened();
            A.CallTo(() => _redis.ListRightPushAsync("{Subscriber2:TestEvent}:PublishedEvents", _serializedEvent))
                .MustHaveHappened();
        }

        [Test]
        public void PublishesEvents()
        {
            A.CallTo(() => _redis.PublishAsync("TestEvent", true)).MustHaveHappened();
        }

    }
}
