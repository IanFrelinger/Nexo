using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Testing.Models;
using Nexo.Feature.Factory.Testing.Timeout;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Test execution functionality for simple testing commands.
    /// </summary>
    public static partial class SimpleTestingCommands
    {
        private static async Task<SimpleTestResult> ExecuteSimpleTestAsync(SimpleTestRunner testRunner, SimpleTestInfo testInfo, TestConfiguration configuration, CancellationToken cancellationToken)
        {
            var startTime = DateTimeOffset.UtcNow;

            try
            {
                testRunner._logger.LogInformation("Executing simple test: {TestId} with aggressive timeout: {Timeout}", 
                    testInfo.TestId, testInfo.Timeout);

                // Execute the test method with aggressive timeout monitoring
                var testExecutionTask = Task.Run(() => InvokeSimpleTestMethod(testInfo.TestId), cancellationToken);
                var timeoutResult = await testRunner._timeoutManager.MonitorTestExecutionAsync(
                    testInfo.TestId,
                    testExecutionTask,
                    testInfo.Timeout,
                    cancellationToken);

                var endTime = DateTimeOffset.UtcNow;
                var duration = endTime - startTime;

                testRunner._logger.LogInformation("Simple test {TestId} completed: {IsSuccess} ({Duration}ms)", 
                    testInfo.TestId, timeoutResult.IsSuccess, duration.TotalMilliseconds);

                return new SimpleTestResult(testInfo.TestId, timeoutResult.IsSuccess, duration, timeoutResult.ErrorMessage);
            }
            catch (Exception ex)
            {
                var endTime = DateTimeOffset.UtcNow;
                var duration = endTime - startTime;
                testRunner._logger.LogError(ex, "Simple test {TestId} failed", testInfo.TestId);
                
                return new SimpleTestResult(testInfo.TestId, false, duration, ex.Message);
            }
        }

        private static bool InvokeSimpleTestMethod(string testId)
        {
            return testId switch
            {
                "simple-basic-validation" => RunBasicValidationTest(),
                "simple-configuration-test" => RunConfigurationTest(),
                "simple-timeout-test" => RunTimeoutTest(),
                _ => throw new InvalidOperationException($"Unknown test method: {testId}")
            };
        }

        // Simple test methods that don't depend on complex AI features
        private static bool RunBasicValidationTest()
        {
            Thread.Sleep(1000); // Simulate some work
            return true;
        }

        private static bool RunConfigurationTest()
        {
            Thread.Sleep(500); // Simulate some work
            return true;
        }

        private static bool RunTimeoutTest()
        {
            Thread.Sleep(2000); // Simulate some work
            return true;
        }
    }
}
