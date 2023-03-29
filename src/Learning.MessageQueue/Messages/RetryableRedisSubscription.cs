using System;
using System.Threading.Tasks;
using Learning.MessageQueue.Logging;
using Learning.MessageQueue.Repository;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Learning.MessageQueue.Messages
{
    public abstract class RetryableRedisSubscription<T> : RedisSubscription<T>, IRetryableSubscription where T: IMessage
    {
        private readonly IMessageQueueRepository _messageQueueRepository;
        private readonly ILog _logger;

        protected RetryableRedisSubscription(IEventSubscriber subscriber, IMessageQueueRepository messageQueueRepository)
            : this(subscriber, messageQueueRepository, false, false)
        {
        }

        protected RetryableRedisSubscription(IEventSubscriber subscriber, IMessageQueueRepository messageQueueRepository, bool useLock)
            : this(subscriber, messageQueueRepository, useLock, false)
        {
        }

        protected RetryableRedisSubscription(IEventSubscriber subscriber, IMessageQueueRepository messageQueueRepository, bool useLock, bool sequentialProcessing)
            : base(subscriber, useLock, sequentialProcessing)
        {
            _logger = LogProvider.GetCurrentClassLogger();
            _messageQueueRepository = messageQueueRepository;
        }
        
        public virtual int TimeToLiveHours { get; set; } = 168;

        public virtual int RetryIntervalMinutes { get; set; } = 5;

        public virtual int RetryIntervalMaxMinutes { get; set; } = 60;

        public virtual async Task RetryAsync()
        {
            await MoveStaleProcessingEventsToDeadLetters();

            var eventType = typeof(T).Name;
            var listLength = await _messageQueueRepository.GetDeadLetterListLength<T>().ConfigureAwait(false);
            var eventsProcessed = 0;
            var errors = 0;

            if (listLength > 0)
            {
                _logger.Info($"Beginning retry of {listLength} {typeof(T).Name} events");
            }

            for (var i = 0; i < listLength; i++)
            {
                var indexToGet = i - eventsProcessed;
                var eventData = await _messageQueueRepository.GetUnprocessedMessage<T>(indexToGet).ConfigureAwait(false);

                if (string.IsNullOrEmpty(eventData))
                {
                    continue;
                }

                var @event = JsonConvert.DeserializeObject<T>(eventData);

                try
                {
                    if (await ShouldRetry(@event, eventData).ConfigureAwait(false))
                    {
                        _logger.Info($"Beginning retry of processing for {eventType} event for Aggregate: {@event.Id}");

                        await RetryCallBackAsync(@event).ConfigureAwait(false);
                        await _messageQueueRepository.DeleteFromDeadLetterQueue<T>(eventData, @event).ConfigureAwait(false);
                        eventsProcessed++;

                        _logger.Info($"Completed retry of processing for {eventType} event for Aggregate: {@event.Id}");
                    }
                }
                catch (Exception e)
                {
                    var message = $"{e.Message}{Environment.NewLine}{e.StackTrace}";
                    await _messageQueueRepository.UpdateRetryData(@event, message).ConfigureAwait(false);
                    _logger.WarnException($"Event processing retry failed for {eventType} with message: {e.Message}{Environment.NewLine}{e.StackTrace}", e);

                    errors++;
                }
            }

            _logger.Info($"Retry complete for {eventType}. Processed {eventsProcessed} events with {errors} errors.");
        }

        protected virtual async Task RetryCallBackAsync(T message)
        {
            await Task.Run(() =>
            {
                RetryCallBack(message);
            }).ConfigureAwait(false);
        }

        protected virtual void RetryCallBack(T message)
        {
            CallBack(message);
        }

        protected virtual async Task<bool> ShouldRetry(IMessage @event, RedisValue eventData)
        {
            var retryData = await _messageQueueRepository.GetRetryData(@event).ConfigureAwait(false);

            // exponential backoff
            var mpow = Math.Pow(2, retryData.RetryCount);
            var interval = Math.Min(mpow * RetryIntervalMinutes, RetryIntervalMaxMinutes);
            var intervalPassed = DateTimeOffset.UtcNow > retryData.LastRetryTime?.ToUniversalTime().AddMinutes(interval);

            if (!intervalPassed && retryData.RetryCount > 0)
            {
                _logger.Debug($"Skipping retry for event with Aggregate Id {@event.Id}; Retry interval has not elapsed.");
                return false;
            }

            if (TimeToLiveHours != default(int) &&
                DateTimeOffset.UtcNow > @event.TimeStamp.ToUniversalTime().AddHours(TimeToLiveHours))
            {
                _logger.Debug($"Time to live of {TimeToLiveHours} hours exceeded for event with Aggregate Id {@event.Id}; Deleting from the dead letter queue.");
                await _messageQueueRepository.DeleteFromDeadLetterQueue<T>(eventData, @event).ConfigureAwait(false);
                return false;
            }

            return true;
        }

        private async Task MoveStaleProcessingEventsToDeadLetters()
        {
            const int eventCount = 10;
            var waitTime = TimeSpan.FromMinutes(5);
            var staleTimeStamp = DateTimeOffset.UtcNow - waitTime;

            var unprocessedEvents = await _messageQueueRepository.GetOldestProcessingEvents<T>(eventCount);

            for (var i = 0; i < unprocessedEvents.Length; i++)
            {
                var unprocessedEvent = unprocessedEvents[i];
                var @event = JsonConvert.DeserializeObject<T>(unprocessedEvent);

                if (@event.TimeStamp < staleTimeStamp)
                {
                    _logger.Info($"Moving {typeof(T).Name} {@event.Id} with timestamp @{@event.TimeStamp} to dead letter queue.");

                    await _messageQueueRepository.MoveProcessingEventToDeadLetterQueue<T>(unprocessedEvent, @event);
                }
            }
        }
    }
}
