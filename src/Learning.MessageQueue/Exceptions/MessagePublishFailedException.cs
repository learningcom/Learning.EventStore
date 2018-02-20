using System;

namespace Learning.MessageQueue.Exceptions
{
    public class MessagePublishFailedException : Exception
    {
        public MessagePublishFailedException(string aggregateId, int retries)
            : base($"Failed to publish event for aggregate {aggregateId} after retrying {retries} times")
        { }
    }
}
