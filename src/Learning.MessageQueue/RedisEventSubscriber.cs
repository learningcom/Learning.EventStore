using System;
using System.Threading.Tasks;
using Learning.EventStore.Common;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Learning.MessageQueue
{
    public class RedisEventSubscriber : IEventSubscriber
    {
        private readonly IRedisClient _redis;
        private readonly string _keyPrefix;
        private readonly string _environment;

        public RedisEventSubscriber(IRedisClient redis, string keyPrefix, string environment)
        {
            _redis = redis;
            _keyPrefix = keyPrefix;
            _environment = environment;
        }

        public async Task SubscribeAsync<T>(Action<T> callBack)
        {
            //Register subscriber
            var eventType = typeof(T).Name;
            var eventKey = $"{_environment}:{eventType}";
            var subscriberSetKey = $"Subscribers:{{{eventKey}}}";
            var publishedListKey = $"{_keyPrefix}:{{{eventKey}}}:PublishedEvents";
            await _redis.SetAddAsync(subscriberSetKey, _keyPrefix).ConfigureAwait(false);

            //Create subscription callback
            async void RedisCallback(RedisChannel channel, RedisValue data)
            {
                var processingListKey = $"{_keyPrefix}:{{{eventKey}}}:ProcessingEvents";

                /*
                Pop the event out of the queue and atomicaly push it into another 'processing' list.
                Creates a reliable queue where events can be retried if processing fails, see https://redis.io/commands/rpoplpush.
                */
                var eventData = await _redis.ListRightPopLeftPushAsync(publishedListKey, processingListKey)
                    .ConfigureAwait(false);

                // if the eventData is null, then the event has already been processed by another instance, skip further execution
                if (eventData.HasValue)
                {
                    //Deserialize the event data and invoke the handler
                    var message = JsonConvert.DeserializeObject<T>(eventData);
                    callBack.Invoke(message);

                    //Remove the event from the 'processing' list.
                    await _redis.ListRemoveAsync(processingListKey, eventData).ConfigureAwait(false);
                }
            }

            //Grab any unprocessed events and process them
            //Ensures that events that were fired before the application was started will be picked up 
            var awaitingEvents = await _redis.ListLengthAsync(publishedListKey);
            if (awaitingEvents > 0)
            {
                var events = await _redis.ListRangeAsync(publishedListKey, 0, awaitingEvents);
                foreach (var @event in events)
                {
                    RedisCallback(eventKey, @event);
                }
            }

            //Subscribe to the event
            await _redis.SubscribeAsync(eventKey, RedisCallback).ConfigureAwait(false);
        }
    }
}
