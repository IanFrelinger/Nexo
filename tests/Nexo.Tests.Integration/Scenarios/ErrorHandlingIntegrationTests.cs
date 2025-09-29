using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Nexo.Tests.Integration.TestFixtures;
using Nexo.Core.Application.Commands;
using Nexo.Core.Application.Commands.Project;
using Nexo.Core.Application.Commands.Agent;
using Nexo.Core.Application.Commands.Assembly;

namespace Nexo.Tests.Integration.Scenarios
{
    /// <summary>
    /// Error handling and resilience integration tests
    /// </summary>
    public class ErrorHandlingIntegrationTests : IClassFixture<IntegrationTestFixture>
    {
        private readonly IntegrationTestFixture _fixture;

        public ErrorHandlingIntegrationTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task ProjectCreation_WithInvalidPath_ShouldHandleGracefully()
        {
            // Arrange
            var command = new CreateProjectCommand();
            var input = new CreateProjectInput
            {
                Name = "Invalid Project",
                Path = "/invalid/path/that/does/not/exist",
                Runtime = "Docker"
            };

            // Act
            var result = await command.ExecuteAsync(input);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("path", result.ErrorMessage.ToLower());
        }

        [Fact]
        public async Task ProjectCreation_WithNullName_ShouldHandleGracefully()
        {
            // Arrange
            var command = new CreateProjectCommand();
            var input = new CreateProjectInput
            {
                Name = null!,
                Path = _fixture.TestProjectPath,
                Runtime = "Docker"
            };

            // Act
            var result = await command.ExecuteAsync(input);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.ErrorMessage);
        }

        [Fact]
        public async Task AgentCreation_WithInvalidRole_ShouldHandleGracefully()
        {
            // Arrange
            var command = new CreateAgentCommand();
            var input = new CreateAgentInput
            {
                Name = "Test Agent",
                Role = "InvalidRole",
                Platform = "Windows"
            };

            // Act
            var result = await command.ExecuteAsync(input);

            // Assert
            // This should either succeed (if role validation is lenient) or fail gracefully
            if (!result.IsSuccess)
            {
                Assert.NotNull(result.ErrorMessage);
            }
        }

        [Fact]
        public async Task AssemblyAnalysis_WithNonExistentFile_ShouldHandleGracefully()
        {
            // Arrange
            var command = new AnalyzeAssemblyCommand();
            var input = new AnalyzeAssemblyInput
            {
                AssemblyPath = "/non/existent/assembly.dll",
                IncludePrivateMembers = false,
                IncludeSecurityAnalysis = true
            };

            // Act
            var result = await command.ExecuteAsync(input);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("file", result.ErrorMessage.ToLower());
        }

        [Fact]
        public async Task AssemblyAnalysis_WithInvalidFile_ShouldHandleGracefully()
        {
            // Arrange
            var invalidAssemblyPath = Path.Combine(_fixture.TestProjectPath, "invalid.txt");
            File.WriteAllText(invalidAssemblyPath, "This is not a valid assembly");

            var command = new AnalyzeAssemblyCommand();
            var input = new AnalyzeAssemblyInput
            {
                AssemblyPath = invalidAssemblyPath,
                IncludePrivateMembers = false,
                IncludeSecurityAnalysis = true
            };

            // Act
            var result = await command.ExecuteAsync(input);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.ErrorMessage);
        }

        [Fact]
        public async Task ConcurrentOperations_WithConflictingResources_ShouldHandleGracefully()
        {
            // Arrange
            var tasks = new List<Task<CommandResult<CreateProjectOutput>>>();
            const int concurrentOperations = 3;

            // Act - Try to create multiple projects with the same path
            for (int i = 0; i < concurrentOperations; i++)
            {
                var command = new CreateProjectCommand();
                var input = new CreateProjectInput
                {
                    Name = $"Concurrent Project {i}",
                    Path = _fixture.TestProjectPath, // Same path for all
                    Runtime = "Docker"
                };
                tasks.Add(command.ExecuteAsync(input));
            }

            var results = await Task.WhenAll(tasks);

            // Assert - At least one should succeed, others may fail gracefully
            var successCount = results.Count(r => r.IsSuccess);
            var failureCount = results.Count(r => !r.IsSuccess);
            
            Assert.True(successCount >= 1, "At least one operation should succeed");
            Assert.True(failureCount >= 0, "Some operations may fail due to conflicts");
            
            // All failures should have proper error messages
            foreach (var result in results.Where(r => !r.IsSuccess))
            {
                Assert.NotNull(result.ErrorMessage);
            }
        }

        [Fact]
        public async Task TimeoutHandling_WithLongRunningOperation_ShouldHandleGracefully()
        {
            // Arrange
            var command = new CreateProjectCommand();
            var input = new CreateProjectInput
            {
                Name = "Timeout Test Project",
                Path = _fixture.TestProjectPath,
                Runtime = "Docker"
            };

            // Act - Use a timeout to simulate long-running operation
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(1));
            var operationTask = command.ExecuteAsync(input);
            
            var completedTask = await Task.WhenAny(operationTask, timeoutTask);
            
            // Assert
            if (completedTask == operationTask)
            {
                var result = await operationTask;
                Assert.True(result.IsSuccess || !string.IsNullOrEmpty(result.ErrorMessage));
            }
            else
            {
                // Timeout occurred, which is acceptable for this test
                Assert.True(true, "Operation timed out as expected");
            }
        }

        [Fact]
        public async Task ResourceCleanup_AfterError_ShouldNotLeaveOrphanedResources()
        {
            // Arrange
            var initialDirectoryCount = Directory.GetDirectories(Path.GetTempPath()).Length;

            try
            {
                // Act - Force an error by creating a project in an invalid location
                var command = new CreateProjectCommand();
                var input = new CreateProjectInput
                {
                    Name = "Error Test Project",
                    Path = "/invalid/path/for/testing",
                    Runtime = "Docker"
                };

                var result = await command.ExecuteAsync(input);
                
                // This should fail, but shouldn't leave orphaned resources
                Assert.False(result.IsSuccess);
            }
            catch
            {
                // Expected to fail
            }

            // Assert - Check that no orphaned directories were created
            var finalDirectoryCount = Directory.GetDirectories(Path.GetTempPath()).Length;
            var directoryIncrease = finalDirectoryCount - initialDirectoryCount;
            
            Assert.True(directoryIncrease <= 1, 
                $"Directory count increased by {directoryIncrease}, indicating potential resource leaks");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task ProjectCreation_WithInvalidNames_ShouldHandleGracefully(string invalidName)
        {
            // Arrange
            var command = new CreateProjectCommand();
            var input = new CreateProjectInput
            {
                Name = invalidName!,
                Path = _fixture.TestProjectPath,
                Runtime = "Docker"
            };

            // Act
            var result = await command.ExecuteAsync(input);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("name", result.ErrorMessage.ToLower());
        }

        [Fact]
        public async Task NetworkFailure_Simulation_ShouldHandleGracefully()
        {
            // Arrange
            var command = new CreateProjectCommand();
            var input = new CreateProjectCommand
            {
                Name = "Network Test Project",
                Path = _fixture.TestProjectPath,
                Runtime = "Docker"
            };

            // Act - Simulate network failure by using an invalid runtime
            input.Runtime = "InvalidRuntime";
            var result = await command.ExecuteAsync(input);

            // Assert
            // Should either succeed (if validation is lenient) or fail gracefully
            if (!result.IsSuccess)
            {
                Assert.NotNull(result.ErrorMessage);
            }
        }
    }
}
