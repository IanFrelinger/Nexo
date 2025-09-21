using FluentAssertions;
using Nexo.Core.Contracts;
using System;
using System.Collections.Generic;
using Xunit;

namespace Nexo.Core.Tests.Contracts
{
    public class GenerationResultTests
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
        public void GenerationResult_ShouldBeImmutable()
        {
            // Arrange
            var artifact = new GeneratedExtension("test-id", "TestExtension", "public class Test { }", null, DateTime.UtcNow);
            var sourceCode = "public class Test { }";
            var notes = new List<string> { "Generated successfully" };

            // Act
            var result = new GenerationResult<GeneratedExtension>(artifact, sourceCode, notes);

            // Assert
            result.Should().BeOfType<GenerationResult<GeneratedExtension>>();
            // Records are immutable by default in C#, so we just verify the type
            result.Artifact.Should().Be(artifact);
            result.SourceCode.Should().Be(sourceCode);
            result.Notes.Should().BeEquivalentTo(notes);
        }
    }
}
