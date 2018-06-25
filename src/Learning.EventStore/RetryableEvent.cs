using System;
using Learning.EventStore.Common.Retry;

namespace Learning.EventStore
{
    public abstract class RetryableEvent : IEvent, IRetryable
    {
        public string Id { get; set; }
        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;
        public int Version { get; set; }
        public string AggregateType { get; set; }
        public virtual int RetryForHours { get; set; } = 0;
        public virtual int RetryLimit { get; set; } = 5;
        public virtual int RetryIntervalMinutes { get; set; } = 5;
        public virtual int RetryIntervalMaxMinutes { get; set; } = 60;
    }
}
