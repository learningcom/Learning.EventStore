using System.Threading.Tasks;
using Learning.MessageQueue.Messages;
using Learning.MessageQueue.Retry;
using StackExchange.Redis;

namespace Learning.MessageQueue.Repository
{
    public interface IMessageQueueRepository
    {
        Task<long> GetDeadLetterListLength<T>() where T : IMessage;

        Task<RedisValue> GetUnprocessedMessage<T>(int index) where T : IMessage;

        Task DeleteFromDeadLetterQueue<T>(RedisValue valueToRemove, T @event) where T : IMessage;

        Task UpdateRetryData(IMessage @event, string exceptionMessage);

        Task<RetryData> GetRetryData(IMessage @event);
    }
}
