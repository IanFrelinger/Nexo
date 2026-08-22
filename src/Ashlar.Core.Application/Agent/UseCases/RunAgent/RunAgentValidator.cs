using FluentValidation;

namespace Ashlar.Core.Application.Agent.UseCases.RunAgent;

/// <summary>
/// Validator for RunAgentCommand.
/// </summary>
public class RunAgentValidator : AbstractValidator<RunAgentCommand>
{
    /// <summary>Creates a validator requiring a non-empty agent name.</summary>
    public RunAgentValidator()
    {
        RuleFor(x => x.AgentName)
            .NotEmpty()
            .WithMessage("Agent name is required");
    }
}

