using System;
using System.Collections.Generic;
using Learning.EventStore.Domain.Exceptions;
using Learning.EventStore.Infrastructure;

namespace Learning.EventStore.Domain
{
    public abstract class AggregateRoot
    {
        private readonly List<IEvent> _changes = new List<IEvent>();

        public string Id { get; set; }
        public int Version { get; set; }

        public IEvent[] GetUncommittedChanges()
        {
            lock (_changes)
            {
                return _changes.ToArray();
            }
        }

        public IEnumerable<IEvent> FlushUncommitedChanges()
        {
            lock (_changes)
            {
                var changes = _changes.ToArray();
                var i = 0;
                foreach (var @event in changes)
                {
                    var aggregateType = GetType();
                    if (string.IsNullOrEmpty(@event.Id) && string.IsNullOrEmpty(Id))
                    {
                        throw new AggregateOrEventMissingIdException(aggregateType, @event.GetType());
                    }
                    if (string.IsNullOrEmpty(@event.Id))
                    {
                        @event.Id = Id;
                    }
                    i++;
                    @event.Version = Version + i;
                    @event.TimeStamp = DateTimeOffset.UtcNow;
                    @event.AggregateType = aggregateType.Name;
                }
                Version = Version + _changes.Count;
                _changes.Clear();
                return changes;
            }
        }

        public void LoadFromHistory(IEnumerable<IEvent> history)
        {
            lock (_changes)
            {
                foreach (var e in history)
                {
                    if (e.Version != Version + 1)
                    {
                        throw new EventsOutOfOrderException(e.Id);
                    }
                    ApplyEvent(e);
                    Id = e.Id;
                    Version++;
                }
            }
        }

        protected void ApplyChange(IEvent @event)
        {
            lock (_changes)
            {
                ApplyEvent(@event);
                _changes.Add(@event);
            }
        }

        /// <summary>
        /// Overrideable method for applying events on aggregate
        /// This is called interally when rehydrating aggregates.
        /// Can be overridden if you want custom handling.
        /// </summary>
        /// <param name="event">Event to apply</param>
        protected virtual void ApplyEvent(IEvent @event)
        {
            this.Invoke("Apply", @event);
        }
    }
}
