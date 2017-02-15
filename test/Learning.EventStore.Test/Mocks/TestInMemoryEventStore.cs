using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Packaging;

namespace Learning.EventStore.Test.Mocks
{
    public class TestInMemoryEventStore : IEventStore
    {
        public readonly IList<IEvent> Events = new List<IEvent>(); 

        public async Task Save<T>(IEnumerable<IEvent> events)
        {
            await Task.Run(() =>
            {
                Events.AddRange(events);
            });
        }

        public async Task<IEnumerable<IEvent>> Get<T>(Guid aggregateId, int fromVersion)
        {
            return await Task.Run<IEnumerable<IEvent>>(() =>
            {
                return
                    Events.Where(x => x.Version > fromVersion && x.Id == aggregateId)
                        .OrderBy(x => x.Version)
                        .ToList();
            });
        }
    }
}
