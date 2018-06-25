using System;
using System.Collections.Generic;
using System.Text;

namespace Learning.EventStore
{
    public abstract class Event : IEvent
    {
        public string Id { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
        public int Version { get; set; }
        public string AggregateType { get; set; }
    }
}
