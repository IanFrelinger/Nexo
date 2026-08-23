namespace Ashlar.Core.Application.Pipelines.Models;

/// <summary>
/// Validation diagnostics for a template.
/// </summary>
public sealed record PipelineTemplateValidationResult
{
    /// <summary>Whether the template passed all validation rules.</summary>
    public bool IsValid { get; init; }

    /// <summary>Human-readable validation error messages when invalid.</summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}
