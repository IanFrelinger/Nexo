using System;
using System.Threading;
using System.Threading.Tasks;
using StandaloneTestRunner.Models;

namespace StandaloneTestRunner.Services
{
    public class TimeoutService
    {
        public async Task<TestResult> ExecuteWithTimeoutAsync(
            Func<Task<TestResult>> testAction,
            int timeoutSeconds,
            int heartbeatIntervalSeconds,
            int processTimeoutMinutes)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(processTimeoutMinutes));
            var heartbeatTask = StartHeartbeatAsync(heartbeatIntervalSeconds, cts.Token);
            var testTask = testAction();

            try
            {
                var completedTask = await Task.WhenAny(testTask, Task.Delay(timeoutSeconds * 1000, cts.Token));
                
                if (completedTask == testTask)
                {
                    return await testTask;
                }
                else
                {
                    return new TestResult
                    {
                        Success = false,
                        Message = $"Test execution timed out after {timeoutSeconds} seconds",
                        Errors = { "Timeout exceeded" }
                    };
                }
            }
            catch (OperationCanceledException)
            {
                return new TestResult
                {
                    Success = false,
                    Message = $"Test execution cancelled after {processTimeoutMinutes} minutes",
                    Errors = { "Process timeout exceeded" }
                };
            }
            finally
            {
                cts.Cancel();
            }
        }

        private async Task StartHeartbeatAsync(int intervalSeconds, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine($"Heartbeat: {DateTime.Now:HH:mm:ss}");
                await Task.Delay(intervalSeconds * 1000, cancellationToken);
            }
        }
    }
}
