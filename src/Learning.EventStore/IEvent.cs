using System;
using Learning.EventStore.Messages;

namespace Learning.EventStore
{
    public interface IEvent : IMessage
    {
        string Id { get; set; }
        int Version { get; set; }
        DateTimeOffset TimeStamp { get; set; }
    }
}
