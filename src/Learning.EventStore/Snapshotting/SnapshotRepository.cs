using System;
using System.Linq;
using System.Threading.Tasks;
using Learning.EventStore.DataStores;
using Learning.EventStore.Domain;
using Learning.EventStore.Infrastructure;

namespace Learning.EventStore.Snapshotting
{
    /// <summary>
    /// Repository decorator that can snapshot aggregates.
    /// </summary>
    public class SnapshotRepository : IRepository
    {
        private readonly ISnapshotStore _snapshotStore;
        private readonly ISnapshotStrategy _snapshotStrategy;
        private readonly IRepository _repository;
        private readonly IEventStore _eventStore;

        /// <summary>
        /// Initialize a new instance of SnapshotRepository
        /// </summary>
        /// <param name="snapshotStore">ISnapshotStore snapshots should be saved to and fetched from</param>
        /// <param name="snapshotStrategy">ISnapshotStrategy on when to take and if to restore from snapshot</param>
        /// <param name="repository">Repository that gets aggregate from event store</param>
        /// <param name="eventStore">Event store where events after snapshot can be fetched from</param>
        public SnapshotRepository(ISnapshotStore snapshotStore, ISnapshotStrategy snapshotStrategy, IRepository repository, IEventStore eventStore)
        {
            _snapshotStore = snapshotStore ?? throw new ArgumentNullException(nameof(snapshotStore));
            _snapshotStrategy = snapshotStrategy ?? throw new ArgumentNullException(nameof(snapshotStrategy));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        }

        public async Task SaveAsync<T>(T aggregate, int? expectedVersion = null) where T : AggregateRoot
        {
            await Task.WhenAll(TryMakeSnapshot(aggregate), _repository.SaveAsync(aggregate, expectedVersion)).ConfigureAwait(false);
        }

        public async Task<T> GetAsync<T>(string aggregateId) where T : AggregateRoot
        {
            var aggregate = AggregateFactory.CreateAggregate<T>();
            var snapshotVersion = await TryRestoreAggregateFromSnapshot(aggregateId, aggregate).ConfigureAwait(false);

            if (snapshotVersion == -1)
            {
                return await _repository.GetAsync<T>(aggregateId).ConfigureAwait(false);
            }

            var aggregateType = typeof(T).Name;
            var events = (await _eventStore.GetAsync(aggregateId, aggregateType, snapshotVersion).ConfigureAwait(false)).Where(desc => desc.Version > snapshotVersion);
            aggregate.LoadFromHistory(events);

            return aggregate;
        }

        private async Task<int> TryRestoreAggregateFromSnapshot<T>(string id, T aggregate) where T : AggregateRoot
        {
            if (!_snapshotStrategy.IsSnapshotable(typeof(T)))
            {
                return -1;
            }
                
            var snapshot = await _snapshotStore.GetAsync(id).ConfigureAwait(false);
            if (snapshot == null)
            {
                return -1;
            }
               
            aggregate.Invoke("Restore", snapshot);
            return snapshot.Version;
        }

        private async Task TryMakeSnapshot(AggregateRoot aggregate)
        {
            if (!await _snapshotStrategy.ShouldMakeSnapShot(aggregate).ConfigureAwait(false))
            {
                return;
            }
                
            dynamic snapshot = aggregate.Invoke("GetSnapshot");
            snapshot.Version = aggregate.Version + aggregate.GetUncommittedChanges().Length;
            await _snapshotStore.SaveAsync(snapshot).ConfigureAwait(false);
        }
    }
}
