using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Learning.EventStore
{
    public interface IEventStore
    {
        Task SaveAsync(IEnumerable<IEvent> events);
        Task<IEnumerable<IEvent>> GetAsync(string aggregateId, int fromVersion);
    }
}
