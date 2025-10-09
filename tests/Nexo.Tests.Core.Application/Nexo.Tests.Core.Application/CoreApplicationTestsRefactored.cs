using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Nexo.Tests.Core.Application.Commands;
using Nexo.Tests.Demo.Infrastructure;

namespace Nexo.Tests.Core.Application
{
    /// <summary>
    /// Refactored core application tests using generic test base classes.
    /// Demonstrates consolidation of duplicate test patterns.
    /// </summary>
    public class CoreApplicationTestsRefactored : GenericCommandTestBase<TestAgentCommand, TestAgentInput, TestAgentOutput>
    {
        public CoreApplicationTestsRefactored(ITestOutputHelper output) : base(output) { }

        protected override TestAgentCommand CreateCommand()
        {
            return new TestAgentCommand();
        }

        [Fact]
        public void TestAgentCommand_ShouldBeCreated()
        {
            // Use generic base class method
            AssertCommandCanBeCreated();
        }

        [Fact]
        public async Task TestAgentCommand_ShouldExecuteSuccessfully()
        {
            // Arrange
            var input = new TestAgentInput
            {
                AgentName = "Test Agent",
                AgentType = "CrossPlatform",
                IncludeInitialization = true,
                IncludeCapabilities = true
            };

            // Use generic base class method
            await AssertCommandExecutesSuccessfully(input);
        }

        [Fact]
        public async Task TestAgentCommand_ExecuteAsync_ShouldReturnSuccess()
        {
            // Arrange
            var command = new TestAgentCommand();
            var input = new TestAgentInput
            {
                AgentName = "Test Agent",
                AgentType = "CrossPlatform",
                IncludeInitialization = true,
                IncludeCapabilities = true
            };

            // Act
            var result = await command.ExecuteAsync(input);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("Test Agent", result.Data.AgentName);
            Assert.True(result.Data.Success);
            Assert.True(result.Data.TestResults.Length > 0);
        }

        [Theory]
        [InlineData("Test Agent 1", "CrossPlatform")]
        [InlineData("Test Agent 2", "SecurityAnalysis")]
        [InlineData("Test Agent 3", "CodeGeneration")]
        public async Task TestAgentCommand_ExecuteAsync_WithDifferentInputs_ShouldReturnSuccess(string agentName, string agentType)
        {
            // Arrange
            var command = new TestAgentCommand();
            var input = new TestAgentInput
            {
                AgentName = agentName,
                AgentType = agentType,
                IncludeInitialization = true,
                IncludeCapabilities = true
            };

            // Act
            var result = await command.ExecuteAsync(input);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(agentName, result.Data.AgentName);
            Assert.True(result.Data.Success);
            Assert.True(result.Data.TestResults.Length > 0);
        }
    }

    /// <summary>
    /// Refactored project command tests using generic test base classes.
    /// </summary>
    public class ProjectCommandTestsRefactored : GenericCommandTestBase<TestProjectCommand, TestProjectInput, TestProjectOutput>
    {
        public ProjectCommandTestsRefactored(ITestOutputHelper output) : base(output) { }

        protected override TestProjectCommand CreateCommand()
        {
            return new TestProjectCommand();
        }

        [Fact]
        public void TestProjectCommand_ShouldBeCreated()
        {
            // Use generic base class method
            AssertCommandCanBeCreated();
        }

        [Fact]
        public async Task TestProjectCommand_ShouldExecuteSuccessfully()
        {
            // Arrange
            var input = new TestProjectInput
            {
                ProjectName = "Test Project",
                ProjectPath = "/tmp/test",
                IncludeValidation = true,
                IncludeConfiguration = true
            };

            // Use generic base class method
            await AssertCommandExecutesSuccessfully(input);
        }

        [Fact]
        public async Task TestProjectCommand_ExecuteAsync_ShouldReturnSuccess()
        {
            // Arrange
            var command = new TestProjectCommand();
            var input = new TestProjectInput
            {
                ProjectName = "Test Project",
                ProjectPath = "/tmp/test",
                IncludeValidation = true,
                IncludeConfiguration = true
            };

            // Act
            var result = await command.ExecuteAsync(input);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("Test Project", result.Data.ProjectName);
            Assert.True(result.Data.Success);
            Assert.True(result.Data.TestResults.Length > 0);
        }
    }

    /// <summary>
    /// Refactored assembly command tests using generic test base classes.
    /// </summary>
    public class AssemblyCommandTestsRefactored : GenericCommandTestBase<TestAssemblyCommand, TestAssemblyInput, TestAssemblyOutput>
    {
        public AssemblyCommandTestsRefactored(ITestOutputHelper output) : base(output) { }

        protected override TestAssemblyCommand CreateCommand()
        {
            return new TestAssemblyCommand();
        }

        [Fact]
        public void TestAssemblyCommand_ShouldBeCreated()
        {
            // Use generic base class method
            AssertCommandCanBeCreated();
        }

        [Fact]
        public async Task TestAssemblyCommand_ShouldExecuteSuccessfully()
        {
            // Arrange
            var input = new TestAssemblyInput
            {
                AssemblyPath = "/tmp/test.dll",
                IncludeAnalysis = true,
                IncludeValidation = true
            };

            // Use generic base class method
            await AssertCommandExecutesSuccessfully(input);
        }
    }
}
