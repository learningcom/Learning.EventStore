using System;
using System.Threading.Tasks;
using Learning.MessageQueue.Messages;

namespace Learning.MessageQueue.Retry
{
    public interface IRetry
    {
        Task ExecuteRetry<T>(Action<T> retryAction) where T : IMessage;

        Task ExecuteRetry<T>(Func<T, Task> retryAction) where T : IMessage;
    }
}
