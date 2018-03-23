using System;
using System.Collections.Generic;
using System.Data;
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
            var readDbConnection = A.Fake<IDbConnection>();
            var sqlConnectionFactory = A.Fake<ISqlConnectionFactory>();
            var dapper = A.Fake<IDapperWrapper>();
            A.CallTo(() => sqlConnectionFactory.GetReadConnection()).Returns(readDbConnection);
            var messageQueue = A.Fake<IMessageQueue>();
            var settings = new SqlEventStoreSettings(new SqlConnectionStringBuilder(), "TestApp");
            var sqlEventStore = new DataStores.SqlEventStore(messageQueue, sqlConnectionFactory, dapper, settings);
            var jsonSerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            var evenList = new List<string>
            {
                JsonConvert.SerializeObject(new TestEvent { Version = 1 }, jsonSerializerSettings),
                JsonConvert.SerializeObject(new TestEvent { Version = 2 }, jsonSerializerSettings),
                JsonConvert.SerializeObject(new TestEvent { Version = 3 }, jsonSerializerSettings)
            };


            A.CallTo(() => dapper.QueryAsync<string>(readDbConnection, A<string>._, A<object>._, CommandType.StoredProcedure)).Returns(evenList);

            _events = sqlEventStore.GetAsync(Guid.Empty.ToString(), "TestType", 1).Result;
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
