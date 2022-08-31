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
        private readonly ISubscriber _sub;
        private readonly string _applicationName;
        private readonly string _environment;
        private readonly IDistributedLockFactory _distributedLockFactory;
        private readonly DistributedLockSettings _lockSettings;
        private readonly ILog _logger;

        public RedisEventSubscriber(IRedisClient redis, string applicationName, string environment)
            : this(redis, applicationName, environment, null)
        {
        }

        public RedisEventSubscriber(IRedisClient redis, ISubscriber sub, string applicationName, string environment)
            : this(redis, sub, applicationName, environment, null)
        {
        }

        public RedisEventSubscriber(
            IRedisClient redisClient,
            string applicationName, 
            string environment, 
            IDistributedLockFactory distributedLockFactory, 
            DistributedLockSettings lockSettings = null)
            : this(redisClient, null, applicationName, environment, distributedLockFactory, lockSettings)
        {
        }

        public RedisEventSubscriber(
            IRedisClient redisClient,
            ISubscriber sub,
            string applicationName,
            string environment,
            IDistributedLockFactory distributedLockFactory,
            DistributedLockSettings lockSettings = null)
        {
            _redisClient = redisClient;
            _sub = sub;
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
            await SubscribeAsync(callBack, enableLock, false).ConfigureAwait(false);
        }

        public async Task SubscribeAsync<T>(Action<T> callBack, bool enableLock, bool sequentialProcessing) where T : IMessage
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

            //Create concurrent subscription callback
            void ConcurrentRedisCallback(RedisChannel channel, RedisValue data)
            {
                if (enableLock)
                {
                    ExecuteCallbackWithLock(callBack, eventKey);
                }
                else
                {
                    ExecuteCallback(callBack);
                }
            }

            //Create sequential subscription callback
            void SequentialRedisCallback(ChannelMessage message)
            {
                if (enableLock)
                {
                    ExecuteCallbackWithLock(callBack, eventKey);
                }
                else
                {
                    ExecuteCallback(callBack);
                }
            }

            if (sequentialProcessing)
            {
                _redisClient.Subscribe(eventKey, SequentialRedisCallback);
            }
            else
            {
                await _redisClient.SubscribeAsync(eventKey, ConcurrentRedisCallback).ConfigureAwait(false);
            }

            //Grab any unprocessed events and process them
            //Ensures that events that were fired before the application was started will be picked up
            var awaitingEvents = await _redisClient.ListLengthAsync(publishedListKey).ConfigureAwait(false);
            for (var i = 0; i < awaitingEvents; i++)
            {
                try
                {
                    await Task.Run(() => ConcurrentRedisCallback(eventKey, true)).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.ErrorException(e.Message, e);
                }
            }
        }

        private void ExecuteCallbackWithLock<T>(Action<T> callBack, string eventKey) where T : IMessage
        {
            try
            {
                _logger.Debug($"Distributed lock enabled. Attempting to get lock for message with ID {eventKey}...");
                using(var distributedLock = _distributedLockFactory.CreateLock(
                    eventKey,
                    TimeSpan.FromSeconds(_lockSettings.ExpirySeconds),
                    TimeSpan.FromSeconds(_lockSettings.WaitSeconds),
                    TimeSpan.FromMilliseconds(_lockSettings.RetryMilliseconds)))
                {
                    if (distributedLock.IsAcquired)
                    {
                        _logger.Debug($"Distributed lock acquired for message with ID {eventKey}; LockId: {distributedLock.LockId};");
                        ExecuteCallback(callBack);
                    }
                    else
                    {
                        throw new DistributedLockException(distributedLock, _lockSettings);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.ErrorException($"{e.Message}\n{e.StackTrace}", e);
                throw;
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
                _logger.ErrorException($"{e.Message}\n{e.StackTrace}", e);

                throw;
            }
            finally
            {
                //Remove the event from the 'processing' list.
                _redisClient.ListRemove(processingListKey, eventData);
            }
        }

        public async Task SubscribeAsync<T>(Func<T, Task> callBack) where T : IMessage
        {
            await SubscribeAsync(callBack, false).ConfigureAwait(false);
        }

        public async Task SubscribeAsync<T>(Func<T, Task> callBack, bool enableLock) where T : IMessage
        {
            await SubscribeAsync(callBack, enableLock, false).ConfigureAwait(false);
        }

        public async Task SubscribeAsync<T>(Func<T, Task> callBack, bool enableLock, bool sequentialProcessing) where T : IMessage
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

            // Create async Task subscription callback
            async Task TaskRedisCallback()
            {
                if (enableLock)
                {
                    await ExecuteCallbackWithLock(callBack, eventKey).ConfigureAwait(false);
                }
                else
                {
                    await ExecuteCallback(callBack).ConfigureAwait(false);
                }
            }

            // Create async void subscription callback
            async void VoidRedisCallback()
            {
                if (enableLock)
                {
                    await ExecuteCallbackWithLock(callBack, eventKey).ConfigureAwait(false);
                }
                else
                {
                    await ExecuteCallback(callBack).ConfigureAwait(false);
                }
            }

            if (sequentialProcessing)
            {
                if (_sub != null)
                {
                    _sub.Subscribe(eventKey).OnMessage(message => TaskRedisCallback());
                }
                else
                {
                    _redisClient.Subscribe(eventKey, message => VoidRedisCallback());
                }
            }
            else
            {
                if (_sub != null)
                {
                    (await _sub.SubscribeAsync(eventKey).ConfigureAwait(false)).OnMessage(message => TaskRedisCallback());
                }
                else
                {
                    await _redisClient.SubscribeAsync(eventKey, (channel, data) => VoidRedisCallback()).ConfigureAwait(false);
                }
            }

            //Grab any unprocessed events and process them
            //Ensures that events that were fired before the application was started will be picked up
            var awaitingEvents = await _redisClient.ListLengthAsync(publishedListKey).ConfigureAwait(false);
            for (var i = 0; i < awaitingEvents; i++)
            {
                try
                {
                    await Task.Run(async () => await ConcurrentRedisCallback().ConfigureAwait(false)).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.ErrorException(e.Message, e);
                }
            }
        }

        private async Task ExecuteCallbackWithLock<T>(Func<T, Task> callBack, string eventKey) where T : IMessage
        {
            try
            {
                _logger.Debug($"Distributed lock enabled. Attempting to get lock for message with ID {eventKey}...");
                using (var distributedLock = await _distributedLockFactory.CreateLockAsync(
                    eventKey,
                    TimeSpan.FromSeconds(_lockSettings.ExpirySeconds),
                    TimeSpan.FromSeconds(_lockSettings.WaitSeconds),
                    TimeSpan.FromMilliseconds(_lockSettings.RetryMilliseconds))
                    .ConfigureAwait(false))
                {
                    if (distributedLock.IsAcquired)
                    {
                        _logger.Debug($"Distributed lock acquired for message with ID {eventKey}; LockId: {distributedLock.LockId};");
                        await ExecuteCallback(callBack).ConfigureAwait(false);
                    }
                    else
                    {
                        throw new DistributedLockException(distributedLock, _lockSettings);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.ErrorException($"{e.Message}\n{e.StackTrace}", e);
            }
        }

        private async Task ExecuteCallback<T>(Func<T, Task> callBack) where T : IMessage
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
                await callBack.Invoke(message).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _messageQueueRepository.AddToDeadLetterQueue<T>(eventData, message, e);
                _logger.ErrorException($"{e.Message}\n{e.StackTrace}", e);
            }
            finally
            {
                //Remove the event from the 'processing' list.
                await _redisClient.ListRemoveAsync(processingListKey, eventData).ConfigureAwait(false);
            }
        }
    }
}
