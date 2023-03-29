using System;
using System.Threading.Tasks;
using Learning.EventStore.Common.Redis;
using Learning.MessageQueue.Messages;
using StackExchange.Redis;

namespace Learning.MessageQueue.Repository
{
    public interface IMessageQueueRepository
    {
        Task<long> GetDeadLetterListLength<T>() where T : IMessage;

        // TODO: Rename to GetDeadLetterMessage
        Task<RedisValue> GetUnprocessedMessage<T>(int index) where T : IMessage;

        void AddToDeadLetterQueue<T>(RedisValue eventData, IMessage @event, Exception exception) where T : IMessage;

        Task DeleteFromDeadLetterQueue<T>(RedisValue valueToRemove, IMessage @event) where T : IMessage;

        Task UpdateRetryData(IMessage @event, string exceptionMessage);

        Task<RetryData> GetRetryData(IMessage @event);

        Task<RedisValue[]> GetOldestProcessingEvents<T>(int count) where T : IMessage;

        Task MoveProcessingEventToDeadLetterQueue<T>(RedisValue eventData, IMessage @event) where T : IMessage;
    }
}
