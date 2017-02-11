using System;
using System.Threading.Tasks;

namespace Learning.EventStore.Domain
{
    public interface ISession
    {
        void Add<T>(T aggregate) where T : AggregateRoot;
        Task<T> Get<T>(Guid id, int? expectedVersion = null) where T : AggregateRoot;
        Task Commit();
    }
}
