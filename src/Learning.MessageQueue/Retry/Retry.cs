using System;
using System.Threading.Tasks;
using Learning.EventStore.Common.Retry;
using Learning.MessageQueue.Messages;
using Learning.MessageQueue.Repository;
#if !NET46 && !NET452
using Microsoft.Extensions.Logging;
#endif
using Newtonsoft.Json;

namespace Learning.MessageQueue.Retry
{
    public class Retry : IRetry
    {
        private readonly IMessageQueueRepository _messageQueueRepository;

#if !NET46 && !NET452
        private readonly ILogger _logger;

        protected Retry(ILogger logger, IMessageQueueRepository messageQueueRepository)
        {
            _logger = logger;
            _messageQueueRepository = messageQueueRepository;
        }
#endif

        protected Retry(IMessageQueueRepository messageQueueRepository)
        {
            _messageQueueRepository = messageQueueRepository;
        }

        public async Task ExecuteRetry<T>(Action<T> retryAction) where T : IMessage
        {
            Task Func(T arg)
            {
                return Task.Run(() =>
                {
                    retryAction(arg);
                });
            }

            await ExecuteRetry((Func<T, Task>)Func).ConfigureAwait(false);
        }

        public async Task ExecuteRetry<T>(Func<T, Task> retryAction) where T : IMessage
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

                        await retryAction(@event).ConfigureAwait(false);
                        await _messageQueueRepository.DeleteFromDeadLetterQueue(eventData, @event).ConfigureAwait(false);
                        eventsProcessed++;

                        LogInformation($"Completed retry of processing for {eventType} event for Aggregate: {@event.Id}");
                    }
                    else
                    {
                        LogInformation($"Skipping retry for event with Aggregate Id {@event.Id}; Retry threshold reached");
                    }
                }
                catch (Exception e)
                {
                    var message = $"{e.Message}{Environment.NewLine}{e.StackTrace}";
                    await _messageQueueRepository.UpdateRetryData(@event, message).ConfigureAwait(false);
                    LogWarning($"Event processing retry failed for {GetType()} with message: {e.Message}{Environment.NewLine}{e.StackTrace}");
                    
                    errors++;
                }
            }

            LogInformation($"Retry complete for {eventType}. Processed {eventsProcessed} events with {errors} errors.");
        }


        private async Task<bool> ShouldRetry(IMessage @event)
        {
            if (!(@event is IRetryable retryable))
            {
                return false;
            }

            var retryData = await _messageQueueRepository.GetRetryData(@event).ConfigureAwait(false);

            // exponential backoff
            var mpow = (int)Math.Pow(2, retryData.RetryCount);
            var interval = Math.Min(mpow * retryable.RetryIntervalMinutes, retryable.RetryIntervalMaxMinutes);
            var intervalPassed = DateTimeOffset.UtcNow > retryData.LastRetryTime?.ToUniversalTime().AddMinutes(interval);

            if (!intervalPassed)
            {
                return false;
            }

            if (retryable.RetryForHours != default(int) && 
                DateTimeOffset.UtcNow < @event.TimeStamp.ToUniversalTime().AddHours(retryable.RetryForHours))
            {
                return true;
            }

            return retryData.RetryCount <= retryable.RetryLimit;
        }
        
        private void LogInformation(string message)
        {
#if !NET46 && !NET452
            _logger.LogInformation(message);
#endif
        }

        private void LogWarning(string message)
        {
#if !NET46 && !NET452
            _logger.LogWarning(message);
#endif
        }
    }
}
