using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Services;
using Xunit;
using System.Threading;

namespace Nexo.Feature.AI.Tests
{
    /// <summary>
    /// Test fixture for NaturalLanguageProcessor tests to ensure proper isolation
    /// </summary>
    public class NaturalLanguageProcessorTestFixture : IDisposable
    {
        public ILogger<NaturalLanguageProcessor> Logger { get; }
        public Mock<IModelOrchestrator> ModelOrchestratorMock { get; }
        public NaturalLanguageProcessor Processor { get; }

        public NaturalLanguageProcessorTestFixture()
        {
            Logger = NullLogger<NaturalLanguageProcessor>.Instance;
            ModelOrchestratorMock = new Mock<IModelOrchestrator>();
            
            // Setup default mock behavior
            var mockResponse = new ModelResponse
            {
                Content = "Extracted requirement: User authentication feature",
                Model = "test-model",
                ProcessingTimeMs = 50
            };

            ModelOrchestratorMock
                .Setup(x => x.ExecuteAsync(
                    It.IsAny<ModelRequest>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);
                
            Processor = new NaturalLanguageProcessor(Logger, ModelOrchestratorMock.Object);
        }

        public void Dispose()
        {
            ModelOrchestratorMock.Reset();
        }
    }

    /// <summary>
    /// Tests for the NaturalLanguageProcessor service.
    /// </summary>
    [Collection("NaturalLanguageProcessor Tests - Isolated")]
    public class NaturalLanguageProcessorTests : IClassFixture<NaturalLanguageProcessorTestFixture>, IDisposable
    {
        private readonly NaturalLanguageProcessorTestFixture _fixture;
        private bool _disposed = false;

        public NaturalLanguageProcessorTests(NaturalLanguageProcessorTestFixture fixture)
        {
            _fixture = fixture;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // Clean up any managed resources here
                _fixture.ModelOrchestratorMock.Reset();
                _disposed = true;
            }
        }

        /// <summary>
        /// Creates a fresh instance of NaturalLanguageProcessor with clean mocks for tests that need complete isolation
        /// </summary>
        private NaturalLanguageProcessor CreateFreshProcessor()
        {
            var freshMock = new Mock<IModelOrchestrator>();
            var mockResponse = new ModelResponse
            {
                Content = "Extracted requirement: User authentication feature",
                Model = "test-model",
                ProcessingTimeMs = 50
            };

            freshMock
                .Setup(x => x.ExecuteAsync(
                    It.IsAny<ModelRequest>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            return new NaturalLanguageProcessor(_fixture.Logger, freshMock.Object);
        }

        [Fact]
        public async Task ProcessRequirementsAsync_WithValidInput_ReturnsSuccessResult()
        {
            // Arrange - Create everything from scratch for complete isolation
            var logger = NullLogger<NaturalLanguageProcessor>.Instance;
            var mockOrchestrator = new Mock<IModelOrchestrator>();
            
            var mockResponse = new ModelResponse
            {
                Content = "Extracted requirement: User authentication feature",
                Model = "test-model",
                ProcessingTimeMs = 50
            };

            mockOrchestrator
                .Setup(x => x.ExecuteAsync(
                    It.IsAny<ModelRequest>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            var processor = new NaturalLanguageProcessor(logger, mockOrchestrator.Object);
            
            var input = "Create a user authentication feature with login and registration";
            var context = new ProcessingContext
            {
                Domain = "ecommerce",
                BusinessRules = new List<string> { "Must support authentication", "Must include user features" },
                TechnicalConstraints = new List<string> { "Use JWT tokens" }
            };

            // Act
            var result = await processor.ProcessRequirementsAsync(input, context);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess, $"Expected IsSuccess to be true, but got false. Errors: {string.Join(", ", result.Errors ?? new List<string>())}");
            Assert.NotNull(result.Requirements);
            Assert.NotEmpty(result.Requirements);
            Assert.NotNull(result.Metadata);
            Assert.True(result.Metadata.ProcessingDurationMs > 0);
        }

        [Fact]
        public async Task ValidateInputAsync_WithValidInput_ReturnsSuccessResult()
        {
            // Arrange
            var input = "Title: User authentication feature\nDescription: Login and registration";
            var context = new ValidationContext
            {
                RequiredFields = new List<string> { "title", "description" },
                BusinessRules = new List<string> { "Must be clear and specific" },
                MinimumCompleteness = 0.8,
                MaximumAmbiguity = 0.3
            };

            // Act
            var result = await _fixture.Processor.ValidateInputAsync(input, context);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsValid);
            Assert.NotNull(result.Issues);
            Assert.NotNull(result.Recommendations);
        }

        [Fact]
        public async Task ExtractComponentsAsync_WithValidInput_ReturnsExtractedComponents()
        {
            // Arrange
            var input = "User authentication feature with login and registration";
            var extractionType = ExtractionType.UserStories;
            var context = new ExtractionContext
            {
                Domain = "ecommerce",
                Patterns = new List<string> { "As a", "I want", "so that" },
                IncludeConfidenceScores = true
            };

            // Act
            var result = await _fixture.Processor.ExtractComponentsAsync(input, extractionType, context);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Components);
            Assert.True(result.ConfidenceScore >= 0);
        }

        [Fact]
        public async Task ProcessDomainTerminologyAsync_WithValidInput_ReturnsProcessedResult()
        {
            // Arrange
            var input = "User authentication feature with login and registration";
            var domain = "ecommerce";

            // Act
            var result = await _fixture.Processor.ProcessDomainTerminologyAsync(input, domain);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.RecognizedTerms);
            Assert.NotNull(result.UnrecognizedTerms);
        }

