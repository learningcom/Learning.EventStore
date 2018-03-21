using System.Collections.Generic;
using Dapper;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Learning.EventStore.Common.Sql
{
    public class SqlClient : ISqlClient
    {
        private readonly SqlEventStoreSettings _settings;

        public SqlClient(SqlEventStoreSettings settings)
        {
            _settings = settings;
        }

        public async Task SaveEvent(EventDto eventDto)
        {
            using (var conn = new SqlConnection(_settings.WriteConnectionString))
            {
                await conn.OpenAsync();
                await conn.ExecuteAsync(_settings.SaveSql, eventDto, commandType: _settings.CommandType).ConfigureAwait(false);
            }
        }

        public async Task<IEnumerable<string>> GetEvents(string aggregateId, int fromVersion)
        {
            IEnumerable<string> result;
            using (var conn = new SqlConnection(_settings.ReadConnectionString))
            {
                await conn.OpenAsync();
                result = await conn.QueryAsync<string>(_settings.GetSql, new {AggregateId = aggregateId, FromVersion = fromVersion}, commandType: _settings.CommandType).ConfigureAwait(false);
            }

            return result;
        }
    }
}
