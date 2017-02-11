using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Learning.EventStore.Messages;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Learning.EventStore
{
    public class RedisEventPublisher : IEventPublisher
    {
        private readonly Lazy<IConnectionMultiplexer> _redis;
        private IDatabase Database => _redis.Value.GetDatabase();
        private ISubscriber Subscriber => Database.Multiplexer.GetSubscriber();

        public RedisEventPublisher(Lazy<IConnectionMultiplexer> redis)
        {
            _redis = redis;
        }

        public async Task Publish<T>(T @event) where T : IEvent
        {
            var eventType = @event.GetType().Name;

            var subscriberKey = $"Subscribers:{eventType}";
            var subscribers = Database.SetMembers(subscriberKey);

            foreach (var subscriber in subscribers)
            {
                var publishMessage = JsonConvert.SerializeObject(@event);
                var listKey = $"{{{subscriber}:{eventType}}}:PublishedEvents";

                await Database.ListRightPushAsync(listKey, publishMessage);
            }
            await Subscriber.PublishAsync(eventType, true);
        }
    }
}
