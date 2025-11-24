using Xunit;
using FluentValidation.TestHelper;
using Nexo.Core.Application.Analysis.UseCases.AnalyzeCode;

namespace Nexo.Tests.Core.Application.Validation;

/// <summary>
/// Unit tests for AnalyzeCodeValidator.
/// Tests FluentValidation rules for AnalyzeCodeCommand.
/// </summary>
public class AnalyzeCodeValidatorTests
{
    private readonly AnalyzeCodeValidator _validator;

    public AnalyzeCodeValidatorTests()
    {
        _validator = new AnalyzeCodeValidator();
    }

    [Fact]
    public void Validate_WithNullPath_ShouldHaveValidationError()
    {
        // Arrange
        var command = new AnalyzeCodeCommand(null!);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Path)
            .WithErrorMessage("Path is required");
    }

    [Fact]
    public void Validate_WithNonExistentPath_ShouldHaveValidationError()
    {
        // Arrange
        var path = new DirectoryInfo("/nonexistent/path/that/does/not/exist");
        var command = new AnalyzeCodeCommand(path);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Path)
            .WithErrorMessage("Path must exist");
    }

    [Fact]
    public void Validate_WithValidPath_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var path = new DirectoryInfo(Directory.GetCurrentDirectory());
        var command = new AnalyzeCodeCommand(path);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

