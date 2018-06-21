using System;
using System.Collections.Generic;
using System.Text;

namespace Learning.MessageQueue.Messages
{
    public abstract class Message : IMessage
    {
        public string Id { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
    }
}
