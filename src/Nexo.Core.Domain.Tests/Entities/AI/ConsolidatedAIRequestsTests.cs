using System;
using System.Collections.Generic;
using FluentAssertions;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Xunit;

namespace Nexo.Core.Domain.Tests.Entities.AI
{
    /// <summary>
    /// Tests for consolidated AI requests following hexagonal architecture
    /// </summary>
    public class ConsolidatedAIRequestsTests
    {
        [Fact]
        public void ConsolidatedCodeGenerationRequest_Constructor_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var request = new ConsolidatedCodeGenerationRequest();

            // Assert
            request.Id.Should().NotBeNullOrEmpty();
            request.Code.Should().Be(string.Empty);
            request.Prompt.Should().Be(string.Empty);
            request.Language.Should().Be(string.Empty);
            request.Context.Should().Be(string.Empty);
            request.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            request.Options.Should().NotBeNull();
            request.Options.Should().BeEmpty();
            request.Framework.Should().Be(string.Empty);
            request.Requirements.Should().NotBeNull();
            request.MaxTokens.Should().Be(1000);
            request.Temperature.Should().Be(0.7);
        }

        [Fact]
        public void ConsolidatedCodeGenerationRequest_ShouldInheritFromBaseRequest()
        {
            // Arrange
            var requestType = typeof(ConsolidatedCodeGenerationRequest);

            // Assert
            requestType.BaseType.Should().Be(typeof(BaseRequest));
        }

        [Fact]
        public void ConsolidatedCodeGenerationRequest_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var request = new ConsolidatedCodeGenerationRequest();
            var testFramework = ".NET";
            var testMaxTokens = 2048;
            var testTemperature = 0.8;

            // Act
            request.Framework = testFramework;
            request.MaxTokens = testMaxTokens;
            request.Temperature = testTemperature;

