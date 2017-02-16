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
        private readonly IRedisClient _redis;

        public RedisEventPublisher(IRedisClient redis)
        {
            _redis = redis;
        }

        public async Task Publish<T>(T @event) where T : IEvent
        {
            var eventType = @event.GetType().Name;

            var subscriberKey = $"Subscribers:{eventType}";
            var subscribers = await _redis.SetMembersAsync(subscriberKey).ConfigureAwait(false);

            foreach (var subscriber in subscribers)
            {
                var publishMessage = JsonConvert.SerializeObject(@event);
                var listKey = $"{{{subscriber}:{eventType}}}:PublishedEvents";

                await _redis.ListRightPushAsync(listKey, publishMessage).ConfigureAwait(false);
            }
            await _redis.PublishAsync(eventType, true).ConfigureAwait(false);
        }
    }
}
