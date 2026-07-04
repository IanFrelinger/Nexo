using System.Text.Json;
using Nexo.Core.Application.SelfImprovement.Models;
using Nexo.Core.Application.SelfImprovement.Ports;

namespace Nexo.Infrastructure.SelfImprovement;

/// <summary>
/// File-based persistence for self-improvement metrics.
/// P3.4: Holdout pass rate tracked in metrics.
/// </summary>
public sealed class FileBasedSelfImprovementMetricsStore : ISelfImprovementMetricsStore
{
    private readonly string _path;

    /// <summary>Initializes a new file based self improvement metrics store.</summary>
    public FileBasedSelfImprovementMetricsStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nexo", "self-improvement-metrics.json");
    }

    /// <inheritdoc />
    public async Task SaveAsync(SelfImprovementReport report, CancellationToken cancellationToken = default)
    {
        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_path, json, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SelfImprovementReport?> GetLastAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path))
            return null;

        var json = await File.ReadAllTextAsync(_path, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<SelfImprovementReport>(json);
    }
}
