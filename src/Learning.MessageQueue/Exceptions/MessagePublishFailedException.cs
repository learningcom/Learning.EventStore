using System;
using System.Collections.Generic;
using System.Text;

namespace Learning.EventStore.Domain.Exceptions
{
    public class MessagePublishFailedException : Exception
    {
        public MessagePublishFailedException(string aggregateId, int retries)
            : base($"Failed to publish event for aggregate {aggregateId} after retrying {retries} times")
        { }
    }
}
