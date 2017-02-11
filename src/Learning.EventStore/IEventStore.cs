using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Learning.EventStore
{
    public interface IEventStore
    {
        Task Save<T>(IEnumerable<IEvent> events);
        Task<IEnumerable<IEvent>> Get<T>(Guid aggregateId, int fromVersion);
    }
}
