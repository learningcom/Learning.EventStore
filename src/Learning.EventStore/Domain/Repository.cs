using System;
using System.Linq;
using System.Threading.Tasks;
using Learning.EventStore.DataStores;
using Learning.EventStore.Domain.Exceptions;

namespace Learning.EventStore.Domain
{
    public class Repository : IRepository
    {
        private readonly IEventStore _eventStore;

        public Repository(IEventStore eventStore)
        {
            _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        }

        public async Task SaveAsync<T>(T aggregate, int? expectedVersion = null) where T : AggregateRoot
        {
            var aggregateType = aggregate.GetType().Name;
            if (expectedVersion != null && (await _eventStore.GetAsync(aggregate.Id, aggregateType, expectedVersion.Value).ConfigureAwait(false)).Any())
            {
                throw new ConcurrencyException(aggregate.Id);
            }

            var changes = aggregate.FlushUncommitedChanges();
            await _eventStore.SaveAsync(changes).ConfigureAwait(false);
        }

        public async Task<T> GetAsync<T>(string aggregateId) where T : AggregateRoot
        {
            return await LoadAggregateAsync<T>(aggregateId).ConfigureAwait(false); ;
        }

        private async Task<T> LoadAggregateAsync<T>(string id) where T : AggregateRoot
        {
            var aggregateType = typeof(T).Name;
            var events = await _eventStore.GetAsync(id, aggregateType, -1).ConfigureAwait(false);
            if (!events.Any())
            {
                return default(T);
            }

            var aggregate = AggregateFactory.CreateAggregate<T>();
            aggregate.LoadFromHistory(events);
            return aggregate;
        }
    }
}
