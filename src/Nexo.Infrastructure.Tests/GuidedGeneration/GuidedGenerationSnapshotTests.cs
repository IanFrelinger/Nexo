using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Models.GuidedGeneration;
using Nexo.Infrastructure.GuidedGeneration;
using Xunit;

namespace Nexo.Infrastructure.Tests.GuidedGeneration
{
    /// <summary>
    /// Snapshot tests for guided generation flow including invalid inputs and resume behaviors
    /// </summary>
    public class GuidedGenerationSnapshotTests
    {
        private readonly GuidedGenerationService _service;
        private readonly ILogger<GuidedGenerationService> _logger;

        public GuidedGenerationSnapshotTests()
        {
            _logger = new LoggerFactory().CreateLogger<GuidedGenerationService>();
            _service = new GuidedGenerationService(null, _logger);
        }

        #region Step-by-Step Snapshot Tests

        [Fact]
        public async Task StartSession_ShouldCreateInitialSnapshot()
        {
            // Act
            var session = await _service.StartSessionAsync();

            // Assert
            Assert.NotNull(session);
            Assert.NotEmpty(session.SessionId);
            Assert.Equal(GenerationStep.Start, session.CurrentStep);
            Assert.Empty(session.ToolName);
            Assert.Empty(session.Purpose);
            Assert.Empty(session.Inputs);
            Assert.Empty(session.Outputs);
            Assert.Empty(session.Constraints);
            Assert.NotNull(session.History);
            Assert.Empty(session.History);
        }

        [Fact]
        public async Task ProcessStep_StartToDefinePurpose_ShouldUpdateSnapshot()
        {
            // Arrange
            var session = await _service.StartSessionAsync();
            var toolName = "JSON Formatter";

            // Act
            var updatedSession = await _service.ProcessStepAsync(session, toolName);

            // Assert
            Assert.Equal(GenerationStep.DefinePurpose, updatedSession.CurrentStep);
            Assert.Equal(toolName, updatedSession.ToolName);
            Assert.Contains($"Tool name set to: {toolName}", updatedSession.History);
            Assert.Single(updatedSession.History);
        }

        [Fact]
        public async Task ProcessStep_DefinePurposeToSpecifyInputs_ShouldUpdateSnapshot()
        {
            // Arrange
            var session = await _service.StartSessionAsync();
            session = await _service.ProcessStepAsync(session, "JSON Formatter");
            var purpose = "Format and validate JSON data";

            // Act
            var updatedSession = await _service.ProcessStepAsync(session, purpose);

            // Assert
            Assert.Equal(GenerationStep.SpecifyInputs, updatedSession.CurrentStep);
            Assert.Equal(purpose, updatedSession.Purpose);
            Assert.Contains($"Purpose set to: {purpose}", updatedSession.History);
            Assert.Equal(2, updatedSession.History.Count);
        }

        [Fact]
        public async Task ProcessStep_SpecifyInputsToSpecifyOutputs_ShouldUpdateSnapshot()
        {
            // Arrange
            var session = await _service.StartSessionAsync();
            session = await _service.ProcessStepAsync(session, "JSON Formatter");
            session = await _service.ProcessStepAsync(session, "Format and validate JSON data");
            var input = "JSON string";

            // Act
            var updatedSession = await _service.ProcessStepAsync(session, input);

            // Assert
            Assert.Equal(GenerationStep.SpecifyOutputs, updatedSession.CurrentStep);
            Assert.Contains(input, updatedSession.Inputs);
            Assert.Contains($"Input added: {input}", updatedSession.History);
            Assert.Equal(3, updatedSession.History.Count);
        }

        [Fact]
        public async Task ProcessStep_SpecifyOutputsToAddConstraints_ShouldUpdateSnapshot()
        {
            // Arrange
            var session = await _service.StartSessionAsync();
            session = await _service.ProcessStepAsync(session, "JSON Formatter");
            session = await _service.ProcessStepAsync(session, "Format and validate JSON data");
            session = await _service.ProcessStepAsync(session, "JSON string");
            var output = "Formatted JSON string";

            // Act
            var updatedSession = await _service.ProcessStepAsync(session, output);

            // Assert
            Assert.Equal(GenerationStep.AddConstraints, updatedSession.CurrentStep);
            Assert.Contains(output, updatedSession.Outputs);
            Assert.Contains($"Output added: {output}", updatedSession.History);
            Assert.Equal(4, updatedSession.History.Count);
        }

