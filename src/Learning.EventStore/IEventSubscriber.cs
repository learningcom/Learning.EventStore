using System;
using System.Threading.Tasks;

namespace Learning.EventStore
{
    public interface IEventSubscriber
    {
        Task SubscribeAsync<T>(Action<T> callBack);
    }
}
