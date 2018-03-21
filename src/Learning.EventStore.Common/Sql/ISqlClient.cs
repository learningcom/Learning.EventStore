using System.Collections.Generic;
using System.Threading.Tasks;

namespace Learning.EventStore.Common.Sql
{
    public interface ISqlClient
    {
        Task SaveEvent(EventDto eventDto);

        Task<IEnumerable<string>> GetEvents(string aggregateId, int fromVersion);
    }
}
