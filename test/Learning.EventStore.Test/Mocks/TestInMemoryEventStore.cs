using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Learning.EventStore.Test.Mocks
{
    public class TestInMemoryEventStore : IEventStore
    {
        public readonly IList<IEvent> Events = new List<IEvent>(); 

        public async Task SaveAsync(IEnumerable<IEvent> events)
        {
            await Task.Run(() =>
            {
                foreach (var @event in events)
                {
                    Events.Add(@event);   
                }
            });
        }

        public async Task<IEnumerable<IEvent>> GetAsync(string aggregateId, int fromVersion)
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
