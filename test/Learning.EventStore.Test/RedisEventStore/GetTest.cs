using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using Learning.EventStore.Test.Mocks;
using Newtonsoft.Json;
using NUnit.Framework;
using StackExchange.Redis;

namespace Learning.EventStore.Test.RedisEventStore
{
    public class GetTest
    {
        private readonly IRedisClient _redis;
        private readonly IEnumerable<IEvent> _events;

        public GetTest()
        {
            _redis = A.Fake<IRedisClient>();
            var publisher = A.Fake<IEventPublisher>();
            var redisEventStore = new EventStore.RedisEventStore(_redis, publisher, "test");
            var commits = new RedisValue[] {1, 3, 5};
            var evenList = new List<TestEvent>
            {
                new TestEvent { Version = 1 },
                new TestEvent { Version = 2 },
                new TestEvent { Version = 3 }
            };
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

            A.CallTo(() => _redis.ListLengthAsync($"{{EventStore:test}}:{Guid.Empty}")).Returns(Task.Run(() => (long)3));
            A.CallTo(() => _redis.ListRangeAsync($"{{EventStore:test}}:{Guid.Empty}", 0, 3)).Returns(commits);
            A.CallTo(() => _redis.HashGetAsync("EventStore:test", 1)).Returns(JsonConvert.SerializeObject(evenList[0], settings));
            A.CallTo(() => _redis.HashGetAsync("EventStore:test", 3)).Returns(JsonConvert.SerializeObject(evenList[1], settings));
            A.CallTo(() => _redis.HashGetAsync("EventStore:test", 5)).Returns(JsonConvert.SerializeObject(evenList[2], settings));

            _events = redisEventStore.Get(Guid.Empty, 1).Result;
        }


        [Test]
        public void GetsListLength()
        {
            A.CallTo(() => _redis.ListLengthAsync($"{{EventStore:test}}:{Guid.Empty}")).MustHaveHappened();
        }

        [Test]
        public void GetsListRange()
        {
            A.CallTo(() => _redis.ListRangeAsync($"{{EventStore:test}}:{Guid.Empty}", 0, 3)).MustHaveHappened();
        }

        public void GetsAllEvents()
        {
            A.CallTo(() => _redis.HashGetAsync("EventStore:test", 1)).MustHaveHappened();
            A.CallTo(() => _redis.HashGetAsync("EventStore:test", 3)).MustHaveHappened();
            A.CallTo(() => _redis.HashGetAsync("EventStore:test", 5)).MustHaveHappened();
        }

        [Test]
        public void ReturnsEventsWithVersionGreaterThanFromVersion()
        {
            Assert.AreEqual(2, _events.Count());
            Assert.AreEqual(2, _events.ToList()[0].Version);
            Assert.AreEqual(3, _events.ToList()[1].Version);
        }
    }
}
