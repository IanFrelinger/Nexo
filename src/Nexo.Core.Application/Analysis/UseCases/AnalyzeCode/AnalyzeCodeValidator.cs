using FluentValidation;

namespace Nexo.Core.Application.Analysis.UseCases.AnalyzeCode;

/// <summary>
/// Validator for AnalyzeCodeCommand.
/// </summary>
public class AnalyzeCodeValidator : AbstractValidator<AnalyzeCodeCommand>
{
    public AnalyzeCodeValidator()
    {
        RuleFor(x => x.Path)
            .NotNull()
            .WithMessage("Path is required")
            .Must(p => p.Exists)
            .WithMessage("Path must exist");
    }
}

