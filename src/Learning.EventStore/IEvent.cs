using Learning.MessageQueue.Messages;

namespace Learning.EventStore
{
    public interface IEvent : IMessage
    {
        int Version { get; set; }
    }
}
