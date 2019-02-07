using System;
using System.Collections.Generic;
using System.Text;

namespace Learning.EventStore.Common
{
    public class DistributedLockSettings
    {   
        /// <summary>
        /// The expiration time in seconds of the session lock
        /// </summary>
        public double ExpirySeconds { get; set; } = 30;

        /// <summary>
        /// The length of time in seconds to wait for a session lock to be acquired
        /// </summary>
        public double WaitSeconds { get; set; } = 60;

        /// <summary>
        /// The length of time in milliseconds to wait in between retries to acquire a session lock
        /// </summary>
        public double RetryMilliseconds { get; set; } = 100;
    }
}
