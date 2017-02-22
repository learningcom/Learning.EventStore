using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Learning.EventStore
{
    public class RedisEventStore : IEventStore
    {
        private readonly IRedisClient _redis;
        private readonly IEventPublisher _publisher;
        private readonly string _keyPrefix;

        public RedisEventStore(IRedisClient redis, IEventPublisher publisher, string keyPrefix)
        {
            _redis = redis;
            _publisher = publisher;
            _keyPrefix = keyPrefix;
        }

        public async Task<IEnumerable<IEvent>> Get(Guid aggregateId, int fromVersion)
        {
            //Get all the commits for the aggregateId
            var listLength = await _redis.ListLengthAsync($"{{EventStore:{_keyPrefix}}}:{aggregateId}").ConfigureAwait(false);
            var commits = await _redis.ListRangeAsync($"{{EventStore:{_keyPrefix}}}:{aggregateId}", 0, listLength).ConfigureAwait(false);

            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

            //Get the event data associated with each commit
            var commitList = new List<RedisValue>();
            foreach (var commit in commits)
            {
                var hashSetTask = _redis.HashGetAsync($"EventStore:{_keyPrefix}", commit).ConfigureAwait(false);
                commitList.Add(await hashSetTask);
            }

            //Get the events that have happened since specified fromVersion
            var events = commitList.Select(serializedEvent => JsonConvert.DeserializeObject<IEvent>(serializedEvent, settings))
                                   .Where(x => x.Version > fromVersion)
                                   .ToList();
            return events;
        }

        public async Task Save(IEnumerable<IEvent> events)
        {
            var hashKey = $"EventStore:{_keyPrefix}";
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

            foreach (var @event in events)
            {
                var serializedEvent = JsonConvert.SerializeObject(@event, settings);

                //Create a Redis transaction
                var tran = _redis.Database.CreateTransaction();

                //Increment the commitId
                var commitId = await _redis.HashLengthAsync(hashKey).ConfigureAwait(false) + 1;

                //Write event data to a field named {commitId} in EventStore hash. Allows for fast lookup O(1) of individual events
                var hashSetTask = tran.HashSetAsync(hashKey, commitId, serializedEvent).ConfigureAwait(false);

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
                }
             
            }
        }
    }
}
    