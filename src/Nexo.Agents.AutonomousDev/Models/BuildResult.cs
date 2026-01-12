namespace Nexo.Agents.AutonomousDev.Models;

/// <summary>
/// Result of building the project.
/// </summary>
public record BuildResult
{
    public bool Success { get; init; }
    public TimeSpan Duration { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
    public string? OutputPath { get; init; }
    public string? RawOutput { get; init; }
}
