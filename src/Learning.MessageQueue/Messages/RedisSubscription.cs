using System;
using System.Threading.Tasks;
using Learning.MessageQueue.Logging;

namespace Learning.MessageQueue.Messages
{
    public abstract class RedisSubscription<T> : ISubscription where T : IMessage
    {
        private readonly IEventSubscriber _subscriber;
        private readonly bool _useLock;
        private readonly ILog _logger;

        protected RedisSubscription(IEventSubscriber subscriber)
            : this(subscriber, false)
        {
        }

        protected RedisSubscription(IEventSubscriber subscriber, bool useLock)
        {
            _subscriber = subscriber;
            _useLock = useLock;
            _logger = LogProvider.GetCurrentClassLogger();
        }

        public virtual async Task SubscribeAsync()
        {
            await _subscriber.SubscribeAsync((Action<T>)CallBack, _useLock).ConfigureAwait(false);
            var messageName = typeof(T).Name;
            _logger.Info($"{messageName} subscription created");
        }

        protected abstract void CallBack(T message);
    }
}
