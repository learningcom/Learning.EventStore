using System;

namespace Learning.MessageQueue.Messages
{
    public class RetryData
    {
        public int RetryCount { get; set; }
        public DateTimeOffset? LastRetryTime { get; set; }
    }
}
