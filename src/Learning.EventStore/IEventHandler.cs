using Learning.EventStore.Messages;

namespace Learning.EventStore
{
    public interface IEventHandler<in T> : IHandler<T> where T : IEvent
    { }
}
