using System;
using System.Threading.Tasks;
using FakeItEasy;
using Learning.MessageQueue.Messages;
using Learning.MessageQueue.Repository;
using Learning.MessageQueue.Retry;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Learning.EventStore.Test.RedisMessageQueue
{
    [TestClass]
    public class RetryTest
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
            var retryClass = new TestRetryClass(logger, eventStoreRepository);
            var callBack1Called = false;
            var callBack2Called = false;
            var callBack3Called = false;

            await retryClass.ExecuteRetry<TestMessage>(async @event => {
                await Task.Run(() =>
                {
                    switch (@event.Id)
                    {
                        case "0":
                            callBack1Called = true;
                            break;
                        case "1":
                            callBack2Called = true;
                            break;
                        case "2":
                            callBack3Called = true;
                            break;
                    }
                });
            });

            Assert.IsTrue(callBack1Called);
            Assert.IsTrue(callBack2Called);
            Assert.IsTrue(callBack3Called);
            A.CallTo(() => eventStoreRepository.DeleteFromDeadLetterQueue(A<RedisValue>._, A<TestMessage>._))
                .MustHaveHappened(Repeated.Exactly.Times(3));
        }

        [TestMethod]
        public async Task IncrementsExecutionCounterOnCallbackException()
        {
            var logger = A.Fake<ILogger>();
            var eventStoreRepository = A.Fake<IMessageQueueRepository>();
            A.CallTo(() => eventStoreRepository.GetDeadLetterListLength<TestMessage>()).Returns(1);
            var message = new TestMessage { Id = "0" };
            A.CallTo(() => eventStoreRepository.GetUnprocessedMessage<TestMessage>(0)).Returns(JsonConvert.SerializeObject(message));
            var retryClass = new TestRetryClass(logger, eventStoreRepository);
            var retryData = new RetryData
            {
                LastRetryTime = DateTimeOffset.UtcNow.AddHours(-1)
            };
            A.CallTo(() => eventStoreRepository.GetRetryData(A<TestMessage>._)).Returns(retryData);

            await retryClass.ExecuteRetry<TestMessage>(async @event => {
                await Task.Run(() => throw new Exception("Oh No!"));
            });

            A.CallTo(() => eventStoreRepository.UpdateRetryData(A<IMessage>._, A<string>._)).MustHaveHappened();
            A.CallTo(() => logger.Log(LogLevel.Warning, 0, A<object>._, null, A<Func<object, Exception, string>>._)).MustHaveHappened();
        }

        [TestMethod]
        public async Task DoesNotCallCallbackIfRetryCounterIsGreaterThanThreshold()
        {
            var logger = A.Fake<ILogger>();
            var eventStoreRepository = A.Fake<IMessageQueueRepository>();
            A.CallTo(() => eventStoreRepository.GetDeadLetterListLength<TestMessage>()).Returns(1);
            var message = new TestMessage { Id = "0" };
            A.CallTo(() => eventStoreRepository.GetUnprocessedMessage<TestMessage>(0)).Returns(JsonConvert.SerializeObject(message));
            var retryData = new RetryData
            {
                LastRetryTime = DateTimeOffset.UtcNow.AddHours(-1),
                RetryCount = 6
            };
            A.CallTo(() => eventStoreRepository.GetRetryData(A<TestMessage>._)).Returns(retryData);
            var retryClass = new TestRetryClass(logger, eventStoreRepository);
            var callBackCalled = false;

            await retryClass.ExecuteRetry<TestMessage>(async @event => {
                await Task.Run(() => { callBackCalled = true; });
            });

            Assert.IsFalse(callBackCalled);
        }

        [TestMethod]
        public async Task CallsCallbackIfRetryCounterIsGreaterThanThresholdButRetryForHoursIsSetAndHasNotBeenExceeded()
        {
            var logger = A.Fake<ILogger>();
            var eventStoreRepository = A.Fake<IMessageQueueRepository>();
            A.CallTo(() => eventStoreRepository.GetDeadLetterListLength<TestMessage>()).Returns(1);
            var message = new TestRetryHoursMessage { Id = "0", TimeStamp = DateTimeOffset.UtcNow.AddHours(-1)};
            A.CallTo(() => eventStoreRepository.GetUnprocessedMessage<TestMessage>(0)).Returns(JsonConvert.SerializeObject(message));
            var retryData = new RetryData
            {
                LastRetryTime = DateTimeOffset.UtcNow.AddHours(-1),
                RetryCount = 6
            };
            A.CallTo(() => eventStoreRepository.GetRetryData(A<TestMessage>._)).Returns(retryData);
            var retryClass = new TestRetryClass(logger, eventStoreRepository);
            var callBackCalled = false;

            await retryClass.ExecuteRetry<TestMessage>(async @event => {
                await Task.Run(() => { callBackCalled = true; });
            });

            Assert.IsTrue(callBackCalled);
        }

        [TestMethod]
        public async Task DoesNotCallCallbackIfRetryForHoursIsSetAndHasBeenExceeded()
        {
            var logger = A.Fake<ILogger>();
            var eventStoreRepository = A.Fake<IMessageQueueRepository>();
            A.CallTo(() => eventStoreRepository.GetDeadLetterListLength<TestMessage>()).Returns(1);
            var message = new TestRetryHoursMessage { Id = "0", TimeStamp = DateTimeOffset.UtcNow.AddHours(-3) };
            A.CallTo(() => eventStoreRepository.GetUnprocessedMessage<TestMessage>(0)).Returns(JsonConvert.SerializeObject(message));
            var retryData = new RetryData
            {
                LastRetryTime = DateTimeOffset.UtcNow.AddHours(-1),
                RetryCount = 6
            };
            A.CallTo(() => eventStoreRepository.GetRetryData(A<TestMessage>._)).Returns(retryData);
            var retryClass = new TestRetryClass(logger, eventStoreRepository);
            var callBackCalled = false;

            await retryClass.ExecuteRetry<TestMessage>(async @event => {
                await Task.Run(() => { callBackCalled = true; });
            });

            Assert.IsFalse(callBackCalled);
        }
    }

    public class TestRetryClass : Retry
    {
        public TestRetryClass(ILogger logger, IMessageQueueRepository eventStoreRepository)
            : base(logger, eventStoreRepository)
        {
        }
    }

    public class TestMessage : RetryableMessage
    {
    }

    public class TestRetryHoursMessage : RetryableMessage
    {
        public override int RetryForHours { get; set; } = 2;
    }
}
