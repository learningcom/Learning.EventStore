using System;
using Learning.EventStore.Messages;

namespace Learning.EventStore
{
    public interface IEvent : IMessage
    {
        int Version { get; set; }
    }
}
