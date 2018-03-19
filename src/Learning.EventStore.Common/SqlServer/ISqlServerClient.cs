using System.Threading.Tasks;

namespace Learning.EventStore.Common.SqlServer
{
    public interface ISqlServerClient
    {
        Task<long> SaveEvent(EventDto eventDto);

        Task<string> GetEvent(string aggregateId, int fromVersion);

        Task DeleteEvent(long eventId);
    }
}
