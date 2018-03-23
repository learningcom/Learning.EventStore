using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace Learning.EventStore.Common.Sql
{
    public class DapperWrapper : IDapperWrapper
    {
        public async Task ExecuteAsync(IDbConnection connection, string sql, object param, CommandType commandType,
            IDbTransaction transaction)
        {
            await connection.ExecuteAsync(sql, param, commandType: commandType, transaction: transaction).ConfigureAwait(false);
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(IDbConnection connection, string sql, object param, CommandType commandType)
        {
            var result = await connection.QueryAsync<T>(sql,param, commandType: commandType).ConfigureAwait(false);
            return result;
        }
    }
}
