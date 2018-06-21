using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Learning.MessageQueue.Messages;
using StackExchange.Redis;

namespace Learning.MessageQueue.Repository
{
    public interface IMessageQueueRepository
    {
        Task<long> GetDeadLetterListLength<T>() where T : IMessage;

        Task<RedisValue> GetUnprocessedMessage<T>(int index) where T : IMessage;

        Task DeleteFromDeadLetterList<T>(RedisValue valueToRemove) where T : IMessage;

        Task IncrementRetryCounter(IMessage @event);

        Task<int> GetRetryCounter(IMessage @event);
    }
}
