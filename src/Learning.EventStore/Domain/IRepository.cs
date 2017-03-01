using System;
using System.Threading.Tasks;

namespace Learning.EventStore.Domain
{
    public interface IRepository
    {
        Task SaveAsync<T>(T aggregate, int? expectedVersion = null) where T : AggregateRoot;
        Task<T> GetAsync<T>(string aggregateId) where T : AggregateRoot;
    }
}