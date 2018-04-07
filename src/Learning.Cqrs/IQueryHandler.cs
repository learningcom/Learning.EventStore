namespace Learning.Cqrs
{
    /// <summary>
    /// Interface for implementing synchronous command handlers
    /// </summary>
    /// <typeparam name="TQuery"></typeparam>
    /// <typeparam name="TReturn"></typeparam>
    public interface IQueryHandler<in TQuery, out TReturn>
        where TQuery : IQuery<TReturn>
    {
        TReturn Handle(TQuery query);
    }
}
