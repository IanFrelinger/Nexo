using System;
using System.Collections.Generic;
using FluentAssertions;
using Nexo.Core.Domain.Common;
using Xunit;

namespace Nexo.Core.Domain.Tests.Common
{
    /// <summary>
    /// Tests for consolidated common classes following hexagonal architecture
    /// </summary>
    public class ConsolidatedCommonTests
    {
        [Fact]
        public void BaseEntity_Constructor_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var entity = new TestEntity();

            // Assert
            entity.Id.Should().NotBeNullOrEmpty();
            entity.Name.Should().Be(string.Empty);
            entity.Description.Should().Be(string.Empty);
            entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            entity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            entity.Metadata.Should().NotBeNull();
            entity.Metadata.Should().BeEmpty();
        }

        [Fact]
        public void BaseEntity_ShouldHaveCorrectProperties()
        {
            // Arrange
            var entityType = typeof(BaseEntity);

            // Assert
            entityType.GetProperty("Id").Should().NotBeNull();
            entityType.GetProperty("Name").Should().NotBeNull();
            entityType.GetProperty("Description").Should().NotBeNull();
            entityType.GetProperty("CreatedAt").Should().NotBeNull();
            entityType.GetProperty("UpdatedAt").Should().NotBeNull();
            entityType.GetProperty("Metadata").Should().NotBeNull();
        }

        [Fact]
        public void BaseEntity_ShouldBeAbstract()
        {
            // Arrange
            var entityType = typeof(BaseEntity);

            // Assert
            entityType.IsAbstract.Should().BeTrue();
        }

        [Fact]
        public void BaseRequest_Constructor_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var request = new TestRequest();

