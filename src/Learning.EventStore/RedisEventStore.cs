using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Learning.EventStore.Extensions;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Learning.EventStore
{
    public class RedisEventStore : IEventStore
    {
        private readonly IRedisClient _redis;
        private readonly IEventPublisher _publisher;
        private readonly string _keyPrefix;
        private const int SaveRetryCount = 10;

        public RedisEventStore(IRedisClient redis, IEventPublisher publisher, string keyPrefix)
        {
            _redis = redis;
            _publisher = publisher;
            _keyPrefix = keyPrefix;
        }

        public async Task<IEnumerable<IEvent>> GetAsync(string aggregateId, int fromVersion)
        {
            //Get all the commits for the aggregateId
            var listLength = await _redis.ListLengthAsync($"{{EventStore:{_keyPrefix}}}:{aggregateId}").ConfigureAwait(false);
            var commits = await _redis.ListRangeAsync($"{{EventStore:{_keyPrefix}}}:{aggregateId}", 0, listLength).ConfigureAwait(false);

            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

            //Get the event data associated with each commit
            var commitList = new List<string>();
            foreach (var commit in commits)
            {
                var hashSetTask = _redis.HashGetAsync($"EventStore:{_keyPrefix}", commit).ConfigureAwait(false);
                commitList.Add(await hashSetTask);
            }

            //Get the events that have happened since specified fromVersion
            var events = commitList.Select(serializedEvent => JsonConvert.DeserializeObject<IEvent>(serializedEvent.Decompress(), settings))
                                   .Where(x => x.Version > fromVersion)
                                   .ToList();
            return events;
        }

        public async Task SaveAsync(IEnumerable<IEvent> events)
        {
            var hashKey = $"EventStore:{_keyPrefix}";
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

            foreach (var @event in events)
            {
                var serializedEvent = JsonConvert.SerializeObject(@event, settings);
                var compressedEvent = serializedEvent.Compress();

                for (var i = 0; i < SaveRetryCount; i++)
                {
                    //Create a Redis transaction
                    var tran = _redis.Database.CreateTransaction();

                    //Increment the commitId
                    var commitId = await _redis.HashLengthAsync(hashKey).ConfigureAwait(false) + 1;

                    //Ensure that the hash length has not been changed by another thread between now and when the transaction is committed to avoid commitId collisions
                    tran.AddCondition(Condition.HashLengthEqual(hashKey, commitId));

                    //Write event data to a field named {commitId} in EventStore hash. Allows for fast lookup O(1) of individual events
                    var hashSetTask = tran.HashSetAsync(hashKey, commitId, compressedEvent).ConfigureAwait(false);

                    //Write the commitId to a list mapping commitIds to individual events for a particular aggregate (@event.Id)
                    var listPushTask = tran.ListRightPushAsync($"{{EventStore:{_keyPrefix}}}:{@event.Id}", commitId.ToString()).ConfigureAwait(false);

                    //Publish the event
                    var publishTask = _publisher.Publish(@event).ConfigureAwait(false);

                    //Execute the transaction
                    if (await tran.ExecuteAsync())
                    {
                        await hashSetTask;
                        await listPushTask;
                        await publishTask;
                        return;
                    }
                }

                throw new InvalidOperationException(string.Format($"Failed to save value in key {hashKey}"));
            }
        }
    }
}
    