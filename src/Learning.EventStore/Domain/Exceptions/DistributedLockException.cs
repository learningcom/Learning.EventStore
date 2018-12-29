using System;
using Learning.EventStore.Common;

namespace Learning.EventStore.Domain.Exceptions
{
    public class DistributedLockException : System.Exception
    {
        public DistributedLockException(string id, EventStoreSettings settings)
            : base($"Failed to get lock for Aggregate '{id}' within {settings.SessionLockWaitSeconds} seconds")
        { }
    }
}