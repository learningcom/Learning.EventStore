using System;
using System.Threading.Tasks;

namespace Learning.EventStore
{
    public interface IEventSubscriber
    {
        Task Subscribe<T>(Action<T> callBack);
    }
}
