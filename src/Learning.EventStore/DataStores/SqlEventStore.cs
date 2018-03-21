using System.Collections.Generic;
using System.Threading.Tasks;
using Learning.MessageQueue;
using Newtonsoft.Json;
using System.Linq;
using System.Transactions;
using Learning.EventStore.Common.Sql;

namespace Learning.EventStore.DataStores
{
    public class SqlEventStore
    {
        private readonly SqlEventStoreSettings _settings;
        private readonly IMessageQueue _messageQueue;
        private readonly ISqlClient _sqlClient;
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

        public SqlEventStore(IMessageQueue messageQueue, ISqlClient sqlClient, SqlEventStoreSettings settings)
        {
            _settings = settings;
            _messageQueue = messageQueue;
            _sqlClient = sqlClient;
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
                    EventData = eventData,
                };

                using (TransactionScope trans = new TransactionScope())
                {
                    await _sqlClient.SaveEvent(dto).ConfigureAwait(false);
                    await _messageQueue.PublishAsync(eventData, @event.Id, eventType).ConfigureAwait(false);
                    trans.Complete();
                }
            }
        }

        public async Task<IEnumerable<IEvent>> GetAsync(string aggregateId, int fromVersion)
        {
            var result = await _sqlClient.GetEvents(aggregateId, fromVersion).ConfigureAwait(false);

            var events = result.Select(serializedEvent => 
                JsonConvert.DeserializeObject<IEvent>(serializedEvent, JsonSerializerSettings));

            return events;
        }
    }
}
