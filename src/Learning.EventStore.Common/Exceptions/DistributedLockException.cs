using System;
using RedLockNet;

namespace Learning.EventStore.Common.Exceptions
{
    public class DistributedLockException : Exception
    {
        public DistributedLockException(IRedLock redLock, DistributedLockSettings settings)
            : base($"Failed to get lock for Aggregate '{redLock.Resource}' within {settings.WaitSeconds} seconds with status: {redLock.Status}")
        { }

        public DistributedLockException(string message)
            : base(message)
        {
        }
    }
}