        [Fact]
        public async Task CategorizeAndPrioritizeAsync_WithValidRequirements_ReturnsPrioritizedResult()
        {
            // Arrange
            var requirements = new List<FeatureRequirement>
            {
                new FeatureRequirement
                {
                    Title = "User Authentication",
                    Description = "Implement user login and registration",
                    Type = RequirementType.Functional,
                    Priority = RequirementPriority.High,
                    BusinessValue = 0.9,
                    TechnicalComplexity = 0.7,
                    EstimatedEffort = 8
                },
                new FeatureRequirement
                {
                    Title = "Password Reset",
                    Description = "Allow users to reset their passwords",
                    Type = RequirementType.Functional,
                    Priority = RequirementPriority.Medium,
                    BusinessValue = 0.6,
                    TechnicalComplexity = 0.4,
                    EstimatedEffort = 5
                },
                new FeatureRequirement
                {
                    Title = "Performance Optimization",
                    Description = "Optimize authentication response times",
                    Type = RequirementType.Performance,
                    Priority = RequirementPriority.Low,
                    BusinessValue = 0.3,
                    TechnicalComplexity = 0.8,
                    EstimatedEffort = 13
                }
            };

            var context = new PrioritizationContext
            {
                BusinessValueWeights = new Dictionary<string, double>
                {
                    ["Functional"] = 1.0,
                    ["Performance"] = 0.8
                },
                TechnicalComplexityWeights = new Dictionary<string, double>
                {
                    ["Functional"] = 0.7,
                    ["Performance"] = 1.0
                }
            };

            // Act
            var result = await _fixture.Processor.CategorizeAndPrioritizeAsync(requirements, context);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.PrioritizedRequirements);
            Assert.NotNull(result.Categories);
            Assert.NotNull(result.Metrics);
            Assert.True(result.PrioritizedRequirements.Count > 0);
            Assert.True(result.Categories.Count > 0);
        }

        [Fact]
        public void SupportsFormat_WithSupportedFormat_ReturnsTrue()
        {
            // Arrange
            var supportedFormat = InputFormat.PlainText;

            // Act
            var result = _fixture.Processor.SupportsFormat(supportedFormat);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void SupportsFormat_WithUnsupportedFormat_ReturnsFalse()
        {
            // Arrange
            var unsupportedFormat = InputFormat.Email;

            // Act
            var result = _fixture.Processor.SupportsFormat(unsupportedFormat);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetSupportedFormats_ReturnsNonEmptyCollection()
        {
            // Act
            var formats = _fixture.Processor.GetSupportedFormats();

            // Assert
            Assert.NotNull(formats);
            Assert.True(formats.Any());
        }

        [Fact]
        public void GetSupportedDomains_ReturnsNonEmptyCollection()
        {
            // Act
            var domains = _fixture.Processor.GetSupportedDomains();

            // Assert
            Assert.NotNull(domains);
            Assert.True(domains.Any());
        }

        [Fact]
        public async Task ProcessRequirementsAsync_WithEmptyInput_ReturnsFailureResult()
        {
            // Arrange
            var input = "";
            var context = new ProcessingContext
            {
                Domain = "ecommerce"
            };

            // Act
            var result = await _fixture.Processor.ProcessRequirementsAsync(input, context);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task ProcessRequirementsAsync_WithNullInput_ThrowsArgumentNullException()
        {
            // Arrange
            string input = null;
            var context = new ProcessingContext
            {
                Domain = "ecommerce"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _fixture.Processor.ProcessRequirementsAsync(input, context));
        }

        [Fact]
        public async Task ProcessRequirementsAsync_WithNullContext_ThrowsArgumentNullException()
        {
            // Arrange
            var input = "User authentication feature";
            ProcessingContext context = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _fixture.Processor.ProcessRequirementsAsync(input, context));
        }


    }
}