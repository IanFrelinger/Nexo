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