            // Assert
            request.Id.Should().NotBeNullOrEmpty();
            request.Code.Should().Be(string.Empty);
            request.Prompt.Should().Be(string.Empty);
            request.Language.Should().Be(string.Empty);
            request.Context.Should().Be(string.Empty);
            request.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            request.Options.Should().NotBeNull();
            request.Options.Should().BeEmpty();
        }

        [Fact]
        public void BaseRequest_ShouldHaveCorrectProperties()
        {
            // Arrange
            var requestType = typeof(BaseRequest);

            // Assert
            requestType.GetProperty("Id").Should().NotBeNull();
            requestType.GetProperty("Code").Should().NotBeNull();
            requestType.GetProperty("Prompt").Should().NotBeNull();
            requestType.GetProperty("Language").Should().NotBeNull();
            requestType.GetProperty("Context").Should().NotBeNull();
            requestType.GetProperty("CreatedAt").Should().NotBeNull();
            requestType.GetProperty("Options").Should().NotBeNull();
        }

        [Fact]
        public void BaseRequest_ShouldBeAbstract()
        {
            // Arrange
            var requestType = typeof(BaseRequest);

            // Assert
            requestType.IsAbstract.Should().BeTrue();
        }

        [Fact]
        public void BaseResult_Constructor_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var result = new TestResult();

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().BeNull();
            result.Exception.Should().BeNull();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
            result.ValidationErrors.Should().NotBeNull();
            result.ValidationErrors.Should().BeEmpty();
            result.Warnings.Should().NotBeNull();
            result.Warnings.Should().BeEmpty();
            result.SuccessMessage.Should().BeNull();
            result.Duration.Should().Be(TimeSpan.Zero);
            result.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void BaseResult_ShouldHaveCorrectProperties()
        {
            // Arrange
            var resultType = typeof(BaseResult);

            // Assert
            resultType.GetProperty("IsSuccess").Should().NotBeNull();
            resultType.GetProperty("ErrorMessage").Should().NotBeNull();
            resultType.GetProperty("Exception").Should().NotBeNull();
            resultType.GetProperty("Data").Should().NotBeNull();
            resultType.GetProperty("ValidationErrors").Should().NotBeNull();
            resultType.GetProperty("Warnings").Should().NotBeNull();
            resultType.GetProperty("SuccessMessage").Should().NotBeNull();
            resultType.GetProperty("Duration").Should().NotBeNull();
            resultType.GetProperty("CompletedAt").Should().NotBeNull();
        }

        [Fact]
        public void BaseResult_ShouldBeAbstract()
        {
            // Arrange
            var resultType = typeof(BaseResult);

            // Assert
            resultType.IsAbstract.Should().BeTrue();
        }

        [Fact]
        public void BaseEntity_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var entity = new TestEntity();
            var testId = "test-id";
            var testName = "Test Entity";
            var testDescription = "Test Description";
            var testCreatedAt = DateTime.UtcNow.AddDays(-1);
            var testUpdatedAt = DateTime.UtcNow;
            var testMetadata = new Dictionary<string, object> { ["key"] = "value" };

            // Act
            entity.Id = testId;
            entity.Name = testName;
            entity.Description = testDescription;
            entity.CreatedAt = testCreatedAt;
            entity.UpdatedAt = testUpdatedAt;
            entity.Metadata = testMetadata;

            // Assert
            entity.Id.Should().Be(testId);
            entity.Name.Should().Be(testName);
            entity.Description.Should().Be(testDescription);
            entity.CreatedAt.Should().Be(testCreatedAt);
            entity.UpdatedAt.Should().Be(testUpdatedAt);
            entity.Metadata.Should().Be(testMetadata);
        }

        [Fact]
        public void BaseRequest_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var request = new TestRequest();
            var testId = "test-request-id";
            var testCode = "Console.WriteLine(\"Hello World\");";
            var testPrompt = "Generate a hello world program";
            var testLanguage = "C#";
            var testContext = "Test context";
            var testCreatedAt = DateTime.UtcNow.AddDays(-1);
            var testOptions = new Dictionary<string, object> { ["key"] = "value" };

            // Act
            request.Id = testId;
            request.Code = testCode;
            request.Prompt = testPrompt;
            request.Language = testLanguage;
            request.Context = testContext;
            request.CreatedAt = testCreatedAt;
            request.Options = testOptions;

            // Assert
            request.Id.Should().Be(testId);
            request.Code.Should().Be(testCode);
            request.Prompt.Should().Be(testPrompt);
            request.Language.Should().Be(testLanguage);
            request.Context.Should().Be(testContext);
            request.CreatedAt.Should().Be(testCreatedAt);
            request.Options.Should().Be(testOptions);
        }

        [Fact]
        public void BaseResult_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var result = new TestResult();
            var testIsSuccess = true;
            var testErrorMessage = "Test error message";
            var testException = new Exception("Test exception");
            var testData = new Dictionary<string, object> { ["key"] = "value" };
            var testValidationErrors = new List<string> { "Validation error 1", "Validation error 2" };
            var testWarnings = new List<string> { "Warning 1", "Warning 2" };
            var testSuccessMessage = "Test success message";
            var testDuration = TimeSpan.FromSeconds(5);
            var testCompletedAt = DateTime.UtcNow;

            // Act
            result.IsSuccess = testIsSuccess;
            result.ErrorMessage = testErrorMessage;
            result.Exception = testException;
            result.Data = testData;
            result.ValidationErrors = testValidationErrors;
            result.Warnings = testWarnings;
            result.SuccessMessage = testSuccessMessage;
            result.Duration = testDuration;
            result.CompletedAt = testCompletedAt;

            // Assert
            result.IsSuccess.Should().Be(testIsSuccess);
            result.ErrorMessage.Should().Be(testErrorMessage);
            result.Exception.Should().Be(testException);
            result.Data.Should().Be(testData);
            result.ValidationErrors.Should().Be(testValidationErrors);
            result.Warnings.Should().Be(testWarnings);
            result.SuccessMessage.Should().Be(testSuccessMessage);
            result.Duration.Should().Be(testDuration);
            result.CompletedAt.Should().Be(testCompletedAt);
        }
    }

    /// <summary>
    /// Test implementation of BaseEntity for testing purposes
    /// </summary>
    public class TestEntity : BaseEntity
    {
    }

    /// <summary>
    /// Test implementation of BaseRequest for testing purposes
    /// </summary>
    public class TestRequest : BaseRequest
    {
    }

    /// <summary>
    /// Test implementation of BaseResult for testing purposes
    /// </summary>
    public class TestResult : BaseResult
    {
    }
}