using Nexo.Agents.AutonomousDev.Models;

namespace Nexo.Agents.AutonomousDev.Adapters;

/// <summary>
/// Adapter for interacting with different types of projects.
/// </summary>
public interface IProjectAdapter
{
    /// <summary>
    /// Initialize the adapter with the project path.
    /// </summary>
    Task InitializeAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Get context about the project.
    /// </summary>
    Task<ProjectContext> GetProjectContextAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Get relevant code from specified areas.
    /// </summary>
    Task<IReadOnlyList<CodeContext>> GetRelevantCodeAsync(string[] areas, CancellationToken ct = default);
    
    /// <summary>
    /// Read a file from the project.
    /// </summary>
    Task<string?> ReadFileAsync(string path, CancellationToken ct = default);
    
    /// <summary>
    /// Write a file to the project.
    /// </summary>
    Task WriteFileAsync(string path, string content, CancellationToken ct = default);
    
    /// <summary>
    /// Patch a file (apply diff).
    /// </summary>
    Task PatchFileAsync(string path, string patch, CancellationToken ct = default);
    
    /// <summary>
    /// Check if a file exists.
    /// </summary>
    Task<bool> FileExistsAsync(string path, CancellationToken ct = default);
    
    /// <summary>
    /// Build the project.
    /// </summary>
    Task<BuildResult> BuildAsync(bool fullRebuild, CancellationToken ct = default);
    
    /// <summary>
    /// Create a backup of the project.
    /// </summary>
    Task<string> CreateBackupAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Get the test target (URL, executable, etc.) for Universal Tester.
    /// </summary>
    Task<string> GetTestTargetAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Commit changes to version control.
    /// </summary>
    Task CommitChangesAsync(string message, CancellationToken ct = default);
}

/// <summary>
/// Context about a project.
/// </summary>
public record ProjectContext
{
    public required string ProjectType { get; init; }
    public required string PrimaryLanguage { get; init; }
    public string? Framework { get; init; }
    public IReadOnlyList<string> KeyFiles { get; init; } = Array.Empty<string>();
    public string? ProjectPath { get; init; }
}

/// <summary>
/// Context about a code file.
/// </summary>
public record CodeContext
{
    public required string Path { get; init; }
    public required string Content { get; init; }
    public string? Language { get; init; }
}
