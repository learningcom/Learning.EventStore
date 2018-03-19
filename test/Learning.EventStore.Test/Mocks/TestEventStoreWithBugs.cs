using System.Collections.Generic;
using System.Threading.Tasks;
using Learning.EventStore.DataStores;

namespace Learning.EventStore.Test.Mocks
{
    public class TestEventStoreWithBugs : IEventStore
    {
        public Task SaveAsync(IEnumerable<IEvent> events)
        {
            return Task.CompletedTask;
        }

        public Task<IEnumerable<IEvent>> GetAsync(string aggregateId, int version)
        {
            return Task.Run<IEnumerable<IEvent>>(() =>
            {
                if (string.IsNullOrEmpty(aggregateId))
                {
                    return new List<IEvent>();
                }

                return new List<IEvent>
                {
                    new TestAggregateDidSomething {Id = aggregateId, Version = 3},
                    new TestAggregateDidSomething {Id = aggregateId, Version = 2},
                    new TestAggregateDidSomeethingElse {Id = aggregateId, Version = 1},
                };
            });

        }
    }
}
