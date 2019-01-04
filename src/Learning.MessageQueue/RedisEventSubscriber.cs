using System;
using System.Threading.Tasks;
using System.Xml;
using Learning.EventStore.Common;
using Learning.EventStore.Common.Redis;
using Learning.MessageQueue.Messages;
using Learning.MessageQueue.Repository;
#if !NET46 && !NET452
using Microsoft.Extensions.Logging;
#endif
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Learning.MessageQueue
{
    public class RedisEventSubscriber : IEventSubscriber
    {
        private readonly IMessageQueueRepository _messageQueueRepository;
        private readonly IRedisClient _redisClient;
        private readonly string _applicationName;
        private readonly string _environment;

#if !NET46 && !NET452
        private readonly ILogger _logger;

        public RedisEventSubscriber(IRedisClient redisClient, string applicationName, string environment, ILoggerFactory loggerFactory)
        {
            _redisClient = redisClient;
            _messageQueueRepository = new MessageQueueRepository(_redisClient, environment, applicationName);
            _applicationName = applicationName;
            _environment = environment;
            _logger = loggerFactory.CreateLogger(GetType().Name);
        }
#endif

        public RedisEventSubscriber(IRedisClient redis, string applicationName, string environment)
        {
            _redisClient = redis;
            _messageQueueRepository = new MessageQueueRepository(_redisClient, environment, applicationName);
            _applicationName = applicationName;
            _environment = environment;
        }

        public async Task SubscribeAsync<T>(Action<T> callBack) where T : IMessage
        {
            //Register subscriber
            var eventType = typeof(T).Name;
            var eventKey = $"{_environment}:{eventType}";
            var subscriberSetKey = $"Subscribers:{{{eventKey}}}";
            var publishedListKey = $"{_applicationName}:{{{eventKey}}}:PublishedEvents";
            await _redisClient.SetAddAsync(subscriberSetKey, _applicationName).ConfigureAwait(false);

            //Create subscription callback
            void RedisCallback(RedisChannel channel, RedisValue data)
            {
                try
                {
                    var processingListKey = $"{_applicationName}:{{{eventKey}}}:ProcessingEvents";

                    /*
                    Pop the event out of the queue and atomicaly push it into another 'processing' list.
                    Creates a reliable queue where events can be retried if processing fails, see https://redis.io/commands/rpoplpush.
                    */
                    var eventData = _redisClient.ListRightPopLeftPush(publishedListKey, processingListKey);

                    // if the eventData is null, then the event has already been processed by another instance, skip further execution
                    if (!eventData.HasValue)
                    {
                        return;
                    }

                    //Deserialize the event data and invoke the handler
                    var message = JsonConvert.DeserializeObject<T>(eventData);
                    try
                    {
                        
                        callBack.Invoke(message);
                    }
                    catch (Exception e)
                    {
                        _messageQueueRepository.AddToDeadLetterQueue<T>(eventData, message, e);

                        throw;
                    }
                    finally
                    {
                        //Remove the event from the 'processing' list.
                        _redisClient.ListRemove(processingListKey, eventData);
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

            //Subscribe to the event
            await _redisClient.SubscribeAsync(eventKey, RedisCallback).ConfigureAwait(false);

            //Grab any unprocessed events and process them
            //Ensures that events that were fired before the application was started will be picked up
            var awaitingEvents = await _redisClient.ListLengthAsync(publishedListKey).ConfigureAwait(false);
            for (var i = 0; i < awaitingEvents; i++)
            {
                try
                {
                    await Task.Run(() => RedisCallback(eventKey, true)).ConfigureAwait(false);
                }
                catch (Exception e)
                {
#if !NET46 && !NET452
                    _logger.LogError(e, e.Message);
#endif
                }
            }
        }
    }
}
