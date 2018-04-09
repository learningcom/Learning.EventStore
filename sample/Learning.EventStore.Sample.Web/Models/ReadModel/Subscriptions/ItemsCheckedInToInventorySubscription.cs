using System;
using System.Threading.Tasks;
using Learning.EventStore.Sample.Web.Models.Events;
using Learning.EventStore.Sample.Web.Models.ReadModel.Infrastructure;
using Learning.MessageQueue;
using Learning.MessageQueue.Messages;
using Microsoft.Extensions.Logging;

namespace Learning.EventStore.Sample.Web.Models.ReadModel.Subscriptions
{
    public class ItemsCheckedInToInventorySubscription : ISubscription
    {
        private readonly IEventSubscriber _subscriber;
        private readonly ILogger _logger;

        public ItemsCheckedInToInventorySubscription(IEventSubscriber subscriber, ILogger logger)
        {
            _subscriber = subscriber;
            _logger = logger;
        }

        public async Task SubscribeAsync()
        {
            void CallBack(ItemsCheckedInToInventory data)
            {
                _logger.LogInformation($"Beginning processing of ItemsCheckedInToInventory event for AggregateId {data.Id}");
                InMemoryDatabase.Details[data.Id].CurrentCount += data.Count;
                _logger.LogInformation($"Completed processing of ItemsCheckedInToInventory event for AggregateId {data.Id}");
            }

            await _subscriber.SubscribeAsync((Action<ItemsCheckedInToInventory>)CallBack);
            _logger.LogInformation("ItemsCheckedInToInventory subscription created");
        }
    }
}
