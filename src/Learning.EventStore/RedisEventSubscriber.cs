using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Learning.EventStore
{
    public class RedisEventSubscriber : IEventSubscriber
    {
        private readonly IRedisClient _redis;
        private readonly string _keyPrefix;

        public RedisEventSubscriber(IRedisClient redis, string keyPrefix)
        {
            _redis = redis;
            _keyPrefix = keyPrefix;
        }

        public async Task SubscribeAsync<T>(Action<T> callBack)
        {
            //Register subscriber
            var eventType = typeof(T).Name;
            var eventKey = $"{_keyPrefix}:{eventType}";
            var setKey = $"Subscribers:{eventKey}";
            await _redis.SetAddAsync(setKey, _keyPrefix).ConfigureAwait(false);

            //Create subscription callback
            async void RedisCallback(RedisChannel channel, RedisValue data)
            {
                var listKey = $"{{{eventKey}}}:PublishedEvents";
                var processingListKey = $"{{{eventKey}}}:ProcessingEvents";

                /*
                Pop the event out of the queue and atomicaly push it into another 'processing' list.
                Creates a reliable queue where events can be retried if processing fails, see https://redis.io/commands/rpoplpush.
                */
                var eventData = await _redis.ListRightPopLeftPushAsync(listKey, processingListKey).ConfigureAwait(false);

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

            //Subscribe to the event
            await _redis.SubscribeAsync(eventKey, RedisCallback).ConfigureAwait(false);
        }
    }
}
