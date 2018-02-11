using System;
using System.Reflection;
using System.Threading.Tasks;
using Learning.EventStore.Domain;

namespace Learning.EventStore.Snapshotting
{
    /// <inheritdoc />
    /// <summary>
    /// Default implementaion of snapshot strategy interface/
    /// Snapshots aggregates of type SnapshotAggregateRoot every 100th event.
    /// </summary>
    public class DefaultSnapshotStrategy : ISnapshotStrategy
    {
        private readonly int _snapshotInterval;
        private readonly ISnapshotStore _snapshotStore;

        public DefaultSnapshotStrategy(ISnapshotStore snapshotStore, int snapshotInterval = 100)
        {
            _snapshotInterval = snapshotInterval;
            _snapshotStore = snapshotStore;
        }

        public bool IsSnapshotable(Type aggregateType)
        {
            while (true)
            {
                if (aggregateType.GetTypeInfo().BaseType == null)
                {
                    return false;
                }
                    
                if (aggregateType.GetTypeInfo().BaseType.GetTypeInfo().IsGenericType && aggregateType.GetTypeInfo().BaseType.GetGenericTypeDefinition() == typeof(SnapshotAggregateRoot<>))
                {
                    return true;
                }

                aggregateType = aggregateType.GetTypeInfo().BaseType;
            }
        }

        public async Task<bool> ShouldMakeSnapShot(AggregateRoot aggregate)
        {
            if (!IsSnapshotable(aggregate.GetType()))
            {
                return false;
            }
                
            var i = aggregate.Version;
            for (var j = 0; j < aggregate.GetUncommittedChanges().Length; j++)
            {
                if (i != 0 && ++i % _snapshotInterval == 0)
                {
                    return true;
                }

                if (i >= _snapshotInterval && !await _snapshotStore.ExistsAsync(aggregate.Id).ConfigureAwait(false))
                {
                    return true;
                }
            }
                
            return false;
        }
    }
}
