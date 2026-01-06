using Nexo.Abstractions;

namespace Nexo.Tools.Dev.Deltas;

/// <summary>
/// Represents a file edit operation with before/after state.
/// 
/// Contains:
/// - File path
/// - SHA1 hash of file before edit (null if file was created)
/// - SHA1 hash of file after edit
/// - Number of lines/bytes added
/// - Number of lines/bytes removed
/// 
/// Used by RepoDelta to track file changes.
/// </summary>
public sealed record FileEdit(string Path, string? BeforeSha, string AfterSha, int Added, int Removed);

/// <summary>
/// Action delta for repository operations.
/// 
/// Tracks changes to the repository:
/// - Log messages for operations performed
/// - File edits with before/after state
/// - Tick range (from/to) for the delta
/// - Optional cryptographic signature
/// 
/// Implements IActionDelta for use in agent execution.
/// Used by repository tools to track filesystem changes.
/// </summary>
public sealed class RepoDelta : IActionDelta
{
    public int TickFrom { get; init; }
    public int TickTo   { get; init; }
    public IReadOnlyList<byte>? Signature { get; set; }
    public IReadOnlyList<string> Log => _log;
    public IReadOnlyList<FileEdit> Edits => _edits;

    private readonly List<string> _log = new();
    private readonly List<FileEdit> _edits = new();

    public void AddLog(string msg) => _log.Add(msg);
    public void AddEdit(FileEdit e) => _edits.Add(e);
}
