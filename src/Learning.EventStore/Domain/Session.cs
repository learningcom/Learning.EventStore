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
    public class Session : ISession
    {
        private readonly IRepository _repository;
        private readonly Dictionary<string, AggregateDescriptor> _trackedAggregates;
        private readonly ILog _logger;

        public Session(IRepository repository)
        {
            _repository = repository;
            _trackedAggregates = new Dictionary<string, AggregateDescriptor>();
            _logger = LogProvider.GetCurrentClassLogger();
        }


        public void Add<T>(T aggregate) where T : AggregateRoot
        {
            AddAsync(aggregate).GetAwaiter().GetResult();
        }
        
        public Task AddAsync<T>(T aggregate) where T : AggregateRoot
        {
            if (!IsTracked(aggregate.Id))
            {
                _trackedAggregates.Add(aggregate.Id, new AggregateDescriptor { Aggregate = aggregate, Version = aggregate.Version });
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
                var tasks = new Task[_trackedAggregates.Count];
                var i = 0;
                foreach (var descriptor in _trackedAggregates.Values)
                {
                    tasks[i] = _repository.SaveAsync(descriptor.Aggregate, descriptor.Version);
                    i++;
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
