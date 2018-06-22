using System;

namespace Learning.MessageQueue.Retry
{
    public class RetryData
    {
        public int RetryCount { get; set; }
        public DateTimeOffset? LastRetryTime { get; set; }
    }
}
