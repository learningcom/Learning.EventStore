using System;
using System.Threading.Tasks;
using FakeItEasy;
using Learning.EventStore.Cache;
using Learning.EventStore.Common;
using Learning.EventStore.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Learning.EventStore.Test.Cache
{
    [TestClass]
    public class RedisCacheTest
    {
        [TestMethod]
        public void IsTrackedReturnTrueIfMemoryCacheIsTrackedIsTrue()
        {
            var memoryCache = A.Fake<ICache>();
            A.CallTo(() => memoryCache.IsTracked("prefix:test:12345")).Returns(true);
            var redisClient = A.Fake<IRedisClient>();
            A.CallTo(() => redisClient.KeyExistsAsync("prefix:test:12345")).Returns(Task.FromResult(false));
            var redisCache = new RedisCache(memoryCache, redisClient, "test", "prefix");

            var result = redisCache.IsTracked("12345");

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsTrackedReturnTrueIfKeyIsInRedisCache()
        {
            var memoryCache = A.Fake<ICache>();
            A.CallTo(() => memoryCache.IsTracked("prefix:test:12345")).Returns(false);
            var redisClient = A.Fake<IRedisClient>();
            A.CallTo(() => redisClient.KeyExistsAsync("prefix:test:12345")).Returns(Task.FromResult(true));
            var redisCache = new RedisCache(memoryCache, redisClient, "test", "prefix");

            var result = redisCache.IsTracked("12345");

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsTrackedReturnFalseIfKeyIsNotInRedisCacheOrMemoryCache()
        {
            var memoryCache = A.Fake<ICache>();
            A.CallTo(() => memoryCache.IsTracked("prefix:test:12345")).Returns(false);
            var redisClient = A.Fake<IRedisClient>();
            A.CallTo(() => redisClient.KeyExistsAsync("prefix:test:12345")).Returns(Task.FromResult(false));
            var redisCache = new RedisCache(memoryCache, redisClient, "test", "prefix");

            var result = redisCache.IsTracked("12345");

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SetAddsEntryToMemoryCacheAndRedisCache()
        {
            var memoryCache = A.Fake<ICache>();
            var redisClient = A.Fake<IRedisClient>();
            var redisCache = new RedisCache(memoryCache, redisClient, "test", "prefix");
            var aggregate = new TestAggregate();

            redisCache.Set("12345", aggregate);

            A.CallTo(() => redisClient.StringSetAsync("prefix:test:12345", A<string>._, A<TimeSpan>._)).MustHaveHappened();
            A.CallTo(() => memoryCache.Set("prefix:test:12345", aggregate)).MustHaveHappened();
        }

        [TestMethod]
        public void GetReturnsValueFromMemoryCacheIfPresent()
        {
            var aggregate = new TestAggregate();
            var memoryCache = A.Fake<ICache>();
            A.CallTo(() => memoryCache.Get("prefix:test:12345")).Returns(aggregate);
            var redisClient = A.Fake<IRedisClient>();
            var redisCache = new RedisCache(memoryCache, redisClient, "test", "prefix");

            var result = redisCache.Get("12345");

            Assert.IsTrue(result.GetType() == typeof(TestAggregate));
            A.CallTo(() => redisClient.StringGetAsync("prefix:test:12345")).MustNotHaveHappened();
            A.CallTo(() => redisClient.KeyExpireAsync("prefix:test:12345", A<TimeSpan>._)).MustNotHaveHappened();
            A.CallTo(() => memoryCache.Set("prefix:test:12345", aggregate)).MustNotHaveHappened();
        }

        [TestMethod]
        public void GetReturnsValueFromRedisCacheIfNotPresentInMemoryCache()
        {
            var aggregate = new TestAggregate();
            var memoryCache = A.Fake<ICache>();
            A.CallTo(() => memoryCache.Get("prefix:test:12345")).Returns(null);
            var redisClient = A.Fake<IRedisClient>();
            var serializedAggregate = JsonConvert.SerializeObject(aggregate,
                new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.All});
            A.CallTo(() => redisClient.StringGetAsync("prefix:test:12345")).Returns(serializedAggregate);
            var redisCache = new RedisCache(memoryCache, redisClient, "test", "prefix");

            var result = redisCache.Get("12345");

            Assert.IsTrue(result.GetType() == typeof(TestAggregate));
            A.CallTo(() => redisClient.KeyExpireAsync("prefix:test:12345", A<TimeSpan>._)).MustHaveHappened();
            A.CallTo(() => memoryCache.Set("prefix:test:12345", A<TestAggregate>._)).MustHaveHappened();
        }

        [TestMethod]
        public void ReturnsNullWhenAllCachesEmpty()
        {
            var memoryCache = A.Fake<ICache>();
            A.CallTo(() => memoryCache.Get("prefix:test:12345")).Returns(null);
            var redisClient = A.Fake<IRedisClient>();
            A.CallTo(() => redisClient.StringGetAsync("prefix:test:12345")).Returns("");
            var redisCache = new RedisCache(memoryCache, redisClient, "test", "prefix");

            var result = redisCache.Get("12345");

            Assert.IsNull(result);
        }

        [TestMethod]
        public void RemoveDeletesItemFromMemoryCacheAndRedisCache()
        {
            var memoryCache = A.Fake<ICache>();
            var redisClient = A.Fake<IRedisClient>();
            var redisCache = new RedisCache(memoryCache, redisClient, "test", "prefix");

            redisCache.Remove("12345");

            A.CallTo(() => redisClient.KeyDeleteAsync("prefix:test:12345")).MustHaveHappened();
            A.CallTo(() => memoryCache.Remove("prefix:test:12345")).MustHaveHappened();
        }

        public class TestAggregate : AggregateRoot
        {
        }
    }
}
