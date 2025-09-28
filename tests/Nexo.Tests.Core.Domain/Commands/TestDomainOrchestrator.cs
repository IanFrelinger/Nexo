using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Core.Application.Commands;

namespace Nexo.Tests.Core.Domain.Commands
{
    /// <summary>
    /// Orchestrator for domain layer testing
    /// </summary>
    public class TestDomainOrchestrator
    {
        private readonly List<ICommand<object, object>> _commands = new();

        public void RegisterCommand<TInput, TOutput>(ICommand<TInput, TOutput> command)
        {
            _commands.Add((ICommand<object, object>)command);
        }

        public async Task<TestDomainOrchestrationResult> ExecuteDomainTestSuiteAsync(TestDomainOrchestrationInput input)
        {
            var results = new List<TestDomainOutput>();
            var startTime = DateTime.UtcNow;

            try
            {
                // Execute agent domain tests
                if (input.IncludeAgentTests)
                {
                    var agentTest = new TestAgentDomainCommand();
                    var agentInput = new TestAgentDomainInput 
                    { 
                        AgentName = "Test Agent", 
                        AgentRole = "Developer",
                        Platform = "Windows",
                        IncludeCapabilities = true,
                        IncludeStatus = true
                    };
                    var agentResult = await agentTest.ExecuteAsync(agentInput);
                    
                    if (agentResult.IsSuccess && agentResult.Data != null)
                    {
                        results.Add(new TestDomainOutput
                        {
                            TestType = "Agent",
                            Success = agentResult.Data.Success,
                            ExecutionTime = agentResult.Data.ExecutionTime,
                            TestResults = agentResult.Data.TestResults
                        });
                    }
                }

                // Execute value object tests
                if (input.IncludeValueObjectTests)
                {
                    var valueObjectTest = new TestValueObjectCommand();
                    var valueObjectInput = new TestValueObjectInput 
                    { 
                        ValueObjectType = "All",
                        AgentName = "Test Agent",
                        IncludeEqualityTests = true,
                        IncludeCreationTests = true
                    };
                    var valueObjectResult = await valueObjectTest.ExecuteAsync(valueObjectInput);
                    
                    if (valueObjectResult.IsSuccess && valueObjectResult.Data != null)
                    {
                        results.Add(new TestDomainOutput
                        {
                            TestType = "ValueObject",
                            Success = valueObjectResult.Data.Success,
                            ExecutionTime = valueObjectResult.Data.ExecutionTime,
                            TestResults = valueObjectResult.Data.TestResults
                        });
                    }
                }

                var endTime = DateTime.UtcNow;
                var totalDuration = endTime - startTime;

                return new TestDomainOrchestrationResult
                {
                    Success = results.All(r => r.Success),
                    TotalTests = results.Count,
                    PassedTests = results.Count(r => r.Success),
                    FailedTests = results.Count(r => !r.Success),
                    ExecutionTime = totalDuration,
                    TestResults = results.ToArray()
                };
            }
            catch (Exception ex)
            {
                return new TestDomainOrchestrationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime,
                    TestResults = results.ToArray()
                };
            }
        }
    }

    public class TestDomainOrchestrationInput
    {
        public bool IncludeAgentTests { get; set; } = true;
        public bool IncludeValueObjectTests { get; set; } = true;
        public string TestEnvironment { get; set; } = "Development";
    }

    public class TestDomainOrchestrationResult
    {
        public bool Success { get; set; }
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string? ErrorMessage { get; set; }
        public TestDomainOutput[] TestResults { get; set; } = Array.Empty<TestDomainOutput>();
    }

    public class TestDomainOutput
    {
        public string TestType { get; set; } = string.Empty;
        public bool Success { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string[] TestResults { get; set; } = Array.Empty<string>();
    }
}
