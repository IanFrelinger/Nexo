using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Trust.Ports;

namespace Nexo.CLI.Commands;

/// <summary>
/// CLI handler for Trust & Information Architecture: audit log and access boundary.
/// </summary>
public class TrustCommand
{
    private readonly IDataDecisionAuditLog? _auditLog;
    private readonly IAccessBoundary? _accessBoundary;
    private readonly ILogger<TrustCommand> _logger;

    public TrustCommand(
        IDataDecisionAuditLog? auditLog,
        IAccessBoundary? accessBoundary,
        ILogger<TrustCommand> logger)
    {
        _auditLog = auditLog;
        _accessBoundary = accessBoundary;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Show recent audit entries or export to JSON/Markdown.
    /// </summary>
    public Task<int> AuditAsync(int count, string? since, bool formatJson, bool formatMarkdown, CancellationToken ct = default)
    {
        try
        {
            if (_auditLog == null)
            {
                if (formatJson)
                    Console.Out.WriteLine("{\"ok\":false,\"error\":\"Trust audit log not registered\"}");
                else
                    Console.Error.WriteLine("Trust audit log not registered.");
                return Task.FromResult(1);
            }

            DateTimeOffset? sinceDt = ParseSince(since);

            if (formatJson)
            {
                var json = _auditLog.ExportToJson(count, sinceDt);
                Console.Out.WriteLine(json);
            }
            else if (formatMarkdown)
            {
                var md = _auditLog.ExportToMarkdown(count, sinceDt);
                Console.Out.WriteLine(md);
            }
            else
            {
                var entries = _auditLog.GetRecent(count, sinceDt);
                Console.Out.WriteLine($"Data Decision Audit ({entries.Count} entries):");
                foreach (var e in entries)
                {
                    Console.Out.WriteLine($"  [{e.Timestamp:HH:mm:ss}] {e.EventType}: {Summarize(e)}");
                }
            }

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trust audit failed");
            if (formatJson)
                Console.Out.WriteLine($"{{\"ok\":false,\"error\":\"{ex.Message}\"}}");
            else
                Console.Error.WriteLine(ex.Message);
            return Task.FromResult(1);
        }
    }

    /// <summary>
    /// Show access boundary status (pause state, categories, sources).
    /// </summary>
    public Task<int> BoundaryAsync(bool formatJson, CancellationToken ct = default)
    {
        try
        {
            if (_accessBoundary == null)
            {
                if (formatJson)
                    Console.Out.WriteLine("{\"ok\":false,\"error\":\"Access boundary not registered\"}");
                else
                    Console.Error.WriteLine("Access boundary not registered.");
                return Task.FromResult(1);
            }

            if (formatJson)
            {
                var payload = new
                {
                    isPaused = _accessBoundary.IsObservationPaused,
                    status = _accessBoundary.IsObservationPaused ? "Observation paused" : "Observing",
                };
                Console.Out.WriteLine(System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                Console.Out.WriteLine("Access Boundary:");
                Console.Out.WriteLine($"  Paused: {(_accessBoundary.IsObservationPaused ? "Yes" : "No")}");
                Console.Out.WriteLine($"  Status: {(_accessBoundary.IsObservationPaused ? "Observation halted" : "Observing (subject to category/source rules)")}");
            }

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trust boundary status failed");
            if (formatJson)
                Console.Out.WriteLine($"{{\"ok\":false,\"error\":\"{ex.Message}\"}}");
            else
                Console.Error.WriteLine(ex.Message);
            return Task.FromResult(1);
        }
    }

    private static string Summarize(Nexo.Core.Application.Trust.Models.DataDecisionAuditEntry e)
    {
        return e.EventType switch
        {
            "Sanitization" => $"{e.FieldOrType} {e.Disposition}",
            "BoundaryChange" => $"{e.ChangeType} {e.PreviousState} → {e.NewState}",
            "Classification" => $"{e.DataType} → {e.LevelName}",
            _ => "",
        };
    }

    private static DateTimeOffset? ParseSince(string? since)
    {
        if (string.IsNullOrWhiteSpace(since))
            return null;
        if (TimeSpan.TryParse(since, out var ts))
            return DateTimeOffset.UtcNow - ts;
        if (DateTimeOffset.TryParse(since, out var dt))
            return dt;
        return null;
    }
}
