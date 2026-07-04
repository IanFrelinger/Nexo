using FluentValidation;

namespace Nexo.Core.Application.Analysis.UseCases.AnalyzeCode;

/// <summary>
/// Validator for AnalyzeCodeCommand.
/// </summary>
public class AnalyzeCodeValidator : AbstractValidator<AnalyzeCodeCommand>
{
    /// <summary>Creates a validator requiring an existing analysis path.</summary>
    public AnalyzeCodeValidator()
    {
        RuleFor(x => x.Path)
            .NotNull()
            .WithMessage("Path is required");
            
        RuleFor(x => x.Path)
            .Must(p => p != null && p.Exists)
            .WithMessage("Path must exist")
            .When(x => x.Path != null);
    }
}
