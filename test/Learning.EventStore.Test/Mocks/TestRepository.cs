using System;
using System.Threading.Tasks;
using Learning.EventStore.Domain;

namespace Learning.EventStore.Test.Mocks
{
    public class TestRepository : IRepository
    {
        public Task Save<T>(T aggregate, int? expectedVersion = null) where T : AggregateRoot
        {
            return Task.Run(() =>
            {
                Saved = aggregate;
                if (expectedVersion == 100)
                {
                    throw new Exception();
                }
            });
        }

        public AggregateRoot Saved { get; private set; }

        public Task<T> Get<T>(Guid aggregateId) where T : AggregateRoot
        {
            return Task.Run(() =>
            {
                var obj = (T) Activator.CreateInstance(typeof(T), true);
                obj.LoadFromHistory(new[] {new TestAggregateDidSomething {Id = aggregateId, Version = 1}});
                return obj;
            });

        }
    }
}
