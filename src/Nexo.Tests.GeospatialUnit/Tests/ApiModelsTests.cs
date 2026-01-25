using FluentAssertions;
using System.ComponentModel.DataAnnotations;
using Nexo.API.Models;
using Xunit;

namespace Nexo.Tests.GeospatialUnit.Tests;

public class ApiModelsTests
{
    [Fact]
    public void TerrainGenerationRequest_ShouldHaveRequiredBounds()
    {
        // Arrange
        var request = new TerrainGenerationRequest
        {
            Bounds = "37.0,-122.0,37.1,-121.9",
            Format = "obj"
        };

        // Act
        var validationContext = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void TerrainGenerationRequest_ShouldFailValidation_WhenBoundsIsEmpty()
    {
        // Arrange
        var request = new TerrainGenerationRequest
        {
            Bounds = string.Empty,
            Format = "obj"
        };

        // Act
        var validationContext = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().Contain(v => v.MemberNames.Contains(nameof(TerrainGenerationRequest.Bounds)));
    }

    [Fact]
    public void VectorExtractionRequest_ShouldHaveRequiredBounds()
    {
        // Arrange
        var request = new VectorExtractionRequest
        {
            Bounds = "37.0,-122.0,37.1,-121.9",
            FeatureKind = "building",
            Format = "json"
        };

        // Act
        var validationContext = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VectorExtractionRequest_ShouldHaveRequiredFeatureKind()
    {
        // Arrange
        var request = new VectorExtractionRequest
        {
            Bounds = "37.0,-122.0,37.1,-121.9",
            FeatureKind = string.Empty,
            Format = "json"
        };

        // Act
        var validationContext = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().Contain(v => v.MemberNames.Contains(nameof(VectorExtractionRequest.FeatureKind)));
    }

    [Fact]
    public void JobStatusResponse_ShouldInitialize_WithDefaultValues()
    {
        // Act
        var job = new JobStatusResponse
        {
            JobId = "test-id",
            Status = "pending",
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        job.JobId.Should().Be("test-id");
        job.Status.Should().Be("pending");
        job.Progress.Should().Be(0);
        job.ErrorMessage.Should().BeNull();
        job.OutputPath.Should().BeNull();
        job.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void JobStatusResponse_ShouldSupportRecordMutation()
    {
        // Arrange
        var original = new JobStatusResponse
        {
            JobId = "test-id",
            Status = "pending",
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var updated = original with
        {
            Status = "processing",
            Progress = 50
        };

        // Assert
        updated.JobId.Should().Be(original.JobId);
        updated.Status.Should().Be("processing");
        updated.Progress.Should().Be(50);
        original.Status.Should().Be("pending"); // Original unchanged
    }

    [Fact]
    public void ValidationResponse_ShouldContainIssues_WhenInvalid()
    {
        // Arrange
        var response = new ValidationResponse
        {
            IsValid = false,
            Issues = new[] { "Issue 1", "Issue 2" }
        };

        // Assert
        response.IsValid.Should().BeFalse();
        response.Issues.Should().HaveCount(2);
        response.Issues.Should().Contain("Issue 1");
        response.Issues.Should().Contain("Issue 2");
    }

    [Fact]
    public void ValidationResponse_ShouldBeValid_WhenNoIssues()
    {
        // Arrange
        var response = new ValidationResponse
        {
            IsValid = true,
            Issues = Array.Empty<string>()
        };

        // Assert
        response.IsValid.Should().BeTrue();
        response.Issues.Should().BeEmpty();
    }
}
