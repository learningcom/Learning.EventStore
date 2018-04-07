using System;
using System.Threading.Tasks;

namespace Learning.Cqrs
{
    public class Hub : IHub
    {
        private readonly Func<Type, object> _activator;

        public Hub(Func<Type, object> activator)
        {
            _activator = activator;
        }

        public bool Command(ICommand command)
        {
            var handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());
            dynamic handler = _activator(handlerType);
            ValidateHandlerExists(handler, handlerType, false);

            return handler.Handle(command as dynamic);
        }

        public TResult Command<TResult>(ICommand<TResult> command)
        {
            var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
            dynamic handler = _activator(handlerType);
            ValidateHandlerExists(handler, handlerType, false);

            return (TResult)handler.Handle(command as dynamic);
        }

        public TResult Query<TResult>(IQuery<TResult> query)
        {
            var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));
            dynamic handler = _activator(handlerType);
            ValidateHandlerExists(handler, handlerType, false);

            return (TResult)handler.Handle(query as dynamic);
        }

        public async Task CommandAsync(ICommand command)
        {
            var handlerType = typeof(IAsyncCommandHandler<>).MakeGenericType(command.GetType());
            dynamic handler = _activator(handlerType);
            ValidateHandlerExists(handler, handlerType, true);

            await ((Task)handler.Handle(command as dynamic)).ConfigureAwait(false);
        }

        public async Task<TResult> CommandAsync<TResult>(ICommand<TResult> command)
        {
            var handlerType = typeof(IAsyncCommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
            dynamic handler = _activator(handlerType);
            ValidateHandlerExists(handler, handlerType, true);

            return await ((Task<TResult>)handler.Handle(command as dynamic)).ConfigureAwait(false);
        }

        public async Task<TResult> QueryAsync<TResult>(IQuery<TResult> query)
        {
            var handlerType = typeof(IAsyncQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));
            dynamic handler = _activator(handlerType);
            ValidateHandlerExists(handler, handlerType, true);

            return await ((Task<TResult>)handler.Handle(query as dynamic)).ConfigureAwait(false);
        }

        private static void ValidateHandlerExists(dynamic handler, Type handlerType, bool async)
        {
            if (handler == null)
            {
                throw new CqrsException($"{(async ? "Async" : "Non-Async")} handler was not found for type {handlerType.GenericTypeArguments[0].Name}");
            }
        }
    }
}

