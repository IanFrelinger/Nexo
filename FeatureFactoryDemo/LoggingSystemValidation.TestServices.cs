using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FeatureFactoryDemo
{
    /// <summary>
    /// Test service implementations
    /// </summary>
    public partial class LoggingSystemValidation
    {
        // Test services are defined in this partial class
    }

    /// <summary>
    /// Test service that uses logging to demonstrate DI patterns
    /// </summary>
    public class TestServiceWithLogging
    {
        public ILogger<TestServiceWithLogging> Logger { get; }

        public TestServiceWithLogging(ILogger<TestServiceWithLogging> logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void LogAllLevels()
        {
            Logger.LogTrace("Trace message");
            Logger.LogDebug("Debug message");
            Logger.LogInformation("Information message");
            Logger.LogWarning("Warning message");
            Logger.LogError("Error message");
            Logger.LogCritical("Critical message");
        }

        public void LogStructuredMessage(string userName, int userId, bool isActive)
        {
            Logger.LogInformation("User operation completed for {UserName} (ID: {UserId}, Active: {IsActive})", 
                userName, userId, isActive);
        }

        public void LogException(Exception exception)
        {
            Logger.LogError(exception, "An error occurred during operation");
        }

        public void UseScope()
        {
            Logger.LogInformation("Outside scope");
            using (Logger.BeginScope("OperationScope"))
            {
                Logger.LogInformation("Inside scope");
            }
            Logger.LogInformation("Outside scope again");
        }

        public async Task LogAsyncOperation()
        {
            Logger.LogInformation("Async operation started");
            await Task.Delay(10);
            Logger.LogInformation("Async operation completed");
        }
    }

    /// <summary>
    /// Test repository that uses logging to demonstrate DI patterns
    /// </summary>
    public class TestRepositoryWithLogging
    {
        public ILogger<TestRepositoryWithLogging> Logger { get; }

        public TestRepositoryWithLogging(ILogger<TestRepositoryWithLogging> logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void LogRepositoryOperation()
        {
            Logger.LogInformation("Repository operation executed");
        }
    }

    /// <summary>
    /// Test command that uses logging to demonstrate DI patterns
    /// </summary>
    public class TestCommandWithLogging
    {
        public ILogger<TestCommandWithLogging> Logger { get; }

        public TestCommandWithLogging(ILogger<TestCommandWithLogging> logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void LogCommandExecution()
        {
            Logger.LogInformation("Command executed successfully");
        }
    }

    /// <summary>
    /// Test service without logging to demonstrate DI patterns
    /// </summary>
    public class TestServiceWithoutLogging
    {
        public void DoWork()
        {
            // This service doesn't use logging
        }
    }
}
