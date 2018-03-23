using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using Learning.EventStore.Common;
using Learning.EventStore.Test.Mocks;
using Learning.MessageQueue;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Learning.EventStore.Test.RedisEventStore
{
    [TestClass]
    public class GetTest
    {
        private readonly IRedisClient _redis;
        private readonly IEnumerable<IEvent> _events;
        private readonly IMessageQueue _messageQueue;

        public GetTest()
        {
            _redis = A.Fake<IRedisClient>();
            _messageQueue = A.Fake<IMessageQueue>();
            var redisEventStore = new DataStores.RedisEventStore(_redis, "test", _messageQueue);
            var commits = new RedisValue[] {1, 3, 5};
            var evenList = new List<TestEvent>
            {
                new TestEvent { Version = 1 },
                new TestEvent { Version = 2 },
                new TestEvent { Version = 3 }
            };
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

            A.CallTo(() => _redis.ListLengthAsync($"{{EventStore:test}}:{Guid.Empty}")).Returns(Task.Run(() => (long)3));
            A.CallTo(() => _redis.ListRangeAsync($"{{EventStore:test}}:{Guid.Empty}", 1, -1)).Returns(commits);
            A.CallTo(() => _redis.HashGetAsync(A<RedisKey>._, 1)).Returns(JsonConvert.SerializeObject(evenList[0], settings));
            A.CallTo(() => _redis.HashGetAsync(A<RedisKey>._, 3)).Returns(JsonConvert.SerializeObject(evenList[1], settings));
            A.CallTo(() => _redis.HashGetAsync(A<RedisKey>._, 5)).Returns(JsonConvert.SerializeObject(evenList[2], settings));

            _events = redisEventStore.GetAsync(Guid.Empty.ToString(), "TestType", 1).Result;
        }


        [TestMethod]
        public void GetsListRange()
        {
            A.CallTo(() => _redis.ListRangeAsync($"{{EventStore:test}}:{Guid.Empty}", 1, -1)).MustHaveHappened();
        }

        [TestMethod]
        public void GetsAllEvents()
        {
            A.CallTo(() => _redis.HashGetAsync(A<RedisKey>._, 1)).MustHaveHappened();
            A.CallTo(() => _redis.HashGetAsync(A<RedisKey>._, 3)).MustHaveHappened();
            A.CallTo(() => _redis.HashGetAsync(A<RedisKey>._, 5)).MustHaveHappened();
        }

        [TestMethod]
        public void ReturnsEventsWithVersionGreaterThanFromVersion()
        {
            Assert.AreEqual(3, _events.Count());
            Assert.AreEqual(1, _events.ToList()[0].Version);
            Assert.AreEqual(2, _events.ToList()[1].Version);
            Assert.AreEqual(3, _events.ToList()[2].Version);
        }
    }
}
