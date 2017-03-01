using System.Threading.Tasks;

namespace Learning.EventStore.Messages
{
    public interface ISubscription
    {
        Task SubscribeAsync();
    }
}
