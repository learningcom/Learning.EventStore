namespace Learning.Cqrs
{
    public interface ICommandHandler<in TCommand>
        where TCommand : ICommand
    {
        bool Handle(TCommand command);
    }

    public interface ICommandHandler<in TCommand, out TResult>
        where TCommand : ICommand<TResult>
    {
        TResult Handle(TCommand command);
    }
}
