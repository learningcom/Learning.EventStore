using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Learning.EventStore.Test.Mocks
{
    public class TestInMemoryEventStore : IEventStore
    {
        public readonly List<IEvent> Events = new List<IEvent>();

        public Task Save<T>(IEnumerable<IEvent> events)
        {
            return Task.Run(() =>
            {
                Events.AddRange(events);
            });

        }

        public Task<IEnumerable<IEvent>> Get<T>(Guid aggregateId, int fromVersion)
        {
            return Task.Run<IEnumerable<IEvent>>(() =>
            {
                return
                    Events.Where(x => x.Version > fromVersion && x.Id == aggregateId)
                        .OrderBy(x => x.Version)
                        .ToList();
            });
        }
    }
}
