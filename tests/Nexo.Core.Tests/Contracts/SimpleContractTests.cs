using FluentAssertions;
using Nexo.Core.Contracts;
using System;
using System.Collections.Generic;
using Xunit;

namespace Nexo.Core.Tests.Contracts
{
    public class SimpleContractTests
    {
        [Fact]
        public void GenerationResult_ShouldCreateWithCorrectValues()
        {
            // Arrange
            var artifact = new GeneratedExtension("test-id", "TestExtension", "public class Test { }", null, DateTime.UtcNow);
            var sourceCode = "public class Test { }";
            var notes = new List<string> { "Generated successfully" };

            // Act
            var result = new GenerationResult<GeneratedExtension>(artifact, sourceCode, notes);

            // Assert
            result.Artifact.Should().Be(artifact);
            result.SourceCode.Should().Be(sourceCode);
            result.Notes.Should().BeEquivalentTo(notes);
        }

        [Fact]
        public void ValidationResult_ShouldCreateWithCorrectValues()
        {
            // Arrange
            var findings = new List<string> { "No issues found" };

            // Act
            var result = new ValidationResult(true, "TestGate", findings);

            // Assert
            result.Passed.Should().BeTrue();
            result.Name.Should().Be("TestGate");
            result.Findings.Should().BeEquivalentTo(findings);
        }

        [Fact]
        public void PolicyOutcome_ShouldCreateWithCorrectValues()
        {
            // Arrange
            var findings = new List<string> { "All checks passed" };

            // Act
            var result = new PolicyOutcome(true, "SafetyPolicy", findings, 0.95);

            // Assert
            result.Passed.Should().BeTrue();
            result.PolicyPackName.Should().Be("SafetyPolicy");
            result.Findings.Should().BeEquivalentTo(findings);
            result.Score.Should().Be(0.95);
        }

        [Fact]
        public void PipelineReport_ShouldCreateWithCorrectValues()
        {
            // Arrange
            var notes = new List<string> { "Pipeline completed successfully" };

            // Act
            var result = new PipelineReport("artifact-123", true, 0.95, notes);

            // Assert
            result.ArtifactId.Should().Be("artifact-123");
            result.Succeeded.Should().BeTrue();
            result.QualityScore.Should().Be(0.95);
            result.Notes.Should().BeEquivalentTo(notes);
        }

        [Fact]
        public void GeneratedExtension_ShouldCreateWithCorrectValues()
        {
            // Arrange
            var generatedAt = DateTime.UtcNow;
            var sourceCode = "public class Test { }";
            var compiledAssembly = new byte[] { 1, 2, 3, 4 };

            // Act
            var result = new GeneratedExtension("test-id", "TestExtension", sourceCode, compiledAssembly, generatedAt);

            // Assert
            result.Id.Should().Be("test-id");
            result.Name.Should().Be("TestExtension");
            result.SourceCode.Should().Be(sourceCode);
            result.CompiledAssembly.Should().BeEquivalentTo(compiledAssembly);
            result.GeneratedAt.Should().Be(generatedAt);
        }
    }
}