        [Fact]
        public async Task ProcessStep_AddConstraintsToReview_ShouldUpdateSnapshot()
        {
            // Arrange
            var session = await _service.StartSessionAsync();
            session = await _service.ProcessStepAsync(session, "JSON Formatter");
            session = await _service.ProcessStepAsync(session, "Format and validate JSON data");
            session = await _service.ProcessStepAsync(session, "JSON string");
            session = await _service.ProcessStepAsync(session, "Formatted JSON string");
            var constraint = "Must handle malformed JSON gracefully";

            // Act
            var updatedSession = await _service.ProcessStepAsync(session, constraint);

            // Assert
            Assert.Equal(GenerationStep.ReviewAndConfirm, updatedSession.CurrentStep);
            Assert.Contains(constraint, updatedSession.Constraints);
            Assert.Contains($"Constraint added: {constraint}", updatedSession.History);
            Assert.Equal(5, updatedSession.History.Count);
        }

        [Fact]
        public async Task ProcessStep_ReviewAndConfirm_ShouldGenerateFinalDescription()
        {
            // Arrange
            var session = await _service.StartSessionAsync();
            session = await _service.ProcessStepAsync(session, "JSON Formatter");
            session = await _service.ProcessStepAsync(session, "Format and validate JSON data");
            session = await _service.ProcessStepAsync(session, "JSON string");
            session = await _service.ProcessStepAsync(session, "Formatted JSON string");
            session = await _service.ProcessStepAsync(session, "Must handle malformed JSON gracefully");

            // Act
            var updatedSession = await _service.ProcessStepAsync(session, "yes");

            // Assert
            Assert.Equal(GenerationStep.Generating, updatedSession.CurrentStep);
            Assert.NotEmpty(updatedSession.FinalDescription);
            Assert.Contains("JSON Formatter", updatedSession.FinalDescription);
            Assert.Contains("Format and validate JSON data", updatedSession.FinalDescription);
            Assert.Contains("JSON string", updatedSession.FinalDescription);
            Assert.Contains("Formatted JSON string", updatedSession.FinalDescription);
            Assert.Contains("Must handle malformed JSON gracefully", updatedSession.FinalDescription);
        }

        #endregion

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

        #region Resume from Step Tests

        [Fact]
        public async Task ResumeFromStep_DefinePurpose_ShouldMaintainState()
        {
            // Arrange
            var session = await _service.StartSessionAsync();
            session = await _service.ProcessStepAsync(session, "JSON Formatter");
            session.CurrentStep = GenerationStep.DefinePurpose;

            // Act
            var updatedSession = await _service.ProcessStepAsync(session, "Format and validate JSON data");

            // Assert
            Assert.Equal(GenerationStep.SpecifyInputs, updatedSession.CurrentStep);
            Assert.Equal("JSON Formatter", updatedSession.ToolName);
            Assert.Equal("Format and validate JSON data", updatedSession.Purpose);
            Assert.Contains("Purpose set to: Format and validate JSON data", updatedSession.History);
        }

        [Fact]
        public async Task ResumeFromStep_SpecifyInputs_ShouldMaintainPreviousState()
        {
            // Arrange
            var session = await _service.StartSessionAsync();
            session = await _service.ProcessStepAsync(session, "JSON Formatter");
            session = await _service.ProcessStepAsync(session, "Format and validate JSON data");
            session.CurrentStep = GenerationStep.SpecifyInputs;

            // Act
            var updatedSession = await _service.ProcessStepAsync(session, "JSON string");

            // Assert
            Assert.Equal(GenerationStep.SpecifyOutputs, updatedSession.CurrentStep);
            Assert.Equal("JSON Formatter", updatedSession.ToolName);
            Assert.Equal("Format and validate JSON data", updatedSession.Purpose);
            Assert.Contains("JSON string", updatedSession.Inputs);
        }

        [Fact]
        public async Task ResumeFromStep_AddConstraints_ShouldMaintainAllPreviousState()
        {
            // Arrange
            var session = await _service.StartSessionAsync();
            session = await _service.ProcessStepAsync(session, "JSON Formatter");
            session = await _service.ProcessStepAsync(session, "Format and validate JSON data");
            session = await _service.ProcessStepAsync(session, "JSON string");
            session = await _service.ProcessStepAsync(session, "Formatted JSON string");
            session.CurrentStep = GenerationStep.AddConstraints;

            // Act
            var updatedSession = await _service.ProcessStepAsync(session, "Must handle malformed JSON gracefully");

            // Assert
            Assert.Equal(GenerationStep.ReviewAndConfirm, updatedSession.CurrentStep);
            Assert.Equal("JSON Formatter", updatedSession.ToolName);
            Assert.Equal("Format and validate JSON data", updatedSession.Purpose);
            Assert.Contains("JSON string", updatedSession.Inputs);
            Assert.Contains("Formatted JSON string", updatedSession.Outputs);
            Assert.Contains("Must handle malformed JSON gracefully", updatedSession.Constraints);
        }

