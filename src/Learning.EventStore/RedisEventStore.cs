using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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

        public async Task<IEnumerable<IEvent>> Get<T>(Guid aggregateId, int fromVersion)
        {
            var listLength = await _redis.ListLengthAsync($"{{EventStore:{_keyPrefix}}}:{aggregateId}").ConfigureAwait(false);
            var commits = await _redis.ListRangeAsync($"{{EventStore:{_keyPrefix}}}:{aggregateId}", 0, listLength).ConfigureAwait(false);
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

            var commitList = new List<RedisValue>();
            foreach (var commit in commits)
            {
                var hashSetTask = _redis.HashGetAsync($"EventStore:{_keyPrefix}", commit).ConfigureAwait(false);
                commitList.Add(await hashSetTask);
            }

            var events = commitList.Select(serializedEvent => JsonConvert.DeserializeObject<IEvent>(serializedEvent, settings))
                                .ToList();

            return events?.Where(x => x.Version > fromVersion);
        }

        public async Task Save<T>(IEnumerable<IEvent> events)
        {
            foreach (var @event in events)
            {
                var hashKey = $"EventStore:{_keyPrefix}";
                var commitId = await _redis.HashLengthAsync(hashKey).ConfigureAwait(false) + 1;
                var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
                var serializedEvent = JsonConvert.SerializeObject(@event, settings);

                var tran = _redis.Database.CreateTransaction();
                var hashSetTask = tran.HashSetAsync(hashKey, commitId, serializedEvent).ConfigureAwait(false);
                var listPushTask = tran.ListRightPushAsync($"{{EventStore:{_keyPrefix}}}:{@event.Id}", commitId.ToString()).ConfigureAwait(false);
                var publishTask = _publisher.Publish(@event).ConfigureAwait(false);

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
    