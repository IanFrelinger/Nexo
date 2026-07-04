using FluentValidation;

namespace Nexo.Core.Application.Validation.UseCases.RunValidation;

/// <summary>
/// Validator for RunValidationCommand.
/// </summary>
public class RunValidationValidator : AbstractValidator<RunValidationCommand>
{
    /// <summary>Creates a validator with no required fields (filter is optional).</summary>
    public RunValidationValidator()
    {
        // Filter is optional, no validation needed
    }
}

