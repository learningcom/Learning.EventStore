using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace Learning.EventStore.Common.Sql
{
    public interface IDapperWrapper
    {
        Task ExecuteAsync(IDbConnection connection, string sql, object param, CommandType commandType, IDbTransaction transaction);

        Task<IEnumerable<T>> QueryAsync<T>(IDbConnection connection, string sql, object param, CommandType commandType);
    }
}
