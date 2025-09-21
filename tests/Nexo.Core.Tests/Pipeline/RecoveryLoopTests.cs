using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Tests for the recovery loop functionality in ExtensionPipeline.
    /// </summary>
    public class RecoveryLoopTests
    {
        private readonly Mock<IExtensionGenerator<string, string>> _mockGenerator;
        private readonly Mock<ICompilationGate> _mockCompilationGate;
        private readonly Mock<IPolicyGate<string>> _mockPolicyGate;
        private readonly Mock<IArtifactPublisher<string>> _mockPublisher;
        private readonly Mock<ILogger<ExtensionPipeline<string, string>>> _mockLogger;
        private readonly RepairLoopOptions _repairOptions;

        public RecoveryLoopTests()
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
        public async Task RunAsync_WithFailingGateAndRepairStrategy_ShouldAttemptRepair()
        {
            // Arrange
            var request = "test request";
            var artifact = "test artifact";
            var sourceCode = "original source";
            var repairedSource = "repaired source";

            _mockGenerator.Setup(x => x.GenerateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GenerationResult<string>(artifact, sourceCode, new List<string>()));

            _mockCompilationGate.Setup(x => x.ValidateAsync(sourceCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(false, "TestGate", new[] { "Compilation error" }));

            _mockCompilationGate.Setup(x => x.ValidateAsync(repairedSource, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(true, "TestGate", new List<string>()));

            _mockPolicyGate.Setup(x => x.EvaluateAsync(artifact, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PolicyOutcome(true, "TestPolicy", new List<string>(), 1.0));

            _mockPublisher.Setup(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ValidationResult>>(), It.IsAny<PolicyOutcome>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PipelineReport("test-id", true, 1.0, new List<string>()));

            var repairStrategy = new TestRepairStrategy<string, string>(
                Mock.Of<ILogger<TestRepairStrategy<string, string>>>(), 
                shouldSucceed: true, 
                patchedSource: repairedSource);

            var pipeline = new ExtensionPipeline<string, string>(
                _mockGenerator.Object,
                new[] { _mockCompilationGate.Object },
                new[] { _mockPolicyGate.Object },
                _mockPublisher.Object,
                _mockLogger.Object,
                repairStrategy,
                null,
                null,
                Options.Create(_repairOptions));

            // Act
            var result = await pipeline.RunAsync(request);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Equal(1, repairStrategy.CallCount);
            _mockCompilationGate.Verify(x => x.ValidateAsync(sourceCode, It.IsAny<CancellationToken>()), Times.Once);
            _mockCompilationGate.Verify(x => x.ValidateAsync(repairedSource, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RunAsync_WithFailingGateAndNoRepairStrategy_ShouldFailImmediately()
        {
            // Arrange
            var request = "test request";
            var artifact = "test artifact";
            var sourceCode = "original source";

            _mockGenerator.Setup(x => x.GenerateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GenerationResult<string>(artifact, sourceCode, new List<string>()));

            _mockCompilationGate.Setup(x => x.ValidateAsync(sourceCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(false, "TestGate", new[] { "Compilation error" }));

            _mockPublisher.Setup(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ValidationResult>>(), It.IsAny<PolicyOutcome>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PipelineReport("test-id", false, 0.0, new List<string>()));

            var pipeline = new ExtensionPipeline<string, string>(
                _mockGenerator.Object,
                new[] { _mockCompilationGate.Object },
                new[] { _mockPolicyGate.Object },
                _mockPublisher.Object,
                _mockLogger.Object,
                null, // No repair strategy
                null,
                null,
                Options.Create(_repairOptions));

            // Act
            var result = await pipeline.RunAsync(request);

            // Assert
            Assert.False(result.Succeeded);
            _mockCompilationGate.Verify(x => x.ValidateAsync(sourceCode, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RunAsync_WithFailingPolicyAndRepairStrategy_ShouldAttemptRepair()
        {
            // Arrange
            var request = "test request";
            var artifact = "test artifact";
            var sourceCode = "original source";
            var repairedSource = "repaired source";

            _mockGenerator.Setup(x => x.GenerateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GenerationResult<string>(artifact, sourceCode, new List<string>()));

            _mockCompilationGate.Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(true, "TestGate", new List<string>()));

            _mockPolicyGate.Setup(x => x.EvaluateAsync(artifact, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PolicyOutcome(false, "TestPolicy", new[] { "Policy violation" }, 0.5));

            _mockPublisher.Setup(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ValidationResult>>(), It.IsAny<PolicyOutcome>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PipelineReport("test-id", true, 1.0, new List<string>()));

            var repairStrategy = new TestRepairStrategy<string, string>(
                Mock.Of<ILogger<TestRepairStrategy<string, string>>>(), 
                shouldSucceed: true, 
                patchedSource: repairedSource);

            var pipeline = new ExtensionPipeline<string, string>(
                _mockGenerator.Object,
                new[] { _mockCompilationGate.Object },
                new[] { _mockPolicyGate.Object },
                _mockPublisher.Object,
                _mockLogger.Object,
                repairStrategy,
                null,
                null,
                Options.Create(_repairOptions));

            // Act
            var result = await pipeline.RunAsync(request);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Equal(1, repairStrategy.CallCount);
        }

        [Fact]
        public async Task RunAsync_WithSuccessfulCanaryDeployment_ShouldSucceed()
        {
            // Arrange
            var request = "test request";
            var artifact = "test artifact";
            var sourceCode = "original source";

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

            var pipeline = new ExtensionPipeline<string, string>(
                _mockGenerator.Object,
                new[] { _mockCompilationGate.Object },
                new[] { _mockPolicyGate.Object },
                _mockPublisher.Object,
                _mockLogger.Object,
                null,
                canaryDeployer,
                null,
                Options.Create(_repairOptions));

            // Act
            var result = await pipeline.RunAsync(request);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Equal(1, canaryDeployer.CallCount);
        }

        [Fact]
        public async Task RunAsync_WithFailingCanaryDeployment_ShouldTriggerRollback()
        {
            // Arrange
            var request = "test request";
            var artifact = "test artifact";
            var sourceCode = "original source";

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
        }

        [Fact]
        public async Task RunAsync_WithMaxRepairAttemptsExceeded_ShouldFail()
        {
            // Arrange
            var request = "test request";
            var artifact = "test artifact";
            var sourceCode = "original source";

            _mockGenerator.Setup(x => x.GenerateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GenerationResult<string>(artifact, sourceCode, new List<string>()));

            _mockCompilationGate.Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(false, "TestGate", new[] { "Persistent compilation error" }));

            var repairStrategy = new TestRepairStrategy<string, string>(
                Mock.Of<ILogger<TestRepairStrategy<string, string>>>(), 
                shouldSucceed: false); // Always fail repair

            var pipeline = new ExtensionPipeline<string, string>(
                _mockGenerator.Object,
                new[] { _mockCompilationGate.Object },
                new[] { _mockPolicyGate.Object },
                _mockPublisher.Object,
                _mockLogger.Object,
                repairStrategy,
                null,
                null,
                Options.Create(_repairOptions));

            // Act
            var result = await pipeline.RunAsync(request);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal(2, repairStrategy.CallCount); // Should attempt repair 2 times (MaxRepairIterations)
        }

        [Fact]
        public async Task RunAsync_WithDisabledCanaryDeployment_ShouldSkipCanary()
        {
            // Arrange
            var request = "test request";
            var artifact = "test artifact";
            var sourceCode = "original source";

            _repairOptions.EnableCanaryDeployment = false;

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

            var pipeline = new ExtensionPipeline<string, string>(
                _mockGenerator.Object,
                new[] { _mockCompilationGate.Object },
                new[] { _mockPolicyGate.Object },
                _mockPublisher.Object,
                _mockLogger.Object,
                null,
                canaryDeployer,
                null,
                Options.Create(_repairOptions));

            // Act
            var result = await pipeline.RunAsync(request);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Equal(0, canaryDeployer.CallCount); // Should not be called
        }

        [Fact]
        public async Task RunAsync_WithDisabledAutomaticRollback_ShouldNotRollbackOnCanaryFailure()
        {
            // Arrange
            var request = "test request";
            var artifact = "test artifact";
            var sourceCode = "original source";

            _repairOptions.EnableAutomaticRollback = false;

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
            Assert.Equal(0, rollbackStrategy.CallCount); // Should not be called
            Assert.Contains("canary-failure", result.ArtifactId.ToLower());
        }
    }
}
