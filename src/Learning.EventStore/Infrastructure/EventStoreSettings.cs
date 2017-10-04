using System;
using System.Collections.Generic;
using System.Text;

namespace Learning.EventStore.Infrastructure
{
    public class EventStoreSettings
    {
        public string KeyPrefix { get; set; }
        public bool EnableCompression { get; set; } = false;
        public int CompressionThreshold { get; set; } = 1000;
        public int SaveRetryCount { get; set; } = 10;
    }
}
