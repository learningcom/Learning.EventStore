using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Learning.EventStore.Common;
using Learning.EventStore.Common.Redis;
using Learning.EventStore.Domain.Exceptions;
using RedLockNet;

namespace Learning.EventStore.Domain
{
    public class Session : ISession, IDisposable
    {
        private readonly IRepository _repository;
        private readonly Dictionary<string, AggregateDescriptor> _trackedAggregates;
        private readonly IDistributedLockFactory _distributedLockFactory;
        private readonly List<IRedLock> _distributedLocks = new List<IRedLock>();
        private readonly EventStoreSettings _eventStoreSettings;

        public Session(IRepository repository, EventStoreSettings eventStoreSettings, IDistributedLockFactory distributedLockFactory)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }

            _repository = repository;
            _trackedAggregates = new Dictionary<string, AggregateDescriptor>();
            _eventStoreSettings = eventStoreSettings;
            if(eventStoreSettings.SessionLockEnabled) 
            {
                _distributedLockFactory = distributedLockFactory ?? throw new ArgumentNullException(nameof(distributedLockFactory));
            }
        }

        public void Add<T>(T aggregate) where T : AggregateRoot
        {
            if (!IsTracked(aggregate.Id))
            {
                if(_eventStoreSettings.SessionLockEnabled)
                {
                    var distributedLock = _distributedLockFactory.CreateLock(
                        aggregate.Id,
                        TimeSpan.FromSeconds(_eventStoreSettings.SessionLockExpirySeconds),
                        TimeSpan.FromSeconds(_eventStoreSettings.SessionLockWaitSeconds),
                        TimeSpan.FromMilliseconds(_eventStoreSettings.SessionLockRetryMilliseconds));

                    if(distributedLock.IsAcquired)
                    {
                        _distributedLocks.Add(distributedLock);
                    }
                    else
                    {
                        throw new DistributedLockException(aggregate.Id, _eventStoreSettings);
                    }
                }
                _trackedAggregates.Add(aggregate.Id, new AggregateDescriptor { Aggregate = aggregate, Version = aggregate.Version });
            }
            else if (_trackedAggregates[aggregate.Id].Aggregate != aggregate)
            {
                throw new ConcurrencyException(aggregate.Id);
            }
        }

        public async Task<T> GetAsync<T>(string id, int? expectedVersion = null) where T : AggregateRoot
        {
            if (IsTracked(id))
            {
                var trackedAggregate = (T)_trackedAggregates[id].Aggregate;
                if (expectedVersion != null && trackedAggregate.Version != expectedVersion)
                {
                    throw new ConcurrencyException(trackedAggregate.Id);
                }
                return trackedAggregate;
            }

            T aggregate = null;
            if(_eventStoreSettings.SessionLockEnabled)
            {
                using (var distributedLock = _distributedLockFactory.CreateLock(
                        id,
                        TimeSpan.FromSeconds(_eventStoreSettings.SessionLockExpirySeconds),
                        TimeSpan.FromSeconds(_eventStoreSettings.SessionLockWaitSeconds),
                        TimeSpan.FromMilliseconds(_eventStoreSettings.SessionLockRetryMilliseconds)))
                {
                    if (distributedLock.IsAcquired)
                    {
                        aggregate = await _repository.GetAsync<T>(id).ConfigureAwait(false);
                    }
                    else
                    {
                        throw new DistributedLockException(aggregate.Id, _eventStoreSettings);
                    }
                }
            }
            else
            {
                aggregate = await _repository.GetAsync<T>(id).ConfigureAwait(false);
            }
            
            
            if (aggregate == null)
            {
                return null;
            }

            if (expectedVersion != null && aggregate.Version != expectedVersion)
            {
                throw new ConcurrencyException(id);
            }
            
            Add(aggregate);

            return aggregate;
        }

        private bool IsTracked(string id)
        {
            return _trackedAggregates.ContainsKey(id);
        }

        public async Task CommitAsync()
        {
            foreach (var descriptor in _trackedAggregates.Values)
            {
                try
                {
                    await _repository.SaveAsync(descriptor.Aggregate, descriptor.Version).ConfigureAwait(false);
                }
                catch (ConcurrencyException)
                {
                    _trackedAggregates.Remove(descriptor.Aggregate.Id);
                    throw;
                }
                finally
                {
                    var distributedLock = _distributedLocks.FirstOrDefault(x => x.LockId == descriptor.Aggregate.Id);
                    if (distributedLock != null)
                    {
                        distributedLock.Dispose();
                    }
                }
            }
            _trackedAggregates.Clear();
        }

        public void Dispose()
        {
            foreach (var distributedLock in _distributedLocks)
            {
                distributedLock.Dispose();
            }
        }
    }
}
