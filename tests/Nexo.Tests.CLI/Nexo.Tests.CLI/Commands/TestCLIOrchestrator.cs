using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Commands;

namespace Nexo.Tests.CLI.Commands
{
    /// <summary>
    /// Orchestrator for CLI testing commands with comprehensive error handling
    /// </summary>
    public class TestCLIOrchestrator : IDisposable
    {
        private readonly List<ICommand<object, object>> _commands = new();
        private readonly SemaphoreSlim _orchestratorSemaphore = new(1, 1);
        private readonly Dictionary<string, int> _testRetryCounts = new();
        private const int MaxOrchestrationRetries = 2;
        private const int DefaultOrchestrationTimeoutMs = 30000;

        public void RegisterCommand<TInput, TOutput>(ICommand<TInput, TOutput> command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            
            _commands.Add((ICommand<object, object>)command);
        }

        public async Task<TestCLIOrchestrationResult> ExecuteCLITestSuiteAsync(TestCLIOrchestrationInput input)
        {
            if (input == null)
            {
                return new TestCLIOrchestrationResult
                {
                    Success = false,
                    ErrorMessage = "Input cannot be null",
                    ExecutionTime = TimeSpan.Zero
                };
            }

            var startTime = DateTime.UtcNow;
            var results = new List<TestCLIOutput>();
            var orchestrationAttempts = 0;
            Exception? lastException = null;

            while (orchestrationAttempts < MaxOrchestrationRetries)
            {
                try
                {
                    orchestrationAttempts++;
                    await _orchestratorSemaphore.WaitAsync();
                    
                    using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(input.TimeoutMs ?? DefaultOrchestrationTimeoutMs));
                    
                    // Validate orchestration input
                    var validationResult = ValidateOrchestrationInput(input);
                    if (!validationResult.IsValid)
                    {
                        return new TestCLIOrchestrationResult
                        {
                            Success = false,
                            ErrorMessage = $"Orchestration input validation failed: {validationResult.ErrorMessage}",
                            ExecutionTime = DateTime.UtcNow - startTime
                        };
                    }

                    // Execute CLI version test with retry logic
                    var versionResult = await ExecuteTestWithRetryAsync(
                        "Version Test",
                        () => new TestCLIInput 
                        { 
                            TestName = "Version Test", 
                            Arguments = new[] { "--version" },
                            TimeoutMs = input.TestTimeoutMs,
                            EnableRetries = input.EnableRetries
                        },
                        cts.Token);

                    if (versionResult != null)
                        results.Add(versionResult);

                    // Execute CLI help test with retry logic
                    var helpResult = await ExecuteTestWithRetryAsync(
                        "Help Test",
                        () => new TestCLIInput 
                        { 
                            TestName = "Help Test", 
                            Arguments = new[] { "--help" },
                            TimeoutMs = input.TestTimeoutMs,
                            EnableRetries = input.EnableRetries
                        },
                        cts.Token);

                    if (helpResult != null)
                        results.Add(helpResult);

                    // Execute CLI argument parsing test with retry logic
                    var argResult = await ExecuteTestWithRetryAsync(
                        "Argument Parsing Test",
                        () => new TestCLIInput 
                        { 
                            TestName = "Argument Parsing Test", 
                            Arguments = input.TestArguments ?? Array.Empty<string>(),
                            TimeoutMs = input.TestTimeoutMs,
                            EnableRetries = input.EnableRetries
                        },
                        cts.Token);

                    if (argResult != null)
                        results.Add(argResult);

                    // Execute additional tests if specified
                    if (input.IncludeVerboseTests)
                    {
                        var verboseResult = await ExecuteTestWithRetryAsync(
                            "Verbose Test",
                            () => new TestCLIInput 
                            { 
                                TestName = "Verbose Test", 
                                Arguments = new[] { "--verbose" },
                                Verbose = true,
                                TimeoutMs = input.TestTimeoutMs,
                                EnableRetries = input.EnableRetries
                            },
                            cts.Token);

                        if (verboseResult != null)
                            results.Add(verboseResult);
                    }

                    var endTime = DateTime.UtcNow;
                    var totalDuration = endTime - startTime;

                    return new TestCLIOrchestrationResult
                    {
                        Success = results.Count > 0 && results.All(r => r.Success),
                        TotalTests = results.Count,
                        PassedTests = results.Count(r => r.Success),
                        FailedTests = results.Count(r => !r.Success),
                        ExecutionTime = totalDuration,
                        TestResults = results.ToArray(),
                        Warnings = results.SelectMany(r => r.Warnings).ToArray(),
                        Errors = results.SelectMany(r => r.Errors).ToArray(),
                        OrchestrationAttempts = orchestrationAttempts
                    };
                }
                catch (OperationCanceledException) when (orchestrationAttempts < MaxOrchestrationRetries)
                {
                    lastException = new TimeoutException($"CLI orchestration timed out after {input.TimeoutMs ?? DefaultOrchestrationTimeoutMs}ms");
                    await Task.Delay(500 * orchestrationAttempts); // Exponential backoff
                }
                catch (Exception ex) when (orchestrationAttempts < MaxOrchestrationRetries)
                {
                    lastException = ex;
                    await Task.Delay(500 * orchestrationAttempts); // Exponential backoff
                }
                catch (Exception ex)
                {
                    return new TestCLIOrchestrationResult
                    {
                        Success = false,
                        ErrorMessage = $"CLI orchestration failed after {orchestrationAttempts} attempts: {ex.Message}",
                        ExecutionTime = DateTime.UtcNow - startTime,
                        TestResults = results.ToArray()
                    };
                }
                finally
                {
                    _orchestratorSemaphore.Release();
                }
            }

