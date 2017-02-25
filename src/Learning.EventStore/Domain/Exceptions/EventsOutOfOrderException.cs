using System;

namespace Learning.EventStore.Domain.Exceptions
{
    public class EventsOutOfOrderException : System.Exception
    {
        public EventsOutOfOrderException(string id)
            : base($"Eventstore gave event for aggregate {id} out of order")
        { }
    }
}