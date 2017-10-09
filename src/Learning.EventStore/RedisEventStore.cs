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
            var commits = await _redis.ListRangeAsync($"{{EventStore:{_settings.KeyPrefix}}}:{aggregateId}", 0, -1).ConfigureAwait(false);

            //Get the event data associated with each commit
            var eventTasks = commits.Select(commit =>
            {
                var hashGetTask = _redis.HashGetAsync($"EventStore:{_settings.KeyPrefix}", commit);
                return hashGetTask;
            });
            var commitList = await Task.WhenAll(eventTasks).ConfigureAwait(false);

            //Get the events that have happened since specified fromVersion
            var events = commitList.Select(serializedEvent => JsonConvert.DeserializeObject<IEvent>(serializedEvent.ToString().Decompress(), JsonSerializerSettings))
                                   .Where(x => x.Version > fromVersion)
                                   .OrderBy(x => x.Version)
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

                for (var i = 0; i < _settings.TransactionRetryCount; i++)
                {
                    //Create a Redis transaction
                    var tran = _redis.Database.CreateTransaction();

                    //Increment the commitId
                    var commitId = Guid.NewGuid().ToString();

                    //Write event data to a field named {commitId} in EventStore hash. Allows for fast lookup O(1) of individual events
                    tran.HashSetAsync(hashKey, commitId, eventData).ConfigureAwait(false);

                    //Write the commitId to a list mapping commitIds to individual events for a particular aggregate (@event.Id)
                    tran.ListRightPushAsync($"{{EventStore:{_settings.KeyPrefix}}}:{@event.Id}", commitId).ConfigureAwait(false);

                    //Publish the event
                    var publishTask = _publisher.Publish(@event).ConfigureAwait(false);

                    //Execute the transaction
                    if (await tran.ExecuteAsync())
                    {
                        await publishTask;
                        return;
                    }

                    //Wait before retrying
                    await Task.Delay(_settings.TransactionRetryDelay); 
                }

                throw new InvalidOperationException(string.Format($"Failed to save value in key {hashKey} after retrying {_settings.TransactionRetryCount} times"));
            }
        }
    }
}
    