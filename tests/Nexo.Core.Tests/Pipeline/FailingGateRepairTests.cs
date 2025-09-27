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
    /// Tests for the specific scenario: failing gate that becomes passing after repair.
    /// </summary>
    public partial class FailingGateRepairTests
    {
        private readonly Mock<IExtensionGenerator<string, string>> _mockGenerator;
        private readonly Mock<ICompilationGate> _mockCompilationGate;
        private readonly Mock<IPolicyGate<string>> _mockPolicyGate;
        private readonly Mock<IArtifactPublisher<string>> _mockPublisher;
        private readonly Mock<ILogger<ExtensionPipeline<string, string>>> _mockLogger;
        private readonly RepairLoopOptions _repairOptions;

        public FailingGateRepairTests()
        {
            _mockGenerator = new Mock<IExtensionGenerator<string, string>>();
            _mockCompilationGate = new Mock<ICompilationGate>();
            _mockPolicyGate = new Mock<IPolicyGate<string>>();
            _mockPublisher = new Mock<IArtifactPublisher<string>>();
            _mockLogger = new Mock<ILogger<ExtensionPipeline<string, string>>>();
            _repairOptions = new RepairLoopOptions
            {
                MaxRepairIterations = 2,
                EnableCanaryDeployment = false, // Disable for this test
                EnableAutomaticRollback = false
            };
        }

        [Fact]
        public async Task RunAsync_WithFailingGateThatBecomesPassingAfterFirstRepair_ShouldSucceed()
        {
            // Arrange
            var request = "test request";
            var artifact = "test artifact";
            var originalSource = "original source with error";
            var repairedSource = "repaired source without error";

            _mockGenerator.Setup(x => x.GenerateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GenerationResult<string>(artifact, originalSource, new List<string>()));

            // First validation fails, second validation (after repair) passes
            _mockCompilationGate.SetupSequence(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(false, "TestGate", new[] { "Syntax error: missing semicolon" }))
                .ReturnsAsync(new ValidationResult(true, "TestGate", new List<string>()));

            _mockPolicyGate.Setup(x => x.EvaluateAsync(artifact, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PolicyOutcome(true, "TestPolicy", new List<string>(), 1.0));

            _mockPublisher.Setup(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ValidationResult>>(), It.IsAny<PolicyOutcome>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PipelineReport("test-id", true, 1.0, new List<string>()));

            var repairStrategy = new FailingGateRepairStrategy<string, string>(
                Mock.Of<ILogger<FailingGateRepairStrategy<string, string>>>());

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
            Assert.Equal(1, repairStrategy.CallCount); // Should only need one repair attempt
            
            // Verify that validation was called twice: once with original source, once with repaired source
            _mockCompilationGate.Verify(x => x.ValidateAsync(originalSource, It.IsAny<CancellationToken>()), Times.Once);
            _mockCompilationGate.Verify(x => x.ValidateAsync(repairedSource, It.IsAny<CancellationToken>()), Times.Once);
            
            // Verify that the final report was published
            _mockPublisher.Verify(x => x.PublishAsync(artifact, It.IsAny<IReadOnlyList<ValidationResult>>(), It.IsAny<PolicyOutcome>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RunAsync_WithFailingGateThatRemainsFailingAfterRepair_ShouldFailAfterMaxAttempts()
        {
            // Arrange
            var request = "test request";
            var artifact = "test artifact";
            var originalSource = "original source with error";
            var repairedSource = "repaired source still with error";

            _mockGenerator.Setup(x => x.GenerateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GenerationResult<string>(artifact, originalSource, new List<string>()));

            // All validations fail (original and repaired)
            _mockCompilationGate.Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(false, "TestGate", new[] { "Persistent syntax error" }));

            _mockPublisher.Setup(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ValidationResult>>(), It.IsAny<PolicyOutcome>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PipelineReport("test-id", false, 0.0, new List<string>()));

            var repairStrategy = new TestRepairStrategy<string, string>(
                Mock.Of<ILogger<TestRepairStrategy<string, string>>>(), 
                shouldSucceed: true, // Repair succeeds but validation still fails
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
            Assert.False(result.Succeeded);
            Assert.Equal(2, repairStrategy.CallCount); // Should attempt repair 2 times (MaxRepairIterations)
            
            // Verify that validation was called 3 times: original + 2 repair attempts
            _mockCompilationGate.Verify(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        [Fact]
        public async Task RunAsync_WithRepairStrategyThrowingException_ShouldHandleGracefully()
        {
            // Arrange
            var request = "test request";
            var artifact = "test artifact";
            var originalSource = "original source with error";

            _mockGenerator.Setup(x => x.GenerateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GenerationResult<string>(artifact, originalSource, new List<string>()));

            _mockCompilationGate.Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(false, "TestGate", new[] { "Syntax error" }));

            _mockPublisher.Setup(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ValidationResult>>(), It.IsAny<PolicyOutcome>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PipelineReport("test-id", false, 0.0, new List<string>()));

            var repairStrategy = new Mock<IRepairStrategy<string, string>>();
            repairStrategy.Setup(x => x.TryRepairAsync(It.IsAny<string>(), It.IsAny<GenerationResult<string>>(), It.IsAny<IReadOnlyList<ValidationResult>>(), It.IsAny<PolicyOutcome?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Repair strategy failed"));

            var pipeline = new ExtensionPipeline<string, string>(
                _mockGenerator.Object,
                new[] { _mockCompilationGate.Object },
                new[] { _mockPolicyGate.Object },
                _mockPublisher.Object,
                _mockLogger.Object,
                repairStrategy.Object,
                null,
                null,
                Options.Create(_repairOptions));

            // Act
            var result = await pipeline.RunAsync(request);

            // Assert
            Assert.False(result.Succeeded);
            repairStrategy.Verify(x => x.TryRepairAsync(It.IsAny<string>(), It.IsAny<GenerationResult<string>>(), It.IsAny<IReadOnlyList<ValidationResult>>(), It.IsAny<PolicyOutcome?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
