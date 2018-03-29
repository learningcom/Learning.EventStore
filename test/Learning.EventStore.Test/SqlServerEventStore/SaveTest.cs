using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using Learning.EventStore.Common.Sql;
using Learning.EventStore.DataStores;
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
        private readonly List<TestEvent> _eventList;
        private readonly string _serializedEvent;
        private readonly IMessageQueue _messageQueue;
        private readonly SqlEventStore _sqlEventStore;
        private readonly IDapperWrapper _dapper;
        private readonly IDbConnection _writeDbConnection;
        private readonly IDbTransaction _transaction;

        public SaveTest()
        {
            _writeDbConnection = A.Fake<IDbConnection>();
            _transaction = A.Fake<IDbTransaction>();
            A.CallTo(() => _writeDbConnection.BeginTransaction()).Returns(_transaction);
            var sqlConnectionFactory = A.Fake<ISqlConnectionFactory>();
            _dapper = A.Fake<IDapperWrapper>();
            A.CallTo(() => sqlConnectionFactory.GetWriteConnection()).Returns(_writeDbConnection);
            _eventList = new List<TestEvent> {new TestEvent {Id = "12345"}};
            _messageQueue = A.Fake<IMessageQueue>();
            var settings = A.Fake<ISqlEventStoreSettings>();
            A.CallTo(() => settings.CommandType).Returns(CommandType.StoredProcedure);
            _sqlEventStore = new SqlEventStore(_messageQueue, sqlConnectionFactory, _dapper, settings);

            var jsonSerializerSettings = new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.All};
            _serializedEvent = JsonConvert.SerializeObject(_eventList.First(), jsonSerializerSettings);
        }

        [TestMethod]
        public async Task CallsMessageQueuePublish()
        {
            await _sqlEventStore.SaveAsync(_eventList);

            A.CallTo(() => _dapper.ExecuteAsync(_writeDbConnection, A<string>._, A<EventDto>._, CommandType.StoredProcedure, _transaction)).MustHaveHappened();
            A.CallTo(() => _messageQueue.PublishAsync(_serializedEvent, "12345", A<string>._ ))
                .MustHaveHappened();
            A.CallTo(() => _transaction.Commit()).MustHaveHappened();
        }

        [TestMethod]
        public async Task RollsBackTransactionIfPublishFails()
        {
            A.CallTo(() => _messageQueue.PublishAsync(_serializedEvent, "12345", A<string>._)).Throws(new Exception("Publish Failed"));

            try
            {
                await _sqlEventStore.SaveAsync(_eventList);
            }
            catch (Exception e)
            {
                A.CallTo(() => _dapper.ExecuteAsync(_writeDbConnection, A<string>._, A<EventDto>._, CommandType.StoredProcedure, _transaction)).MustHaveHappened();
                A.CallTo(() => _messageQueue.PublishAsync(_serializedEvent, "12345", A<string>._))
                    .MustHaveHappened();
                A.CallTo(() => _transaction.Commit()).MustNotHaveHappened();
                A.CallTo(() => _transaction.Rollback()).MustHaveHappened();
            }
        }
    }
}
