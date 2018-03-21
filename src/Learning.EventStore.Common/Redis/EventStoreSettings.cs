namespace Learning.EventStore.Common.Redis
{
    public class EventStoreSettings
    {
        /// <summary>
        /// The application name
        /// </summary>
        public string KeyPrefix { get; set; }

        /// <summary>
        /// Enable gzip compression of event store entries
        /// </summary>
        public bool EnableCompression { get; set; } = false;

        /// <summary>
        /// The minimum size of an event store that will be compressed if compression is enabled. Entries larger than this will not be compressed.
        /// </summary>
        public int CompressionThreshold { get; set; } = 1000;

        /// <summary>
        /// How many times to retry failed transactions when save or retrieveing objects from the event store
        /// </summary>
        public int TransactionRetryCount { get; set; } = 10;

        /// <summary>
        /// How long to wait before retrying a transaction
        /// </summary>
        public int TransactionRetryDelay { get; set; } = 100;
    }
}
