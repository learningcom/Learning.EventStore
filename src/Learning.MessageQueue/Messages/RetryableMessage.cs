using System;
using Learning.EventStore.Common.Retry;

namespace Learning.MessageQueue.Messages
{
    public abstract class RetryableMessage : IMessage, IRetryable
    {
        public string Id { get; set; }

        public DateTimeOffset TimeStamp { get; set; }

        public virtual int RetryForHours { get; set; } = 0;

        public virtual int RetryLimit { get; set; } = 5;

        public virtual int RetryIntervalMinutes { get; set; } = 5;

        public virtual int RetryIntervalMaxMinutes { get; set; } = 60;
    }
}
