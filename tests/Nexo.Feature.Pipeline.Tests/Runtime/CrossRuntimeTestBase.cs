using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace Nexo.Feature.Pipeline.Tests.Runtime
{
    /// <summary>
    /// Base class for cross-runtime tests that provides common functionality.
    /// </summary>
    public abstract class CrossRuntimeTestBase : IDisposable
    {
        protected readonly ILogger Logger;
        protected readonly ITestOutputHelper? TestOutput;
        protected readonly RuntimeDetection.RuntimeType CurrentRuntime;

        protected CrossRuntimeTestBase(ITestOutputHelper? testOutput = null)
        {
            TestOutput = testOutput;
            CurrentRuntime = RuntimeDetection.CurrentRuntime;
            
            // Create logger that outputs to test output if available
            if (testOutput != null)
            {
                Logger = new TestOutputLogger(testOutput);
            }
            else
            {
                Logger = NullLogger.Instance;
            }
            
            Logger.LogInformation($"Test initialized on runtime: {RuntimeDetection.GetRuntimeInfo()}");
        }

        /// <summary>
        /// Asserts that a condition is true, with runtime-specific messaging.
        /// </summary>
        protected void AssertRuntimeCondition(bool condition, string message = null)
        {
            var runtimeInfo = RuntimeDetection.GetRuntimeInfo();
            var fullMessage = message != null 
                ? $"{message} (Runtime: {runtimeInfo})" 
                : $"Runtime condition failed (Runtime: {runtimeInfo})";
            
            Assert.True(condition, fullMessage);
        }

        /// <summary>
        /// Asserts that an exception is thrown, with runtime-specific handling.
        /// </summary>
        protected T AssertRuntimeException<T>(Action action, string message = null) where T : Exception
        {
            try
            {
                action();
                var runtimeInfo = RuntimeDetection.GetRuntimeInfo();
                var fullMessage = message != null 
                    ? $"{message} (Runtime: {runtimeInfo})" 
                    : $"Expected exception of type {typeof(T).Name} was not thrown (Runtime: {runtimeInfo})";
                
                Assert.Fail(fullMessage);
                return null; // This should never be reached
            }
            catch (T ex)
            {
                Logger.LogInformation($"Expected exception caught on {CurrentRuntime}: {ex.GetType().Name}");
                return ex;
            }
            catch (Exception ex)
            {
                var runtimeInfo = RuntimeDetection.GetRuntimeInfo();
                var fullMessage = $"Expected exception of type {typeof(T).Name}, but got {ex.GetType().Name} (Runtime: {runtimeInfo})";
                Assert.Fail(fullMessage);
                return null; // This should never be reached
            }
        }

        /// <summary>
        /// Runs a test action with runtime-specific timeout.
        /// </summary>
        protected void RunWithRuntimeTimeout(Action action, int timeoutMs = 10000)
        {
            var adjustedTimeout = GetRuntimeAdjustedTimeout(timeoutMs);
            Logger.LogInformation($"Running test with timeout: {adjustedTimeout}ms (original: {timeoutMs}ms)");
            
            var startTime = DateTime.UtcNow;
            action();
            var elapsed = DateTime.UtcNow - startTime;
            
            Logger.LogInformation($"Test completed in {elapsed.TotalMilliseconds:F2}ms");
            
            if (elapsed.TotalMilliseconds > adjustedTimeout)
            {
                Logger.LogWarning($"Test exceeded adjusted timeout: {elapsed.TotalMilliseconds:F2}ms > {adjustedTimeout}ms");
            }
        }

        /// <summary>
        /// Gets a runtime-adjusted timeout value.
        /// </summary>
        protected int GetRuntimeAdjustedTimeout(int baseTimeoutMs)
        {
            switch (CurrentRuntime)
            {
                case RuntimeDetection.RuntimeType.Unity:
                    return baseTimeoutMs * 2; // Unity can be slower
                case RuntimeDetection.RuntimeType.Mono:
                    return baseTimeoutMs * 3; // Mono can be significantly slower
                case RuntimeDetection.RuntimeType.CoreCLR:
                    return baseTimeoutMs; // CoreCLR is typically fast
                case RuntimeDetection.RuntimeType.DotNet:
                    return baseTimeoutMs; // .NET is typically fast
                default:
                    return baseTimeoutMs * 2; // Conservative default
            }
        }

        /// <summary>
        /// Checks if the current runtime supports a specific feature.
        /// </summary>
        protected bool RuntimeSupportsFeature(string feature)
        {
            var supports = RuntimeDetection.SupportsFeature(feature);
            Logger.LogInformation($"Runtime {CurrentRuntime} supports feature '{feature}': {supports}");
            return supports;
        }

        /// <summary>
        /// Skips a test if the current runtime doesn't support a specific feature.
        /// </summary>
        protected void SkipIfFeatureNotSupported(string feature, string reason = null)
        {
            if (!RuntimeSupportsFeature(feature))
            {
                var skipReason = reason ?? $"Feature '{feature}' is not supported on runtime {CurrentRuntime}";
                Logger.LogInformation($"Skipping test: {skipReason}");
                throw new SkipException(skipReason);
            }
        }

        /// <summary>
        /// Runs a test only on specific runtimes.
        /// </summary>
        protected void RunOnlyOnRuntimes(params RuntimeDetection.RuntimeType[] runtimes)
        {
            if (!runtimes.Contains(CurrentRuntime))
            {
                var skipReason = $"Test is not supported on runtime {CurrentRuntime}. Supported runtimes: {string.Join(", ", runtimes)}";
                Logger.LogInformation($"Skipping test: {skipReason}");
                throw new SkipException(skipReason);
            }
        }

        /// <summary>
        /// Gets runtime-specific test data.
        /// </summary>
        protected T GetRuntimeSpecificData<T>(Dictionary<RuntimeDetection.RuntimeType, T> data, T defaultValue = default(T))
        {
            if (data.TryGetValue(CurrentRuntime, out var value))
            {
                Logger.LogInformation($"Using runtime-specific data for {CurrentRuntime}: {value}");
                return value;
            }
            
            Logger.LogInformation($"Using default data for {CurrentRuntime}: {defaultValue}");
            return defaultValue;
        }

        public virtual void Dispose()
        {
            Logger.LogInformation($"Test disposed on runtime: {CurrentRuntime}");
        }
    }

    /// <summary>
    /// Custom exception for skipping tests.
    /// </summary>
    public class SkipException : Exception
    {
        public SkipException(string message) : base(message) { }
    }

    /// <summary>
    /// Logger that outputs to test output.
    /// </summary>
    public class TestOutputLogger : ILogger
    {
        private readonly ITestOutputHelper _testOutput;

        public TestOutputLogger(ITestOutputHelper testOutput)
        {
            _testOutput = testOutput;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var message = formatter(state, exception);
            var logMessage = $"[{logLevel}] {message}";
            
            if (exception != null)
            {
                logMessage += $"\nException: {exception}";
            }
            
            _testOutput.WriteLine(logMessage);
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

        private class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new NullScope();
            public void Dispose() { }
        }
    }
} 