using System.Threading.Tasks;

namespace Learning.MessageQueue.Messages
{
    public interface ISubscription
    {
        Task SubscribeAsync();
    }
}
