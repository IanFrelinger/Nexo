using System.Text.Json;
using Nexo.Agents.AutonomousDev;
using Nexo.Agents.AutonomousDev.Models;

namespace Nexo.CLI.Commands;

/// <summary>
/// Persists dev sessions to a JSON file as they progress.
/// </summary>
public sealed class DevSessionFileStore : ISessionStore
{
    private readonly FileInfo _path;
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public DevSessionFileStore(FileInfo path)
    {
        _path = path;
    }

    public async Task SaveAsync(DevelopmentSession session, CancellationToken ct = default)
    {
        var dir = _path.Directory;
        if (dir != null && !dir.Exists)
        {
            dir.Create();
        }

        var json = JsonSerializer.Serialize(session, Options);
        await File.WriteAllTextAsync(_path.FullName, json, ct);
    }
}

