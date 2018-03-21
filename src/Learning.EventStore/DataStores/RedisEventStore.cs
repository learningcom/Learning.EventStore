using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Learning.EventStore.Common;
using Learning.EventStore.Common.Redis;
using Learning.EventStore.Domain.Exceptions;
using Learning.EventStore.Extensions;
using Learning.MessageQueue;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Learning.EventStore.DataStores
{
    public class RedisEventStore : IEventStore
    {
        private readonly IRedisClient _redis;
        private readonly EventStoreSettings _settings;
        private readonly IMessageQueue _messageQueue;
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

        public RedisEventStore(IRedisClient redis, string keyPrefix, IMessageQueue messageQueue)
            : this(redis, new EventStoreSettings {KeyPrefix = keyPrefix}, messageQueue)
        {
        }

        public RedisEventStore(IRedisClient redis, EventStoreSettings settings, IMessageQueue messageQueue)
        {
            if (string.IsNullOrWhiteSpace(settings.KeyPrefix))
            {
                throw new ArgumentException("KeyPrefix must be specified in EventStoreSettings");
            }

            _redis = redis;
            _settings = settings;
            _messageQueue = messageQueue;
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
                var partition = commit.ToString().CalculatePartition();
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
                var newPartition = commitId.CalculatePartition();
                var newHashKey = $"{hashKeyBase}:{newPartition}";

                //Write event data to a field named {commitId} in EventStore hash. Allows for fast lookup O(1) of individual events
                await _redis.HashSetAsync(newHashKey, commitId, eventData).ConfigureAwait(false);

                for (var i = 0; i < _settings.TransactionRetryCount; i++)
                {
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
                            try
                            {
                                await _messageQueue.PublishAsync(serializedEvent, @event.Id, @event.GetType().Name).ConfigureAwait(false);
                                return;
                            }
                            catch
                            {
                                await _redis.ListRemoveAsync(listKey, commitId, -1).ConfigureAwait(false);
                                throw;
                            }
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

                //The commit list push transaction failed so delete the entry from the event store hash
                await _redis.HashDeleteAsync(newHashKey, commitId).ConfigureAwait(false);

                throw new ConcurrencyException(@event.Id);
            }
        }
    }
}
    