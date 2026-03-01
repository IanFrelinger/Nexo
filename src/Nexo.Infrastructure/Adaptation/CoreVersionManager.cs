using System.Diagnostics;
using Nexo.Core.Application.Adaptation.Ports;

namespace Nexo.Infrastructure.Adaptation;

/// <summary>
/// Tracks core version. Minimal: timestamp or git rev.
/// </summary>
public sealed class CoreVersionManager : ICoreVersionManager
{
    private readonly string _repoRoot;
    private readonly List<string> _changes = new();

    public CoreVersionManager(string? repoRoot = null)
    {
        _repoRoot = repoRoot ?? Directory.GetCurrentDirectory();
    }

    /// <inheritdoc />
    public Task<string> GetCurrentVersionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rev = TryGetGitRev();
        return Task.FromResult(rev ?? DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss"));
    }

    /// <inheritdoc />
    public Task RecordChangeAsync(string changeDescription, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_changes)
        {
            _changes.Add($"{DateTimeOffset.UtcNow:O}: {changeDescription}");
        }
        return Task.CompletedTask;
    }

    private string? TryGetGitRev()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "rev-parse --short HEAD",
                WorkingDirectory = _repoRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
            };
            using var proc = Process.Start(psi);
            if (proc == null) return null;
            var output = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit();
            return proc.ExitCode == 0 && !string.IsNullOrEmpty(output) ? output : null;
        }
        catch
        {
            return null;
        }
    }
}
