using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Contracts;
using Nexo.Core.Pipeline;
using Xunit;

namespace Nexo.Core.Tests.Pipeline
{
    public class ExtensionPipelineTests
    {
        private readonly Mock<IExtensionGenerator<TestRequest, TestArtifact>> _mockGenerator;
        private readonly Mock<ICompilationGate> _mockCompilationGate;
        private readonly Mock<IPolicyGate<TestArtifact>> _mockPolicyGate;
        private readonly Mock<IArtifactPublisher<TestArtifact>> _mockPublisher;
        private readonly Mock<ILogger<ExtensionPipeline<TestRequest, TestArtifact>>> _mockLogger;

        public ExtensionPipelineTests()
        {
            _mockGenerator = new Mock<IExtensionGenerator<TestRequest, TestArtifact>>();
            _mockCompilationGate = new Mock<ICompilationGate>();
            _mockPolicyGate = new Mock<IPolicyGate<TestArtifact>>();
            _mockPublisher = new Mock<IArtifactPublisher<TestArtifact>>();
            _mockLogger = new Mock<ILogger<ExtensionPipeline<TestRequest, TestArtifact>>>();
        }

        [Fact]
        public async Task RunAsync_HappyPath_ShouldCallAllComponentsAndReturnSuccess()
        {
            // Arrange
            var request = new TestRequest { Id = "test-request", Name = "Test Extension" };
            var artifact = new TestArtifact { Id = "test-artifact", Name = "Test Extension" };
            var sourceCode = "public class TestExtension { }";
            var notes = new List<string> { "Generated successfully" };

            var generationResult = new GenerationResult<TestArtifact>(
                artifact,
                sourceCode,
                notes
            );

            var validationResult = new ValidationResult(true, "TestGate", new[] { "No issues found" });
            var policyOutcome = new PolicyOutcome(true, "TestPolicy", new[] { "All checks passed" }, 0.95);
            var pipelineReport = new PipelineReport("test-artifact", true, 0.95, new[] { "Pipeline completed successfully" });

            _mockGenerator.Setup(g => g.GenerateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(generationResult);

            _mockCompilationGate.Setup(g => g.ValidateAsync(sourceCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            _mockPolicyGate.Setup(p => p.EvaluateAsync(artifact, It.IsAny<CancellationToken>()))
                .ReturnsAsync(policyOutcome);

            _mockPublisher.Setup(p => p.PublishAsync(It.IsAny<TestArtifact>(), It.IsAny<IReadOnlyList<ValidationResult>>(), It.IsAny<PolicyOutcome>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(pipelineReport);

            var pipeline = new ExtensionPipeline<TestRequest, TestArtifact>(
                _mockGenerator.Object,
                new[] { _mockCompilationGate.Object },
                new[] { _mockPolicyGate.Object },
                _mockPublisher.Object,
                _mockLogger.Object
            );

            // Act
            var result = await pipeline.RunAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.QualityScore.Should().Be(0.95);
            result.ArtifactId.Should().Be("test-artifact");

            _mockGenerator.Verify(g => g.GenerateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
            _mockCompilationGate.Verify(g => g.ValidateAsync(sourceCode, It.IsAny<CancellationToken>()), Times.Once);
            _mockPolicyGate.Verify(p => p.EvaluateAsync(artifact, It.IsAny<CancellationToken>()), Times.Once);
            _mockPublisher.Verify(p => p.PublishAsync(It.IsAny<TestArtifact>(), It.IsAny<IReadOnlyList<ValidationResult>>(), It.IsAny<PolicyOutcome>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RunAsync_CompilationGateFails_ShouldSkipPoliciesAndReturnFailure()
        {
            // Arrange
            var request = new TestRequest { Id = "test-request", Name = "Test Extension" };
            var artifact = new TestArtifact { Id = "test-artifact", Name = "Test Extension" };
            var sourceCode = "public class TestExtension { }";
            var notes = new List<string> { "Generated successfully" };

            var generationResult = new GenerationResult<TestArtifact>(
                artifact,
                sourceCode,
                notes
            );

            var validationResult = new ValidationResult(false, "TestGate", new[] { "Syntax error found" });
            var pipelineReport = new PipelineReport("test-artifact", false, 45.0, new[] { "Pipeline failed due to compilation gate failures" });

            _mockGenerator.Setup(g => g.GenerateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(generationResult);

            _mockCompilationGate.Setup(g => g.ValidateAsync(sourceCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            _mockPublisher.Setup(p => p.PublishAsync(artifact, It.IsAny<IReadOnlyList<ValidationResult>>(), It.IsAny<PolicyOutcome>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(pipelineReport);

            var pipeline = new ExtensionPipeline<TestRequest, TestArtifact>(
                _mockGenerator.Object,
                new[] { _mockCompilationGate.Object },
                new[] { _mockPolicyGate.Object },
                _mockPublisher.Object,
                _mockLogger.Object
            );

            // Act
            var result = await pipeline.RunAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Succeeded.Should().BeFalse();
            result.QualityScore.Should().Be(45.0);

            _mockGenerator.Verify(g => g.GenerateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
            _mockCompilationGate.Verify(g => g.ValidateAsync(sourceCode, It.IsAny<CancellationToken>()), Times.Once);
            _mockPolicyGate.Verify(p => p.EvaluateAsync(It.IsAny<TestArtifact>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockPublisher.Verify(p => p.PublishAsync(artifact, It.IsAny<IReadOnlyList<ValidationResult>>(), It.IsAny<PolicyOutcome>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RunAsync_PolicyGateFails_ShouldReturnFailure()
        {
            // Arrange
            var request = new TestRequest { Id = "test-request", Name = "Test Extension" };
            var artifact = new TestArtifact { Id = "test-artifact", Name = "Test Extension" };
            var sourceCode = "public class TestExtension { }";
            var notes = new List<string> { "Generated successfully" };

            var generationResult = new GenerationResult<TestArtifact>(
                artifact,
                sourceCode,
                notes
            );

            var validationResult = new ValidationResult(true, "TestGate", new[] { "No issues found" });
            var policyOutcome = new PolicyOutcome(false, "TestPolicy", new[] { "Policy violation found" }, 0.3);
            var pipelineReport = new PipelineReport("test-artifact", false, 0.3, new[] { "Pipeline failed due to policy violations" });

            _mockGenerator.Setup(g => g.GenerateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(generationResult);

            _mockCompilationGate.Setup(g => g.ValidateAsync(sourceCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            _mockPolicyGate.Setup(p => p.EvaluateAsync(artifact, It.IsAny<CancellationToken>()))
                .ReturnsAsync(policyOutcome);

            _mockPublisher.Setup(p => p.PublishAsync(It.IsAny<TestArtifact>(), It.IsAny<IReadOnlyList<ValidationResult>>(), It.IsAny<PolicyOutcome>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(pipelineReport);

            var pipeline = new ExtensionPipeline<TestRequest, TestArtifact>(
                _mockGenerator.Object,
                new[] { _mockCompilationGate.Object },
                new[] { _mockPolicyGate.Object },
                _mockPublisher.Object,
                _mockLogger.Object
            );

            // Act
            var result = await pipeline.RunAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Succeeded.Should().BeFalse();
            result.QualityScore.Should().Be(0.3);

            _mockGenerator.Verify(g => g.GenerateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
            _mockCompilationGate.Verify(g => g.ValidateAsync(sourceCode, It.IsAny<CancellationToken>()), Times.Once);
            _mockPolicyGate.Verify(p => p.EvaluateAsync(artifact, It.IsAny<CancellationToken>()), Times.Once);
            _mockPublisher.Verify(p => p.PublishAsync(It.IsAny<TestArtifact>(), It.IsAny<IReadOnlyList<ValidationResult>>(), It.IsAny<PolicyOutcome>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RunAsync_MultipleGatesAndPolicies_ShouldRunAllAndAggregateResults()
        {
            // Arrange
            var request = new TestRequest { Id = "test-request", Name = "Test Extension" };
            var artifact = new TestArtifact { Id = "test-artifact", Name = "Test Extension" };
            var sourceCode = "public class TestExtension { }";
            var notes = new List<string> { "Generated successfully" };

            var generationResult = new GenerationResult<TestArtifact>(
                artifact,
                sourceCode,
                notes
            );

            var gate1 = new Mock<ICompilationGate>();
            var gate2 = new Mock<ICompilationGate>();
            var policy1 = new Mock<IPolicyGate<TestArtifact>>();
            var policy2 = new Mock<IPolicyGate<TestArtifact>>();

            gate1.Setup(g => g.ValidateAsync(sourceCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(true, "Gate1", new[] { "Gate1 passed" }));

            gate2.Setup(g => g.ValidateAsync(sourceCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(true, "Gate2", new[] { "Gate2 passed" }));

            policy1.Setup(p => p.EvaluateAsync(artifact, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PolicyOutcome(true, "Policy1", new[] { "Policy1 passed" }, 0.8));

            policy2.Setup(p => p.EvaluateAsync(artifact, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PolicyOutcome(true, "Policy2", new[] { "Policy2 passed" }, 0.9));

            var pipelineReport = new PipelineReport("test-artifact", true, 0.85, new[] { "Pipeline completed successfully" });

            _mockGenerator.Setup(g => g.GenerateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(generationResult);

            _mockPublisher.Setup(p => p.PublishAsync(artifact, It.IsAny<IReadOnlyList<ValidationResult>>(), It.IsAny<PolicyOutcome>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(pipelineReport);

            var pipeline = new ExtensionPipeline<TestRequest, TestArtifact>(
                _mockGenerator.Object,
                new[] { gate1.Object, gate2.Object },
                new[] { policy1.Object, policy2.Object },
                _mockPublisher.Object,
                _mockLogger.Object
            );

            // Act
            var result = await pipeline.RunAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();

            gate1.Verify(g => g.ValidateAsync(sourceCode, It.IsAny<CancellationToken>()), Times.Once);
            gate2.Verify(g => g.ValidateAsync(sourceCode, It.IsAny<CancellationToken>()), Times.Once);
            policy1.Verify(p => p.EvaluateAsync(artifact, It.IsAny<CancellationToken>()), Times.Once);
            policy2.Verify(p => p.EvaluateAsync(artifact, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RunAsync_GeneratorThrowsException_ShouldReturnErrorReport()
        {
            // Arrange
            var request = new TestRequest { Id = "test-request", Name = "Test Extension" };

            _mockGenerator.Setup(g => g.GenerateAsync(request, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Generation failed"));

            var pipeline = new ExtensionPipeline<TestRequest, TestArtifact>(
                _mockGenerator.Object,
                new[] { _mockCompilationGate.Object },
                new[] { _mockPolicyGate.Object },
                _mockPublisher.Object,
                _mockLogger.Object
            );

            // Act
            var result = await pipeline.RunAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Succeeded.Should().BeFalse();
            result.QualityScore.Should().Be(0.0);
            result.ArtifactId.Should().StartWith("error-");
            result.Notes.Should().Contain(note => note.Contains("Generation failed"));
        }

        [Fact]
        public void Constructor_WithNullGenerator_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var action = () => new ExtensionPipeline<TestRequest, TestArtifact>(
                null!,
                new[] { _mockCompilationGate.Object },
                new[] { _mockPolicyGate.Object },
                _mockPublisher.Object,
                _mockLogger.Object
            );

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("generator");
        }

        [Fact]
        public void Constructor_WithNullGates_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var action = () => new ExtensionPipeline<TestRequest, TestArtifact>(
                _mockGenerator.Object,
                null!,
                new[] { _mockPolicyGate.Object },
                _mockPublisher.Object,
                _mockLogger.Object
            );

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("gates");
        }

        [Fact]
        public void Constructor_WithNullPolicies_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var action = () => new ExtensionPipeline<TestRequest, TestArtifact>(
                _mockGenerator.Object,
                new[] { _mockCompilationGate.Object },
                null!,
                _mockPublisher.Object,
                _mockLogger.Object
            );

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("policies");
        }

        [Fact]
        public void Constructor_WithNullPublisher_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var action = () => new ExtensionPipeline<TestRequest, TestArtifact>(
                _mockGenerator.Object,
                new[] { _mockCompilationGate.Object },
                new[] { _mockPolicyGate.Object },
                null!,
                _mockLogger.Object
            );

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("publisher");
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var action = () => new ExtensionPipeline<TestRequest, TestArtifact>(
                _mockGenerator.Object,
                new[] { _mockCompilationGate.Object },
                new[] { _mockPolicyGate.Object },
                _mockPublisher.Object,
                null!
            );

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }
    }

    // Test types for the pipeline tests
    public class TestRequest
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class TestArtifact
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
