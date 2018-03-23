using System;

namespace Learning.EventStore.Common.Sql
{
    public class EventDto
    {
        public string AggregateId { get; set; }
        public string AggregateType { get; set; }
        public string ApplicationName { get; set; }
        public int Version { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
        public string EventType { get; set; }
        public string EventData { get; set; }
    }
}
