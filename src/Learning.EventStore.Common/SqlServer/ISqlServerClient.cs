using System.Collections.Generic;
using System.Threading.Tasks;

namespace Learning.EventStore.Common.SqlServer
{
    public interface ISqlServerClient
    {
        Task<long> SaveEvent(EventDto eventDto);

        Task<IEnumerable<string>> GetEvents(string aggregateId, int fromVersion);

        Task DeleteEvent(long eventId);
    }
}
