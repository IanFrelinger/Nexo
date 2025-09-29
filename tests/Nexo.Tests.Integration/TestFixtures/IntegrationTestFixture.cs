using System;
using System.IO;
using System.Threading.Tasks;
using Nexo.Core.Application.Commands;
using Nexo.Core.Application.Commands.Project;
using Nexo.Core.Application.Commands.Agent;
using Nexo.Core.Application.Commands.Assembly;
using Nexo.Runtime;
using Nexo.Core.Domain.Values;

namespace Nexo.Tests.Integration.TestFixtures
{
    /// <summary>
    /// Integration test fixture providing test data and setup for integration tests
    /// </summary>
    public class IntegrationTestFixture : IDisposable
    {
        public string TestProjectPath { get; private set; }
        public string TestAgentName { get; private set; }
        public string TestAssemblyPath { get; private set; }
        public AgentHost? TestAgentHost { get; private set; }
        public bool IsDisposed { get; private set; }

        public IntegrationTestFixture()
        {
            TestProjectPath = Path.Combine(Path.GetTempPath(), $"nexo-integration-test-{Guid.NewGuid():N}");
            TestAgentName = $"IntegrationTestAgent-{Guid.NewGuid():N}";
            TestAssemblyPath = Path.Combine(TestProjectPath, "test-assembly.dll");
            
            SetupTestEnvironment();
        }

        private void SetupTestEnvironment()
        {
            try
            {
                // Create test directory
                if (!Directory.Exists(TestProjectPath))
                {
                    Directory.CreateDirectory(TestProjectPath);
                }

                // Create a dummy assembly file for testing
                File.WriteAllText(TestAssemblyPath, "// Dummy assembly for integration testing");
                
                // Initialize test agent host
                TestAgentHost = new AgentHost();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to setup test environment: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Creates a test project for integration testing
        /// </summary>
        public async Task<CommandResult<CreateProjectOutput>> CreateTestProjectAsync()
        {
            var command = new CreateProjectCommand();
            var input = new CreateProjectInput
            {
                Name = "Integration Test Project",
                Path = TestProjectPath,
                Runtime = "Docker",
                Description = "Test project for integration testing"
            };

            return await command.ExecuteAsync(input);
        }

        /// <summary>
        /// Creates a test agent for integration testing
        /// </summary>
        public async Task<CommandResult<CreateAgentOutput>> CreateTestAgentAsync()
        {
            var command = new CreateAgentCommand();
            var input = new CreateAgentInput
            {
                Name = TestAgentName,
                Role = "Developer",
                Platform = "Windows",
                Capabilities = new[] { "CodeGeneration", "Analysis" }
            };

            return await command.ExecuteAsync(input);
        }

        /// <summary>
        /// Performs assembly analysis for integration testing
        /// </summary>
        public async Task<CommandResult<AnalyzeAssemblyOutput>> AnalyzeTestAssemblyAsync()
        {
            var command = new AnalyzeAssemblyCommand();
            var input = new AnalyzeAssemblyInput
            {
                AssemblyPath = TestAssemblyPath,
                IncludePrivateMembers = false,
                IncludeSecurityAnalysis = true,
                IncludeDependencies = true
            };

            return await command.ExecuteAsync(input);
        }

        /// <summary>
        /// Simulates a complete workflow for integration testing
        /// </summary>
        public async Task<IntegrationWorkflowResult> ExecuteCompleteWorkflowAsync()
        {
            var startTime = DateTime.UtcNow;
            var results = new List<string>();
            var success = true;

            try
            {
                // Step 1: Create project
                var projectResult = await CreateTestProjectAsync();
                results.Add($"Project Creation: {(projectResult.IsSuccess ? "SUCCESS" : "FAILED")}");
                if (!projectResult.IsSuccess) success = false;

                // Step 2: Create agent
                var agentResult = await CreateTestAgentAsync();
                results.Add($"Agent Creation: {(agentResult.IsSuccess ? "SUCCESS" : "FAILED")}");
                if (!agentResult.IsSuccess) success = false;

                // Step 3: Analyze assembly
                var assemblyResult = await AnalyzeTestAssemblyAsync();
                results.Add($"Assembly Analysis: {(assemblyResult.IsSuccess ? "SUCCESS" : "FAILED")}");
                if (!assemblyResult.IsSuccess) success = false;

                // Step 4: Simulate agent task execution
                if (TestAgentHost != null)
                {
                    var taskResult = await SimulateAgentTaskAsync();
                    results.Add($"Agent Task Execution: {(taskResult ? "SUCCESS" : "FAILED")}");
                    if (!taskResult) success = false;
                }

                return new IntegrationWorkflowResult
                {
                    Success = success,
                    ExecutionTime = DateTime.UtcNow - startTime,
                    StepResults = results.ToArray(),
                    ProjectCreated = projectResult.IsSuccess,
                    AgentCreated = agentResult.IsSuccess,
                    AssemblyAnalyzed = assemblyResult.IsSuccess
                };
            }
            catch (Exception ex)
            {
                return new IntegrationWorkflowResult
                {
                    Success = false,
                    ExecutionTime = DateTime.UtcNow - startTime,
                    StepResults = results.ToArray(),
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<bool> SimulateAgentTaskAsync()
        {
            try
            {
                // Simulate agent task execution
                await Task.Delay(50); // Simulate processing time
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                try
                {
                    // Clean up test environment
                    if (Directory.Exists(TestProjectPath))
                    {
                        Directory.Delete(TestProjectPath, true);
                    }

                    TestAgentHost?.Dispose();
                }
                catch (Exception ex)
                {
                    // Log cleanup errors but don't throw
                    Console.WriteLine($"Warning: Error during test cleanup: {ex.Message}");
                }
                finally
                {
                    IsDisposed = true;
                }
            }
        }
    }

    public class IntegrationWorkflowResult
    {
        public bool Success { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string[] StepResults { get; set; } = Array.Empty<string>();
        public bool ProjectCreated { get; set; }
        public bool AgentCreated { get; set; }
        public bool AssemblyAnalyzed { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
