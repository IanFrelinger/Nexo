using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Models.GuidedGeneration;
using Nexo.Infrastructure.GuidedGeneration;
using Xunit;

namespace Nexo.Infrastructure.Tests.GuidedGeneration
{
    /// <summary>
    /// Error handling test cases for GuidedGenerationSnapshotTests.
    /// </summary>
    public partial class GuidedGenerationSnapshotTests
    {
        #region Invalid Input Tests

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task ProcessStep_EmptyInput_ShouldNotAdvanceStep(string input)
        {
            // Arrange
            var session = await _service.StartSessionAsync();

            // Act
            var updatedSession = await _service.ProcessStepAsync(session, input);

            // Assert
            Assert.Equal(GenerationStep.Start, updatedSession.CurrentStep);
            Assert.Empty(updatedSession.History);
        }

        [Fact]
        public async Task ProcessStep_InvalidStep_ShouldHandleGracefully()
        {
            // Arrange
            var session = await _service.StartSessionAsync();
            session.CurrentStep = (GenerationStep)999; // Invalid step

            // Act
            var updatedSession = await _service.ProcessStepAsync(session, "test input");

            // Assert
            Assert.Equal((GenerationStep)999, updatedSession.CurrentStep);
            Assert.Empty(updatedSession.History);
        }

        [Fact]
        public async Task ProcessStep_ReviewAndConfirmWithNo_ShouldRestartPurposeDefinition()
        {
            // Arrange
            var session = await _service.StartSessionAsync();
            session = await _service.ProcessStepAsync(session, "JSON Formatter");
            session = await _service.ProcessStepAsync(session, "Format and validate JSON data");
            session = await _service.ProcessStepAsync(session, "JSON string");
            session = await _service.ProcessStepAsync(session, "Formatted JSON string");
            session = await _service.ProcessStepAsync(session, "Must handle malformed JSON gracefully");

            // Act
            var updatedSession = await _service.ProcessStepAsync(session, "no");

            // Assert
            Assert.Equal(GenerationStep.DefinePurpose, updatedSession.CurrentStep);
            Assert.Contains("User cancelled or requested changes. Restarting purpose definition.", updatedSession.History);
        }

        [Fact]
        public async Task ProcessStep_ReviewAndConfirmWithInvalidInput_ShouldRestartPurposeDefinition()
        {
            // Arrange
            var session = await _service.StartSessionAsync();
            session = await _service.ProcessStepAsync(session, "JSON Formatter");
            session = await _service.ProcessStepAsync(session, "Format and validate JSON data");
            session = await _service.ProcessStepAsync(session, "JSON string");
            session = await _service.ProcessStepAsync(session, "Formatted JSON string");
            session = await _service.ProcessStepAsync(session, "Must handle malformed JSON gracefully");

            // Act
            var updatedSession = await _service.ProcessStepAsync(session, "maybe");

            // Assert
            Assert.Equal(GenerationStep.DefinePurpose, updatedSession.CurrentStep);
            Assert.Contains("User cancelled or requested changes. Restarting purpose definition.", updatedSession.History);
        }

        #endregion

        #region Edge Cases and Error Handling

        [Fact]
        public async Task ProcessStep_NullSession_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _service.ProcessStepAsync(null, "test input"));
        }

        [Fact]
        public async Task ProcessStep_ConcurrentOperations_ShouldMaintainConsistency()
        {
            // Arrange
            var session = await _service.StartSessionAsync();
            var tasks = new List<Task<GenerationSession>>();

            // Act - Simulate concurrent operations
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(_service.ProcessStepAsync(session, $"input {i}"));
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.All(results, result => Assert.NotNull(result));
            Assert.All(results, result => Assert.NotEmpty(result.SessionId));
        }

        #endregion
    }
}
