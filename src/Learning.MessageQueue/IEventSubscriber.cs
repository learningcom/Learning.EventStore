using System;
using System.Threading.Tasks;
using Learning.MessageQueue.Messages;

namespace Learning.MessageQueue
{
    public interface IEventSubscriber
    {
        Task SubscribeAsync<T>(Action<T> callBack) where T : IMessage;
    }
}