        #endregion

        #region Session State Persistence Tests

        [Fact]
        public async Task SessionState_ShouldPersistAcrossMultipleOperations()
        {
            // Arrange
            var session = await _service.StartSessionAsync();

            // Act - Multiple operations
            session = await _service.ProcessStepAsync(session, "JSON Formatter");
            session = await _service.ProcessStepAsync(session, "Format and validate JSON data");
            session = await _service.ProcessStepAsync(session, "JSON string");
            session = await _service.ProcessStepAsync(session, "Formatted JSON string");
            session = await _service.ProcessStepAsync(session, "Must handle malformed JSON gracefully");

            // Assert
            Assert.Equal("JSON Formatter", session.ToolName);
            Assert.Equal("Format and validate JSON data", session.Purpose);
            Assert.Contains("JSON string", session.Inputs);
            Assert.Contains("Formatted JSON string", session.Outputs);
            Assert.Contains("Must handle malformed JSON gracefully", session.Constraints);
            Assert.Equal(5, session.History.Count);
        }

        [Fact]
        public async Task SessionHistory_ShouldMaintainChronologicalOrder()
        {
            // Arrange
            var session = await _service.StartSessionAsync();
            var expectedHistory = new List<string>();

            // Act
            session = await _service.ProcessStepAsync(session, "JSON Formatter");
            expectedHistory.Add("Tool name set to: JSON Formatter");

            session = await _service.ProcessStepAsync(session, "Format and validate JSON data");
            expectedHistory.Add("Purpose set to: Format and validate JSON data");

            session = await _service.ProcessStepAsync(session, "JSON string");
            expectedHistory.Add("Input added: JSON string");

            // Assert
            Assert.Equal(expectedHistory, session.History);
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
        public async Task ProcessStep_CancelledToken_ShouldHandleGracefully()
        {
            // Arrange
            var session = await _service.StartSessionAsync();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            var updatedSession = await _service.ProcessStepAsync(session, "test input", cts.Token);

            // Assert
            Assert.NotNull(updatedSession);
            Assert.Equal(GenerationStep.Start, updatedSession.CurrentStep);
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

        #region Snapshot Validation Tests

        [Fact]
        public async Task SessionSnapshot_ShouldContainAllRequiredFields()
        {
            // Arrange
            var session = await _service.StartSessionAsync();
            session = await _service.ProcessStepAsync(session, "Test Tool");
            session = await _service.ProcessStepAsync(session, "Test Purpose");

            // Act
            var snapshot = session;

            // Assert
            Assert.NotEmpty(snapshot.SessionId);
            Assert.NotNull(snapshot.ToolName);
            Assert.NotNull(snapshot.Purpose);
            Assert.NotNull(snapshot.Inputs);
            Assert.NotNull(snapshot.Outputs);
            Assert.NotNull(snapshot.Constraints);
            Assert.NotNull(snapshot.History);
            Assert.True(snapshot.StartedAt <= DateTime.UtcNow);
            Assert.Null(snapshot.CompletedAt);
            Assert.Null(snapshot.GeneratedTool);
        }

        [Fact]
        public async Task SessionSnapshot_ShouldBeSerializable()
        {
            // Arrange
            var session = await _service.StartSessionAsync();
            session = await _service.ProcessStepAsync(session, "Test Tool");
            session = await _service.ProcessStepAsync(session, "Test Purpose");

            // Act
            var json = System.Text.Json.JsonSerializer.Serialize(session);
            var deserializedSession = System.Text.Json.JsonSerializer.Deserialize<GenerationSession>(json);

            // Assert
            Assert.NotNull(deserializedSession);
            Assert.Equal(session.SessionId, deserializedSession.SessionId);
            Assert.Equal(session.ToolName, deserializedSession.ToolName);
            Assert.Equal(session.Purpose, deserializedSession.Purpose);
            Assert.Equal(session.CurrentStep, deserializedSession.CurrentStep);
        }

        #endregion
    }
}
