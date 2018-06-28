using System;
using System.Threading.Tasks;
#if !NET46 && !NET452
using Microsoft.Extensions.Logging;
#endif

namespace Learning.MessageQueue.Messages
{
    public abstract class RedisSubscription<T> : ISubscription where T : IMessage
    {
        private readonly IEventSubscriber _subscriber;

#if !NET46 && !NET452
        private readonly ILogger _logger;

        protected RedisSubscription(IEventSubscriber subscriber, ILogger logger)
        {
            _subscriber = subscriber;
            _logger = logger;
        }
#endif

        protected RedisSubscription(IEventSubscriber subscriber)
        {
            _subscriber = subscriber;
        }

        public virtual async Task SubscribeAsync()
        {
            await _subscriber.SubscribeAsync((Action<T>)CallBack).ConfigureAwait(false);
            var messageName = typeof(T).Name;
            LogInformation($"{messageName} subscription created");
        }

        protected abstract void CallBack(T message);

        protected virtual void LogInformation(string message)
        {
#if !NET46 && !NET452
            _logger.LogInformation(message);
#endif
        }

        protected virtual void LogWarning(string message)
        {
#if !NET46 && !NET452
            _logger.LogWarning(message);
#endif
        }
    }
}
