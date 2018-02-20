using System.Threading.Tasks;
using Learning.MessageQueue.Messages;

namespace Learning.MessageQueue
{
    public interface IMessageQueue
    {
        Task PublishAsync(IMessage message);

        Task PublishAsync(string serializedMessage, string messageId, string messageType);
    }
}
