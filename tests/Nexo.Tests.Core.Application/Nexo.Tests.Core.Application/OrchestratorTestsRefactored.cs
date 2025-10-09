using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Nexo.Tests.Core.Application.Commands;
using Nexo.Tests.Demo.Infrastructure;

namespace Nexo.Tests.Core.Application
{
    /// <summary>
    /// Refactored orchestrator tests using generic test base classes.
    /// Demonstrates consolidation of duplicate orchestrator test patterns.
    /// </summary>
    public class OrchestratorTestsRefactored : GenericOrchestratorTestBase<TestApplicationOrchestrator>
    {
        public OrchestratorTestsRefactored(ITestOutputHelper output) : base(output) { }

        protected override TestApplicationOrchestrator CreateOrchestrator()
        {
            return new TestApplicationOrchestrator();
        }

        [Fact]
        public void TestApplicationOrchestrator_ShouldBeCreated()
        {
            // Use generic base class method
            AssertOrchestratorCanBeCreated();
        }

        [Fact]
        public async Task TestApplicationOrchestrator_ShouldExecuteSuccessfully()
        {
            // Use generic base class method
            await AssertOrchestratorExecutesSuccessfully();
        }

        [Fact]
        public async Task TestApplicationOrchestrator_ExecuteApplicationTestSuiteAsync_ShouldReturnSuccess()
        {
            // Arrange
            var orchestrator = new TestApplicationOrchestrator();
            var input = new TestApplicationOrchestrationInput
            {
                IncludeProjectTests = true,
                IncludeAgentTests = true,
                IncludeAssemblyTests = true,
                TestTimeout = TimeSpan.FromMinutes(5)
            };

            // Act
            var result = await orchestrator.ExecuteApplicationTestSuiteAsync(input);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.True(result.Results.Count > 0);
            Assert.True(result.TotalExecutionTime > TimeSpan.Zero);
        }

        [Fact]
        public async Task TestApplicationOrchestrator_WithPartialTests_ShouldReturnSuccess()
        {
            // Arrange
            var orchestrator = new TestApplicationOrchestrator();
            var input = new TestApplicationOrchestrationInput
            {
                IncludeProjectTests = true,
                IncludeAgentTests = false,
                IncludeAssemblyTests = false,
                TestTimeout = TimeSpan.FromMinutes(2)
            };

            // Act
            var result = await orchestrator.ExecuteApplicationTestSuiteAsync(input);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.True(result.Results.Count > 0);
            Assert.True(result.TotalExecutionTime > TimeSpan.Zero);
        }

        [Fact]
        public void TestApplicationOrchestrator_RegisterCommand_ShouldWork()
        {
            // Arrange
            var orchestrator = new TestApplicationOrchestrator();
            var command = new TestProjectCommand();

            // Act & Assert - Should not throw
            orchestrator.RegisterCommand(command);
        }
    }
}
