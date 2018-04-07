using System.Threading.Tasks;

namespace Learning.Cqrs
{
    /// <summary>
    /// Interface for implementing async commands
    /// </summary>
    /// <typeparam name="TCommand"></typeparam>
    public interface IAsyncCommandHandler<in TCommand>
        where TCommand : ICommand
    {
        /// <summary>
        /// Handle the command
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        Task Handle(TCommand command);
    }

    public interface IAsyncCommandHandler<in TCommand, TResult>
        where TCommand : ICommand<TResult>
    {
        /// <summary>
        /// Handle the command
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        Task<TResult> Handle(TCommand command);
    }
}
