using System;
using System.Threading.Tasks;

namespace Learning.MessageQueue
{
    public interface IEventSubscriber
    {
        Task SubscribeAsync<T>(Action<T> callBack);

        Task SubscribeAsync<T>(Func<T, Task> callBack);
    }
}
