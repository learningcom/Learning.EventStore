using System.Threading.Tasks;

namespace Learning.Cqrs
{
    /// <summary>
    /// Interface for implementing async query handlers
    /// </summary>
    /// <typeparam name="TQuery"></typeparam>
    /// <typeparam name="TReturn"></typeparam>
    public interface IAsyncQueryHandler<in TQuery, TReturn>
        where TQuery : IQuery<TReturn>
    {
        Task<TReturn> Handle(TQuery query);
    }
}
