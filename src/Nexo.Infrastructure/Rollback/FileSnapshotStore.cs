using System.Text.Json;
using Nexo.Core.Application.Paths;
using Nexo.Core.Application.Rollback.Models;
using Nexo.Core.Application.Rollback.Ports;

namespace Nexo.Infrastructure.Rollback;

/// <summary>
/// File-system based snapshot store. Saves file contents to a directory.
/// </summary>
public sealed class FileSnapshotStore : ISnapshotStore
{
    private readonly string _basePath;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public FileSnapshotStore(string? basePath = null)
    {
        _basePath = basePath ?? Path.Combine(RepoPathResolver.FindRepoRoot(), "nexo-snapshots");
    }

    public Task<string> TakeSnapshotAsync(string label, IReadOnlyList<string> componentPaths, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString("N");
        var snapshotDir = Path.Combine(_basePath, id);
        Directory.CreateDirectory(snapshotDir);

        var components = new List<string>();
        foreach (var path in componentPaths ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(path)) continue;
            var fullPath = Path.IsPathRooted(path) ? path : Path.Combine(RepoPathResolver.FindRepoRoot(), path);
            if (!File.Exists(fullPath)) continue;

            var content = File.ReadAllText(fullPath);
            var safeName = Path.GetFileName(path).Replace("..", "_");
            if (string.IsNullOrEmpty(safeName)) safeName = "file";
            var destFile = Path.Combine(snapshotDir, $"{components.Count}_{safeName}");
            File.WriteAllText(destFile, content);
            components.Add(path);
        }

        var meta = new SnapshotMeta
        {
            SnapshotId = id,
            Label = label,
            TakenAt = DateTimeOffset.UtcNow,
            ComponentsIncluded = components
        };
        File.WriteAllText(Path.Combine(snapshotDir, "meta.json"), JsonSerializer.Serialize(meta, JsonOptions));
        return Task.FromResult(id);
    }

    public Task<IEnumerable<SnapshotEntry>> ListSnapshotsAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(_basePath))
            return Task.FromResult(Enumerable.Empty<SnapshotEntry>());

        var list = new List<SnapshotEntry>();
        foreach (var dir in Directory.GetDirectories(_basePath))
        {
            var metaPath = Path.Combine(dir, "meta.json");
            if (!File.Exists(metaPath)) continue;
            try
            {
                var meta = JsonSerializer.Deserialize<SnapshotMeta>(File.ReadAllText(metaPath));
                if (meta != null)
                    list.Add(new SnapshotEntry(meta.SnapshotId, meta.Label, meta.TakenAt, meta.ComponentsIncluded));
            }
            catch { /* skip corrupted */ }
        }
        return Task.FromResult<IEnumerable<SnapshotEntry>>(list.OrderByDescending(x => x.TakenAt).ToList());
    }

    public Task RestoreSnapshotAsync(string snapshotId, CancellationToken ct = default)
    {
        var snapshotDir = Path.Combine(_basePath, snapshotId);
        if (!Directory.Exists(snapshotDir))
            throw new InvalidOperationException($"Snapshot not found: {snapshotId}");

        var metaPath = Path.Combine(snapshotDir, "meta.json");
        if (!File.Exists(metaPath))
            throw new InvalidOperationException($"Snapshot metadata not found: {snapshotId}");

        var meta = JsonSerializer.Deserialize<SnapshotMeta>(File.ReadAllText(metaPath))
            ?? throw new InvalidOperationException($"Invalid snapshot metadata: {snapshotId}");

        var repoRoot = RepoPathResolver.FindRepoRoot();
        for (var i = 0; i < meta.ComponentsIncluded.Count; i++)
        {
            var componentPath = meta.ComponentsIncluded[i];
            var fullPath = Path.IsPathRooted(componentPath) ? componentPath : Path.Combine(repoRoot, componentPath);
            var snapshotFile = Path.Combine(snapshotDir, $"{i}_{Path.GetFileName(componentPath).Replace("..", "_")}");
            if (string.IsNullOrEmpty(Path.GetFileName(componentPath)))
                snapshotFile = Path.Combine(snapshotDir, $"{i}_file");
            if (File.Exists(snapshotFile))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                File.WriteAllText(fullPath, File.ReadAllText(snapshotFile));
            }
        }
        return Task.CompletedTask;
    }

    public Task DeleteSnapshotAsync(string snapshotId, CancellationToken ct = default)
    {
        var snapshotDir = Path.Combine(_basePath, snapshotId);
        if (Directory.Exists(snapshotDir))
            Directory.Delete(snapshotDir, recursive: true);
        return Task.CompletedTask;
    }

    private sealed class SnapshotMeta
    {
        public string SnapshotId { get; set; } = "";
        public string Label { get; set; } = "";
        public DateTimeOffset TakenAt { get; set; }
        public List<string> ComponentsIncluded { get; set; } = new();
    }
}
