using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Learning.EventStore
{
    public class RedisEventPublisher : IEventPublisher
    {
        private readonly IRedisClient _redis;
        private readonly string _keyPrefix;

        public RedisEventPublisher(IRedisClient redis, string keyPrefix)
        {
            _redis = redis;
            _keyPrefix = keyPrefix;
        }

        public async Task Publish<T>(T @event) where T : IEvent
        {
            var eventType = @event.GetType().Name;
            var eventKey = $"{_keyPrefix}:{eventType}";

            //Get all registered subscribers for this event stored in the Redis set at 'subscriberKey'
            var subscriberKey = $"Subscribers:{eventKey}";
            var subscribers = await _redis.SetMembersAsync(subscriberKey).ConfigureAwait(false);

            /*
            Push event data into the queue for each type of subscriber
            Ensures that even if there are multiple instances of a particular subscriber, the event will only be processed once.
            */ 
            foreach (var subscriber in subscribers)
            {
                var publishMessage = JsonConvert.SerializeObject(@event);
                var listKey = $"{{{subscriber}:{eventKey}}}:PublishedEvents";

                await _redis.ListRightPushAsync(listKey, publishMessage).ConfigureAwait(false);
            }

            /*
            Publish event that notifies subscribers that an item was added to the queue
            All instances will receive the notification, but only one will actually process it since the event can only be popped from the queue once
            */
            await _redis.PublishAsync(eventKey, true).ConfigureAwait(false);
        }
    }
}
