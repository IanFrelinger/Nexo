namespace Nexo.Infrastructure.Adaptation;

/// <summary>
/// Helper for snapshot and rollback of file content during adaptation.
/// </summary>
public sealed class AdaptationRollbackHelper
{
    private readonly Dictionary<string, string> _snapshots = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Snapshots the content of a file. Call before modifying.
    /// </summary>
    public void Snapshot(string filePath)
    {
        if (!File.Exists(filePath)) return;
        var fullPath = Path.GetFullPath(filePath);
        if (_snapshots.ContainsKey(fullPath)) return;
        _snapshots[fullPath] = File.ReadAllText(fullPath);
    }

    /// <summary>
    /// Snapshots multiple files.
    /// </summary>
    public void SnapshotAll(IEnumerable<string> filePaths)
    {
        foreach (var p in filePaths)
            Snapshot(p);
    }

    /// <summary>
    /// Restores a file from snapshot. Call on regression failure.
    /// </summary>
    public void Rollback(string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);
        if (_snapshots.TryGetValue(fullPath, out var content))
        {
            File.WriteAllText(fullPath, content);
            _snapshots.Remove(fullPath);
        }
    }

    /// <summary>
    /// Restores all snapshotted files.
    /// </summary>
    public void RollbackAll()
    {
        foreach (var path in _snapshots.Keys.ToList())
            Rollback(path);
    }

    /// <summary>
    /// Clears snapshots without restoring (e.g. after successful promotion).
    /// </summary>
    public void Clear()
    {
        _snapshots.Clear();
    }

    /// <summary>
    /// Number of files currently snapshotted.
    /// </summary>
    public int SnapshotCount => _snapshots.Count;
}
