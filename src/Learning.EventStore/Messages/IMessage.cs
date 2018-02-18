using System;

namespace Learning.EventStore.Messages
{
    public interface IMessage
    {
        string Id { get; set; }
        DateTimeOffset TimeStamp { get; set; }
    }
}