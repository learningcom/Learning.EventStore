using System;
using System.Threading.Tasks;
using Learning.EventStore.Common;
using Learning.EventStore.Common.Redis;
using Learning.EventStore.Extensions;
using Newtonsoft.Json;

namespace Learning.EventStore.Snapshotting
{
    public class RedisSnapshotStore : ISnapshotStore
    {
        private readonly IRedisClient _redis;
        private readonly SnapshotStoreSettings _settings;
        private readonly string _hashKeyBase;
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

        public RedisSnapshotStore(IRedisClient redis, string applicationName)
            : this(redis, new SnapshotStoreSettings { ApplicationName = applicationName })
        {
        }

        public RedisSnapshotStore(IRedisClient redis, SnapshotStoreSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.ApplicationName))
            {
                throw new ArgumentException("ApplicationName must be specified in EventStoreSettings");
            }

            _redis = redis;
            _settings = settings;
            _hashKeyBase = $"Snapshots:{_settings.ApplicationName}";
        }

        public async Task<bool> ExistsAsync(string id)
        {
            var partition = id.CalculatePartition();
            var exists = await _redis.HashExistsAsync($"{_hashKeyBase}:{partition}", id).ConfigureAwait(false);

            return exists;
        }

        public async Task<Snapshot> GetAsync(string id)
        {
            Snapshot snapshot = null;
            var partition = id.CalculatePartition();
            var serializedSnapshot = await _redis.HashGetAsync($"{_hashKeyBase}:{partition}", id).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(serializedSnapshot))
            {
                snapshot = JsonConvert.DeserializeObject<Snapshot>(serializedSnapshot.ToString().Decompress(), JsonSerializerSettings);
            }

            return snapshot;
        }

        public async Task SaveAsync(Snapshot snapshot)
        {
            var partition = snapshot.Id.CalculatePartition();
            var hashKey = $"{_hashKeyBase}:{partition}";

            var serializedSnapshot = JsonConvert.SerializeObject(snapshot, JsonSerializerSettings);
            var snapshotData = _settings.EnableCompression
                ? serializedSnapshot.Compress(_settings.CompressionThreshold)
                : serializedSnapshot;

            await _redis.HashSetAsync(hashKey, snapshot.Id, snapshotData).ConfigureAwait(false);
        }
    }
}
