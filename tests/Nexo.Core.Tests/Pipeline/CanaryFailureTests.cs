using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Nexo.Core.Contracts;
using Nexo.Core.Pipeline;
using Nexo.Core.Configuration;
using Nexo.Core.Tests.Pipeline;

namespace Nexo.Core.Tests.Pipeline
{
    /// <summary>
    /// Tests for canary failure scenarios that trigger rollback.
    /// </summary>
    public partial class CanaryFailureTests
    {
        private readonly Mock<IExtensionGenerator<string, string>> _mockGenerator;
        private readonly Mock<ICompilationGate> _mockCompilationGate;
        private readonly Mock<IPolicyGate<string>> _mockPolicyGate;
        private readonly Mock<IArtifactPublisher<string>> _mockPublisher;
        private readonly Mock<ILogger<ExtensionPipeline<string, string>>> _mockLogger;
        private readonly RepairLoopOptions _repairOptions;

        public CanaryFailureTests()
        {
            _mockGenerator = new Mock<IExtensionGenerator<string, string>>();
            _mockCompilationGate = new Mock<ICompilationGate>();
            _mockPolicyGate = new Mock<IPolicyGate<string>>();
            _mockPublisher = new Mock<IArtifactPublisher<string>>();
            _mockLogger = new Mock<ILogger<ExtensionPipeline<string, string>>>();
            _repairOptions = new RepairLoopOptions
            {
                MaxRepairIterations = 2,
                EnableCanaryDeployment = true,
                EnableAutomaticRollback = true
            };
        }

        [Fact]
        public async Task RunAsync_WithCanaryFailure_ShouldTriggerRollback()
        {
            // Arrange
            var request = "test request";
            var artifact = "test artifact";
            var sourceCode = "valid source code";

            _mockGenerator.Setup(x => x.GenerateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GenerationResult<string>(artifact, sourceCode, new List<string>()));

            _mockCompilationGate.Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(true, "TestGate", new List<string>()));

            _mockPolicyGate.Setup(x => x.EvaluateAsync(artifact, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PolicyOutcome(true, "TestPolicy", new List<string>(), 1.0));

            var canaryDeployer = new FailingCanaryDeployer<string>(
                Mock.Of<ILogger<FailingCanaryDeployer<string>>>());

            var rollbackStrategy = new TestRollbackStrategy<string>(
                Mock.Of<ILogger<TestRollbackStrategy<string>>>(), 
                shouldSucceed: true);

            var pipeline = new ExtensionPipeline<string, string>(
                _mockGenerator.Object,
                new[] { _mockCompilationGate.Object },
                new[] { _mockPolicyGate.Object },
                _mockPublisher.Object,
                _mockLogger.Object,
                null,
                canaryDeployer,
                rollbackStrategy,
                Options.Create(_repairOptions));

            // Act
            var result = await pipeline.RunAsync(request);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal(1, canaryDeployer.CallCount);
            Assert.Equal(1, rollbackStrategy.CallCount);
            Assert.Contains("rollback", result.ArtifactId.ToLower());
            Assert.Contains("Canary deployment failed", result.Notes);
            Assert.Contains("Rollback completed", result.Notes);
        }

        [Fact]
        public async Task RunAsync_WithCanaryException_ShouldTriggerRollback()
        {
            // Arrange
            var request = "test request";
            var artifact = "test artifact";
            var sourceCode = "valid source code";

            _mockGenerator.Setup(x => x.GenerateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GenerationResult<string>(artifact, sourceCode, new List<string>()));

            _mockCompilationGate.Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(true, "TestGate", new List<string>()));

