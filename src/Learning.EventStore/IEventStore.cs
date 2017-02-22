using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Learning.EventStore
{
    public interface IEventStore
    {
        Task Save(IEnumerable<IEvent> events);
        Task<IEnumerable<IEvent>> Get(Guid aggregateId, int fromVersion);
    }
}
