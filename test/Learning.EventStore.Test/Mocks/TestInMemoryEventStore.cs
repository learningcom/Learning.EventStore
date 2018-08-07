using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Learning.EventStore.DataStores;

namespace Learning.EventStore.Test.Mocks
{
    public class TestInMemoryEventStore : IEventStore
    {
        public readonly List<IEvent> Events = new List<IEvent>(); 

        public Task SaveAsync(IEnumerable<IEvent> events)
        {
            lock (Events)
            {
                Events.AddRange(events);
            }

            return Task.CompletedTask;
        }

        public Task<IEnumerable<IEvent>> GetAsync(string aggregateId, string aggregateType, int fromVersion)
        {
            lock (Events)
            {
                return Task.FromResult((IEnumerable<IEvent>)Events
                    .Where(x => x.Version > fromVersion && x.Id == aggregateId)
                    .OrderBy(x => x.Version)
                    .ToList());
            }
        }
    }
}
