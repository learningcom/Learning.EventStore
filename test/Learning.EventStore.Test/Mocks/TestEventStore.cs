using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Learning.EventStore.DataStores;

namespace Learning.EventStore.Test.Mocks
{
    public class TestEventStore : IEventStore
    {
        private readonly string _emptyGuid;

        public TestEventStore()
        {
            _emptyGuid = Guid.NewGuid().ToString();
            SavedEvents = new List<IEvent>();
        }

        public Task<IEnumerable<IEvent>> GetAsync(string aggregateId, string aggregateType, int fromVersion)
        {
            return Task.Run(() =>
            {
                if (string.IsNullOrEmpty(aggregateId))
                {
                    return new List<IEvent>();
                }

                return new List<IEvent>
                {
                    new TestAggregateDidSomething {Id = aggregateId, Version = 1},
                    new TestAggregateDidSomeethingElse {Id = aggregateId, Version = 2},
                    new TestAggregateDidSomething {Id = aggregateId, Version = 3},
                }.Where(x => x.Version > fromVersion);
            });
        }

        public Task SaveAsync(IEnumerable<IEvent> events)
        {
            return Task.Run(() =>
            {
                SavedEvents.AddRange(events);
            });
        }

        private List<IEvent> SavedEvents { get; }
    }
}
