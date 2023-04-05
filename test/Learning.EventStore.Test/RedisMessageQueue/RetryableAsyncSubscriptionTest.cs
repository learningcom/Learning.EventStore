using System;
using System.Threading.Tasks;
using FakeItEasy;
using Learning.MessageQueue;
using Learning.MessageQueue.Messages;
using Learning.MessageQueue.Repository;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Learning.EventStore.Test.RedisMessageQueue
{
    [TestClass]
    public class RetryableAsyncSubscriptionTest
    {
        [TestMethod]
        public async Task CallsCallbackAndDeletesFromDeadLetterQueue()
        {
            var logger = A.Fake<ILogger>();
            var eventStoreRepository = A.Fake<IMessageQueueRepository>();
            A.CallTo(() => eventStoreRepository.GetDeadLetterListLength<TestAsyncMessage>()).Returns(3);
            var message1 = JsonConvert.SerializeObject(new TestAsyncMessage { Id = "0" });
            var message2 = JsonConvert.SerializeObject(new TestAsyncMessage { Id = "1" });
            var message3 = JsonConvert.SerializeObject(new TestAsyncMessage { Id = "2" });
            A.CallTo(() => eventStoreRepository.GetDeadLetterMessage<TestAsyncMessage>(0)).ReturnsNextFromSequence(message1, message2, message3);
            var retryData = new RetryData
            {
                LastRetryTime = DateTimeOffset.UtcNow.AddHours(-1)
            };
            A.CallTo(() => eventStoreRepository.GetRetryData(A<TestAsyncMessage>._)).Returns(retryData);
            var subscriber = A.Fake<IEventSubscriber>();
            var retryClass = new TestAsyncRetryClass(subscriber, eventStoreRepository);

            await retryClass.RetryAsync().ConfigureAwait(false);

            Assert.IsTrue(retryClass.CallBack1Called);
            Assert.IsTrue(retryClass.CallBack2Called);
            Assert.IsTrue(retryClass.CallBack3Called);
            A.CallTo(() => eventStoreRepository.DeleteFromDeadLetterQueue<TestAsyncMessage>(A<RedisValue>._, A<TestAsyncMessage>._))
                .MustHaveHappened(Repeated.Exactly.Times(3));
        }

        [TestMethod]
        public async Task IncrementsExecutionCounterOnCallbackException()
        {
            var eventStoreRepository = A.Fake<IMessageQueueRepository>();
            A.CallTo(() => eventStoreRepository.GetDeadLetterListLength<TestAsyncMessage>()).Returns(1);
            var message = new TestAsyncMessage { Id = "0" };
            A.CallTo(() => eventStoreRepository.GetDeadLetterMessage<TestAsyncMessage>(0)).Returns(JsonConvert.SerializeObject(message));
            var retryData = new RetryData
            {
                LastRetryTime = DateTimeOffset.UtcNow.AddHours(-1)
            };
            A.CallTo(() => eventStoreRepository.GetRetryData(A<TestAsyncMessage>._)).Returns(retryData);
            var subscriber = A.Fake<IEventSubscriber>();
            var retryClass = new TestAsyncExceptionRetryClass(subscriber, eventStoreRepository);

            await retryClass.RetryAsync().ConfigureAwait(false); ;

            A.CallTo(() => eventStoreRepository.UpdateRetryData(A<IMessage>._, A<string>._)).MustHaveHappened();
            A.CallTo(() => eventStoreRepository.DeleteFromDeadLetterQueue<TestAsyncMessage>(A<RedisValue>._, A<IMessage>.That.Matches(x => x.Id == message.Id))).MustNotHaveHappened();
        }

        [TestMethod]
        public async Task CallsCallbackIfTimeToLiveHasNotBeenExceeded()
        {
            var eventStoreRepository = A.Fake<IMessageQueueRepository>();
            A.CallTo(() => eventStoreRepository.GetDeadLetterListLength<TestAsyncMessage>()).Returns(1);
            var message = new TestAsyncMessage { Id = "0", TimeStamp = DateTimeOffset.UtcNow.AddHours(-1) };
            A.CallTo(() => eventStoreRepository.GetDeadLetterMessage<TestAsyncMessage>(0)).Returns(JsonConvert.SerializeObject(message));
            var retryData = new RetryData
            {
                LastRetryTime = DateTimeOffset.UtcNow.AddHours(-1),
                RetryCount = 6
            };
            A.CallTo(() => eventStoreRepository.GetRetryData(A<TestAsyncMessage>._)).Returns(retryData);
            var subscriber = A.Fake<IEventSubscriber>();
            var retryClass = new TestAsyncRetryHoursSubscription(subscriber, eventStoreRepository);

            await retryClass.RetryAsync().ConfigureAwait(false);

            Assert.IsTrue(retryClass.CallBack1Called);
            A.CallTo(() => eventStoreRepository.DeleteFromDeadLetterQueue<TestAsyncMessage>(A<RedisValue>._, A<IMessage>.That.Matches(x => x.Id == message.Id))).MustHaveHappened();
        }

        [TestMethod]
        public async Task DoesNotCallCallbackAndDeletesFromDeadLetterQueueIfTimeToLiveIsExceeded()
        {
            var eventStoreRepository = A.Fake<IMessageQueueRepository>();
            A.CallTo(() => eventStoreRepository.GetDeadLetterListLength<TestAsyncMessage>()).Returns(1);
            var message = new TestAsyncMessage { Id = "0", TimeStamp = DateTimeOffset.UtcNow.AddHours(-169) };
            A.CallTo(() => eventStoreRepository.GetDeadLetterMessage<TestAsyncMessage>(0)).Returns(JsonConvert.SerializeObject(message));
            var retryData = new RetryData
            {
                LastRetryTime = DateTimeOffset.UtcNow.AddHours(-1),
                RetryCount = 6
            };
            A.CallTo(() => eventStoreRepository.GetRetryData(A<TestAsyncMessage>._)).Returns(retryData);
            var subscriber = A.Fake<IEventSubscriber>();
            var retryClass = new TestAsyncRetryHoursSubscription(subscriber, eventStoreRepository);

            await retryClass.RetryAsync().ConfigureAwait(false);

            Assert.IsFalse(retryClass.CallBack1Called);
            A.CallTo(() => eventStoreRepository.DeleteFromDeadLetterQueue<TestAsyncMessage>(A<RedisValue>._, A<IMessage>.That.Matches(x => x.Id == message.Id))).MustHaveHappened();
        }

        [TestMethod]
        public async Task DoesNotFailWithLargeNumberOfRetries()
        {
            var eventStoreRepository = A.Fake<IMessageQueueRepository>();
            A.CallTo(() => eventStoreRepository.GetDeadLetterListLength<TestAsyncMessage>()).Returns(1);
            var message = new TestAsyncMessage { Id = "0", TimeStamp = DateTimeOffset.UtcNow.AddHours(-1) };
            A.CallTo(() => eventStoreRepository.GetDeadLetterMessage<TestAsyncMessage>(0)).Returns(JsonConvert.SerializeObject(message));
            var retryData = new RetryData
            {
                LastRetryTime = DateTimeOffset.UtcNow.AddHours(-1),
                RetryCount = 4000
            };
            A.CallTo(() => eventStoreRepository.GetRetryData(A<TestAsyncMessage>._)).Returns(retryData);
            var subscriber = A.Fake<IEventSubscriber>();
            var retryClass = new TestAsyncRetryHoursSubscription(subscriber, eventStoreRepository);

            await retryClass.RetryAsync().ConfigureAwait(false);

            Assert.IsTrue(retryClass.CallBack1Called);
        }

        [TestMethod]
        public async Task ChecksForStaleProcessingEvents()
        {
            var logger = A.Fake<ILogger>();
            var eventStoreRepository = A.Fake<IMessageQueueRepository>();
            A.CallTo(() => eventStoreRepository.GetDeadLetterListLength<TestAsyncMessage>()).Returns(3);
            var message1 = JsonConvert.SerializeObject(new TestAsyncMessage { Id = "0", TimeStamp = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(0) });
            var message2 = JsonConvert.SerializeObject(new TestAsyncMessage { Id = "1", TimeStamp = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(4) });
            var message3 = JsonConvert.SerializeObject(new TestAsyncMessage { Id = "2", TimeStamp = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(5) });
            A.CallTo(() => eventStoreRepository.GetOldestProcessingEvents<TestAsyncMessage>(A<int>._)).Returns(new RedisValue[] { message1, message2, message3 });
            var subscriber = A.Fake<IEventSubscriber>();
            var retryClass = new TestAsyncRetryClass(subscriber, eventStoreRepository);

            await retryClass.RetryAsync().ConfigureAwait(false);
            A.CallTo(() => eventStoreRepository.MoveProcessingEventToDeadLetterQueue<TestAsyncMessage>(A<RedisValue>._, A<TestAsyncMessage>._))
                .MustHaveHappened(Repeated.Exactly.Once);
        }
    }

    public class TestAsyncRetryClass : RetryableAsyncRedisSubscription<TestAsyncMessage>
    {
        public bool CallBack1Called;
        public bool CallBack2Called;
        public bool CallBack3Called;

        public TestAsyncRetryClass(IEventSubscriber subscriber, IMessageQueueRepository eventStoreRepository)
            : base(subscriber, eventStoreRepository)
        {
            CallBack1Called = false;
            CallBack2Called = false;
            CallBack3Called = false;
        }

        protected override async Task CallBack(TestAsyncMessage message)
        {
            await Task.Delay(20);

            switch (message.Id)
            {
                case "0":
                    CallBack1Called = true;
                    break;
                case "1":
                    CallBack2Called = true;
                    break;
                case "2":
                    CallBack3Called = true;
                    break;
            }
        }
    }

    public class TestAsyncExceptionRetryClass : RetryableAsyncRedisSubscription<TestAsyncMessage>
    {
        public bool CallBack1Called;
        public bool CallBack2Called;
        public bool CallBack3Called;

        public TestAsyncExceptionRetryClass(IEventSubscriber subscriber, IMessageQueueRepository eventStoreRepository)
            : base(subscriber, eventStoreRepository)
        {
            CallBack1Called = false;
            CallBack2Called = false;
            CallBack3Called = false;
        }

        protected override async Task CallBack(TestAsyncMessage message)
        {
            await Task.Delay(20);

            throw new Exception("Oh No!");
        }
    }

    public class TestAsyncRetryHoursSubscription : RetryableAsyncRedisSubscription<TestAsyncMessage>
    {
        public bool CallBack1Called;
        public bool CallBack2Called;
        public bool CallBack3Called;

        public override int TimeToLiveHours { get; set; } = 2;

        public TestAsyncRetryHoursSubscription(IEventSubscriber subscriber, IMessageQueueRepository messageQueueRepository) 
            : base(subscriber, messageQueueRepository)
        {
        }

        protected override async Task CallBack(TestAsyncMessage message)
        {
            await Task.Delay(20);

            switch (message.Id)
            {
                case "0":
                    CallBack1Called = true;
                    break;
                case "1":
                    CallBack2Called = true;
                    break;
                case "2":
                    CallBack3Called = true;
                    break;
            }
        }
    }

    public class TestAsyncMessage : IMessage
    {
        public string Id { get; set; }
        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;
    }
}
