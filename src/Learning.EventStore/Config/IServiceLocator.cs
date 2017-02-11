using System;

namespace Learning.EventStore.Config
{
    public interface IServiceLocator
    {
        T GetService<T>();
        object GetService(Type type);
    }
}
