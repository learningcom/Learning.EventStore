using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using Learning.EventStore.Common.Sql;
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
        private readonly ISqlClient _sqlClient;
        private readonly List<TestEvent> _eventList;
        private readonly string _serializedEvent;
        private readonly IMessageQueue _messageQueue;
        private readonly DataStores.SqlEventStore _sqlEventStore;

        public SaveTest()
        {
            _sqlClient = A.Fake<ISqlClient>();
            _eventList = new List<TestEvent> {new TestEvent {Id = "12345"}};
            _messageQueue = A.Fake<IMessageQueue>();
            var settings = new SqlEventStoreSettings(new SqlConnectionStringBuilder(), "TestApp");
            _sqlEventStore = new DataStores.SqlEventStore(_messageQueue, _sqlClient, settings);

            var jsonSerializerSettings = new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.All};
            _serializedEvent = JsonConvert.SerializeObject(_eventList.First(), jsonSerializerSettings);
        }

        [TestMethod]
        public async Task CallsMessageQueuePublish()
        {
            await _sqlEventStore.SaveAsync(_eventList);

            A.CallTo(() => _sqlClient.SaveEvent(A<EventDto>._)).MustHaveHappened();
            A.CallTo(() => _messageQueue.PublishAsync(_serializedEvent, "12345", A<string>._ ))
                .MustHaveHappened();
        }
    }
}
