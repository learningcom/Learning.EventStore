using System;
using System.Threading.Tasks;
using Learning.EventStore.Domain;

namespace Learning.EventStore.Cache
{
    public interface ICache
    {
        Task<bool> IsTracked(string id);
        Task Set(string id, AggregateRoot aggregate);
        Task<AggregateRoot> Get(string id);
        Task Remove(string id);
        void RegisterEvictionCallback(Action<string> action);
    }
}
