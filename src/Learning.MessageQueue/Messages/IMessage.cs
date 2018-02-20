using System;

namespace Learning.MessageQueue.Messages
{
    public interface IMessage
    {
        string Id { get; set; }
        DateTimeOffset TimeStamp { get; set; }
    }
}