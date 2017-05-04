using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using Learning.EventStore.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Learning.EventStore.Test.RedisEventStore
{
    [TestClass]
    public class SaveTest
    {
        private readonly IRedisClient _redis;
        private readonly ITransaction _trans;
        private readonly IEventPublisher _publisher;
        private readonly List<TestEvent> _eventList;
        private readonly string _serializedEvent;

        public SaveTest()
        {
            _redis = A.Fake<IRedisClient>();
            _trans = A.Fake<ITransaction>();
            _publisher = A.Fake<IEventPublisher>();
            _eventList = new List<TestEvent> { new TestEvent() };
            var database = A.Fake<IDatabase>();
            var redisEventStore = new EventStore.RedisEventStore(_redis, _publisher, "test");

            A.CallTo(() => _trans.ExecuteAsync(CommandFlags.None)).Returns(Task.Run(() => true));
            A.CallTo(() => _redis.Database).Returns(database);
            A.CallTo(() => _redis.Database.CreateTransaction(null)).Returns(_trans);
            A.CallTo(() => _redis.HashLengthAsync("EventStore:test")).Returns(Task.Run(() => (long) 2));
                        
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            _serializedEvent = JsonConvert.SerializeObject(_eventList.First(), settings);

            redisEventStore.SaveAsync(_eventList).Wait();
        }

        [TestMethod]
        public void CreatesTransaction()
        {
            A.CallTo(() => _redis.Database.CreateTransaction(null)).MustHaveHappened();
        }

        [TestMethod]
        public void ExecutesTransaction()
        {
            A.CallTo(() => _trans.ExecuteAsync(CommandFlags.None)).MustHaveHappened();
        }

        [TestMethod]
        public void GetsHashLength()
        {
            A.CallTo(() => _redis.HashLengthAsync("EventStore:test")).MustHaveHappened();
        }

        [TestMethod]
        public void SetsNewHashEntry()
        {
            A.CallTo(() => _trans.HashSetAsync("EventStore:test", 3, _serializedEvent, When.Always, CommandFlags.None)).MustHaveHappened();
        }

        [TestMethod]
        public void AddsToCommitList()
        {
            A.CallTo(() => _trans.ListRightPushAsync($"{{EventStore:test}}:{_eventList.First().Id}", "3", When.Always, CommandFlags.None)).MustHaveHappened();
        }

        [TestMethod]
        public void PublishesEvent()
        {
            A.CallTo(() => _publisher.Publish(A<IEvent>.That.IsSameAs(_eventList.First()))).MustHaveHappened();
        }
    }
}
