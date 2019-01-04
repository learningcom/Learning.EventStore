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

        Task<RedisValue> GetUnprocessedMessage<T>(int index) where T : IMessage;

        void AddToDeadLetterQueue<T>(RedisValue eventData, IMessage @event, Exception exception) where T : IMessage;

        Task DeleteFromDeadLetterQueue<T>(RedisValue valueToRemove, T @event) where T : IMessage;

        Task UpdateRetryData(IMessage @event, string exceptionMessage);

        Task<RetryData> GetRetryData(IMessage @event);
    }
}