            _mockPolicyGate.Setup(x => x.EvaluateAsync(artifact, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PolicyOutcome(true, "TestPolicy", new List<string>(), 1.0));

            var canaryDeployer = new Mock<ICanaryDeployer<string>>();
            canaryDeployer.Setup(x => x.CanaryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Canary deployment exception"));

            var rollbackStrategy = new TestRollbackStrategy<string>(
                Mock.Of<ILogger<TestRollbackStrategy<string>>>(), 
                shouldSucceed: true);

            var pipeline = new ExtensionPipeline<string, string>(
                _mockGenerator.Object,
                new[] { _mockCompilationGate.Object },
                new[] { _mockPolicyGate.Object },
                _mockPublisher.Object,
                _mockLogger.Object,
                null,
                canaryDeployer.Object,
                rollbackStrategy,
                Options.Create(_repairOptions));

            // Act
            var result = await pipeline.RunAsync(request);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal(1, rollbackStrategy.CallCount);
            Assert.Contains("canary-exception", result.ArtifactId.ToLower());
            Assert.Contains("Canary deployment exception", result.Notes);
            Assert.Contains("Rollback completed after canary exception", result.Notes);
        }

        [Fact]
        public async Task RunAsync_WithCanaryFailureAndRollbackFailure_ShouldHandleGracefully()
        {
            // Arrange
            var request = "test request";
            var artifact = "test artifact";
            var sourceCode = "valid source code";

            _mockGenerator.Setup(x => x.GenerateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GenerationResult<string>(artifact, sourceCode, new List<string>()));

            _mockCompilationGate.Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(true, "TestGate", new List<string>()));

            _mockPolicyGate.Setup(x => x.EvaluateAsync(artifact, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PolicyOutcome(true, "TestPolicy", new List<string>(), 1.0));

            var canaryDeployer = new FailingCanaryDeployer<string>(
                Mock.Of<ILogger<FailingCanaryDeployer<string>>>());

            var rollbackStrategy = new FailingRollbackStrategy<string>(
                Mock.Of<ILogger<FailingRollbackStrategy<string>>>());

            var pipeline = new ExtensionPipeline<string, string>(
                _mockGenerator.Object,
                new[] { _mockCompilationGate.Object },
                new[] { _mockPolicyGate.Object },
                _mockPublisher.Object,
                _mockLogger.Object,
                null,
                canaryDeployer,
                rollbackStrategy,
                Options.Create(_repairOptions));

            // Act
            var result = await pipeline.RunAsync(request);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal(1, canaryDeployer.CallCount);
            Assert.Equal(1, rollbackStrategy.CallCount);
            Assert.Contains("canary-failure", result.ArtifactId.ToLower());
            Assert.Contains("Canary deployment failed", result.Notes);
            Assert.Contains("Rollback failed", result.Notes);
        }

        [Fact]
        public async Task RunAsync_WithCanaryFailureAndNoRollbackStrategy_ShouldFailWithoutRollback()
        {
            // Arrange
            var request = "test request";
            var artifact = "test artifact";
            var sourceCode = "valid source code";

            _mockGenerator.Setup(x => x.GenerateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GenerationResult<string>(artifact, sourceCode, new List<string>()));

            _mockCompilationGate.Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(true, "TestGate", new List<string>()));

            _mockPolicyGate.Setup(x => x.EvaluateAsync(artifact, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PolicyOutcome(true, "TestPolicy", new List<string>(), 1.0));

            var canaryDeployer = new FailingCanaryDeployer<string>(
                Mock.Of<ILogger<FailingCanaryDeployer<string>>>());

            var pipeline = new ExtensionPipeline<string, string>(
                _mockGenerator.Object,
                new[] { _mockCompilationGate.Object },
                new[] { _mockPolicyGate.Object },
                _mockPublisher.Object,
                _mockLogger.Object,
                null,
                canaryDeployer,
                null, // No rollback strategy
                Options.Create(_repairOptions));

            // Act
            var result = await pipeline.RunAsync(request);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal(1, canaryDeployer.CallCount);
            Assert.Contains("canary-failure", result.ArtifactId.ToLower());
            Assert.Contains("Canary deployment failed", result.Notes);
            Assert.DoesNotContain("Rollback", result.Notes);
        }

        [Fact]
        public async Task RunAsync_WithSuccessfulCanaryDeployment_ShouldSucceed()
        {
            // Arrange
            var request = "test request";
            var artifact = "test artifact";
            var sourceCode = "valid source code";

            _mockGenerator.Setup(x => x.GenerateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GenerationResult<string>(artifact, sourceCode, new List<string>()));

            _mockCompilationGate.Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(true, "TestGate", new List<string>()));

            _mockPolicyGate.Setup(x => x.EvaluateAsync(artifact, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PolicyOutcome(true, "TestPolicy", new List<string>(), 1.0));

            _mockPublisher.Setup(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ValidationResult>>(), It.IsAny<PolicyOutcome>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PipelineReport("test-id", true, 1.0, new List<string>()));

            var canaryDeployer = new TestCanaryDeployer<string>(
                Mock.Of<ILogger<TestCanaryDeployer<string>>>(), 
                shouldSucceed: true);

            var rollbackStrategy = new TestRollbackStrategy<string>(
                Mock.Of<ILogger<TestRollbackStrategy<string>>>(), 
                shouldSucceed: true);

            var pipeline = new ExtensionPipeline<string, string>(
                _mockGenerator.Object,
                new[] { _mockCompilationGate.Object },
                new[] { _mockPolicyGate.Object },
                _mockPublisher.Object,
                _mockLogger.Object,
                null,
                canaryDeployer,
                rollbackStrategy,
                Options.Create(_repairOptions));

            // Act
            var result = await pipeline.RunAsync(request);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Equal(1, canaryDeployer.CallCount);
            Assert.Equal(0, rollbackStrategy.CallCount); // Should not be called on success
            Assert.Contains("Canary deployment successful", result.Notes);
        }
    }
}
