using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Learning.EventStore.Domain.Exceptions;
using Learning.EventStore.Extensions;
using Learning.EventStore.Infrastructure;
using Learning.EventStore.Messages;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Learning.EventStore.MessageQueue
{
    public class RedisMessageQueue : IMessageQueue
    {
        private readonly IRedisClient _redis;
        private readonly EventStoreSettings _settings;
        private readonly string _environment;
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

        public RedisMessageQueue(IRedisClient redis, string keyPrefix, string environment)
            : this(redis, new EventStoreSettings { KeyPrefix = keyPrefix }, environment)
        {
        }

        public RedisMessageQueue(IRedisClient redis, EventStoreSettings settings, string environment)
        {
            if (string.IsNullOrWhiteSpace(settings.KeyPrefix))
            {
                throw new ArgumentException("KeyPrefix must be specified in EventStoreSettings");
            }

            _redis = redis;
            _settings = settings;
            _environment = environment;
        }

        public async Task PublishAsync(IMessage message)
        {
            var serializedEvent = JsonConvert.SerializeObject(message, JsonSerializerSettings);
            var messageType = message.GetType().Name;

            await PublishAsync(serializedEvent, message.Id, messageType).ConfigureAwait(false);
        }

        public async Task PublishAsync(string serializedMessage, string messageId, string messageType)
        {
            //Publish the event
            for (var i = 0; i < _settings.TransactionRetryCount; i++)
            {
                var publishTran = await GeneratePublishTransaction(serializedMessage, messageType).ConfigureAwait(false);
                if (await publishTran.ExecuteAsync().ConfigureAwait(false))
                {
                    return;
                }

                await Task.Delay(_settings.TransactionRetryDelay).ConfigureAwait(false);
            }

            throw new MessagePublishFailedException(messageId, _settings.TransactionRetryCount);
        }

        private async Task<ITransaction> GeneratePublishTransaction(string serializedEvent, string messageType)
        {
            var eventKey = $"{_environment}:{messageType}";

            //Get all registered subscribers for this event stored in the Redis set at 'subscriberKey'
            var subscriberKey = $"Subscribers:{{{eventKey}}}";
            var subscribers = await _redis.SetMembersAsync(subscriberKey).ConfigureAwait(false);

            //Create a Redis transaction
            var tran = _redis.Database.CreateTransaction();
            tran.AddCondition(Condition.SetLengthEqual(subscriberKey, subscribers.Length));

            /*
            Push event data into the queue for each type of subscriber
            Ensures that even if there are multiple instances of a particular subscriber, the event will only be processed once.
            */
            foreach (var subscriber in subscribers)
            {
                var listKey = $"{subscriber}:{{{eventKey}}}:PublishedEvents";

                //Write the commitId to a list mapping commitIds to individual events for a particular aggregate (@event.Id)
                tran.ListRightPushAsync(listKey, serializedEvent).ConfigureAwait(false);
            }

            /*
            Publish event that notifies subscribers that an item was added to the queue
            All instances will receive the notification, but only one will actually process it since the event can only be popped from the queue once
            */
            tran.PublishAsync(eventKey, true).ConfigureAwait(false);

            return tran;
        }
    }
}
