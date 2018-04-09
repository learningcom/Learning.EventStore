using System;
using System.Linq;
using System.Threading.Tasks;
using Learning.EventStore.Sample.Web.Models.Events;
using Learning.EventStore.Sample.Web.Models.ReadModel.Infrastructure;
using Learning.MessageQueue;
using Learning.MessageQueue.Messages;
using Microsoft.Extensions.Logging;

namespace Learning.EventStore.Sample.Web.Models.ReadModel.Subscriptions
{
    public class InventoryItemRenamedSubscription : ISubscription
    {
        private readonly IEventSubscriber _subscriber;
        private readonly ILogger _logger;

        public InventoryItemRenamedSubscription(IEventSubscriber subscriber, ILogger logger)
        {
            _subscriber = subscriber;
            _logger = logger;
        }

        public async Task SubscribeAsync()
        {
            void CallBack(InventoryItemRenamed data)
            {
                _logger.LogInformation($"Beginning processing of InventoryItemRenamed event for AggregateId {data.Id}");
                InMemoryDatabase.Details[data.Id].Name = data.Name;
                _logger.LogInformation($"Completed processing of InventoryItemRenamed event for AggregateId {data.Id}");
            }

            await _subscriber.SubscribeAsync((Action<InventoryItemRenamed>)CallBack);
            _logger.LogInformation("InventoryItemRenamed subscription created");
        }
    }
}
