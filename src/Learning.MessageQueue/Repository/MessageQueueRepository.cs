using System;
using System.Threading.Tasks;
using Learning.EventStore.Common.Redis;
using Learning.MessageQueue.Messages;
using StackExchange.Redis;

namespace Learning.MessageQueue.Repository
{
    public class MessageQueueRepository : IMessageQueueRepository
    {
        private readonly IRedisClient _redisClient;
        private readonly string _environment;
        private readonly string _keyPrefix;

        public MessageQueueRepository(IRedisClient redisClient, string environment, string keyPrefix)
        {
            _redisClient = redisClient;
            _environment = environment;
            _keyPrefix = keyPrefix;
        }

        public async Task<long> GetDeadLetterListLength<T>() where T : IMessage
        {
            var deadLetterListKey = GetDeadLetterListKey<T>();

            var listLength = await _redisClient.ListLengthAsync(deadLetterListKey).ConfigureAwait(false);

            return listLength;
        }

        public async Task<RedisValue> GetUnprocessedMessage<T>(int index) where T : IMessage
        {
            var deadLetterListKey = GetDeadLetterListKey<T>();

            var unprocessedEvent = await _redisClient.ListGetByIndexAsync(deadLetterListKey, index).ConfigureAwait(false);

            return unprocessedEvent;
        }
        public async Task DeleteFromDeadLetterList<T>(RedisValue valueToRemove) where T : IMessage
        {
            var deadLetterListKey = GetDeadLetterListKey<T>();

            await _redisClient.ListRemoveAsync(deadLetterListKey, valueToRemove).ConfigureAwait(false);
        }

        public async Task IncrementRetryCounter(IMessage @event)
        {
            var retryCounterKey = GetRetryCounterKey(@event);

            await _redisClient.StringIncrementAsync(retryCounterKey).ConfigureAwait(false); ;
        }

        public async Task<int> GetRetryCounter(IMessage @event)
        {
            var retryCounterKey = GetRetryCounterKey(@event);
            var retryCountString = await _redisClient.StringGetAsync(retryCounterKey).ConfigureAwait(false);
            int.TryParse(retryCountString, out var retryCount);

            return retryCount;
        }

        private string GetDeadLetterListKey<T>() where T : IMessage
        {
            var eventType = typeof(T).Name;
            var eventKey = $"{_environment}:{eventType}";
            var processingListKey = $"{_keyPrefix}:{{{eventKey}}}:DeadLetters";

            return processingListKey;
        }

        private string GetRetryCounterKey(IMessage @event)
        {
            var eventType = @event.GetType().Name;
            var eventKey = $"{_environment}:{eventType}";
            var retryCounterKey = $"{_keyPrefix}:{{{eventKey}}}:RetryCounter:{@event.Id}:{@event.TimeStamp}";

            return retryCounterKey;
        }
    }
}
