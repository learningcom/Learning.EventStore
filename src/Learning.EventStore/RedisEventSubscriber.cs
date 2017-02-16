using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        public async Task Subscribe<T>(Action<T> callBack)
        {
            //Register subscriber
            var eventType = typeof(T).Name;
            var setKey = $"Subscribers:{eventType}";
            await _redis.SetAddAsync(setKey, _keyPrefix).ConfigureAwait(false);

            //Subscribe to the event
            Action<RedisChannel, RedisValue> redisCallback = async (channel, data) =>
            {
                var listKey = $"{{{_keyPrefix}:{eventType}}}:PublishedEvents";
                var processingListKey = $"{{{_keyPrefix}:{eventType}}}:ProcessingEvents";
                var eventData = await _redis.ListRightPopLeftPushAsync(listKey, processingListKey).ConfigureAwait(false);
                var message = JsonConvert.DeserializeObject<T>(eventData);
                callBack.Invoke(message);
                await _redis.ListRemoveAsync(processingListKey,eventData).ConfigureAwait(false);
            };
            await _redis.SubscribeAsync(eventType, redisCallback).ConfigureAwait(false);
        }
    }
}
