using System;
using System.Threading.Tasks;
using Learning.EventStore.Messages;

namespace Learning.EventStore
{
    public interface IEventPublisher
    {
        Task Publish<T>(T @event) where T : IEvent;
    }
}
