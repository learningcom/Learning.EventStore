using System;
using Learning.EventStore.Messages;

namespace Learning.EventStore
{
    public interface IHandlerRegistrar
    {
        void RegisterHandler<T>(Action<T> handler) where T : IMessage;
    }
}
