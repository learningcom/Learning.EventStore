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
        private readonly bool _useLock;

#if !NET46 && !NET452
        private readonly ILogger _logger;

        protected RedisSubscription(IEventSubscriber subscriber, ILogger logger)
            : this(subscriber, logger, false)
        {
        }

        protected RedisSubscription(IEventSubscriber subscriber, ILogger logger, bool useLock)
        {
            _subscriber = subscriber;
            _logger = logger;
            _useLock = useLock;
        }
#endif

        protected RedisSubscription(IEventSubscriber subscriber)
            : this(subscriber, false)
        {
        }

        protected RedisSubscription(IEventSubscriber subscriber, bool useLock)
        {
            _subscriber = subscriber;
            _useLock = useLock;
        }

        public virtual async Task SubscribeAsync()
        {
            await _subscriber.SubscribeAsync((Action<T>)CallBack, _useLock).ConfigureAwait(false);
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

        protected virtual void LogDebug(string message)
        {
#if !NET46 && !NET452
            _logger.LogDebug(message);
#endif
        }
    }
}
