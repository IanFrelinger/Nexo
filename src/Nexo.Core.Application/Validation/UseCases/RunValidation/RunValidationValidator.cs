using FluentValidation;

namespace Nexo.Core.Application.Validation.UseCases.RunValidation;

/// <summary>
/// Validator for RunValidationCommand.
/// </summary>
public class RunValidationValidator : AbstractValidator<RunValidationCommand>
{
    public RunValidationValidator()
    {
        // Filter is optional, no validation needed
    }
}

