using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Force.Crc32;
using Learning.EventStore.Domain.Exceptions;
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
            //Get all the commits for the aggregateId that have happened since specified fromVersion
            var rangeStart = fromVersion < 0 ? 0 : fromVersion;
            var listKey = $"{{EventStore:{_settings.KeyPrefix}}}:{aggregateId}";
            var commits = await _redis.ListRangeAsync(listKey, rangeStart, -1).ConfigureAwait(false);
            
            //Retrieve event data for each commit
            var eventTasks = commits.Select(commit =>
            {
                var partition = CalculatePartition(commit);
                var hashKeyBase = $"EventStore:{_settings.KeyPrefix}";

                var hashGetTask = _redis.HashGetAsync($"{hashKeyBase}:{partition}", commit);
                return hashGetTask;
            });
            var commitList = await Task.WhenAll(eventTasks).ConfigureAwait(false);

            //Get the events
            var events = commitList.Select(serializedEvent => JsonConvert.DeserializeObject<IEvent>(serializedEvent.ToString().Decompress(), JsonSerializerSettings))
                                   .OrderBy(x => x.Version)
                                   .ToList();
            return events;
        }

        public async Task SaveAsync(IEnumerable<IEvent> events)
        {
            foreach (var @event in events)
            {
                var hashKeyBase = $"EventStore:{_settings.KeyPrefix}";
                var listKey = $"{{EventStore:{_settings.KeyPrefix}}}:{@event.Id}";

                var serializedEvent = JsonConvert.SerializeObject(@event, JsonSerializerSettings);
                var eventData = _settings.EnableCompression
                    ? serializedEvent.Compress(_settings.CompressionThreshold)
                    : serializedEvent;

                //Generate the commitId
                var commitId = Guid.NewGuid().ToString();
                var newPartition = CalculatePartition(commitId);
                var newHashKey = $"{hashKeyBase}:{newPartition}";

                //Write event data to a field named {commitId} in EventStore hash. Allows for fast lookup O(1) of individual events
                await _redis.HashSetAsync(newHashKey, commitId, eventData).ConfigureAwait(false);

                //Write the commitId to a list mapping commitIds to individual events for a particular aggregate (@event.Id)
                var commitListTransaction = _redis.Database.CreateTransaction();
                var commitlistLength = await _redis.ListLengthAsync(listKey).ConfigureAwait(false);
                commitListTransaction.ListRightPushAsync(listKey, commitId).ConfigureAwait(false);
                commitListTransaction.AddCondition(Condition.ListLengthEqual(listKey, commitlistLength));

                try
                {
                    //Execute the commit list and publish transactions
                    if (await commitListTransaction.ExecuteAsync().ConfigureAwait(false))
                    {
                        if (await Publish(serializedEvent, @event, listKey, commitId).ConfigureAwait(false))
                        {
                            return;
                        }
                    }
                    else
                    {
                        throw new ConcurrencyException(@event.Id);
                    }
                }
                catch
                {
                    //The commit list push transaction failed so delete the entry from the event store hash
                    await _redis.HashDeleteAsync(newHashKey, commitId).ConfigureAwait(false);
                    throw;
                }

                await Task.Delay(_settings.TransactionRetryDelay).ConfigureAwait(false);
            }
        }

        private async Task<bool> Publish(string serializedEvent, IEvent @event, string commitListKey, string commitId)
        {
            //Publish the event
            var publishTran = await GeneratePublishTransaction(serializedEvent, @event).ConfigureAwait(false);
            for (var j = 0; j < _settings.TransactionRetryCount; j++)
            {
                try
                {
                    if (await publishTran.ExecuteAsync().ConfigureAwait(false))
                    {
                        return true;
                    }
                }
                catch
                {
                    //The publish transaction failed so delete the entry from the commit list
                    await _redis.ListRemoveAsync(commitListKey, commitId, -1).ConfigureAwait(false);
                    throw;
                }

                await Task.Delay(_settings.TransactionRetryDelay).ConfigureAwait(false);
            }

            //The publish transaction failed so delete the entry from the commit list
            await _redis.ListRemoveAsync(commitListKey, commitId, -1).ConfigureAwait(false);

            throw new InvalidOperationException(string.Format($"Failed to publish event for aggregate {@event.Id} after retrying {_settings.TransactionRetryCount} times"));
        }

        private async Task<ITransaction> GeneratePublishTransaction(string serializedEvent, IEvent @event)
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
    