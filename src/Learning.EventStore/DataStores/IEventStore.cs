using System.Collections.Generic;
using System.Threading.Tasks;

namespace Learning.EventStore.DataStores
{
    public interface IEventStore
    {
        Task SaveAsync(IEnumerable<IEvent> events);
        Task<IEnumerable<IEvent>> GetAsync(string aggregateId, int fromVersion);
    }
}
