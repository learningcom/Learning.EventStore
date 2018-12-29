namespace Learning.EventStore.Common
{
    public abstract class EventStoreSettings
    {
        /// <summary>
        /// The application name
        /// </summary>
        public string ApplicationName { get; set; }

        //// <summary>
        /// Use a distributed lock for aggregates within a session
        /// </summary>
        public bool SessionLockEnabled { get; set; } = false;
        
        /// <summary>
        /// The expiration time in seconds of the session lock
        /// </summary>
        public double SessionLockExpirySeconds { get; set; } = 30;

        /// <summary>
        /// The length of time in seconds to wait for a session lock to be acquired
        /// </summary>
        public double SessionLockWaitSeconds { get; set; } = 60;

        /// <summary>
        /// The length of time in milliseconds to wait in between retries to acquire a session lock
        /// </summary>
        public double SessionLockRetryMilliseconds { get; set; } = 100;
    }
}
