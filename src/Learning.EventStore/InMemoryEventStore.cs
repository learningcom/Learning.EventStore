using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Learning.EventStore
{
    public class InMemoryEventStore : IEventStore
    {

        private readonly Dictionary<string, List<IEvent>> _inMemoryDb = new Dictionary<string, List<IEvent>>();
        private readonly IEventPublisher _publisher;
        public InMemoryEventStore(IEventPublisher publisher)
        {
            _publisher = publisher;
        }

        public async Task Save(IEnumerable<IEvent> events)
        {
            foreach (var @event in events)
            {
                List<IEvent> list;
                _inMemoryDb.TryGetValue(@event.Id, out list);
                if (list == null)
                {
                    list = new List<IEvent>();
                    _inMemoryDb.Add(@event.Id, list);
                }
                list.Add(@event);
                await _publisher.Publish(@event);
            }
        }

        public async Task<IEnumerable<IEvent>> Get(string aggregateId, int fromVersion)
        {
            return await Task.Run(() =>
            {
                List<IEvent> events;
                _inMemoryDb.TryGetValue(aggregateId, out events);
                return events?.Where(x => x.Version > fromVersion) ?? new List<IEvent>();
            });
        }
    }
}
