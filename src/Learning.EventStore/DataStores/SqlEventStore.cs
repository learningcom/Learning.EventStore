using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Learning.MessageQueue;
using Newtonsoft.Json;
using System.Linq;
using Learning.EventStore.Common.Sql;

namespace Learning.EventStore.DataStores
{
    public class SqlEventStore : IEventStore
    {
        private readonly ISqlEventStoreSettings _settings;
        private readonly IMessageQueue _messageQueue;
        private readonly ISqlConnectionFactory _sqlConnectionFactory;
        private readonly IDapperWrapper _dapper;
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

        public SqlEventStore(IMessageQueue messageQueue, ISqlConnectionFactory sqlConnectionFactory, IDapperWrapper dapper, ISqlEventStoreSettings settings)
        {
            _settings = settings;
            _messageQueue = messageQueue;
            _sqlConnectionFactory = sqlConnectionFactory;
            _dapper = dapper;
        }

        public async Task SaveAsync(IEnumerable<IEvent> events)
        {
            foreach (var @event in events)
            {
                var eventData = JsonConvert.SerializeObject(@event, JsonSerializerSettings);
                var eventType = @event.GetType().Name;

                var dto = new EventDto
                {
                    AggregateId = @event.Id,
                    AggregateType = @event.AggregateType,
                    ApplicationName = _settings.ApplicationName,
                    Version = @event.Version,
                    TimeStamp = @event.TimeStamp,
                    EventType = eventType,
                    EventData = eventData
                };

                using (var conn = _sqlConnectionFactory.GetWriteConnection())
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        try
                        {
                            await _dapper.ExecuteAsync(conn, _settings.SaveSql, dto, _settings.CommandType, trans).ConfigureAwait(false);
                            await _messageQueue.PublishAsync(eventData, @event.Id, eventType).ConfigureAwait(false);
                            trans.Commit();
                        }
                        catch (Exception)
                        {
                            trans.Rollback();
                            throw;
                        }
                    }
                }
            }
        }

        public async Task<IEnumerable<IEvent>> GetAsync(string aggregateId, string aggregateType, int fromVersion)
        {
            IEnumerable<string> result;
            using (var conn = _sqlConnectionFactory.GetReadConnection())
            {
                conn.Open();
                result = await _dapper.QueryAsync<string>(conn, _settings.GetSql, new { AggregateId = aggregateId, _settings.ApplicationName, AggregateType = aggregateType, FromVersion = fromVersion }, _settings.CommandType).ConfigureAwait(false);
            }

            var events = result.Select(serializedEvent => 
                JsonConvert.DeserializeObject<IEvent>(serializedEvent, JsonSerializerSettings));

            return events;
        }
    }
}
