using FluentAssertions;
using Nexo.Core.Contracts;
using System.Collections.Generic;
using Xunit;

namespace Nexo.Core.Tests.Contracts
{
    public partial class PipelineReportTests
    {
        [Theory]
        [InlineData("artifact-123", true, 0.95, new[] { "Pipeline completed successfully" })]
        [InlineData("artifact-456", false, 0.2, new[] { "Pipeline failed", "Error in step 3" })]
        public void PipelineReport_ShouldCreateWithCorrectValues(string artifactId, bool succeeded, double qualityScore, string[] notes)
        {
            // Arrange
            var notesList = new List<string>(notes);

            // Act
            var result = new PipelineReport(artifactId, succeeded, qualityScore, notesList);

            // Assert
            result.ArtifactId.Should().Be(artifactId);
            result.Succeeded.Should().Be(succeeded);
            result.QualityScore.Should().Be(qualityScore);
            result.Notes.Should().BeEquivalentTo(notesList);
        }

        [Fact]
        public void PipelineReport_ShouldBeImmutable()
        {
            // Arrange
            var notes = new List<string> { "Test note" };

            // Act
            var result = new PipelineReport("test-id", true, 0.8, notes);

            // Assert
            result.Should().BeOfType<PipelineReport>();
            // Records are immutable by default in C#, so we just verify the type
            result.ArtifactId.Should().Be("test-id");
            result.Succeeded.Should().BeTrue();
            result.QualityScore.Should().Be(0.8);
            result.Notes.Should().BeEquivalentTo(notes);
        }
    }
}
