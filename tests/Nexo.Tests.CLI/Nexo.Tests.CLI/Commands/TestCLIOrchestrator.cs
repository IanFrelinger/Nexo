using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Core.Application.Commands;

namespace Nexo.Tests.CLI.Commands
{
    /// <summary>
    /// Orchestrator for CLI testing commands
    /// </summary>
    public class TestCLIOrchestrator
    {
        private readonly List<ICommand<object, object>> _commands = new();

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

            try
            {
                // Execute CLI version test
                var versionCommand = new TestCLICommand();
                var versionInput = new TestCLIInput 
                { 
                    TestName = "Version Test", 
                    Arguments = new[] { "--version" }
                };
                var versionResult = await versionCommand.ExecuteAsync(versionInput);
                
                if (versionResult.IsSuccess && versionResult.Data != null)
                {
                    results.Add(versionResult.Data);
                }

                // Execute CLI help test
                var helpCommand = new TestCLICommand();
                var helpInput = new TestCLIInput 
                { 
                    TestName = "Help Test", 
                    Arguments = new[] { "--help" }
                };
                var helpResult = await helpCommand.ExecuteAsync(helpInput);
                
                if (helpResult.IsSuccess && helpResult.Data != null)
                {
                    results.Add(helpResult.Data);
                }

                // Execute CLI argument parsing test
                var argCommand = new TestCLICommand();
                var argInput = new TestCLIInput 
                { 
                    TestName = "Argument Parsing Test", 
                    Arguments = input.TestArguments ?? Array.Empty<string>()
                };
                var argResult = await argCommand.ExecuteAsync(argInput);
                
                if (argResult.IsSuccess && argResult.Data != null)
                {
                    results.Add(argResult.Data);
                }

                // Execute additional tests if specified
                if (input.IncludeVerboseTests)
                {
                    var verboseCommand = new TestCLICommand();
                    var verboseInput = new TestCLIInput 
                    { 
                        TestName = "Verbose Test", 
                        Arguments = new[] { "--verbose" },
                        Verbose = true
                    };
                    var verboseResult = await verboseCommand.ExecuteAsync(verboseInput);
                    
                    if (verboseResult.IsSuccess && verboseResult.Data != null)
                    {
                        results.Add(verboseResult.Data);
                    }
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
                    TestResults = results.ToArray()
                };
            }
            catch (Exception ex)
            {
                return new TestCLIOrchestrationResult
                {
                    Success = false,
                    ErrorMessage = $"CLI orchestration failed: {ex.Message}",
                    ExecutionTime = DateTime.UtcNow - startTime,
                    TestResults = results.ToArray()
                };
            }
        }


    }

    public class TestCLIOrchestrationInput
    {
        public string[] TestArguments { get; set; } = Array.Empty<string>();
        public bool IncludeVerboseTests { get; set; }
        public string TestEnvironment { get; set; } = "Development";
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
    }
}
