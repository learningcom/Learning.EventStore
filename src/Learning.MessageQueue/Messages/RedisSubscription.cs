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
        private readonly bool _sequentialProcessing;

        protected RedisSubscription(IEventSubscriber subscriber)
            : this(subscriber, false, false)
        {
        }

        protected RedisSubscription(IEventSubscriber subscriber, bool useLock)
            : this(subscriber, useLock, false)
        {
        }

        protected RedisSubscription(IEventSubscriber subscriber, bool useLock, bool sequentialProcessing)
        {
            _subscriber = subscriber;
            _useLock = useLock;
            _logger = LogProvider.GetCurrentClassLogger();
            _sequentialProcessing = sequentialProcessing;
        }

        public virtual async Task SubscribeAsync()
        {
            await _subscriber.SubscribeAsync((Action<T>)CallBack, _useLock, _sequentialProcessing).ConfigureAwait(false);
            var messageName = typeof(T).Name;
            _logger.Info($"{messageName} subscription created");
        }

        protected abstract void CallBack(T message);
    }
}
