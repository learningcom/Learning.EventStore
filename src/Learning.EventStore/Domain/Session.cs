using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Learning.EventStore.Common;
using Learning.EventStore.Common.Exceptions;
using Learning.EventStore.Domain.Exceptions;
using Learning.EventStore.Logging;
using RedLockNet;

namespace Learning.EventStore.Domain
{
    public class Session : ISession, IDisposable
    {
        private readonly IRepository _repository;
        private readonly Dictionary<string, AggregateDescriptor> _trackedAggregates;
        private readonly IDistributedLockFactory _distributedLockFactory;
        private readonly List<IRedLock> _distributedLocks = new List<IRedLock>();
        private readonly DistributedLockSettings _distributedLockSettings;
        private readonly bool _sessionLockEnabled;
        private readonly ILog _logger;

        public Session(IRepository repository)
            : this(repository, null, false, null)
        {
        }

        public Session(IRepository repository, IDistributedLockFactory distributedLockFactory, bool sessionLockEnabled)
            : this(repository, distributedLockFactory, sessionLockEnabled, new DistributedLockSettings())
        {
        }

        public Session(IRepository repository, IDistributedLockFactory distributedLockFactory, bool sessionLockEnabled, DistributedLockSettings distributedLockSettings)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _trackedAggregates = new Dictionary<string, AggregateDescriptor>();
            _sessionLockEnabled = sessionLockEnabled;
            _logger = LogProvider.GetCurrentClassLogger();

            if(sessionLockEnabled)
            {
                _distributedLockSettings = distributedLockSettings ?? throw new ArgumentException(nameof(distributedLockSettings));
                _distributedLockFactory = distributedLockFactory ?? throw new ArgumentNullException(nameof(distributedLockFactory));
            }
        }

        public void Add<T>(T aggregate) where T : AggregateRoot
        {
            AddAsync(aggregate).GetAwaiter().GetResult();
        }
        
        public async Task AddAsync<T>(T aggregate) where T : AggregateRoot
        {
            if (!IsTracked(aggregate.Id))
            {
                await GetLock(aggregate.Id).ConfigureAwait(false);
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

            await GetLock(id).ConfigureAwait(false);

            var aggregate = await _repository.GetAsync<T>(id).ConfigureAwait(false);
             
            if (aggregate == null)
            {
                return null;
            }

            if (expectedVersion != null && aggregate.Version != expectedVersion)
            {
                throw new ConcurrencyException(id);
            }
            
            await AddAsync(aggregate).ConfigureAwait(false);

            return aggregate;
        }

        public async Task CommitAsync()
        {
            ValidateLockState();

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
                    if (_sessionLockEnabled)
                    {
                        var distributedLock = _distributedLocks.FirstOrDefault(x => x.Resource == descriptor.Aggregate.Id);
                        distributedLock?.Dispose();
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

        private void ValidateLockState()
        {
            if (_sessionLockEnabled)
            {
                foreach (var descriptor in _trackedAggregates.Values)
                {
                    var distributedLock = _distributedLocks.FirstOrDefault(x => x.Resource == descriptor.Aggregate.Id);

                    if (distributedLock == null)
                    {
                        throw new DistributedLockException($"No lock found for aggregate '{descriptor.Aggregate.Id}. Aborting session commit.");
                    }

                    if (distributedLock.Status == RedLockStatus.Expired)
                    {
                        throw new DistributedLockException($"Session lock expired for aggregate '{descriptor.Aggregate.Id} after {_distributedLockSettings.ExpirySeconds} seconds. Aborting session commit.");
                    }
                }
            }
        }

        private async Task GetLock(string aggregateId)
        {
            if (_sessionLockEnabled)
            {
                _logger.Debug($"Session lock enabled. Attempting to get lock for Aggregate {aggregateId}...");
                var existingLock = _distributedLocks.FirstOrDefault(x => x.Resource == aggregateId);

                if  (existingLock != null)
                {
                    if (existingLock.Status == RedLockStatus.Expired)
                    {
                        _distributedLocks.Remove(existingLock);
                        existingLock.Dispose();
                        throw new DistributedLockException($"Existing session lock expired for aggregate '{aggregateId} after {_distributedLockSettings.ExpirySeconds} seconds.");
                    }
                }
                else
                {
                    var distributedLock = await _distributedLockFactory.CreateLockAsync(
                        aggregateId,
                        TimeSpan.FromSeconds(_distributedLockSettings.ExpirySeconds),
                        TimeSpan.FromSeconds(_distributedLockSettings.WaitSeconds),
                        TimeSpan.FromMilliseconds(_distributedLockSettings.RetryMilliseconds))
                        .ConfigureAwait(false);

                    if(distributedLock.IsAcquired)
                    {
                        _logger.Debug($"Session lock acquired for aggregate {aggregateId}; LockId: {distributedLock.LockId};");
                        _distributedLocks.Add(distributedLock);
                    }
                    else
                    {
                        throw new DistributedLockException(distributedLock, _distributedLockSettings);
                    }
                }
            }
        }

        private bool IsTracked(string id)
        {
            return _trackedAggregates.ContainsKey(id);
        }

    }
}
