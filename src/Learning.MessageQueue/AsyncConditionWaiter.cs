using System;
using System.Threading.Tasks;
using System.Threading;

namespace Learning.MessageQueue
{
    public class AsyncConditionWaiter
    {
        private readonly Func<Task<bool>> _conditionCheck;
        private readonly int _pollingInterval;
        private TaskCompletionSource<object> _tcs;

        public AsyncConditionWaiter(Func<Task<bool>> conditionCheck, int pollingInterval = 100)
        {
            _conditionCheck = conditionCheck;
            _pollingInterval = pollingInterval;
            _tcs = new TaskCompletionSource<object>();
        }

        public async Task WaitForConditionAsync(CancellationToken cancellationToken = default)
        {
            while (!await _conditionCheck())
            {
                await Task.Delay(_pollingInterval, cancellationToken); // Wait before checking again to avoid busy-waiting
            }

            // Condition is met, signal completion
            _tcs.SetResult(null);
        }

        public Task WaitForCompletionAsync()
        {
            return _tcs.Task;
        }
    }
}
