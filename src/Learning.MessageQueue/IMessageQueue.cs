using System.Threading.Tasks;
using Learning.MessageQueue.Messages;

namespace Learning.MessageQueue
{
    public interface IMessageQueue
    {
        Task PublishAsync(IMessage message);

        Task PublishAsync(IMessage message, int? capacity = null);

        Task PublishAsync(string serializedMessage, string messageId, string messageType, int? capacity = null);
    }
}
