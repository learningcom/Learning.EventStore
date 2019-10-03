﻿using System;
using System.Threading.Tasks;
using Learning.MessageQueue.Repository;
#if !NET46 && !NET452
using Microsoft.Extensions.Logging;
#endif
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Learning.MessageQueue.Messages
{
    public abstract class RetryableRedisSubscription<T> : RedisSubscription<T>, IRetryableSubscription where T: IMessage
    {
        private readonly IMessageQueueRepository _messageQueueRepository;

#if !NET46 && !NET452
        private readonly ILogger _logger;

        protected RetryableRedisSubscription(IEventSubscriber subscriber, ILogger logger, IMessageQueueRepository messageQueueRepository)
            : this(subscriber, logger, messageQueueRepository, false)
        {
        }

        protected RetryableRedisSubscription(IEventSubscriber subscriber, ILogger logger, IMessageQueueRepository messageQueueRepository, bool useLock)
            : base(subscriber, logger, useLock)
        {
            _logger = logger;
            _messageQueueRepository = messageQueueRepository;
        }
#endif

        protected RetryableRedisSubscription(IEventSubscriber subscriber, IMessageQueueRepository messageQueueRepository)
            : this(subscriber, messageQueueRepository, false)
        {
        }

        protected RetryableRedisSubscription(IEventSubscriber subscriber, IMessageQueueRepository messageQueueRepository, bool useLock)
            :base(subscriber, useLock)
        {
            _messageQueueRepository = messageQueueRepository;
        }

        
        public virtual int TimeToLiveHours { get; set; } = 168;

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
                    if (await ShouldRetry(@event, eventData).ConfigureAwait(false))
                    {
                        LogInformation($"Beginning retry of processing for {eventType} event for Aggregate: {@event.Id}");

                        await RetryCallBackAsync(@event).ConfigureAwait(false);
                        await _messageQueueRepository.DeleteFromDeadLetterQueue<T>(eventData, @event).ConfigureAwait(false);
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
                LogDebug($"Skipping retry for event with Aggregate Id {@event.Id}; Retry interval has not elapsed.");
                return false;
            }

            if (TimeToLiveHours != default(int) &&
                DateTimeOffset.UtcNow > @event.TimeStamp.ToUniversalTime().AddHours(TimeToLiveHours))
            {
                LogDebug($"Time to live of {TimeToLiveHours} hours exceeded for event with Aggregate Id {@event.Id}; Deleting from the dead letter queue.");
                await _messageQueueRepository.DeleteFromDeadLetterQueue<T>(eventData, @event).ConfigureAwait(false);
                return false;
            }

            return true;
        }
    }
}
