using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Core.Application.Commands;
using Nexo.Tests.CLI.Commands;
using Nexo.Tests.Core.Application.Commands;
using Nexo.Tests.Core.Domain.Commands;
using Nexo.Tests.Shared.Commands;

namespace Nexo.Tests.Integration.Commands
{
    /// <summary>
    /// Master orchestrator for running all test suites
    /// </summary>
    public class TestMasterOrchestrator
    {
        private readonly List<ICommand<object, object>> _commands = new();

        public void RegisterCommand<TInput, TOutput>(ICommand<TInput, TOutput> command)
        {
            _commands.Add((ICommand<object, object>)command);
        }

        public async Task<TestMasterOrchestrationResult> ExecuteFullTestSuiteAsync(TestMasterOrchestrationInput input)
        {
            var results = new List<TestMasterOutput>();
            var startTime = DateTime.UtcNow;

            try
            {
                // Execute CLI tests
                if (input.IncludeCLITests)
                {
                    var cliOrchestrator = new TestCLIOrchestrator();
                    var cliInput = new TestCLIOrchestrationInput
                    {
                        TestArguments = new[] { "--test", "--verbose" },
                        IncludeVerboseTests = true,
                        TestEnvironment = "Test"
                    };
                    var cliResult = await cliOrchestrator.ExecuteCLITestSuiteAsync(cliInput);
                    
                    results.Add(new TestMasterOutput
                    {
                        TestSuite = "CLI",
                        Success = cliResult.Success,
                        TotalTests = cliResult.TotalTests,
                        PassedTests = cliResult.PassedTests,
                        FailedTests = cliResult.FailedTests,
                        ExecutionTime = cliResult.ExecutionTime,
                        TestResults = cliResult.TestResults.Select(r => r.TestName).ToArray()
                    });
                }

                // Execute Core Application tests
                if (input.IncludeCoreApplicationTests)
                {
                    var appOrchestrator = new TestApplicationOrchestrator();
                    var appInput = new TestApplicationOrchestrationInput
                    {
                        IncludeProjectTests = true,
                        IncludeAgentTests = true,
                        IncludeAssemblyTests = true,
                        TestEnvironment = "Test"
                    };
                    var appResult = await appOrchestrator.ExecuteApplicationTestSuiteAsync(appInput);
                    
                    results.Add(new TestMasterOutput
                    {
                        TestSuite = "CoreApplication",
                        Success = appResult.Success,
                        TotalTests = appResult.TotalTests,
                        PassedTests = appResult.PassedTests,
                        FailedTests = appResult.FailedTests,
                        ExecutionTime = appResult.ExecutionTime,
                        TestResults = appResult.TestResults.Select(r => r.TestType).ToArray()
                    });
                }

                // Execute Core Domain tests
                if (input.IncludeCoreDomainTests)
                {
                    var domainOrchestrator = new TestDomainOrchestrator();
                    var domainInput = new TestDomainOrchestrationInput
                    {
                        IncludeAgentTests = true,
                        IncludeValueObjectTests = true,
                        TestEnvironment = "Test"
                    };
                    var domainResult = await domainOrchestrator.ExecuteDomainTestSuiteAsync(domainInput);
                    
                    results.Add(new TestMasterOutput
                    {
                        TestSuite = "CoreDomain",
                        Success = domainResult.Success,
                        TotalTests = domainResult.TotalTests,
                        PassedTests = domainResult.PassedTests,
                        FailedTests = domainResult.FailedTests,
                        ExecutionTime = domainResult.ExecutionTime,
                        TestResults = domainResult.TestResults.Select(r => r.TestType).ToArray()
                    });
                }

                // Execute Shared tests
                if (input.IncludeSharedTests)
                {
                    var sharedCommand = new TestSharedCommand();
                    var sharedInput = new TestSharedInput
                    {
                        SharedComponentType = "All",
                        IncludeResourceTests = true,
                        IncludePlatformTests = true,
                        IncludeModelTests = true
                    };
                    var sharedResult = await sharedCommand.ExecuteAsync(sharedInput);
                    
                    results.Add(new TestMasterOutput
                    {
                        TestSuite = "Shared",
                        Success = sharedResult.IsSuccess,
                        TotalTests = 1,
                        PassedTests = sharedResult.IsSuccess ? 1 : 0,
                        FailedTests = sharedResult.IsSuccess ? 0 : 1,
                        ExecutionTime = sharedResult.Data?.ExecutionTime ?? TimeSpan.Zero,
                        TestResults = new[] { "Shared Component Test" }
                    });
                }

                // Execute Integration tests
                if (input.IncludeIntegrationTests)
                {
                    var integrationCommand = new TestIntegrationCommand();
                    var integrationInput = new TestIntegrationInput
                    {
                        IntegrationTestType = "Full",
                        IncludeProjectTests = true,
                        IncludeAgentTests = true,
                        IncludeAssemblyTests = true,
                        TestEnvironment = "Test"
                    };
                    var integrationResult = await integrationCommand.ExecuteAsync(integrationInput);
                    
                    results.Add(new TestMasterOutput
                    {
                        TestSuite = "Integration",
                        Success = integrationResult.IsSuccess,
                        TotalTests = 1,
                        PassedTests = integrationResult.IsSuccess ? 1 : 0,
                        FailedTests = integrationResult.IsSuccess ? 0 : 1,
                        ExecutionTime = integrationResult.Data?.ExecutionTime ?? TimeSpan.Zero,
                        TestResults = new[] { "Integration Test" }
                    });
                }

                var endTime = DateTime.UtcNow;
                var totalDuration = endTime - startTime;

                return new TestMasterOrchestrationResult
                {
                    Success = results.All(r => r.Success),
                    TotalTestSuites = results.Count,
                    PassedTestSuites = results.Count(r => r.Success),
                    FailedTestSuites = results.Count(r => !r.Success),
                    TotalTests = results.Sum(r => r.TotalTests),
                    TotalPassedTests = results.Sum(r => r.PassedTests),
                    TotalFailedTests = results.Sum(r => r.FailedTests),
                    ExecutionTime = totalDuration,
                    TestSuiteResults = results.ToArray()
                };
            }
            catch (Exception ex)
            {
                return new TestMasterOrchestrationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime,
                    TestSuiteResults = results.ToArray()
                };
            }
        }
    }

    public class TestMasterOrchestrationInput
    {
        public bool IncludeCLITests { get; set; } = true;
        public bool IncludeCoreApplicationTests { get; set; } = true;
        public bool IncludeCoreDomainTests { get; set; } = true;
        public bool IncludeSharedTests { get; set; } = true;
        public bool IncludeIntegrationTests { get; set; } = true;
        public string TestEnvironment { get; set; } = "Development";
        public bool GenerateCoverageReport { get; set; } = true;
    }

    public class TestMasterOrchestrationResult
    {
        public bool Success { get; set; }
        public int TotalTestSuites { get; set; }
        public int PassedTestSuites { get; set; }
        public int FailedTestSuites { get; set; }
        public int TotalTests { get; set; }
        public int TotalPassedTests { get; set; }
        public int TotalFailedTests { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string? ErrorMessage { get; set; }
        public TestMasterOutput[] TestSuiteResults { get; set; } = Array.Empty<TestMasterOutput>();
    }

    public class TestMasterOutput
    {
        public string TestSuite { get; set; } = string.Empty;
        public bool Success { get; set; }
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string[] TestResults { get; set; } = Array.Empty<string>();
    }
}
