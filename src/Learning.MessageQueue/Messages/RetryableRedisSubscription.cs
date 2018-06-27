using System;
using System.Threading.Tasks;
using Learning.MessageQueue.Repository;
#if !NET46 && !NET452
using Microsoft.Extensions.Logging;
#endif
using Newtonsoft.Json;

namespace Learning.MessageQueue.Messages
{
    public abstract class RetryableRedisSubscription<T> : RedisSubscription<T>, IRetryableSubscription where T: IMessage
    {
        private readonly IMessageQueueRepository _messageQueueRepository;

#if !NET46 && !NET452
        private readonly ILogger _logger;

        protected RetryableRedisSubscription(IEventSubscriber subscriber, ILogger logger, IMessageQueueRepository messageQueueRepository)
            : base(subscriber, logger)
        {
            _logger = logger;
            _messageQueueRepository = messageQueueRepository;
        }
#endif

        protected RetryableRedisSubscription(IEventSubscriber subscriber, IMessageQueueRepository messageQueueRepository)
            :base(subscriber)
        {
            _messageQueueRepository = messageQueueRepository;
        }

        public virtual int RetryForHours { get; set; } = 0;

        public virtual int RetryLimit { get; set; } = 5;

        public virtual int RetryIntervalMinutes { get; set; } = 5;

        public virtual int RetryIntervalMaxMinutes { get; set; } = 60;

        public virtual async Task RetryAsync()
        {
            var eventType = typeof(T).Name;
            var listLength = await _messageQueueRepository.GetDeadLetterListLength<T>().ConfigureAwait(false);
            var eventsProcessed = 0;
            var errors = 0;

            if (listLength > 0)
            {
                LogInformation($"Beginning retry of {listLength} {typeof(T).Name} events");
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
                    if (await ShouldRetry(@event).ConfigureAwait(false))
                    {
                        LogInformation($"Beginning retry of processing for {eventType} event for Aggregate: {@event.Id}");

                        RetryCallBack(@event);
                        await _messageQueueRepository.DeleteFromDeadLetterQueue(eventData, @event).ConfigureAwait(false);
                        eventsProcessed++;

                        LogInformation($"Completed retry of processing for {eventType} event for Aggregate: {@event.Id}");
                    }
                }
                catch (Exception e)
                {
                    var message = $"{e.Message}{Environment.NewLine}{e.StackTrace}";
                    await _messageQueueRepository.UpdateRetryData(@event, message).ConfigureAwait(false);
                    LogWarning($"Event processing retry failed for {eventType} with message: {e.Message}{Environment.NewLine}{e.StackTrace}");

                    errors++;
                }
            }

            LogInformation($"Retry complete for {eventType}. Processed {eventsProcessed} events with {errors} errors.");
        }

        protected virtual void RetryCallBack(T message)
        {
            CallBack(message);
        }

        protected virtual async Task<bool> ShouldRetry(IMessage @event)
        {
            var retryData = await _messageQueueRepository.GetRetryData(@event).ConfigureAwait(false);

            // exponential backoff
            var mpow = (int)Math.Pow(2, retryData.RetryCount);
            var interval = Math.Min(mpow * RetryIntervalMinutes, RetryIntervalMaxMinutes);
            var intervalPassed = DateTimeOffset.UtcNow > retryData.LastRetryTime?.ToUniversalTime().AddMinutes(interval);

            if (!intervalPassed)
            {
                LogInformation($"Skipping retry for event with Aggregate Id {@event.Id}; Retry interval has not elapsed.");
                return false;
            }

            if (RetryForHours != default(int) &&
                DateTimeOffset.UtcNow < @event.TimeStamp.ToUniversalTime().AddHours(RetryForHours))
            {
                return true;
            }

            if (retryData.RetryCount > RetryLimit)
            {
                LogInformation($"Skipping retry for event with Aggregate Id {@event.Id}; Retry threshold reached");
                return false;
            }

            return true;
        }
    }
}
