namespace Learning.EventStore.Snapshotting
{
    public class SnapshotStoreSettings 
    {
        // <summary>
        /// The application name
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Enable gzip compression of event store entries
        /// </summary>
        public bool EnableCompression { get; set; } = false;

        /// <summary>
        /// The minimum size of an event store that will be compressed if compression is enabled. Entries larger than this will not be compressed.
        /// </summary>
        public int CompressionThreshold { get; set; } = 1000;
    }
}