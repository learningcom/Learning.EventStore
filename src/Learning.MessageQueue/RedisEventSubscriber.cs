using System;
using System.Threading.Tasks;
using Learning.EventStore.Common;
using Learning.EventStore.Common.Exceptions;
using Learning.EventStore.Common.Redis;
using Learning.MessageQueue.Logging;
using Learning.MessageQueue.Messages;
using Learning.MessageQueue.Repository;
using Newtonsoft.Json;
using RedLockNet;
using StackExchange.Redis;

namespace Learning.MessageQueue
{
    public class RedisEventSubscriber : IEventSubscriber
    {
        private readonly IMessageQueueRepository _messageQueueRepository;
        private readonly IRedisClient _redisClient;
        private readonly string _applicationName;
        private readonly string _environment;
        private readonly IDistributedLockFactory _distributedLockFactory;
        private readonly DistributedLockSettings _lockSettings;
        private readonly ILog _logger;

        public RedisEventSubscriber(IRedisClient redis, string applicationName, string environment)
            : this(redis, applicationName, environment, null)
        {
        }

        public RedisEventSubscriber(
            IRedisClient redisClient,
            string applicationName, 
            string environment, 
            IDistributedLockFactory distributedLockFactory, 
            DistributedLockSettings lockSettings = null)
        {
            _redisClient = redisClient;
            _messageQueueRepository = new MessageQueueRepository(_redisClient, environment, applicationName);
            _applicationName = applicationName;
            _environment = environment;
            _distributedLockFactory = distributedLockFactory;
            _logger = LogProvider.GetCurrentClassLogger();
            _lockSettings = lockSettings ?? new DistributedLockSettings();
        }

        public async Task SubscribeAsync<T>(Action<T> callBack) where T : IMessage
        {
            await SubscribeAsync(callBack, false).ConfigureAwait(false);
        }

        public async Task SubscribeAsync<T>(Action<T> callBack, bool enableLock) where T : IMessage
        {
            if (enableLock && _distributedLockFactory == null)
            {
                throw new ArgumentNullException(nameof(_distributedLockFactory), "IDistributedLockFactory must be set in constructor if lock is enabled");
            }

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
                    if (enableLock)
                    {
                        using(var distributedLock = _distributedLockFactory.CreateLock(
                            eventKey,
                            TimeSpan.FromSeconds(_lockSettings.ExpirySeconds),
                            TimeSpan.FromSeconds(_lockSettings.WaitSeconds),
                            TimeSpan.FromMilliseconds(_lockSettings.RetryMilliseconds)))
                        {
                            if (distributedLock.IsAcquired)
                            {
                                ExecuteCallback(callBack);
                            }
                            else
                            {
                                throw new DistributedLockException(distributedLock, _lockSettings);
                            }
                        }
                    }
                    else
                    {
                        ExecuteCallback(callBack);
                    }
                }
                catch (Exception e)
                {
                    _logger.ErrorException($"{e.Message}\n{e.StackTrace}", e);
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
                    _logger.ErrorException(e.Message, e);
                }
            }
        }

        private void ExecuteCallback<T>(Action<T> callBack) where T : IMessage
        {
            var eventType = typeof(T).Name;
            var eventKey = $"{_environment}:{eventType}";
            var publishedListKey = $"{_applicationName}:{{{eventKey}}}:PublishedEvents";
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
    }
}
