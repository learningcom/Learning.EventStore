using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Learning.Cqrs
{
    /// <summary>
    /// CQRS Hub interface
    /// </summary>
    public interface IHub
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        bool Command(ICommand command);

        TResult Command<TResult>(ICommand<TResult> command);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="query"></param>
        /// <returns></returns>
        TResult Query<TResult>(IQuery<TResult> query);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        Task CommandAsync(ICommand command);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        Task<TResult> CommandAsync<TResult>(ICommand<TResult> command);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="query"></param>
        /// <returns></returns>
        Task<TResult> QueryAsync<TResult>(IQuery<TResult> query);
    }
}
