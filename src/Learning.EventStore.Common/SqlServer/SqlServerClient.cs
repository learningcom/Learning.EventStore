using System.Collections.Generic;
using Dapper;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Learning.EventStore.Common.SqlServer
{
    public class SqlServerClient : ISqlServerClient
    {
        private readonly SqlServerEventStoreSettings _settings;

        public SqlServerClient(SqlServerEventStoreSettings settings)
        {
            _settings = settings;
        }

        public async Task<long> SaveEvent(EventDto eventDto)
        {
            long eventId;
            using (var conn = new SqlConnection(_settings.WriteConnectionString))
            {
                await conn.OpenAsync();
                eventId = await conn.QuerySingleAsync<long>(_settings.SaveSql, eventDto, commandType: _settings.CommandType);
            }

            return eventId;
        }

        public async Task<IEnumerable<string>> GetEvents(string aggregateId, int fromVersion)
        {
            IEnumerable<string> result;
            using (var conn = new SqlConnection(_settings.WriteConnectionString))
            {
                await conn.OpenAsync();
                result = await conn.QueryAsync<string>(_settings.GetSql, new {AggregateId = aggregateId, FromVersion = fromVersion}, commandType: _settings.CommandType);
            }

            return result;
        }

        public async Task DeleteEvent(long eventId)
        {
            using (var conn = new SqlConnection(_settings.WriteConnectionString))
            {
                await conn.ExecuteAsync(_settings.DeleteSql, new {Id = eventId }, commandType: _settings.CommandType);
            }
        }
    }
}
