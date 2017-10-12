using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Force.Crc32;
using Learning.EventStore.Extensions;
using Learning.EventStore.Infrastructure;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Learning.EventStore
{
    public class RedisEventStore : IEventStore
    {
        private readonly IRedisClient _redis;
        private readonly EventStoreSettings _settings;
        private readonly string _environment;
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

        public RedisEventStore(IRedisClient redis, string keyPrefix, string environment)
            : this(redis, new EventStoreSettings {KeyPrefix = keyPrefix}, environment)
        {
        }

        public RedisEventStore(IRedisClient redis, EventStoreSettings settings, string environment)
        {
            if (string.IsNullOrWhiteSpace(settings.KeyPrefix))
            {
                throw new ArgumentException("KeyPrefix must be specified in EventStoreSettings");
            }

            _redis = redis;
            _settings = settings;
            _environment = environment;
        }

        public async Task<IEnumerable<IEvent>> GetAsync(string aggregateId, int fromVersion)
        {
            //Get all the commits for the aggregateId
            var commits = await _redis.ListRangeAsync($"{{EventStore:{_settings.KeyPrefix}}}:{aggregateId}", 0, -1).ConfigureAwait(false);

            var commitList = new List<RedisValue>();
            foreach (var commit in commits)
            {
                var partition = CalculatePartition(commit);
                var hashKeyBase = $"EventStore:{_settings.KeyPrefix}";

                var partitionedValue = await _redis.HashGetAsync($"{hashKeyBase}:{partition}", commit).ConfigureAwait(false);
                string hashValue = !string.IsNullOrWhiteSpace(partitionedValue) ? partitionedValue : await _redis.HashGetAsync($"{hashKeyBase}", commit).ConfigureAwait(false);

                commitList.Add(hashValue);
            }

            //Get the events that have happened since specified fromVersion
            var events = commitList.Select(serializedEvent => JsonConvert.DeserializeObject<IEvent>(serializedEvent.ToString().Decompress(), JsonSerializerSettings))
                                   .Where(x => x.Version > fromVersion)
                                   .OrderBy(x => x.Version)
                                   .ToList();
            return events;
        }

        public async Task SaveAsync(IEnumerable<IEvent> events)
        {
            foreach (var @event in events)
            {
                var serializedEvent = JsonConvert.SerializeObject(@event, JsonSerializerSettings);
                var eventData = _settings.EnableCompression
                    ? serializedEvent.Compress(_settings.CompressionThreshold)
                    : serializedEvent;

                //Generate the commitId
                var commitId = Guid.NewGuid().ToString();
                var partition = CalculatePartition(commitId);
                var hashKey = $"EventStore:{_settings.KeyPrefix}:{partition}";

                for (var i = 0; i < _settings.TransactionRetryCount; i++)
                {
                    //Write event data to a field named {commitId} in EventStore hash. Allows for fast lookup O(1) of individual events
                    await _redis.HashSetAsync(hashKey, commitId, eventData).ConfigureAwait(false);

                    //Create a Redis transaction
                    var tran = _redis.Database.CreateTransaction();

                    //Write the commitId to a list mapping commitIds to individual events for a particular aggregate (@event.Id)
                    var listKey = $"{{EventStore:{_settings.KeyPrefix}}}:{@event.Id}";
                    tran.ListRightPushAsync(listKey, commitId);

                    try
                    {
                        //Execute the commit list transaction
                        if (await tran.ExecuteAsync().ConfigureAwait(false))
                        {
                            //Publish the event
                            var publishTran = await PublishEvent(serializedEvent, @event).ConfigureAwait(false);
                            for (int j = 0; j < _settings.TransactionRetryCount; j++)
                            {
                                try
                                {
                                    if (await publishTran.ExecuteAsync().ConfigureAwait(false))
                                    {
                                        return;
                                    }
                                }
                                catch
                                {
                                    //The publish transaction failed so delete the entry from the commit list
                                    await _redis.ListRemoveAsync(listKey, commitId, -1).ConfigureAwait(false);
                                    throw;
                                }

                                await Task.Delay(_settings.TransactionRetryDelay).ConfigureAwait(false);
                            }

                            //The publish transaction failed so delete the entry from the commit list
                            await _redis.ListRemoveAsync(listKey, commitId, -1).ConfigureAwait(false);

                            throw new InvalidOperationException(string.Format($"Failed to publish event {hashKey} after retrying {_settings.TransactionRetryCount} times"));
                        }
                    }
                    catch
                    {
                        //The commit list push transaction failed so delete the entry from the event store hash
                        await _redis.HashDeleteAsync(hashKey, commitId).ConfigureAwait(false);
                        throw;
                    }

                    await Task.Delay(_settings.TransactionRetryDelay).ConfigureAwait(false);
                }

                //The commit list push transaction failed so delete the entry from the event store hash
                await _redis.HashDeleteAsync(hashKey, commitId);

                throw new InvalidOperationException(string.Format($"Failed to save value in key {hashKey} after retrying {_settings.TransactionRetryCount} times"));
            }
        }

        private async Task<ITransaction> PublishEvent(string serializedEvent, IEvent @event)
        {
            var eventType = @event.GetType().Name;
            var eventKey = $"{_environment}:{eventType}";

            //Get all registered subscribers for this event stored in the Redis set at 'subscriberKey'
            var subscriberKey = $"Subscribers:{eventKey}";
            var subscribers = await _redis.SetMembersAsync(subscriberKey);

            //Create a Redis transaction
            var tran = _redis.Database.CreateTransaction();

            /*
            Push event data into the queue for each type of subscriber
            Ensures that even if there are multiple instances of a particular subscriber, the event will only be processed once.
            */
            foreach (var subscriber in subscribers)
            {
                var listKey = $"{subscriber}:{{{eventKey}}}:PublishedEvents";

                //Write the commitId to a list mapping commitIds to individual events for a particular aggregate (@event.Id)
                tran.ListRightPushAsync(listKey, serializedEvent).ConfigureAwait(false);
            }

            /*
            Publish event that notifies subscribers that an item was added to the queue
            All instances will receive the notification, but only one will actually process it since the event can only be popped from the queue once
            */
            tran.PublishAsync(eventKey, true).ConfigureAwait(false);

            return tran;
        }

        private static string CalculatePartition(string commitId)
        {
            var bytes = Encoding.UTF8.GetBytes(commitId);
            var hash = Crc32Algorithm.Compute(bytes);
            var partition = hash % 655360;

            return partition.ToString();
        }

    }
}
    