using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Learning.EventStore.Test.Mocks
{
    public class TestEventStoreWithBugs : IEventStore
    {
        public Task Save(IEnumerable<IEvent> events)
        {
            return Task.CompletedTask;
        }

        public Task<IEnumerable<IEvent>> Get(Guid aggregateId, int version)
        {
            return Task.Run<IEnumerable<IEvent>>(() =>
            {
                if (aggregateId == Guid.Empty)
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
