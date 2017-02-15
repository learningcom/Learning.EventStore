using System;
using System.Linq;
using System.Threading.Tasks;
using Learning.EventStore.Domain.Exceptions;

namespace Learning.EventStore.Domain
{
    public class Repository : IRepository
    {
        private readonly IEventStore _eventStore;

        public Repository(IEventStore eventStore)
        {
            if (eventStore == null)
            {
                throw new ArgumentNullException(nameof(eventStore));
            }

            _eventStore = eventStore;
        }

        public async Task Save<T>(T aggregate, int? expectedVersion = null) where T : AggregateRoot
        {
            if (expectedVersion != null && (await _eventStore.Get<T>(aggregate.Id, expectedVersion.Value).ConfigureAwait(false)).Any())
            {
                throw new ConcurrencyException(aggregate.Id);
            }

            var changes = aggregate.FlushUncommitedChanges();
            await _eventStore.Save<T>(changes).ConfigureAwait(false);
        }

        public async Task<T> Get<T>(Guid aggregateId) where T : AggregateRoot
        {
            return await LoadAggregate<T>(aggregateId).ConfigureAwait(false); ;
        }

        private async Task<T> LoadAggregate<T>(Guid id) where T : AggregateRoot
        {
            var events = await _eventStore.Get<T>(id, -1).ConfigureAwait(false); ;
            if (!events.Any())
            {
                throw new AggregateNotFoundException(typeof(T), id);
            }

            var aggregate = AggregateFactory.CreateAggregate<T>();
            aggregate.LoadFromHistory(events);
            return aggregate;
        }
    }
}
