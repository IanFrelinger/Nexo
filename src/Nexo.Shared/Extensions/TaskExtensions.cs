using System;
using System.Threading.Tasks;

namespace Nexo.Shared.Extensions
{
    /// <summary>
    /// Extension methods for task operations
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// Executes an action with a timeout
        /// </summary>
        public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
        {
            var timeoutTask = Task.Delay(timeout);
            var completedTask = await Task.WhenAny(task, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                throw new TimeoutException($"Operation timed out after {timeout.TotalMilliseconds}ms");
            }
            
            return await task;
        }

        /// <summary>
        /// Retries an operation with exponential backoff
        /// </summary>
        public static async Task<T> RetryWithBackoff<T>(
            this Func<Task<T>> operation,
            int maxRetries = 3,
            TimeSpan initialDelay = default,
            double backoffMultiplier = 2.0)
        {
            if (initialDelay == default)
                initialDelay = TimeSpan.FromMilliseconds(100);

            var delay = initialDelay;
            Exception? lastException = null;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    if (i < maxRetries - 1)
                    {
                        await Task.Delay(delay);
                        delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * backoffMultiplier);
                    }
                }
            }

            throw lastException ?? new InvalidOperationException("Retry operation failed");
        }
    }
}
