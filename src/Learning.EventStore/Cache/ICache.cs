using Learning.EventStore.Domain;

namespace Learning.EventStore.Cache
{
    public interface ICache
    {
        bool IsTracked(string id);
        void Set(string id, AggregateRoot aggregate);
        AggregateRoot Get(string id);
        void Remove(string id);
    }
}
