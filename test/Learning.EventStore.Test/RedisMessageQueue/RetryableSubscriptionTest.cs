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
    public class RetryableSubscriptionTest
    {
        [TestMethod]
        public async Task CallsCallbackAndDeletesFromDeadLetterQueue()
        {
            var logger = A.Fake<ILogger>();
            var eventStoreRepository = A.Fake<IMessageQueueRepository>();
            A.CallTo(() => eventStoreRepository.GetDeadLetterListLength<TestMessage>()).Returns(3);
            var message1 = JsonConvert.SerializeObject(new TestMessage { Id = "0" });
            var message2 = JsonConvert.SerializeObject(new TestMessage { Id = "1" });
            var message3 = JsonConvert.SerializeObject(new TestMessage { Id = "2" });
            A.CallTo(() => eventStoreRepository.GetUnprocessedMessage<TestMessage>(0)).ReturnsNextFromSequence(message1, message2, message3);
            var retryData = new RetryData
            {
                LastRetryTime = DateTimeOffset.UtcNow.AddHours(-1)
            };
            A.CallTo(() => eventStoreRepository.GetRetryData(A<TestMessage>._)).Returns(retryData);
            var subscriber = A.Fake<IEventSubscriber>();
            var retryClass = new TestRetryClass(subscriber, eventStoreRepository);

            await retryClass.RetryAsync().ConfigureAwait(false);

            Assert.IsTrue(retryClass.CallBack1Called);
            Assert.IsTrue(retryClass.CallBack2Called);
            Assert.IsTrue(retryClass.CallBack3Called);
            A.CallTo(() => eventStoreRepository.DeleteFromDeadLetterQueue<TestMessage>(A<RedisValue>._, A<TestMessage>._))
                .MustHaveHappened(Repeated.Exactly.Times(3));
        }

        [TestMethod]
        public async Task IncrementsExecutionCounterOnCallbackException()
        {
            var eventStoreRepository = A.Fake<IMessageQueueRepository>();
            A.CallTo(() => eventStoreRepository.GetDeadLetterListLength<TestMessage>()).Returns(1);
            var message = new TestMessage { Id = "0" };
            A.CallTo(() => eventStoreRepository.GetUnprocessedMessage<TestMessage>(0)).Returns(JsonConvert.SerializeObject(message));
            var retryData = new RetryData
            {
                LastRetryTime = DateTimeOffset.UtcNow.AddHours(-1)
            };
            A.CallTo(() => eventStoreRepository.GetRetryData(A<TestMessage>._)).Returns(retryData);
            var subscriber = A.Fake<IEventSubscriber>();
            var retryClass = new TestExceptionRetryClass(subscriber, eventStoreRepository);

            await retryClass.RetryAsync().ConfigureAwait(false); ;

            A.CallTo(() => eventStoreRepository.UpdateRetryData(A<IMessage>._, A<string>._)).MustHaveHappened();
            A.CallTo(() => eventStoreRepository.DeleteFromDeadLetterQueue<TestMessage>(A<RedisValue>._, A<IMessage>.That.Matches(x => x.Id == message.Id))).MustNotHaveHappened();
        }

        [TestMethod]
        public async Task CallsCallbackIfTimeToLiveHasNotBeenExceeded()
        {
            var eventStoreRepository = A.Fake<IMessageQueueRepository>();
            A.CallTo(() => eventStoreRepository.GetDeadLetterListLength<TestMessage>()).Returns(1);
            var message = new TestMessage { Id = "0", TimeStamp = DateTimeOffset.UtcNow.AddHours(-1) };
            A.CallTo(() => eventStoreRepository.GetUnprocessedMessage<TestMessage>(0)).Returns(JsonConvert.SerializeObject(message));
            var retryData = new RetryData
            {
                LastRetryTime = DateTimeOffset.UtcNow.AddHours(-1),
                RetryCount = 6
            };
            A.CallTo(() => eventStoreRepository.GetRetryData(A<TestMessage>._)).Returns(retryData);
            var subscriber = A.Fake<IEventSubscriber>();
            var retryClass = new TestRetryHoursSubscription(subscriber, eventStoreRepository);

            await retryClass.RetryAsync().ConfigureAwait(false);

            Assert.IsTrue(retryClass.CallBack1Called);
            A.CallTo(() => eventStoreRepository.DeleteFromDeadLetterQueue<TestMessage>(A<RedisValue>._, A<IMessage>.That.Matches(x => x.Id == message.Id))).MustHaveHappened();
        }

        [TestMethod]
        public async Task DoesNotCallCallbackAndDeletesFromDeadLetterQueueIfTimeToLiveIsExceeded()
        {
            var eventStoreRepository = A.Fake<IMessageQueueRepository>();
            A.CallTo(() => eventStoreRepository.GetDeadLetterListLength<TestMessage>()).Returns(1);
            var message = new TestMessage { Id = "0", TimeStamp = DateTimeOffset.UtcNow.AddHours(-169) };
            A.CallTo(() => eventStoreRepository.GetUnprocessedMessage<TestMessage>(0)).Returns(JsonConvert.SerializeObject(message));
            var retryData = new RetryData
            {
                LastRetryTime = DateTimeOffset.UtcNow.AddHours(-1),
                RetryCount = 6
            };
            A.CallTo(() => eventStoreRepository.GetRetryData(A<TestMessage>._)).Returns(retryData);
            var subscriber = A.Fake<IEventSubscriber>();
            var retryClass = new TestRetryHoursSubscription(subscriber, eventStoreRepository);

            await retryClass.RetryAsync().ConfigureAwait(false);

            Assert.IsFalse(retryClass.CallBack1Called);
            A.CallTo(() => eventStoreRepository.DeleteFromDeadLetterQueue<TestMessage>(A<RedisValue>._, A<IMessage>.That.Matches(x => x.Id == message.Id))).MustHaveHappened();
        }

        [TestMethod]
        public async Task DoesNotFailWithLargeNumberOfRetries()
        {
            var eventStoreRepository = A.Fake<IMessageQueueRepository>();
            A.CallTo(() => eventStoreRepository.GetDeadLetterListLength<TestMessage>()).Returns(1);
            var message = new TestMessage { Id = "0", TimeStamp = DateTimeOffset.UtcNow.AddHours(-1) };
            A.CallTo(() => eventStoreRepository.GetUnprocessedMessage<TestMessage>(0)).Returns(JsonConvert.SerializeObject(message));
            var retryData = new RetryData
            {
                LastRetryTime = DateTimeOffset.UtcNow.AddHours(-1),
                RetryCount = 4000
            };
            A.CallTo(() => eventStoreRepository.GetRetryData(A<TestMessage>._)).Returns(retryData);
            var subscriber = A.Fake<IEventSubscriber>();
            var retryClass = new TestRetryHoursSubscription(subscriber, eventStoreRepository);

            await retryClass.RetryAsync().ConfigureAwait(false);

            Assert.IsTrue(retryClass.CallBack1Called);
        }
    }

    public class TestRetryClass : RetryableRedisSubscription<TestMessage>
    {
        public bool CallBack1Called;
        public bool CallBack2Called;
        public bool CallBack3Called;

        public TestRetryClass(IEventSubscriber subscriber, IMessageQueueRepository eventStoreRepository)
            : base(subscriber, eventStoreRepository)
        {
            CallBack1Called = false;
            CallBack2Called = false;
            CallBack3Called = false;
        }

        protected override void CallBack(TestMessage message)
        {
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

    public class TestExceptionRetryClass : RetryableRedisSubscription<TestMessage>
    {
        public bool CallBack1Called;
        public bool CallBack2Called;
        public bool CallBack3Called;

        public TestExceptionRetryClass(IEventSubscriber subscriber, IMessageQueueRepository eventStoreRepository)
            : base(subscriber, eventStoreRepository)
        {
            CallBack1Called = false;
            CallBack2Called = false;
            CallBack3Called = false;
        }

        protected override void CallBack(TestMessage message)
        {
            throw new Exception("Oh No!");
        }
    }

    public class TestRetryHoursSubscription : RetryableRedisSubscription<TestMessage>
    {
        public bool CallBack1Called;
        public bool CallBack2Called;
        public bool CallBack3Called;

        public override int TimeToLiveHours { get; set; } = 2;

        public TestRetryHoursSubscription(IEventSubscriber subscriber, IMessageQueueRepository messageQueueRepository) 
            : base(subscriber, messageQueueRepository)
        {
        }

        protected override void CallBack(TestMessage message)
        {
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

    public class TestMessage : IMessage
    {
        public string Id { get; set; }
        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;
    }
}
