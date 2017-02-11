using System;
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
        private readonly Lazy<IConnectionMultiplexer> _redis;
        private readonly IEventPublisher _publisher;
        private readonly string _keyPrefix;
        private IDatabase Database => _redis.Value.GetDatabase();
        

        public RedisEventStore(Lazy<IConnectionMultiplexer> redis, IEventPublisher publisher, string keyPrefix)
        {
            _redis = redis;
            _publisher = publisher;
            _keyPrefix = keyPrefix;
        }

        public async Task<IEnumerable<IEvent>> Get<T>(Guid aggregateId, int fromVersion)
        {
            var listLength = await Database.ListLengthAsync($"{{EventStore:{_keyPrefix}}}:{aggregateId}");
            var commitList = await Database.ListRangeAsync($"{{EventStore:{_keyPrefix}}}:{aggregateId}", 0, listLength);

            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            var events = commitList.Select(commit => Database.HashGet($"EventStore:{_keyPrefix}", commit))
                                   .Select(serializedEvent => JsonConvert.DeserializeObject<IEvent>(serializedEvent, settings))
                                   .ToList();

            return events?.Where(x => x.Version > fromVersion);
        }

        public async Task Save<T>(IEnumerable<IEvent> events)
        {
            foreach (var @event in events)
            {
                var hashKey = $"EventStore:{_keyPrefix}";
                var commitId = await Database.HashLengthAsync(hashKey) + 1;
                var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
                var serializedEvent = JsonConvert.SerializeObject(@event, settings);

                var taskList = new List<Task>();
                var tran = Database.CreateTransaction();
                taskList.Add(tran.HashSetAsync(hashKey, commitId, serializedEvent));
                taskList.Add(tran.ListRightPushAsync($"{{EventStore:{_keyPrefix}}}:{@event.Id}", commitId.ToString()));
                taskList.Add(_publisher.Publish(@event));

                await tran.ExecuteAsync();
                Task.WaitAll(taskList.ToArray());
            }
        }
    }
}
    