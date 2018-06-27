using System.Threading.Tasks;

namespace Learning.MessageQueue.Messages
{
    public interface IRetryableSubscription : ISubscription
    {
        int RetryForHours { get; set; }
        int RetryLimit { get; set; }
        int RetryIntervalMinutes { get; set; }
        int RetryIntervalMaxMinutes { get; set; }

        Task RetryAsync();
    }
}
