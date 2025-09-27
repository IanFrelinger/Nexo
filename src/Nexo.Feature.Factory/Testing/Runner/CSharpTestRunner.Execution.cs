using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Testing.Attributes;
using Nexo.Feature.Factory.Testing.Models;
using Nexo.Feature.Factory.Testing.Commands;

namespace Nexo.Feature.Factory.Testing.Runner
{
    /// <summary>
    /// Test execution functionality
    /// </summary>
    public sealed partial class CSharpTestRunner : ITestRunner
    {
        private async Task<TestCommandResult> ExecuteTestAsync(TestInfo testInfo, TestConfiguration configuration, Dictionary<string, object> sharedData, CancellationToken cancellationToken)
        {
            var startTime = DateTimeOffset.UtcNow;
            var artifacts = new List<TestArtifact>();
            var outputData = new Dictionary<string, object>();

            try
            {
                _logger.LogInformation("Executing C# test: {TestId} with timeout: {Timeout}", 
                    testInfo.TestId, testInfo.Timeout);

                // Create test instance
                var testInstance = Activator.CreateInstance(testInfo.TestClass);
                if (testInstance == null)
                {
                    throw new InvalidOperationException($"Failed to create instance of {testInfo.TestClass.Name}");
                }

                // Inject services if the test class has a constructor that takes IServiceProvider
                var constructor = testInfo.TestClass.GetConstructor(new[] { typeof(IServiceProvider) });
                if (constructor != null)
                {
                    testInstance = Activator.CreateInstance(testInfo.TestClass, _serviceProvider);
                }

                // Run setup methods
                if (testInstance != null)
                {
                    await RunSetupMethodsAsync(testInstance, testInfo.TestClass, cancellationToken);
                }

                // Execute the test method with robust timeout monitoring
                var testExecutionTask = testInstance != null 
                    ? ExecuteTestMethodAsync(testInstance, testInfo.Method, outputData, artifacts, cancellationToken)
                    : Task.FromResult(false);
                var timeoutResult = await _timeoutManager.MonitorTestExecutionAsync(
                    testInfo.TestId,
                    testExecutionTask,
                    testInfo.Timeout,
                    cancellationToken);

                // Run cleanup methods
                if (testInstance != null)
                {
                    await RunCleanupMethodsAsync(testInstance, testInfo.TestClass, cancellationToken);
                }

                var endTime = DateTimeOffset.UtcNow;
                var duration = endTime - startTime;

                _logger.LogInformation("C# test {TestId} completed: {IsSuccess} ({Duration}ms)", 
                    testInfo.TestId, timeoutResult.IsSuccess, duration.TotalMilliseconds);

                // Add timeout information to output data
                outputData["TimeoutOccurred"] = timeoutResult.IsTimeout;
                outputData["ForceCancelled"] = timeoutResult.IsForceCancelled;
                outputData["CancellationReason"] = timeoutResult.CancellationReason ?? "Unknown";
                outputData["ActualDuration"] = duration;
                outputData["TimeoutDuration"] = testInfo.Timeout;

                var command = new SimpleTestCommand(
                    testInfo.TestId, 
                    testInfo.DisplayName, 
                    testInfo.Description,
                    testInfo.Category, 
                    testInfo.Priority, 
                    testInfo.EstimatedDuration,
                    logger: _logger);
                
                var validationResult = new TestValidationResult(true, new List<string>(), new List<string>(), TimeSpan.Zero);
                var executionResult = new Nexo.Feature.Factory.Testing.Models.TestExecutionResult(
                    timeoutResult.IsSuccess,
                    duration,
                    timeoutResult.ErrorMessage,
                    outputData,
                    new TestPerformanceMetrics(0, 0, 0, TimeSpan.Zero, artifacts.Count, 0),
                    artifacts
                );
                var cleanupResult = new TestCleanupResult(true, TimeSpan.Zero, null, 0);
                
                return new TestCommandResult(
                    command,
                    validationResult,
                    executionResult,
                    cleanupResult,
                    timeoutResult.IsSuccess
                );
            }
            catch (Exception ex)
            {
                var endTime = DateTimeOffset.UtcNow;
                var duration = endTime - startTime;
                _logger.LogError(ex, "C# test {TestId} failed", testInfo.TestId);
                
                // Add error information to output data
                outputData["Exception"] = ex.Message;
                outputData["ActualDuration"] = duration;
                outputData["TimeoutDuration"] = testInfo.Timeout;
                
                var command = new SimpleTestCommand(
                    testInfo.TestId, 
                    testInfo.DisplayName, 
                    testInfo.Description,
                    testInfo.Category, 
                    testInfo.Priority, 
                    testInfo.EstimatedDuration,
                    logger: _logger);
                
                var validationResult = new TestValidationResult(false, new[] { ex.Message }, new List<string>(), TimeSpan.Zero);
                var executionResult = new Nexo.Feature.Factory.Testing.Models.TestExecutionResult(
                    false,
                    duration,
                    ex.Message,
                    outputData,
                    new TestPerformanceMetrics(0, 0, 0, TimeSpan.Zero, artifacts.Count, 0),
                    artifacts
                );
                var cleanupResult = new TestCleanupResult(true, TimeSpan.Zero, null, 0);
                
                return new TestCommandResult(
                    command,
                    validationResult,
                    executionResult,
                    cleanupResult,
                    false
                );
            }
        }

        private async Task<bool> ExecuteTestMethodAsync(object testInstance, MethodInfo method, Dictionary<string, object> outputData, List<TestArtifact> artifacts, CancellationToken cancellationToken)
        {
            try
            {
                var result = method.Invoke(testInstance, null);
                
                if (result is Task<bool> boolTask)
                {
                    return await boolTask;
                }
                else if (result is Task task)
                {
                    await task;
                    return true;
                }
                else if (result is bool boolResult)
                {
                    return boolResult;
                }
                else
                {
                    return true; // Assume success if no return value
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Test method {MethodName} threw an exception", method.Name);
                outputData["Exception"] = ex.Message;
                return false;
            }
        }
    }
}
