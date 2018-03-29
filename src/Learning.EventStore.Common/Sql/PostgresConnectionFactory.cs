using System.Data;
using Npgsql;

namespace Learning.EventStore.Common.Sql
{
    public class PostgresConnectionFactory : ISqlConnectionFactory
    {
        private readonly string _readConnectionString;
        private readonly string _writeConnectionString;

        public PostgresConnectionFactory(PostgresEventStoreSettings settings)
        {
            _readConnectionString = settings.ReadConnectionString;
            _writeConnectionString = settings.WriteConnectionString;
        }

        public IDbConnection GetReadConnection()
        {
            var conn = new NpgsqlConnection(_readConnectionString);
            return conn;
        }

        public IDbConnection GetWriteConnection()
        {
            var conn = new NpgsqlConnection(_writeConnectionString);
            return conn;
        }
    }
}
