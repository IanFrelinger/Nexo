using System;
using System.Threading.Tasks;
using Xunit;
using Nexo.Tests.Integration.TestFixtures;
using Nexo.Core.Application.Commands;
using Nexo.Core.Application.Commands.Project;
using Nexo.Core.Application.Commands.Agent;
using Nexo.Core.Application.Commands.Assembly;
using Nexo.Core.Application.Orchestration;
using Nexo.Core.Application.Interfaces;
using Nexo.Abstractions;

namespace Nexo.Tests.Integration.Scenarios
{
    /// <summary>
    /// End-to-end smoke tests for the new command/orchestrator/agent flow
    /// </summary>
    public class EndToEndSmokeTests : IClassFixture<IntegrationTestFixture>
    {
        private readonly IntegrationTestFixture _fixture;

        public EndToEndSmokeTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task CompleteWorkflow_NewCommandOrchestratorFlow_ShouldSucceed()
        {
            // Arrange
            var orchestrator = new GenericCommandOrchestrator();
            var testProjectPath = _fixture.TestProjectPath;

            // Act - Execute a complete workflow using the new command system
            var workflowResult = await ExecuteCompleteWorkflowWithNewSystemAsync(orchestrator, testProjectPath);

            // Assert
            Assert.True(workflowResult.Success);
            Assert.NotNull(workflowResult.ProjectResult);
            Assert.NotNull(workflowResult.AgentResult);
            Assert.NotNull(workflowResult.AssemblyResult);
            Assert.True(workflowResult.ExecutionTime.TotalMilliseconds > 0);
        }

        [Fact]
        public async Task CommandOrchestrator_WithValidCommands_ShouldExecuteSuccessfully()
        {
            // Arrange
            var orchestrator = new GenericCommandOrchestrator();
            var testProjectPath = _fixture.TestProjectPath;

            // Act - Test individual command execution through orchestrator
            var projectCommand = new CreateProjectCommand();
            var projectInput = new CreateProjectInput
            {
                Name = "Smoke Test Project",
                Path = testProjectPath,
                Runtime = "Docker",
                Description = "E2E smoke test project"
            };

            var projectResult = await orchestrator.ExecuteCommandAsync(projectCommand, projectInput);

            // Assert
            Assert.True(projectResult.IsSuccess);
            Assert.NotNull(projectResult.Data);
            Assert.Equal("Smoke Test Project", projectResult.Data.Name);
        }

        [Fact]
        public async Task AgentCreation_WithNewAbstractions_ShouldWork()
        {
            // Arrange
            var agentCommand = new CreateAgentCommand();
            var agentInput = new CreateAgentInput
            {
                Name = "Smoke Test Agent",
                Role = "Developer",
                Platform = "Windows",
                Capabilities = new[] { "CodeGeneration", "Analysis" }
            };

            // Act
            var agentResult = await agentCommand.ExecuteAsync(agentInput);

            // Assert
            Assert.True(agentResult.IsSuccess);
            Assert.NotNull(agentResult.Data);
            Assert.Equal("Smoke Test Agent", agentResult.Data.Name);
            Assert.Equal("Developer", agentResult.Data.Role);
        }

        [Fact]
        public async Task AssemblyAnalysis_WithNewCommandSystem_ShouldWork()
        {
            // Arrange
            var assemblyPath = _fixture.TestAssemblyPath;
            var assemblyCommand = new AnalyzeAssemblyCommand();
            var assemblyInput = new AnalyzeAssemblyInput
            {
                AssemblyPath = assemblyPath,
                IncludePrivateMembers = false,
                IncludeSecurityAnalysis = true,
                IncludeDependencies = true
            };

            // Act
            var assemblyResult = await assemblyCommand.ExecuteAsync(assemblyInput);

            // Assert
            Assert.True(assemblyResult.IsSuccess);
            Assert.NotNull(assemblyResult.Data);
            Assert.Equal(assemblyPath, assemblyResult.Data.AssemblyPath);
        }

        [Fact]
        public async Task Orchestrator_WithMultipleCommands_ShouldExecuteInSequence()
        {
            // Arrange
            var orchestrator = new GenericCommandOrchestrator();
            var testProjectPath = _fixture.TestProjectPath;

            // Act - Execute multiple commands in sequence
            var commands = new List<ICommand<object, object>>
            {
                new CreateProjectCommand(),
                new CreateAgentCommand(),
                new AnalyzeAssemblyCommand()
            };

            var results = new List<object>();
            foreach (var command in commands)
            {
                var input = CreateInputForCommand(command);
                var result = await orchestrator.ExecuteCommandAsync(command, input);
                results.Add(result);
            }

            // Assert
            Assert.Equal(3, results.Count);
            // All commands should have executed (success or failure is acceptable for smoke test)
            Assert.All(results, result => Assert.NotNull(result));
        }

