using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using Learning.EventStore.Extensions;
using Learning.MessageQueue;
using Newtonsoft.Json;
using System.Linq;
using Learning.EventStore.Common.SqlServer;

namespace Learning.EventStore.DataStores.SqlServer
{
    public class SqlServerEventStore : IEventStore
    {
        private readonly SqlServerEventStoreSettings _settings;
        private readonly IMessageQueue _messageQueue;
        private readonly ISqlServerClient _sqlServerClient;
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

        public SqlServerEventStore(SqlServerEventStoreSettings settings, IMessageQueue messageQueue, ISqlServerClient sqlServerClient)
        {
            _settings = settings;
            _messageQueue = messageQueue;
            _sqlServerClient = sqlServerClient;
        }

        public async Task SaveAsync(IEnumerable<IEvent> events)
        {
            foreach (var @event in events)
            {
                var serializedEvent = JsonConvert.SerializeObject(@event, JsonSerializerSettings);
                var eventData = _settings.EnableCompression
                    ? serializedEvent.Compress(_settings.CompressionThreshold)
                    : serializedEvent;

                var dto = new EventDto
                {
                    AggregateId = @event.Id,
                    Version = @event.Version,
                    TimeStamp = @event.TimeStamp,
                    EventType = @event.GetType().FullName,
                    Data = eventData,
                };

                var eventId = await _sqlServerClient.SaveEvent(dto);

                try
                {
                    await _messageQueue.PublishAsync(serializedEvent, @event.Id, @event.GetType().Name).ConfigureAwait(false);
                    return;
                }
                catch
                {
                    await _sqlServerClient.DeleteEvent(eventId).ConfigureAwait(false);
                    throw;
                }

            }
        }

        public async Task<IEnumerable<IEvent>> GetAsync(string aggregateId, int fromVersion)
        {
            var result = await _sqlServerClient.GetEvents(aggregateId, fromVersion).ConfigureAwait(false);

            var events = result.Select(serializedEvent => JsonConvert.DeserializeObject<IEvent>(serializedEvent.ToString().Decompress(), JsonSerializerSettings))
                .OrderBy(x => x.Version)
                .ToList();

            return events;
        }
    }
}
