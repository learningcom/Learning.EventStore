using System;

namespace Learning.EventStore.Common.SqlServer
{
    public class EventDto
    {
        public string AggregateId { get; set; }
        public int Version { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
        public string EventType { get; set; }
        public string Data { get; set; }
    }
}
