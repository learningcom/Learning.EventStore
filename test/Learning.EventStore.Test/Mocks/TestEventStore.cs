using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Learning.EventStore.Test.Mocks
{
    public class TestEventStore : IEventStore
    {
        private readonly Guid _emptyGuid;

        public TestEventStore()
        {
            _emptyGuid = Guid.NewGuid();
            SavedEvents = new List<IEvent>();
        }

        public Task<IEnumerable<IEvent>> Get<T>(Guid aggregateId, int version)
        {
            return Task.Run(() =>
            {
                if (aggregateId == _emptyGuid || aggregateId == Guid.Empty)
                {
                    return new List<IEvent>();
                }

                return new List<IEvent>
                {
                    new TestAggregateDidSomething {Id = aggregateId, Version = 1},
                    new TestAggregateDidSomeethingElse {Id = aggregateId, Version = 2},
                    new TestAggregateDidSomething {Id = aggregateId, Version = 3},
                }.Where(x => x.Version > version);
            });
        }

        public Task Save<T>(IEnumerable<IEvent> events)
        {
            return Task.Run(() =>
            {
                SavedEvents.AddRange(events);
            });
        }

        private List<IEvent> SavedEvents { get; }
    }
}
