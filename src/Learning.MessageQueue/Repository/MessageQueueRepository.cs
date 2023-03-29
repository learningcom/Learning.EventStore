using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Learning.EventStore.Common.Redis;
using Learning.EventStore.Common.Sql;
using Learning.MessageQueue.Messages;
using StackExchange.Redis;

namespace Learning.MessageQueue.Repository
{
    public class MessageQueueRepository : IMessageQueueRepository
    {
        private readonly IRedisClient _redisClient;
        private readonly string _environment;
        private readonly string _applicationName;

        private const string RetryCountFieldName = "RetryCount";
        private const string LastRetryTimeFieldName = "LastRetryTime";
        private const string LastExceptionFieldName = "LastException";

        public MessageQueueRepository(IRedisClient redisClient, string environment, string applicationName)
        {
            _redisClient = redisClient;
            _environment = environment;
            _applicationName = applicationName;
        }

        public async Task<long> GetDeadLetterListLength<T>() where T : IMessage
        {
            var deadLetterListKey = GetDeadLetterListKey<T>();

            var listLength = await _redisClient.ListLengthAsync(deadLetterListKey).ConfigureAwait(false);

            return listLength;
        }

        // TODO: Rename to GetDeadLetterMessage
        public async Task<RedisValue> GetUnprocessedMessage<T>(int index) where T : IMessage
        {
            var deadLetterListKey = GetDeadLetterListKey<T>();

            var unprocessedEvent = await _redisClient.ListGetByIndexAsync(deadLetterListKey, index).ConfigureAwait(false);

            return unprocessedEvent;
        }

        public void AddToDeadLetterQueue<T>(RedisValue eventData, IMessage @event, Exception exception) where T : IMessage
        {   
            var message = $"{exception.Message}{Environment.NewLine}{exception.StackTrace}";
            var retryDataHashKey = GetRetryDataHashKey(@event);
            var deadLetterListKey = GetDeadLetterListKey<T>();
            var tran = _redisClient.CreateTransaction();

            tran.ListLeftPushAsync(deadLetterListKey, eventData);
            tran.HashSetAsync(retryDataHashKey, LastExceptionFieldName, message);

            ExcecuteTransaction(tran, @event.Id).GetAwaiter().GetResult();
        }

        public async Task DeleteFromDeadLetterQueue<T>(RedisValue valueToRemove, IMessage @event) where T : IMessage
        {
            var deadLetterListKey = GetDeadLetterListKey<T>();
            var retryDataHashKey = GetRetryDataHashKey(@event);
            var tran = _redisClient.CreateTransaction();

            tran.ListRemoveAsync(deadLetterListKey, valueToRemove);
            tran.KeyDeleteAsync(retryDataHashKey);

            await ExcecuteTransaction(tran, @event.Id).ConfigureAwait(false);
        }

        public async Task UpdateRetryData(IMessage @event, string exceptionMessage)
        {
            var retryDataHashKey = GetRetryDataHashKey(@event);
            var tran = _redisClient.CreateTransaction();

            tran.HashIncrementAsync(retryDataHashKey, RetryCountFieldName);
            tran.HashSetAsync(retryDataHashKey, LastExceptionFieldName, exceptionMessage);
            tran.HashSetAsync(retryDataHashKey, LastRetryTimeFieldName, DateTimeOffset.UtcNow.ToString());

            await ExcecuteTransaction(tran, @event.Id).ConfigureAwait(false);
        }

        public async Task<RetryData> GetRetryData(IMessage @event)
        {
            var retryDataHashName = GetRetryDataHashKey(@event);
            var hashData = await _redisClient.HashGetAllAsync(retryDataHashName).ConfigureAwait(false);
            var lastRetryTime = hashData?.FirstOrDefault(x => x.Name == LastRetryTimeFieldName).Value;
            var retryCount = hashData?.FirstOrDefault(x => x.Name == RetryCountFieldName).Value;

            var data = new RetryData
            {
                RetryCount = retryCount == RedisValue.Null ? 0 : int.Parse(retryCount),
                LastRetryTime = lastRetryTime == RedisValue.Null ? DateTimeOffset.UtcNow : DateTimeOffset.Parse(lastRetryTime)
            };

            return data;
        }

        public async Task<RedisValue[]> GetOldestProcessingEvents<T>(int count) where T : IMessage
        {
            var processingEventsListKey = GetProcessingEventsListKey<T>();
            var listLength = await _redisClient.ListLengthAsync(processingEventsListKey).ConfigureAwait(false);
            long startIndex;
            
            if (listLength == 0)
            {
                return new RedisValue[0];
            }
            if (listLength > count)
            {
                startIndex = listLength - count;
            }
            else
            {
                startIndex = 0;
            }

            var unprocessedEvents = await _redisClient.ListRangeAsync(processingEventsListKey, startIndex, -1).ConfigureAwait(false);
            return unprocessedEvents;
        }

        public async Task MoveProcessingEventToDeadLetterQueue<T>(RedisValue eventData, IMessage @event) where T : IMessage
        {
            var deadLetterListKey = GetDeadLetterListKey<T>();
            var processingEventsListKey = GetProcessingEventsListKey<T>();
            var tran = _redisClient.CreateTransaction();

            tran.ListLeftPushAsync(deadLetterListKey, eventData);
            tran.ListRemoveAsync(processingEventsListKey, eventData, -1);

            await ExcecuteTransaction(tran, @event.Id).ConfigureAwait(false);
        }

        private string GetDeadLetterListKey<T>() where T : IMessage
        {
            var eventType = typeof(T).Name;
            var eventKey = $"{_environment}:{eventType}";
            var processingListKey = $"{_applicationName}:{{{eventKey}}}:DeadLetters";

            return processingListKey;
        }

        private string GetProcessingEventsListKey<T>() where T : IMessage
        {
            var eventType = typeof(T).Name;
            var eventKey = $"{_environment}:{eventType}";
            var processingListKey = $"{_applicationName}:{{{eventKey}}}:ProcessingEvents";

            return processingListKey;
        }

        private string GetRetryDataHashKey(IMessage @event)
        {
            var eventType = @event.GetType().Name;
            var eventKey = $"{_environment}:{eventType}";
            var retryDataHashName = $"RetryData:{{{eventKey}}}:{@event.Id}";

            return retryDataHashName;
        }

        private async Task ExcecuteTransaction(ITransaction tran, string aggregateId)
        {
            var result = await _redisClient.ExecuteTransactionAsync(tran).ConfigureAwait(false);

            if (!result)
            {
                throw new Exception($"Redis transaction failed for AggregateId {aggregateId}");
            }
        }

    }
}
