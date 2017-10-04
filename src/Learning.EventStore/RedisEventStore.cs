using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Learning.EventStore.Extensions;
using Learning.EventStore.Infrastructure;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Learning.EventStore
{
    public class RedisEventStore : IEventStore
    {
        private readonly IRedisClient _redis;
        private readonly IEventPublisher _publisher;
        private readonly EventStoreSettings _settings;
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

        public RedisEventStore(IRedisClient redis, IEventPublisher publisher, string keyPrefix)
            : this(redis, publisher, new EventStoreSettings {KeyPrefix = keyPrefix})
        {
        }

        public RedisEventStore(IRedisClient redis, IEventPublisher publisher, EventStoreSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.KeyPrefix))
            {
                throw new ArgumentException("KeyPrefix must be specified in EventStoreSettings");
            }

            _redis = redis;
            _publisher = publisher;
            _settings = settings;
        }

        public async Task<IEnumerable<IEvent>> GetAsync(string aggregateId, int fromVersion)
        {
            //Get all the commits for the aggregateId
            var listLength = await _redis.ListLengthAsync($"{{EventStore:{_settings.KeyPrefix}}}:{aggregateId}").ConfigureAwait(false);
            var commits = await _redis.ListRangeAsync($"{{EventStore:{_settings.KeyPrefix}}}:{aggregateId}", 0, listLength).ConfigureAwait(false);

            //Get the event data associated with each commit
            var commitList = new List<string>();
            foreach (var commit in commits)
            {
                var hashSetTask = _redis.HashGetAsync($"EventStore:{_settings.KeyPrefix}", commit).ConfigureAwait(false);
                commitList.Add(await hashSetTask);
            }

            //Get the events that have happened since specified fromVersion
            var events = commitList.Select(serializedEvent => JsonConvert.DeserializeObject<IEvent>(serializedEvent.Decompress(), JsonSerializerSettings))
                                   .Where(x => x.Version > fromVersion)
                                   .ToList();
            return events;
        }

        public async Task SaveAsync(IEnumerable<IEvent> events)
        {
            var hashKey = $"EventStore:{_settings.KeyPrefix}";

            foreach (var @event in events)
            {
                var serializedEvent = JsonConvert.SerializeObject(@event, JsonSerializerSettings);
                var eventData = _settings.EnableCompression
                    ? serializedEvent.Compress(_settings.CompressionThreshold)
                    : serializedEvent;

                for (var i = 0; i < _settings.SaveRetryCount; i++)
                {
                    //Create a Redis transaction
                    var tran = _redis.Database.CreateTransaction();

                    var counterKey = $"{hashKey}:Counter";
                    var counterValue = await _redis.StringGetAsync(counterKey).ConfigureAwait(false);
                    Task counterSetTask = null;
                    long commitId;

                    //this if/else statement is here to transition from using the HashLength to derive the commit ID to using a Redis incrementer. It will be removed in a subsequent version.
                    if (string.IsNullOrWhiteSpace(counterValue))
                    {
                        //Increment the commitId
                        commitId = await _redis.HashLengthAsync(hashKey).ConfigureAwait(false) + 1;
                        //Ensure that the hash length has not been changed by another thread between now and when the transaction is committed to avoid commitId collisions
                        tran.AddCondition(Condition.HashLengthEqual(hashKey, commitId));
                        counterSetTask = tran.StringSetAsync(counterKey, commitId);
                    }
                    else
                    {
                        var currentCommit = await _redis.StringGetAsync(counterKey).ConfigureAwait(false);
                        commitId = await _redis.StringIncrementAsync($"{hashKey}:Counter").ConfigureAwait(false);
                        //ensure the counter hasn't been changed by another instance between now and when the transaction is committed to avoid commitId collisions
                        tran.AddCondition(Condition.StringEqual(counterKey, currentCommit));
                    }

                    //Write event data to a field named {commitId} in EventStore hash. Allows for fast lookup O(1) of individual events
                    var hashSetTask = tran.HashSetAsync(hashKey, commitId, eventData).ConfigureAwait(false);

                    //Write the commitId to a list mapping commitIds to individual events for a particular aggregate (@event.Id)
                    var listPushTask = tran.ListRightPushAsync($"{{EventStore:{_settings.KeyPrefix}}}:{@event.Id}", commitId.ToString()).ConfigureAwait(false);

                    //Publish the event
                    var publishTask = _publisher.Publish(@event).ConfigureAwait(false);

                    //Execute the transaction
                    if (await tran.ExecuteAsync())
                    {
                        await hashSetTask;
                        await listPushTask;
                        await publishTask;
                        if (counterSetTask != null)
                        {
                            await counterSetTask;
                        }
                        return;
                    }
                }

                throw new InvalidOperationException(string.Format($"Failed to save value in key {hashKey}"));
            }
        }
    }
}
    