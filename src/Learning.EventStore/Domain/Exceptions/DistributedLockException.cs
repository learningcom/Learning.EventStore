using System;
using Learning.EventStore.Common;
using RedLockNet;

namespace Learning.EventStore.Domain.Exceptions
{
    public class DistributedLockException : System.Exception
    {
        public DistributedLockException(IRedLock redLock, EventStoreSettings settings)
            : base($"Failed to get lock for Aggregate '{redLock.Resource}' within {settings.SessionLockWaitSeconds} seconds with status: {redLock.Status}")
        { }

        public DistributedLockException(string message)
            : base(message)
        {
        }
    }
}