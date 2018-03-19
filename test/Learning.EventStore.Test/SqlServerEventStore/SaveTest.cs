using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using Learning.EventStore.Common.SqlServer;
using Learning.EventStore.Test.Mocks;
using Learning.MessageQueue;
using Learning.MessageQueue.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Learning.EventStore.Test.SqlServerEventStore
{
    [TestClass]
    public class SaveTest
    {
        private readonly ISqlServerClient _sqlServerClient;
        private readonly List<TestEvent> _eventList;
        private readonly string _serializedEvent;
        private readonly IMessageQueue _messageQueue;
        private readonly DataStores.SqlServer.SqlServerEventStore _sqlEventStore;

        public SaveTest()
        {
            _sqlServerClient = A.Fake<ISqlServerClient>();
            _eventList = new List<TestEvent> {new TestEvent {Id = "12345"}};
            _messageQueue = A.Fake<IMessageQueue>();
            var settings = new SqlServerEventStoreSettings(new SqlConnectionStringBuilder());
            _sqlEventStore = new DataStores.SqlServer.SqlServerEventStore(settings, _messageQueue, _sqlServerClient);

            var jsonSerializerSettings = new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.All};
            _serializedEvent = JsonConvert.SerializeObject(_eventList.First(), jsonSerializerSettings);
        }

        [TestMethod]
        public async Task CallsMessageQueuePublish()
        {
            A.CallTo(() => _sqlServerClient.SaveEvent(A<EventDto>._)).Returns(1);
            await _sqlEventStore.SaveAsync(_eventList);

            A.CallTo(() => _messageQueue.PublishAsync(A<string>._, "12345", A<string>._ ))
                .MustHaveHappened();
        }

        [TestMethod]
        public async Task DeletesFromEventStorePublishThrowsException()
        {
            A.CallTo(() => _sqlServerClient.SaveEvent(A<EventDto>._)).Returns(1);
            A.CallTo(() => _messageQueue.PublishAsync(A<string>._, A<string>._, A<string>._)).Throws(new MessagePublishFailedException("12345", 10));

            try
            {
                await _sqlEventStore.SaveAsync(_eventList);
                Assert.Fail("Should have thrown MessagePublishFailedException");
            }
            catch (MessagePublishFailedException)
            {
                A.CallTo(() => _sqlServerClient.DeleteEvent(1)).MustHaveHappened();
            }
        }
    }
}