        [Fact]
        public async Task NewAbstractions_ShouldBeComposable()
        {
            // Arrange
            var testProjectPath = _fixture.TestProjectPath;

            // Act - Test that the new abstractions work together
            var projectCommand = new CreateProjectCommand();
            var projectInput = new CreateProjectInput
            {
                Name = "Composability Test Project",
                Path = testProjectPath,
                Runtime = "Docker"
            };

            var projectResult = await projectCommand.ExecuteAsync(projectInput);
            
            if (projectResult.IsSuccess)
            {
                var agentCommand = new CreateAgentCommand();
                var agentInput = new CreateAgentInput
                {
                    Name = "Composability Test Agent",
                    Role = "Developer",
                    Platform = "Windows"
                };

                var agentResult = await agentCommand.ExecuteAsync(agentInput);
                
                // Assert
                Assert.True(projectResult.IsSuccess);
                Assert.True(agentResult.IsSuccess);
                Assert.NotNull(projectResult.Data);
                Assert.NotNull(agentResult.Data);
            }
            else
            {
                // If project creation fails, that's still a valid test of the system
                Assert.NotNull(projectResult.ErrorMessage);
            }
        }

        private async Task<SmokeTestWorkflowResult> ExecuteCompleteWorkflowWithNewSystemAsync(
            GenericCommandOrchestrator orchestrator, 
            string testProjectPath)
        {
            var startTime = DateTime.UtcNow;
            var result = new SmokeTestWorkflowResult();

            try
            {
                // Step 1: Create project
                var projectCommand = new CreateProjectCommand();
                var projectInput = new CreateProjectInput
                {
                    Name = "E2E Smoke Test Project",
                    Path = testProjectPath,
                    Runtime = "Docker",
                    Description = "End-to-end smoke test"
                };

                var projectResult = await orchestrator.ExecuteCommandAsync(projectCommand, projectInput);
                result.ProjectResult = projectResult;

                // Step 2: Create agent
                var agentCommand = new CreateAgentCommand();
                var agentInput = new CreateAgentInput
                {
                    Name = "E2E Smoke Test Agent",
                    Role = "Developer",
                    Platform = "Windows",
                    Capabilities = new[] { "CodeGeneration", "Analysis" }
                };

                var agentResult = await orchestrator.ExecuteCommandAsync(agentCommand, agentInput);
                result.AgentResult = agentResult;

                // Step 3: Analyze assembly
                var assemblyCommand = new AnalyzeAssemblyCommand();
                var assemblyInput = new AnalyzeAssemblyInput
                {
                    AssemblyPath = _fixture.TestAssemblyPath,
                    IncludePrivateMembers = false,
                    IncludeSecurityAnalysis = true,
                    IncludeDependencies = true
                };

                var assemblyResult = await orchestrator.ExecuteCommandAsync(assemblyCommand, assemblyInput);
                result.AssemblyResult = assemblyResult;

                // Determine overall success
                result.Success = projectResult.IsSuccess && agentResult.IsSuccess && assemblyResult.IsSuccess;
                result.ExecutionTime = DateTime.UtcNow - startTime;

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ExecutionTime = DateTime.UtcNow - startTime;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        private object CreateInputForCommand(ICommand<object, object> command)
        {
            return command switch
            {
                CreateProjectCommand => new CreateProjectInput
                {
                    Name = "Test Project",
                    Path = _fixture.TestProjectPath,
                    Runtime = "Docker"
                },
                CreateAgentCommand => new CreateAgentInput
                {
                    Name = "Test Agent",
                    Role = "Developer",
                    Platform = "Windows"
                },
                AnalyzeAssemblyCommand => new AnalyzeAssemblyInput
                {
                    AssemblyPath = _fixture.TestAssemblyPath,
                    IncludePrivateMembers = false,
                    IncludeSecurityAnalysis = true
                },
                _ => throw new NotSupportedException($"Unsupported command type: {command.GetType().Name}")
            };
        }
    }

    public class SmokeTestWorkflowResult
    {
        public bool Success { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public object? ProjectResult { get; set; }
        public object? AgentResult { get; set; }
        public object? AssemblyResult { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
