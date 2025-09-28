using System;
using System.Threading.Tasks;
using Nexo.Core.Application.Commands;
using Nexo.Core.Application.Commands.Project;
using Nexo.Core.Application.Commands.Agent;
using Nexo.Core.Application.Commands.Assembly;

namespace Nexo.Tests.Integration.Commands
{
    /// <summary>
    /// Test command for integration testing
    /// </summary>
    public class TestIntegrationCommand : ICommand<TestIntegrationInput, TestIntegrationOutput>
    {
        public async Task<CommandResult<TestIntegrationOutput>> ExecuteAsync(TestIntegrationInput input)
        {
            try
            {
                await Task.Delay(10); // Simulate async work
                
                var testResults = new List<string>();
                
                // Test project creation
                var createProjectCommand = new CreateProjectCommand();
                var createProjectInput = new CreateProjectInput
                {
                    Name = "Integration Test Project",
                    Path = "/tmp/integration-test",
                    Runtime = "Docker"
                };
                var createProjectResult = await createProjectCommand.ExecuteAsync(createProjectInput);
                testResults.Add($"Project creation test: {(createProjectResult.IsSuccess ? "PASSED" : "FAILED")}");
                
                // Test agent creation
                var createAgentCommand = new CreateAgentCommand();
                var createAgentInput = new CreateAgentInput
                {
                    Name = "Integration Test Agent",
                    Role = "Developer",
                    Platform = "Windows"
                };
                var createAgentResult = await createAgentCommand.ExecuteAsync(createAgentInput);
                testResults.Add($"Agent creation test: {(createAgentResult.IsSuccess ? "PASSED" : "FAILED")}");
                
                // Test assembly analysis
                var analyzeAssemblyCommand = new AnalyzeAssemblyCommand();
                var analyzeAssemblyInput = new AnalyzeAssemblyInput
                {
                    AssemblyPath = "/tmp/test.dll",
                    IncludePrivateMembers = false,
                    IncludeSecurityAnalysis = true
                };
                var analyzeAssemblyResult = await analyzeAssemblyCommand.ExecuteAsync(analyzeAssemblyInput);
                testResults.Add($"Assembly analysis test: {(analyzeAssemblyResult.IsSuccess ? "PASSED" : "FAILED")}");
                
                var output = new TestIntegrationOutput
                {
                    IntegrationTestType = input.IntegrationTestType,
                    Success = createProjectResult.IsSuccess && createAgentResult.IsSuccess && analyzeAssemblyResult.IsSuccess,
                    ExecutionTime = TimeSpan.FromMilliseconds(100),
                    TestResults = testResults.ToArray()
                };

                return CommandResult<TestIntegrationOutput>.Success(output, TimeSpan.FromMilliseconds(100));
            }
            catch (Exception ex)
            {
                return CommandResult<TestIntegrationOutput>.Failure($"Integration test failed: {ex.Message}", TimeSpan.FromMilliseconds(100));
            }
        }
    }

    public class TestIntegrationInput
    {
        public string IntegrationTestType { get; set; } = "Full";
        public bool IncludeProjectTests { get; set; } = true;
        public bool IncludeAgentTests { get; set; } = true;
        public bool IncludeAssemblyTests { get; set; } = true;
        public string TestEnvironment { get; set; } = "Development";
    }

    public class TestIntegrationOutput
    {
        public string IntegrationTestType { get; set; } = string.Empty;
        public bool Success { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string[] TestResults { get; set; } = Array.Empty<string>();
    }
}
