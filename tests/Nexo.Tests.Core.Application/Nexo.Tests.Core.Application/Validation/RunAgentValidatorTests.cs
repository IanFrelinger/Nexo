using Xunit;
using FluentValidation.TestHelper;
using Nexo.Core.Application.Agent.UseCases.RunAgent;

namespace Nexo.Tests.Core.Application.Validation;

/// <summary>
/// Unit tests for RunAgentValidator.
/// Tests FluentValidation rules for RunAgentCommand.
/// </summary>
public class RunAgentValidatorTests
{
    private readonly RunAgentValidator _validator;

    public RunAgentValidatorTests()
    {
        _validator = new RunAgentValidator();
    }

    [Fact]
    public void Validate_WithEmptyAgentName_ShouldHaveValidationError()
    {
        // Arrange
        var command = new RunAgentCommand(string.Empty, null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AgentName)
            .WithErrorMessage("Agent name is required");
    }

    [Fact]
    public void Validate_WithNullAgentName_ShouldHaveValidationError()
    {
        // Arrange
        var command = new RunAgentCommand(null!, null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AgentName)
            .WithErrorMessage("Agent name is required");
    }

    [Fact]
    public void Validate_WithWhitespaceAgentName_ShouldHaveValidationError()
    {
        // Arrange
        var command = new RunAgentCommand("   ", null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AgentName)
            .WithErrorMessage("Agent name is required");
    }

    [Fact]
    public void Validate_WithValidAgentName_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var command = new RunAgentCommand("director", null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithValidAgentNameAndInputFile_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var inputFile = new FileInfo(Path.GetTempFileName());
        var command = new RunAgentCommand("director", inputFile);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();

        // Cleanup
        if (inputFile.Exists)
            inputFile.Delete();
    }
}

