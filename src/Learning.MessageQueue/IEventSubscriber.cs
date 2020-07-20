using System;
using System.Threading.Tasks;
using Learning.MessageQueue.Messages;

namespace Learning.MessageQueue
{
    public interface IEventSubscriber
    {
        Task SubscribeAsync<T>(Action<T> callBack) where T : IMessage;
        Task SubscribeAsync<T>(Action<T> callBack, bool enableLock) where T : IMessage;
        Task SubscribeAsync<T>(Action<T> callBack, bool enableLock, bool sequentialProcessing) where T : IMessage;
    }
}
