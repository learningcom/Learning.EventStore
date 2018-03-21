using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using FakeItEasy;
using Learning.EventStore.Common.Sql;
using Learning.EventStore.Test.Mocks;
using Learning.MessageQueue;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Learning.EventStore.Test.SqlServerEventStore
{
    [TestClass]
    public class GetTest
    {
        private readonly IEnumerable<IEvent> _events;

        public GetTest()
        {
            var sqlServerClient = A.Fake<ISqlClient>();
            var messageQueue = A.Fake<IMessageQueue>();
            var settings = new SqlEventStoreSettings(new SqlConnectionStringBuilder(), "TestApp");
            var sqlEventStore = new DataStores.SqlEventStore(messageQueue, sqlServerClient, settings);
            var jsonSerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            var evenList = new List<string>
            {
                JsonConvert.SerializeObject(new TestEvent { Version = 1 }, jsonSerializerSettings),
                JsonConvert.SerializeObject(new TestEvent { Version = 2 }, jsonSerializerSettings),
                JsonConvert.SerializeObject(new TestEvent { Version = 3 }, jsonSerializerSettings)
            };

            A.CallTo(() => sqlServerClient.GetEvents(A<string>._, 1)).Returns(evenList);

            _events = sqlEventStore.GetAsync(Guid.Empty.ToString(), 1).Result;
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
