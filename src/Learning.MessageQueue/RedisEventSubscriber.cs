using System;
using System.Threading.Tasks;
using Learning.EventStore.Common;
#if !NET46 && !NET452
using Microsoft.Extensions.Logging;
#endif
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Learning.MessageQueue
{
    public class RedisEventSubscriber : IEventSubscriber
    {
        private readonly IRedisClient _redis;
        private readonly string _keyPrefix;
        private readonly string _environment;

#if !NET46 && !NET452
        private readonly ILogger _logger;

        public RedisEventSubscriber(IRedisClient redis, string keyPrefix, string environment, ILoggerFactory loggerFactory)
        {
            _redis = redis;
            _keyPrefix = keyPrefix;
            _environment = environment;
            _logger = loggerFactory.CreateLogger(GetType().Name);
        }
#endif

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
            void RedisCallback(RedisChannel channel, RedisValue data)
            {
                try
                {
                    var processingListKey = $"{_keyPrefix}:{{{eventKey}}}:ProcessingEvents";

                    /*
                    Pop the event out of the queue and atomicaly push it into another 'processing' list.
                    Creates a reliable queue where events can be retried if processing fails, see https://redis.io/commands/rpoplpush.
                    */
                    var eventData = _redis.ListRightPopLeftPush(publishedListKey, processingListKey);

                    // if the eventData is null, then the event has already been processed by another instance, skip further execution
                    if (eventData.HasValue)
                    {
                        try
                        {
                            //Deserialize the event data and invoke the handler
                            var message = JsonConvert.DeserializeObject<T>(eventData);
                            callBack.Invoke(message);
                        }
                        catch(Exception)
                        {
                            var deadLetterListKey = $"{_keyPrefix}:{{{eventKey}}}:DeadLetters";
                            _redis.ListRightPopLeftPush(processingListKey, deadLetterListKey);

                            throw;
                        }

                        //Remove the event from the 'processing' list.
                        _redis.ListRemove(processingListKey, eventData);
                    }
                }
                catch (Exception e)
                {
#if !NET46 && !NET452
                    _logger?.LogError($"{e.Message}\n{e.StackTrace}", e);
#endif
                    throw;
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
