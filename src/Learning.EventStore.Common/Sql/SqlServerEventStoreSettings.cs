using System.Data;
using System.Data.SqlClient;

namespace Learning.EventStore.Common.Sql
{
    public class SqlServerEventStoreSettings : EventStoreSettings, ISqlEventStoreSettings
    {
        public SqlServerEventStoreSettings(SqlConnectionStringBuilder connectionStringBuilder, string applicationName)
        {
            WriteConnectionString = connectionStringBuilder.ConnectionString;
            connectionStringBuilder.ApplicationIntent = ApplicationIntent.ReadOnly;
            ReadConnectionString = connectionStringBuilder.ConnectionString;
            ApplicationName = applicationName;
        }

        /// <summary>
        /// The connection string for writing to SQL
        /// </summary>
        public string WriteConnectionString { get; }

        /// <summary>
        /// The connection string for reading from SQL. Has ApplicationIntent set to ReadOnly so that read only replicas will be used.
        /// </summary>
        public string ReadConnectionString { get; }

        /// <summary>
        /// SQL for retrieving events
        /// </summary>
        public string GetSql { get; set; } = "[dbo].[GetEventsForAggregate]";

        /// <summary>
        /// SQL for saving events
        /// </summary>
        public string SaveSql { get; set; } = "[dbo].[SaveEventForAggregate]";

        /// <summary>
        /// The CommandType of the SQL calls. Defaults to StoredProcedure
        /// </summary>
        public CommandType CommandType { get; set; } = CommandType.StoredProcedure;
    }
}
