using System.Data;
using Npgsql;

namespace Learning.EventStore.Common.Sql
{
    public class PostgresEventStoreSettings : ISqlEventStoreSettings
    {
        public PostgresEventStoreSettings(NpgsqlConnectionStringBuilder connectionStringBuilder, string applicationName)
        {
            WriteConnectionString = connectionStringBuilder.ConnectionString;
            ReadConnectionString = connectionStringBuilder.ConnectionString;
            ApplicationName = applicationName;
        }

        /// <inheritdoc />
        /// <summary>
        /// The name of the application using the event store
        /// </summary>
        public string ApplicationName { get; }

        /// <summary>
        /// The connection string for writing to SQL
        /// </summary>
        public string WriteConnectionString { get; }

        /// <summary>
        /// The connection string for reading from SQL.
        /// </summary>
        public string ReadConnectionString { get; }

        /// <summary>
        /// SQL for retrieving events
        /// </summary>
        public string GetSql { get; set; } = "get_events_for_aggregate";

        /// <summary>
        /// SQL for saving events
        /// </summary>
        public string SaveSql { get; set; } = "save_event_for_aggregate";

        /// <summary>
        /// The CommandType of the SQL calls. Defaults to StoredProcedure
        /// </summary>
        public CommandType CommandType { get; set; } = CommandType.StoredProcedure;
    }
}
