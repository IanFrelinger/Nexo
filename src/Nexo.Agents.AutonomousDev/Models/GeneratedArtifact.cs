namespace Nexo.Agents.AutonomousDev.Models;

/// <summary>
/// Something the AI generated - code, asset, config, etc.
/// </summary>
public record GeneratedArtifact
{
    public required string Id { get; init; }
    public required string TaskId { get; init; }
    public required ArtifactType Type { get; init; }
    
    /// <summary>
    /// Where this should go in the project.
    /// </summary>
    public required string TargetPath { get; init; }
    
    /// <summary>
    /// The generated content.
    /// </summary>
    public required string Content { get; init; }
    
    /// <summary>
    /// For binary files (images, etc.)
    /// </summary>
    public byte[]? BinaryContent { get; init; }
    
    /// <summary>
    /// AI's explanation of what this does.
    /// </summary>
    public required string Description { get; init; }
    
    /// <summary>
    /// Language/format of the content.
    /// </summary>
    public required string Language { get; init; }
    
    /// <summary>
    /// Is this a complete file or a patch/diff?
    /// </summary>
    public bool IsFullFile { get; init; } = true;
    
    /// <summary>
    /// If modifying, what's the original content?
    /// </summary>
    public string? OriginalContent { get; init; }
    
    /// <summary>
    /// Confidence in this generation (0-1).
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// Things the AI is unsure about.
    /// </summary>
    public IReadOnlyList<string> Uncertainties { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Type of artifact generated.
/// </summary>
public enum ArtifactType
{
    SourceCode,
    Test,
    Configuration,
    Asset,
    Documentation,
    Script,
    Query,
    Schema
}
