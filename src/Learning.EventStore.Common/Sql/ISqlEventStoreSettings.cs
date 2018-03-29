using System.Data;

namespace Learning.EventStore.Common.Sql
{
    public interface ISqlEventStoreSettings
    {
        string ApplicationName { get; }

        string WriteConnectionString { get; }

        string ReadConnectionString { get; }

        string GetSql { get; set; }

        string SaveSql { get; set; }

        CommandType CommandType { get; set; }
    }
}
