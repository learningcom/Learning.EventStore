using System.Data;

namespace Learning.EventStore.Common.Sql
{
    public interface ISqlConnectionFactory
    {
        IDbConnection GetReadConnection();
        IDbConnection GetWriteConnection();
    }
}