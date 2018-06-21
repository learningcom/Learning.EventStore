using System;
using System.Threading.Tasks;
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
        private readonly int _retryThreshold;

#if !NET46 && !NET452
        private readonly ILogger _logger;

        protected Retry(ILogger logger, IMessageQueueRepository messageQueueRepository)
            : this(logger, messageQueueRepository, 5)
        {
        }

        protected Retry(ILogger logger, IMessageQueueRepository messageQueueRepository, int retryThreshold)
        {
            _logger = logger;
            _messageQueueRepository = messageQueueRepository;
            _retryThreshold = retryThreshold;
        }
#endif

        protected Retry(IMessageQueueRepository messageQueueRepository)
            : this(messageQueueRepository, 5)
        {
        }

        protected Retry(IMessageQueueRepository messageQueueRepository, int retryThreshold)
        {
            _messageQueueRepository = messageQueueRepository;
            _retryThreshold = retryThreshold;
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
                try
                {
                    var indexToGet = i - eventsProcessed;
                    var eventData = await _messageQueueRepository.GetUnprocessedMessage<T>(indexToGet).ConfigureAwait(false);

                    if (string.IsNullOrEmpty(eventData))
                    {
                        continue;
                    }

                    var @event = JsonConvert.DeserializeObject<T>(eventData);

                    var retryCount = await _messageQueueRepository.GetRetryCounter(@event).ConfigureAwait(false);

                    if (retryCount >= _retryThreshold)
                    {
                        LogInformation($"Skipping retry for event with Aggregate Id {@event.Id}; Retry threshold reached");

                        continue;
                    }

                    LogInformation($"Beginning retry of processing for {eventType} event for Aggregate: {@event.Id}");

                    await ExecuteCallback(retryAction, @event).ConfigureAwait(false);
                    await _messageQueueRepository.DeleteFromDeadLetterList<T>(eventData).ConfigureAwait(false);
                    eventsProcessed++;

                    LogInformation($"Completed retry of processing for {eventType} event for Aggregate: {@event.Id}");
                }
                catch (Exception e)
                {
                    LogWarning($"Event processing retry failed for {GetType()} with message: {e.Message}{Environment.NewLine}{e.StackTrace}");
                    errors++;
                }
            }

            LogInformation($"Retry complete for {eventType}. Processed {eventsProcessed} events with {errors} errors.");
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

        private async Task ExecuteCallback<T>(Func<T, Task> retryAction, T @event) where T : IMessage
        {
            try
            {
                await retryAction(@event).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await _messageQueueRepository.IncrementRetryCounter(@event).ConfigureAwait(false);
                throw;
            }
        }
    }
}
