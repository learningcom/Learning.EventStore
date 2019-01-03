using System;
using System.Threading.Tasks;

namespace Learning.EventStore.Domain
{
    public interface ISession
    {
        void Add<T>(T aggregate) where T : AggregateRoot;
        Task AddAsync<T>(T aggregate) where T : AggregateRoot;
        Task<T> GetAsync<T>(string id, int? expectedVersion = null) where T : AggregateRoot;
        Task CommitAsync();
    }
}
