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
            A.CallTo(() => eventStoreRepository.GetRetryCounter(A<IMessage>._)).Returns(0);
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
            A.CallTo(() => eventStoreRepository.DeleteFromDeadLetterList<TestMessage>(A<RedisValue>._))
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
            A.CallTo(() => eventStoreRepository.GetRetryCounter(A<IMessage>._)).Returns(0);
            var retryClass = new TestRetryClass(logger, eventStoreRepository, 3);

            await retryClass.ExecuteRetry<TestMessage>(async @event => {
                await Task.Run(() => throw new Exception("Oh No!"));
            });

            A.CallTo(() => eventStoreRepository.IncrementRetryCounter(A<IMessage>._)).MustHaveHappened();
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
            A.CallTo(() => eventStoreRepository.GetRetryCounter(A<IMessage>._)).Returns(3);
            var retryClass = new TestRetryClass(logger, eventStoreRepository, 3);
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

        public TestRetryClass(ILogger logger, IMessageQueueRepository eventStoreRepository, int retryThreshold)
            : base(logger, eventStoreRepository, retryThreshold)
        {
        }
    }

    public class TestMessage : IMessage
    {
        public string Id { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
    }
}
