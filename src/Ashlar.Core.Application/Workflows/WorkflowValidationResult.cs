namespace Ashlar.Core.Application.Workflows;

/// <summary>Outcome of pre-execution workflow graph validation.</summary>
public class WorkflowValidationResult
{
    /// <summary>Whether the workflow passed all validation checks.</summary>
    public bool IsValid { get; init; }

    /// <summary>Human-readable validation errors when <see cref="IsValid"/> is false.</summary>
    public List<string> Errors { get; init; } = new();
}
