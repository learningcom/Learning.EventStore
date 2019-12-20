using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Learning.EventStore.DataStores;
using Learning.EventStore.Domain;

namespace Learning.EventStore.Cache
{
    public class CacheRepository : IRepository
    {
        private readonly IRepository _repository;
        private readonly IEventStore _eventStore;
        private readonly ICache _cache;
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new ConcurrentDictionary<string, SemaphoreSlim>();
        private static SemaphoreSlim CreateLock(string _) => new SemaphoreSlim(1, 1); 

        public CacheRepository(IRepository repository, IEventStore eventStore, ICache cache)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));

            _cache.RegisterEvictionCallback(key => Locks.TryRemove(key, out var _));
        }

        public async Task SaveAsync<T>(T aggregate, int? expectedVersion = null) where T : AggregateRoot
        {
            var @lock = Locks.GetOrAdd(aggregate.Id, CreateLock);
            await @lock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!string.IsNullOrEmpty(aggregate.Id) && !await _cache.IsTracked(aggregate.Id).ConfigureAwait(false))
                {
                    await _cache.Set(aggregate.Id, aggregate).ConfigureAwait(false);
                }
                await _repository.SaveAsync(aggregate, expectedVersion).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await _cache.Remove(aggregate.Id).ConfigureAwait(false);
                throw;
            }
            finally
            {
                @lock.Release();
            }
        }

        public async Task<T> GetAsync<T>(string aggregateId) where T : AggregateRoot
        {
            var @lock = Locks.GetOrAdd(aggregateId, CreateLock);
            await @lock.WaitAsync().ConfigureAwait(false);
            try
            {
                T aggregate;
                if (await _cache.IsTracked(aggregateId).ConfigureAwait(false))
                {
                    aggregate = (T) await _cache.Get(aggregateId).ConfigureAwait(false);;
                    var aggregateType = typeof(T).Name;
                    var events = await _eventStore.GetAsync(aggregateId, aggregateType, aggregate.Version).ConfigureAwait(false);

                    if (events.Any() && events.First().Version != aggregate.Version + 1)
                    {
                        await _cache.Remove(aggregateId).ConfigureAwait(false);
                    }
                    else
                    {
                        aggregate.LoadFromHistory(events);
                        return aggregate;
                    }
                }

                aggregate = await _repository.GetAsync<T>(aggregateId).ConfigureAwait(false);
                await _cache.Set(aggregateId, aggregate).ConfigureAwait(false);
                return aggregate;
            }
            catch (Exception)
            {
                await _cache.Remove(aggregateId).ConfigureAwait(false);
                throw;
            }
            finally
            {
                @lock.Release();
            }
        }
    }
}