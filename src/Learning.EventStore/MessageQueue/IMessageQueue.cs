using System.Collections.Generic;
using System.Threading.Tasks;
using Learning.EventStore.Messages;

namespace Learning.EventStore.MessageQueue
{
    public interface IMessageQueue
    {
        Task PublishAsync(IMessage message);

        Task PublishAsync(string serializedMessage, string messageId, string messageType);
    }
}
