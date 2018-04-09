using System;
using System.Threading.Tasks;
using Learning.EventStore.Sample.Web.Models.Events;
using Learning.EventStore.Sample.Web.Models.ReadModel.Infrastructure;
using Learning.MessageQueue;
using Learning.MessageQueue.Messages;
using Microsoft.Extensions.Logging;

namespace Learning.EventStore.Sample.Web.Models.ReadModel.Subscriptions
{
    public class ItemsRemovedFromInventorySubscription : ISubscription
    {
        private readonly IEventSubscriber _subscriber;
        private readonly ILogger _logger;

        public ItemsRemovedFromInventorySubscription(IEventSubscriber subscriber, ILogger logger)
        {
            _subscriber = subscriber;
            _logger = logger;
        }

        public async Task SubscribeAsync()
        {
            void CallBack(ItemsRemovedFromInventory data)
            {
                _logger.LogInformation($"Beginning processing of ItemsRemovedFromInventory event for AggregateId {data.Id}");
                InMemoryDatabase.Details[data.Id].CurrentCount -= data.Count;
                _logger.LogInformation($"Completed processing of ItemsRemovedFromInventory event for AggregateId {data.Id}");
            }

            await _subscriber.SubscribeAsync((Action<ItemsRemovedFromInventory>)CallBack);
            _logger.LogInformation("ItemsRemovedFromInventory subscription created");
        }
    }
}
