using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Learning.EventStore.Sample.Web.Models.Events;
using Learning.EventStore.Sample.Web.Models.ReadModel.Dto;
using Learning.EventStore.Sample.Web.Models.ReadModel.Infrastructure;
using Learning.MessageQueue;
using Learning.MessageQueue.Messages;
using Microsoft.Extensions.Logging;

namespace Learning.EventStore.Sample.Web.Models.ReadModel.Subscriptions
{
    public class InventoryItemCreatedSubscription : ISubscription
    {
        private readonly IEventSubscriber _subscriber;
        private readonly ILogger _logger;

        public InventoryItemCreatedSubscription(IEventSubscriber subscriber, ILogger logger)
        {
            _subscriber = subscriber;
            _logger = logger;
        }

        public async Task SubscribeAsync()
        {
            void CallBack(InventoryItemCreated data)
            {
                _logger.LogInformation($"Beginning processing of InventoryItemCreated event for AggregateId {data.Id}");
                var inventoryItem = new InventoryItemDetails(data.Id, data.Name, 0, data.Version);
                InMemoryDatabase.Details.Add(data.Id, inventoryItem);
                _logger.LogInformation($"Completed processing of InventoryItemCreated event for AggregateId {data.Id}");
            }

            await _subscriber.SubscribeAsync((Action<InventoryItemCreated>)CallBack);
            _logger.LogInformation("InventoryItemCreated subscription created");
        }
    }
}
