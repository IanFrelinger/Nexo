using Microsoft.Extensions.Logging;
using Nexo.CLI.Formatting;
using Nexo.Core.Application.Maintenance.Models;
using Nexo.Core.Application.Maintenance.Ports;

namespace Nexo.CLI.Commands;

/// <summary>
/// CLI command for artifact cleanup (nexo maintenance clean).
/// </summary>
public sealed class MaintenanceCommand
{
    private readonly IArtifactCleanupService _cleanupService;
    private readonly IConsoleRenderer _renderer;
    private readonly ILogger<MaintenanceCommand> _logger;

    /// <summary>Creates a new MaintenanceCommand instance.</summary>
    public MaintenanceCommand(
        IArtifactCleanupService cleanupService,
        IConsoleRenderer renderer,
        ILogger<MaintenanceCommand> logger)
    {
        _cleanupService = cleanupService ?? throw new ArgumentNullException(nameof(cleanupService));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Executes the command handler and returns a process exit code.</summary>
    public async Task<int> ExecuteAsync(string? strategyId, string? repoRoot, bool json)
    {
        var context = new ArtifactCleanupContext(RepoRoot: repoRoot);

        try
        {
            var result = await _cleanupService.CleanAsync(strategyId, context).ConfigureAwait(false);

            if (json)
            {
                Console.Out.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
                {
                    strategyId = result.StrategyId,
                    bytesReclaimed = result.BytesReclaimed,
                    pathsDeleted = result.PathsDeleted,
                    errors = result.Errors
                }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                _renderer.RenderSuccess(
                    $"Cleanup complete: {FormatBytes(result.BytesReclaimed)} reclaimed, {result.PathsDeleted.Count} path(s) deleted.");
                foreach (var err in result.Errors)
                {
                    Console.Error.WriteLine($"Warning: {err}");
                }
            }

            return result.Errors.Count > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Maintenance clean failed");
            _renderer.RenderError(ex.Message);
            return 2;
        }
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
    }
}
