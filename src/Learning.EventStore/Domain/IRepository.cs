using System;
using System.Threading.Tasks;

namespace Learning.EventStore.Domain
{
    public interface IRepository
    {
        Task Save<T>(T aggregate, int? expectedVersion = null) where T : AggregateRoot;
        Task<T> Get<T>(string aggregateId) where T : AggregateRoot;
    }
}