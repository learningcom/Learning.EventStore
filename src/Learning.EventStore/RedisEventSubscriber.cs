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
        private readonly Lazy<IConnectionMultiplexer> _redis;
        private readonly string _keyPrefix;

        private IDatabase Database => _redis.Value.GetDatabase();
        private ISubscriber Subscriber => Database.Multiplexer.GetSubscriber();

        public RedisEventSubscriber(Lazy<IConnectionMultiplexer> redis, string keyPrefix)
        {
            _redis = redis;
            _keyPrefix = keyPrefix;
        }

        public async Task Subscribe<T>(Action<T> callBack)
        {
            //Register subscriber
            var eventType = typeof(T).Name;
            var setKey = $"Subscribers:{eventType}";
            await Database.SetAddAsync(setKey, _keyPrefix);

            //Subscribe to the event
            Action<RedisChannel, RedisValue> redisCallback = async (channel, data) =>
            {
                var listKey = $"{{{_keyPrefix}:{eventType}}}:PublishedEvents";
                var processingListKey = $"{{{_keyPrefix}:{eventType}}}:ProcessingEvents";
                var eventData = await Database.ListRightPopLeftPushAsync(listKey, processingListKey);
                var message = JsonConvert.DeserializeObject<T>(eventData);
                callBack.Invoke(message);
                await Database.ListRemoveAsync(processingListKey,eventData);
            };
            await Subscriber.SubscribeAsync(eventType, redisCallback);
        }
    }
}
