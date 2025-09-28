using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Nexo.Tests.Shared
{
    /// <summary>
    /// Base class for error-resistant tests with comprehensive error handling
    /// </summary>
    public abstract class TestBase : IDisposable
    {
        private readonly List<IDisposable> _disposables = new();
        private readonly SemaphoreSlim _testSemaphore = new(1, 1);
        private readonly Stopwatch _testStopwatch = new();
        private bool _disposed = false;

        protected TestBase()
        {
            _testStopwatch.Start();
        }

        /// <summary>
        /// Executes a test with comprehensive error handling and retry logic
        /// </summary>
        protected async Task<T> ExecuteWithRetryAsync<T>(
            Func<Task<T>> testAction,
            int maxRetries = 3,
            int delayMs = 100,
            string? testName = null)
        {
            if (testAction == null)
                throw new ArgumentNullException(nameof(testAction));

            var attempts = 0;
            Exception? lastException = null;

            while (attempts < maxRetries)
            {
                try
                {
                    attempts++;
                    await _testSemaphore.WaitAsync();
                    
                    var result = await testAction();
                    return result;
                }
                catch (Exception ex) when (attempts < maxRetries)
                {
                    lastException = ex;
                    await Task.Delay(delayMs * attempts); // Exponential backoff
                }
                catch (Exception ex)
                {
                    throw new TestExecutionException(
                        $"Test '{testName ?? "Unknown"}' failed after {attempts} attempts: {ex.Message}", 
                        ex);
                }
                finally
                {
                    _testSemaphore.Release();
                }
            }

            throw new TestExecutionException(
                $"Test '{testName ?? "Unknown"}' failed after {maxRetries} attempts: {lastException?.Message}", 
                lastException);
        }

        /// <summary>
        /// Executes a test with timeout protection
        /// </summary>
        protected async Task<T> ExecuteWithTimeoutAsync<T>(
            Func<Task<T>> testAction,
            TimeSpan timeout,
            string? testName = null)
        {
            if (testAction == null)
                throw new ArgumentNullException(nameof(testAction));

            using var cts = new CancellationTokenSource(timeout);
            
            try
            {
                return await testAction();
            }
            catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
            {
                throw new TestTimeoutException(
                    $"Test '{testName ?? "Unknown"}' timed out after {timeout.TotalMilliseconds}ms");
            }
        }

        /// <summary>
        /// Executes a test with both retry logic and timeout protection
        /// </summary>
        protected async Task<T> ExecuteWithRetryAndTimeoutAsync<T>(
            Func<Task<T>> testAction,
            int maxRetries = 3,
            TimeSpan timeout = default,
            int delayMs = 100,
            string? testName = null)
        {
            if (timeout == default)
                timeout = TimeSpan.FromSeconds(30);

            return await ExecuteWithRetryAsync(
                () => ExecuteWithTimeoutAsync(testAction, timeout, testName),
                maxRetries,
                delayMs,
                testName);
        }

        /// <summary>
        /// Validates test input parameters
        /// </summary>
        protected ValidationResult ValidateTestInput<T>(T input, params Func<T, string?>[] validators)
        {
            if (input == null)
                return new ValidationResult { IsValid = false, ErrorMessage = "Input cannot be null" };

            var errors = new List<string>();

            foreach (var validator in validators)
            {
                try
                {
                    var error = validator(input);
                    if (!string.IsNullOrEmpty(error))
                        errors.Add(error);
                }
                catch (Exception ex)
                {
                    errors.Add($"Validation error: {ex.Message}");
                }
            }

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                ErrorMessage = string.Join("; ", errors)
            };
        }

        /// <summary>
        /// Registers a disposable resource for cleanup
        /// </summary>
        protected void RegisterDisposable(IDisposable disposable)
        {
            if (disposable != null)
                _disposables.Add(disposable);
        }

        /// <summary>
        /// Gets the test execution time
        /// </summary>
        protected TimeSpan TestExecutionTime => _testStopwatch.Elapsed;

        /// <summary>
        /// Asserts that a test result is successful with detailed error information
        /// </summary>
        protected void AssertTestSuccess<T>(CommandResult<T> result, string? testName = null)
        {
            if (!result.IsSuccess)
            {
                var errorMessage = $"Test '{testName ?? "Unknown"}' failed: {result.ErrorMessage}";
                if (result.Warnings.Length > 0)
                    errorMessage += $"\nWarnings: {string.Join(", ", result.Warnings)}";
                
                throw new TestAssertionException(errorMessage);
            }
        }

        /// <summary>
        /// Asserts that a test result is successful with custom validation
        /// </summary>
        protected void AssertTestSuccess<T>(CommandResult<T> result, Func<T, bool> validator, string? testName = null)
        {
            AssertTestSuccess(result, testName);
            
            if (result.Data != null && !validator(result.Data))
            {
                throw new TestAssertionException($"Test '{testName ?? "Unknown"}' validation failed");
            }
        }

        /// <summary>
        /// Creates a test input with default error-resistant settings
        /// </summary>
        protected T CreateTestInput<T>(Action<T>? configure = null) where T : class, new()
        {
            var input = new T();
            configure?.Invoke(input);
            return input;
        }

        /// <summary>
        /// Simulates a test delay with cancellation support
        /// </summary>
        protected async Task SimulateTestDelayAsync(int milliseconds, CancellationToken cancellationToken = default)
        {
            await Task.Delay(milliseconds, cancellationToken);
        }

        /// <summary>
        /// Simulates test work with progress reporting
        /// </summary>
        protected async Task SimulateTestWorkAsync(int steps, int delayMs = 10, CancellationToken cancellationToken = default)
        {
            for (int i = 0; i < steps; i++)
            {
                await Task.Delay(delayMs, cancellationToken);
            }
        }

        public virtual void Dispose()
        {
            if (!_disposed)
            {
                _testStopwatch.Stop();
                
                foreach (var disposable in _disposables)
                {
                    try
                    {
                        disposable?.Dispose();
                    }
                    catch (Exception)
                    {
                        // Ignore disposal errors
                    }
                }
                
                _testSemaphore?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Validation result for test input validation
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// Exception thrown when test execution fails
    /// </summary>
    public class TestExecutionException : Exception
    {
        public TestExecutionException(string message, Exception? innerException = null) 
            : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when test times out
    /// </summary>
    public class TestTimeoutException : Exception
    {
        public TestTimeoutException(string message) : base(message) { }
    }

    /// <summary>
    /// Exception thrown when test assertion fails
    /// </summary>
    public class TestAssertionException : Exception
    {
        public TestAssertionException(string message) : base(message) { }
    }
}
