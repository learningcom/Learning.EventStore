using System;

namespace Learning.EventStore.Common.Retry
{
    public interface IRetryable
    {
        int RetryForHours {get; set; }
        int RetryLimit { get; set; }
        int RetryIntervalMinutes { get; set; }
        int RetryIntervalMaxMinutes { get; set; }
    }
}
