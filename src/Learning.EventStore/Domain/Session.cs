using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Learning.EventStore.Domain.Exceptions;

namespace Learning.EventStore.Domain
{
    public class Session : ISession
    {
        private readonly IRepository _repository;
        private readonly ConcurrentDictionary<string, AggregateDescriptor> _trackedAggregates;

        public Session(IRepository repository)
        {
            _repository = repository;
            _trackedAggregates = new ConcurrentDictionary<string, AggregateDescriptor>();
        }

        public void Add<T>(T aggregate) where T : AggregateRoot
        {
            AddAsync(aggregate).GetAwaiter().GetResult();
        }
        
        public Task AddAsync<T>(T aggregate) where T : AggregateRoot
        {
            if (!IsTracked(aggregate.Id))
            {
                _trackedAggregates.TryAdd(aggregate.Id, new AggregateDescriptor { Aggregate = aggregate, Version = aggregate.Version });
            }
            else if (_trackedAggregates[aggregate.Id].Aggregate != aggregate)
            {
                throw new ConcurrencyException(aggregate.Id);
            }

            return Task.FromResult(0);
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
            try
            {
                var tasks = new List<Task>();

                while (_trackedAggregates.Count > 0)
                {
                    AggregateDescriptor descriptor = null;

                    var key = _trackedAggregates.Keys.FirstOrDefault();

                    if (key != null)
                    {
                        _trackedAggregates.TryRemove(key, out descriptor);
                    }

                    if (descriptor != null)
                    {
                        tasks.Add(_repository.SaveAsync(descriptor.Aggregate, descriptor.Version));
                    }
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            finally
            {
                _trackedAggregates.Clear();
            }
        }

        private bool IsTracked(string id)
        {
            return _trackedAggregates.ContainsKey(id);
        }

    }
}