            // Assert
            request.Framework.Should().Be(testFramework);
            request.MaxTokens.Should().Be(testMaxTokens);
            request.Temperature.Should().Be(testTemperature);
        }

        [Fact]
        public void ConsolidatedCodeReviewRequest_Constructor_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var request = new ConsolidatedCodeReviewRequest();

            // Assert
            request.Id.Should().NotBeNullOrEmpty();
            request.Code.Should().Be(string.Empty);
            request.Prompt.Should().Be(string.Empty);
            request.Language.Should().Be(string.Empty);
            request.Context.Should().Be(string.Empty);
            request.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            request.Options.Should().NotBeNull();
            request.Options.Should().BeEmpty();
            request.ReviewType.Should().Be(string.Empty);
            request.ReviewCriteria.Should().NotBeNull();
            request.ReviewCriteria.Should().BeEmpty();
        }

        [Fact]
        public void ConsolidatedCodeReviewRequest_ShouldInheritFromBaseRequest()
        {
            // Arrange
            var requestType = typeof(ConsolidatedCodeReviewRequest);

            // Assert
            requestType.BaseType.Should().Be(typeof(BaseRequest));
        }

        [Fact]
        public void ConsolidatedCodeReviewRequest_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var request = new ConsolidatedCodeReviewRequest();
            var testReviewType = "Quality";
            var testReviewCriteria = new List<string> { "Code Quality", "Security", "Performance" };

            // Act
            request.ReviewType = testReviewType;
            request.ReviewCriteria = testReviewCriteria;

            // Assert
            request.ReviewType.Should().Be(testReviewType);
            request.ReviewCriteria.Should().Be(testReviewCriteria);
        }

        [Fact]
        public void ConsolidatedCodeOptimizationRequest_Constructor_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var request = new ConsolidatedCodeOptimizationRequest();

            // Assert
            request.Id.Should().NotBeNullOrEmpty();
            request.Code.Should().Be(string.Empty);
            request.Prompt.Should().Be(string.Empty);
            request.Language.Should().Be(string.Empty);
            request.Context.Should().Be(string.Empty);
            request.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            request.Options.Should().NotBeNull();
            request.Options.Should().BeEmpty();
            request.OptimizationType.Should().Be(string.Empty);
            request.OptimizationLevel.Should().Be("Medium");
        }

        [Fact]
        public void ConsolidatedCodeOptimizationRequest_ShouldInheritFromBaseRequest()
        {
            // Arrange
            var requestType = typeof(ConsolidatedCodeOptimizationRequest);

            // Assert
            requestType.BaseType.Should().Be(typeof(BaseRequest));
        }

        [Fact]
        public void ConsolidatedCodeOptimizationRequest_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var request = new ConsolidatedCodeOptimizationRequest();
            var testOptimizationType = "Performance";
            var testOptimizationLevel = "High";

            // Act
            request.OptimizationType = testOptimizationType;
            request.OptimizationLevel = testOptimizationLevel;

            // Assert
            request.OptimizationType.Should().Be(testOptimizationType);
            request.OptimizationLevel.Should().Be(testOptimizationLevel);
        }

        [Fact]
        public void ConsolidatedDocumentationRequest_Constructor_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var request = new ConsolidatedDocumentationRequest();

            // Assert
            request.Id.Should().NotBeNullOrEmpty();
            request.Code.Should().Be(string.Empty);
            request.Prompt.Should().Be(string.Empty);
            request.Language.Should().Be(string.Empty);
            request.Context.Should().Be(string.Empty);
            request.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            request.Options.Should().NotBeNull();
            request.Options.Should().BeEmpty();
            request.DocumentationType.Should().Be(string.Empty);
            request.DocumentationCriteria.Should().NotBeNull();
            request.DocumentationCriteria.Should().BeEmpty();
        }

        [Fact]
        public void ConsolidatedDocumentationRequest_ShouldInheritFromBaseRequest()
        {
            // Arrange
            var requestType = typeof(ConsolidatedDocumentationRequest);

            // Assert
            requestType.BaseType.Should().Be(typeof(BaseRequest));
        }

        [Fact]
        public void ConsolidatedDocumentationRequest_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var request = new ConsolidatedDocumentationRequest();
            var testDocumentationType = "API";
            var testDocumentationCriteria = new List<string> { "Completeness", "Clarity", "Examples" };

            // Act
            request.DocumentationType = testDocumentationType;
            request.DocumentationCriteria = testDocumentationCriteria;

            // Assert
            request.DocumentationType.Should().Be(testDocumentationType);
            request.DocumentationCriteria.Should().Be(testDocumentationCriteria);
        }

        [Fact]
        public void ConsolidatedTestingRequest_Constructor_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var request = new ConsolidatedTestingRequest();

            // Assert
            request.Id.Should().NotBeNullOrEmpty();
            request.Code.Should().Be(string.Empty);
            request.Prompt.Should().Be(string.Empty);
            request.Language.Should().Be(string.Empty);
            request.Context.Should().Be(string.Empty);
            request.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            request.Options.Should().NotBeNull();
            request.Options.Should().BeEmpty();
            request.TestType.Should().Be(string.Empty);
            request.TestCriteria.Should().NotBeNull();
            request.TestCriteria.Should().BeEmpty();
        }

        [Fact]
        public void ConsolidatedTestingRequest_ShouldInheritFromBaseRequest()
        {
            // Arrange
            var requestType = typeof(ConsolidatedTestingRequest);

            // Assert
            requestType.BaseType.Should().Be(typeof(BaseRequest));
        }

        [Fact]
        public void ConsolidatedTestingRequest_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var request = new ConsolidatedTestingRequest();
            var testTestType = "Unit";
            var testTestCriteria = new List<string> { "Coverage", "Quality", "Performance" };

            // Act
            request.TestType = testTestType;
            request.TestCriteria = testTestCriteria;

            // Assert
            request.TestType.Should().Be(testTestType);
            request.TestCriteria.Should().Be(testTestCriteria);
        }

        [Fact]
        public void ConsolidatedCodeGenerationRequest_ShouldHaveCorrectDefaultValues()
        {
            // Arrange & Act
            var request = new ConsolidatedCodeGenerationRequest();

            // Assert
            request.MaxTokens.Should().Be(1000);
            request.Temperature.Should().Be(0.7);
            request.Requirements.Should().NotBeNull();
        }

        [Fact]
        public void ConsolidatedCodeOptimizationRequest_ShouldHaveCorrectDefaultValues()
        {
            // Arrange & Act
            var request = new ConsolidatedCodeOptimizationRequest();

            // Assert
            request.OptimizationLevel.Should().Be("Medium");
        }

        [Fact]
        public void ConsolidatedCodeGenerationRequest_ShouldSetRequirementsCorrectly()
        {
            // Arrange
            var request = new ConsolidatedCodeGenerationRequest();
            var testRequirements = new AIRequirements
            {
                Language = "C#",
                Framework = ".NET",
                Platform = "Local",
                SafetyLevel = SafetyLevel.Standard
            };

            // Act
            request.Requirements = testRequirements;

            // Assert
            request.Requirements.Should().Be(testRequirements);
            request.Requirements.Language.Should().Be("C#");
            request.Requirements.Framework.Should().Be(".NET");
            request.Requirements.Platform.Should().Be("Local");
            request.Requirements.SafetyLevel.Should().Be(SafetyLevel.Standard);
        }
    }
}