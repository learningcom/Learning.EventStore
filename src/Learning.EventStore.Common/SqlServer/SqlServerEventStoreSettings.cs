using System.Data;
using System.Data.SqlClient;

namespace Learning.EventStore.Common.SqlServer
{
    public class SqlServerEventStoreSettings
    {
        public SqlServerEventStoreSettings(SqlConnectionStringBuilder connectionStringBuilder)
        {
            WriteConnectionString = connectionStringBuilder.ConnectionString;
            connectionStringBuilder.ApplicationIntent = ApplicationIntent.ReadOnly;
            ReadConnectionString = connectionStringBuilder.ConnectionString;
        }

        public string WriteConnectionString { get; }
        public string ReadConnectionString { get; }
        public string GetSql { get; set; } = "[dbo].[GetEventsForAggregate]";
        public string SaveSql { get; set; } = "[dbo].[SaveEventForAggregate]";
        public string DeleteSql { get; set; } = "[dbo].[DeleteEvent]";
        public CommandType CommandType { get; set; } = CommandType.StoredProcedure;
        public bool EnableCompression { get; set; } = false;
        public int CompressionThreshold { get; set; } = 1000;
    }
}