            return new TestCLIOrchestrationResult
            {
                Success = false,
                ErrorMessage = $"CLI orchestration failed after {MaxOrchestrationRetries} attempts: {lastException?.Message}",
                ExecutionTime = DateTime.UtcNow - startTime,
                TestResults = results.ToArray()
            };
        }

        private async Task<TestCLIOutput?> ExecuteTestWithRetryAsync(
            string testName, 
            Func<TestCLIInput> inputFactory, 
            CancellationToken cancellationToken)
        {
            var maxRetries = _testRetryCounts.GetValueOrDefault(testName, 0) < 3 ? 3 : 1;
            
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    var command = new TestCLICommand();
                    var input = inputFactory();
                    var result = await command.ExecuteAsync(input);
                    
                    if (result.IsSuccess && result.Data != null)
                    {
                        _testRetryCounts[testName] = attempt;
                        return result.Data;
                    }
                    
                    if (attempt < maxRetries - 1)
                    {
                        await Task.Delay(100 * (attempt + 1), cancellationToken); // Exponential backoff
                    }
                }
                catch (Exception ex) when (attempt < maxRetries - 1)
                {
                    await Task.Delay(100 * (attempt + 1), cancellationToken); // Exponential backoff
                }
            }
            
            return null;
        }

        private static ValidationResult ValidateOrchestrationInput(TestCLIOrchestrationInput input)
        {
            var errors = new List<string>();

            if (input.TestTimeoutMs.HasValue && input.TestTimeoutMs.Value <= 0)
                errors.Add("Test timeout must be positive");

            if (input.TestTimeoutMs.HasValue && input.TestTimeoutMs.Value > 60000)
                errors.Add("Test timeout cannot exceed 60 seconds");

            if (input.TimeoutMs.HasValue && input.TimeoutMs.Value <= 0)
                errors.Add("Orchestration timeout must be positive");

            if (input.TimeoutMs.HasValue && input.TimeoutMs.Value > 300000)
                errors.Add("Orchestration timeout cannot exceed 5 minutes");

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                ErrorMessage = string.Join("; ", errors)
            };
        }

        public void Dispose()
        {
            _orchestratorSemaphore?.Dispose();
        }
    }

    public class TestCLIOrchestrationInput
    {
        public string[] TestArguments { get; set; } = Array.Empty<string>();
        public bool IncludeVerboseTests { get; set; }
        public string TestEnvironment { get; set; } = "Development";
        public int? TimeoutMs { get; set; }
        public int? TestTimeoutMs { get; set; }
        public bool EnableRetries { get; set; } = true;
        public bool EnableParallelExecution { get; set; } = false;
        public string[]? ExpectedTestNames { get; set; }
    }

    public class TestCLIOrchestrationResult
    {
        public bool Success { get; set; }
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string? ErrorMessage { get; set; }
        public TestCLIOutput[] TestResults { get; set; } = Array.Empty<TestCLIOutput>();
        public string[] Warnings { get; set; } = Array.Empty<string>();
        public string[] Errors { get; set; } = Array.Empty<string>();
        public int OrchestrationAttempts { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
