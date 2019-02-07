namespace Learning.EventStore.Common
{
    public abstract class EventStoreSettings : DistributedLockSettings
    {
        /// <summary>
        /// The application name
        /// </summary>
        public string ApplicationName { get; set; }

        //// <summary>
        /// Use a distributed lock for aggregates within a session
        /// </summary>
        public bool SessionLockEnabled { get; set; } = false;
    }
}
