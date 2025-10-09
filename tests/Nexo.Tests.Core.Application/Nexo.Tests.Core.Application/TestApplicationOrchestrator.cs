using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces;

namespace Nexo.Tests.Core.Application.Commands
{
    /// <summary>
    /// Orchestrator for application layer testing
    /// </summary>
    public class TestApplicationOrchestrator
    {
        private readonly List<ICommand<object, object>> _commands = new();

        public void RegisterCommand<TInput, TOutput>(ICommand<TInput, TOutput> command)
        {
            _commands.Add((ICommand<object, object>)command);
        }

        public async Task<TestApplicationOrchestrationResult> ExecuteApplicationTestSuiteAsync(TestApplicationOrchestrationInput input)
        {
            var results = new List<TestApplicationOutput>();
            var startTime = DateTime.UtcNow;

            try
            {
                // Execute project tests
                if (input.IncludeProjectTests)
                {
                    var projectTest = new TestProjectCommand();
                    var projectInput = new TestProjectInput 
                    { 
                        ProjectName = "Test Project", 
                        ProjectPath = "/tmp/test",
                        IncludeValidation = true,
                        IncludeConfiguration = true
                    };
                    var projectResult = await projectTest.ExecuteAsync(projectInput);
                    
                    if (projectResult.IsSuccess && projectResult.Data != null)
                    {
                        results.Add(new TestApplicationOutput
                        {
                            TestType = "Project",
                            Success = projectResult.Data.Success,
                            ExecutionTime = projectResult.Data.ExecutionTime,
                            TestResults = projectResult.Data.TestResults
                        });
                    }
                }

                // Execute agent tests
                if (input.IncludeAgentTests)
                {
                    var agentTest = new TestAgentCommand();
                    var agentInput = new TestAgentInput 
                    { 
                        AgentName = "Test Agent", 
                        AgentType = "CrossPlatform",
                        IncludeInitialization = true,
                        IncludeCapabilities = true
                    };
                    var agentResult = await agentTest.ExecuteAsync(agentInput);
                    
                    if (agentResult.IsSuccess && agentResult.Data != null)
                    {
                        results.Add(new TestApplicationOutput
                        {
                            TestType = "Agent",
                            Success = agentResult.Data.Success,
                            ExecutionTime = agentResult.Data.ExecutionTime,
                            TestResults = agentResult.Data.TestResults
                        });
                    }
                }

                // Execute assembly tests
                if (input.IncludeAssemblyTests)
                {
                    var assemblyTest = new TestAssemblyCommand();
                    var assemblyInput = new TestAssemblyInput 
                    { 
                        AssemblyPath = "/tmp/test.dll",
                        IncludeAnalysis = true,
                        IncludeDecompilation = true,
                        IncludeSecurityScan = true
                    };
                    var assemblyResult = await assemblyTest.ExecuteAsync(assemblyInput);
                    
                    if (assemblyResult.IsSuccess && assemblyResult.Data != null)
                    {
                        results.Add(new TestApplicationOutput
                        {
                            TestType = "Assembly",
                            Success = assemblyResult.Data.Success,
                            ExecutionTime = assemblyResult.Data.ExecutionTime,
                            TestResults = assemblyResult.Data.TestResults
                        });
                    }
                }

                var endTime = DateTime.UtcNow;
                var totalDuration = endTime - startTime;

                return new TestApplicationOrchestrationResult
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
                return new TestApplicationOrchestrationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime,
                    TestResults = results.ToArray()
                };
            }
        }
    }

    public class TestApplicationOrchestrationInput
    {
        public bool IncludeProjectTests { get; set; } = true;
        public bool IncludeAgentTests { get; set; } = true;
        public bool IncludeAssemblyTests { get; set; } = true;
        public string TestEnvironment { get; set; } = "Development";
    }

    public class TestApplicationOrchestrationResult
    {
        public bool Success { get; set; }
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string? ErrorMessage { get; set; }
        public TestApplicationOutput[] TestResults { get; set; } = Array.Empty<TestApplicationOutput>();
    }

    public class TestApplicationOutput
    {
        public string TestType { get; set; } = string.Empty;
        public bool Success { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string[] TestResults { get; set; } = Array.Empty<string>();
    }
}
