using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Trust.Models;
using Nexo.Core.Application.Trust.Ports;

namespace Nexo.CLI.Commands;

/// <summary>
/// CLI handler for Trust & Information Architecture: audit log and access boundary.
/// </summary>
public class TrustCommand
{
    private readonly IDataDecisionAuditLog? _auditLog;
    private readonly IAccessBoundary? _accessBoundary;
    private readonly ITrustPolicyPackRegistry? _policyPackRegistry;
    private readonly ILogger<TrustCommand> _logger;

    public TrustCommand(
        IDataDecisionAuditLog? auditLog,
        IAccessBoundary? accessBoundary,
        ITrustPolicyPackRegistry? policyPackRegistry,
        ILogger<TrustCommand> logger)
    {
        _auditLog = auditLog;
        _accessBoundary = accessBoundary;
        _policyPackRegistry = policyPackRegistry;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Show recent audit entries or export to JSON/Markdown/CSV.
    /// </summary>
    public Task<int> AuditAsync(int count, string? since, string? until, string? type, bool formatJson, bool formatMarkdown, bool formatCsv, CancellationToken ct = default)
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

            var sinceDt = ParseSince(since);
            var untilDt = ParseUntil(until);

            if (formatJson)
            {
                var json = _auditLog.ExportToJson(count, sinceDt, untilDt, type);
                Console.Out.WriteLine(json);
            }
            else if (formatMarkdown)
            {
                var md = _auditLog.ExportToMarkdown(count, sinceDt, untilDt, type);
                Console.Out.WriteLine(md);
            }
            else if (formatCsv)
            {
                var csv = _auditLog.ExportToCsv(count, sinceDt, untilDt, type);
                Console.Out.WriteLine(csv);
            }
            else
            {
                var entries = _auditLog.GetRecent(count, sinceDt, untilDt, type);
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
    /// Show combined compliance dashboard: boundary status + audit summary.
    /// </summary>
    public Task<int> DashboardAsync(int auditCount, bool formatJson, CancellationToken ct = default)
    {
        try
        {
            if (formatJson)
            {
                var dashboard = new Dictionary<string, object>();
                if (_accessBoundary != null)
                {
                    var activePack = _accessBoundary.GetActivePolicyPack();
                    dashboard["boundary"] = new Dictionary<string, object?>
                    {
                        ["isPaused"] = _accessBoundary.IsObservationPaused,
                        ["status"] = _accessBoundary.IsObservationPaused ? "Observation paused" : "Observing",
                        ["activePolicyPack"] = activePack is null
                            ? (object?)null
                            : new Dictionary<string, object?>
                            {
                                ["id"] = activePack.Id,
                                ["version"] = activePack.Version
                            }
                    };
                }
                else
                {
                    dashboard["boundary"] = new Dictionary<string, object> { ["error"] = "Not registered" };
                }
                if (_auditLog != null)
                {
                    var entries = _auditLog.GetRecent(auditCount, null);
                    var byType = entries.GroupBy(e => e.EventType).ToDictionary(g => g.Key, g => g.Count());
                    dashboard["audit"] = new Dictionary<string, object>
                    {
                        ["recentCount"] = entries.Count,
                        ["byEventType"] = byType
                    };
                }
                else
                {
                    dashboard["audit"] = new Dictionary<string, object> { ["error"] = "Not registered" };
                }
                Console.Out.WriteLine(System.Text.Json.JsonSerializer.Serialize(dashboard, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                if (_accessBoundary != null)
                {
                    var activePack = _accessBoundary.GetActivePolicyPack();
                    Console.Out.WriteLine("Access Boundary:");
                    Console.Out.WriteLine($"  Paused: {(_accessBoundary.IsObservationPaused ? "Yes" : "No")}");
                    Console.Out.WriteLine($"  Status: {(_accessBoundary.IsObservationPaused ? "Observation halted" : "Observing")}");
                    Console.Out.WriteLine($"  Active policy pack: {(activePack is null ? "none" : $"{activePack.Id}@{activePack.Version}")}");
                    Console.Out.WriteLine();
                }
                if (_auditLog != null)
                {
                    var entries = _auditLog.GetRecent(auditCount, null);
                    var byType = entries.GroupBy(e => e.EventType).ToDictionary(g => g.Key, g => g.Count());
                    Console.Out.WriteLine($"Audit Summary (last {entries.Count} entries):");
                    foreach (var kv in byType.OrderByDescending(x => x.Value))
                        Console.Out.WriteLine($"  {kv.Key}: {kv.Value}");
                }
            }
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trust dashboard failed");
            if (formatJson)
                Console.Out.WriteLine($"{{\"ok\":false,\"error\":\"{ex.Message}\"}}");
            else
                Console.Error.WriteLine(ex.Message);
            return Task.FromResult(1);
        }
    }

    /// <summary>
    /// Pause observation (halt all data collection).
    /// </summary>
    public Task<int> PauseAsync(bool formatJson, CancellationToken ct = default)
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
            _accessBoundary.SetPause(true);
            if (formatJson)
                Console.Out.WriteLine("{\"ok\":true,\"status\":\"paused\"}");
            else
                Console.Out.WriteLine("Observation paused.");
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trust pause failed");
            if (formatJson)
                Console.Out.WriteLine($"{{\"ok\":false,\"error\":\"{ex.Message}\"}}");
            else
                Console.Error.WriteLine(ex.Message);
            return Task.FromResult(1);
        }
    }

    /// <summary>
    /// Resume observation.
    /// </summary>
    public Task<int> ResumeAsync(bool formatJson, CancellationToken ct = default)
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
            _accessBoundary.SetPause(false);
            if (formatJson)
                Console.Out.WriteLine("{\"ok\":true,\"status\":\"resumed\"}");
            else
                Console.Out.WriteLine("Observation resumed.");
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trust resume failed");
            if (formatJson)
                Console.Out.WriteLine($"{{\"ok\":false,\"error\":\"{ex.Message}\"}}");
            else
                Console.Error.WriteLine(ex.Message);
            return Task.FromResult(1);
        }
    }

    /// <summary>
    /// Allow a category, source, or project override.
    /// </summary>
    public Task<int> AllowAsync(string? category, string? source, string? project, bool formatJson, CancellationToken ct = default)
    {
        return SetAllowDenyAsync(allowed: true, category, source, project, formatJson);
    }

    /// <summary>
    /// Deny a category, source, or project override.
    /// </summary>
    public Task<int> DenyAsync(string? category, string? source, string? project, bool formatJson, CancellationToken ct = default)
    {
        return SetAllowDenyAsync(allowed: false, category, source, project, formatJson);
    }

    private Task<int> SetAllowDenyAsync(bool allowed, string? category, string? source, string? project, bool formatJson)
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
            var action = allowed ? "allow" : "deny";
            if (!string.IsNullOrWhiteSpace(category))
            {
                _accessBoundary.SetCategoryAllowed(category.Trim(), allowed);
                if (formatJson)
                    Console.Out.WriteLine($"{{\"ok\":true,\"action\":\"{action}\",\"target\":\"category\",\"id\":\"{category.Trim()}\"}}");
                else
                    Console.Out.WriteLine($"Category '{category.Trim()}' {action}ed.");
            }
            else if (!string.IsNullOrWhiteSpace(source))
            {
                _accessBoundary.SetSourceAllowed(source.Trim(), allowed);
                if (formatJson)
                    Console.Out.WriteLine($"{{\"ok\":true,\"action\":\"{action}\",\"target\":\"source\",\"id\":\"{source.Trim()}\"}}");
                else
                    Console.Out.WriteLine($"Source '{source.Trim()}' {action}ed.");
            }
            else if (!string.IsNullOrWhiteSpace(project) && !string.IsNullOrWhiteSpace(source))
            {
                _accessBoundary.SetProjectOverride(project.Trim(), new Dictionary<string, bool> { [source.Trim()] = allowed });
                if (formatJson)
                    Console.Out.WriteLine($"{{\"ok\":true,\"action\":\"{action}\",\"target\":\"project-source\",\"project\":\"{project.Trim()}\",\"source\":\"{source.Trim()}\"}}");
                else
                    Console.Out.WriteLine($"Project '{project.Trim()}' source '{source.Trim()}' {action}ed.");
            }
            else if (!string.IsNullOrWhiteSpace(project))
            {
                if (formatJson)
                    Console.Out.WriteLine("{\"ok\":false,\"error\":\"Project override requires --source\"}");
                else
                    Console.Error.WriteLine("Project override requires --source (e.g. --project /path --source git)");
                return Task.FromResult(1);
            }
            else
            {
                if (formatJson)
                    Console.Out.WriteLine("{\"ok\":false,\"error\":\"Specify --category, --source, or --project\"}");
                else
                    Console.Error.WriteLine("Specify --category, --source, or --project");
                return Task.FromResult(1);
            }
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trust allow/deny failed");
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
                var activePack = _accessBoundary.GetActivePolicyPack();
                var payload = new
                {
                    isPaused = _accessBoundary.IsObservationPaused,
                    status = _accessBoundary.IsObservationPaused ? "Observation paused" : "Observing",
                    activePolicyPack = activePack is null ? null : new { activePack.Id, activePack.Version },
                };
                Console.Out.WriteLine(System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                var activePack = _accessBoundary.GetActivePolicyPack();
                Console.Out.WriteLine("Access Boundary:");
                Console.Out.WriteLine($"  Paused: {(_accessBoundary.IsObservationPaused ? "Yes" : "No")}");
                Console.Out.WriteLine($"  Status: {(_accessBoundary.IsObservationPaused ? "Observation halted" : "Observing (subject to category/source rules)")}");
                Console.Out.WriteLine($"  Active policy pack: {(activePack is null ? "none" : $"{activePack.Id}@{activePack.Version}")}");
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

    private static DateTimeOffset? ParseUntil(string? until)
    {
        if (string.IsNullOrWhiteSpace(until))
            return null;
        if (TimeSpan.TryParse(until, out var ts))
            return DateTimeOffset.UtcNow - ts;
        if (DateTimeOffset.TryParse(until, out var dt))
            return dt;
        return null;
    }

    public Task<int> DescribePolicyPackAsync(string? packId, bool formatJson, CancellationToken ct = default)
    {
        try
        {
            if (_policyPackRegistry == null)
            {
                if (formatJson)
                    Console.Out.WriteLine("{\"ok\":false,\"error\":\"Trust policy pack registry not registered\"}");
                else
                    Console.Error.WriteLine("Trust policy pack registry not registered.");
                return Task.FromResult(1);
            }

            if (string.IsNullOrWhiteSpace(packId))
            {
                if (formatJson)
                    Console.Out.WriteLine("{\"ok\":false,\"error\":\"pack id is required\"}");
                else
                    Console.Error.WriteLine("pack id is required");
                return Task.FromResult(1);
            }

            var pack = _policyPackRegistry.GetById(packId.Trim());
            if (pack == null)
            {
                if (formatJson)
                    Console.Out.WriteLine($"{{\"ok\":false,\"error\":\"unknown pack '{packId.Trim()}'\"}}");
                else
                    Console.Error.WriteLine($"unknown pack '{packId.Trim()}'");
                return Task.FromResult(1);
            }

            if (formatJson)
            {
                Console.Out.WriteLine(System.Text.Json.JsonSerializer.Serialize(
                    new
                    {
                        ok = true,
                        pack = new
                        {
                            pack.Id,
                            pack.Version,
                            pack.DisplayName,
                            pack.Description,
                            pack.PauseObservationByDefault,
                            pack.RecommendedFor,
                            categoryRules = pack.CategoryRules,
                            sourceRules = pack.SourceRules,
                            projectRules = pack.ProjectRules.Select(rule => new
                            {
                                rule.ProjectPath,
                                sourceRules = rule.SourceRules
                            }).ToArray()
                        }
                    },
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                Console.Out.WriteLine($"Trust policy pack: {pack.Id}@{pack.Version}");
                Console.Out.WriteLine($"  {pack.DisplayName}");
                if (!string.IsNullOrWhiteSpace(pack.Description))
                    Console.Out.WriteLine($"  {pack.Description}");
                Console.Out.WriteLine($"  Pause observation by default: {(pack.PauseObservationByDefault ? "yes" : "no")}");
                if (!string.IsNullOrWhiteSpace(pack.RecommendedFor))
                    Console.Out.WriteLine($"  Recommended for: {pack.RecommendedFor}");
                Console.Out.WriteLine("  Category rules:");
                foreach (var kv in pack.CategoryRules.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase))
                    Console.Out.WriteLine($"    - {kv.Key}: {(kv.Value ? "allow" : "deny")}");
                Console.Out.WriteLine("  Source rules:");
                foreach (var kv in pack.SourceRules.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase))
                    Console.Out.WriteLine($"    - {kv.Key}: {(kv.Value ? "allow" : "deny")}");
                Console.Out.WriteLine("  Project rules:");
                if (pack.ProjectRules.Count == 0)
                    Console.Out.WriteLine("    (none)");
                else
                {
                    foreach (var rule in pack.ProjectRules)
                    {
                        Console.Out.WriteLine($"    - Project: {rule.ProjectPath}");
                        foreach (var skv in rule.SourceRules.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase))
                            Console.Out.WriteLine($"        {skv.Key}: {(skv.Value ? "allow" : "deny")}");
                    }
                }
            }

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trust pack describe failed");
            if (formatJson)
                Console.Out.WriteLine($"{{\"ok\":false,\"error\":\"{ex.Message}\"}}");
            else
                Console.Error.WriteLine(ex.Message);
            return Task.FromResult(1);
        }
    }

    public Task<int> ListPolicyPacksAsync(bool formatJson, CancellationToken ct = default)
    {
        try
        {
            if (_policyPackRegistry == null)
            {
                if (formatJson)
                    Console.Out.WriteLine("{\"ok\":false,\"error\":\"Trust policy pack registry not registered\"}");
                else
                    Console.Error.WriteLine("Trust policy pack registry not registered.");
                return Task.FromResult(1);
            }

            var packs = _policyPackRegistry.ListPacks();
            if (formatJson)
            {
                Console.Out.WriteLine(System.Text.Json.JsonSerializer.Serialize(
                    new
                    {
                        ok = true,
                        count = packs.Count,
                        packs = packs.Select(pack => new
                        {
                            pack.Id,
                            pack.Version,
                            pack.DisplayName,
                            pack.Description
                        }).ToArray()
                    },
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                Console.Out.WriteLine($"Trust policy packs ({packs.Count}):");
                foreach (var pack in packs)
                    Console.Out.WriteLine($"  - {pack.Id}@{pack.Version}: {pack.DisplayName}");
            }

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trust pack list failed");
            if (formatJson)
                Console.Out.WriteLine($"{{\"ok\":false,\"error\":\"{ex.Message}\"}}");
            else
                Console.Error.WriteLine(ex.Message);
            return Task.FromResult(1);
        }
    }

    public Task<int> ApplyPolicyPackAsync(string packId, bool formatJson, CancellationToken ct = default)
    {
        try
        {
            if (_policyPackRegistry == null)
            {
                if (formatJson)
                    Console.Out.WriteLine("{\"ok\":false,\"error\":\"Trust policy pack registry not registered\"}");
                else
                    Console.Error.WriteLine("Trust policy pack registry not registered.");
                return Task.FromResult(1);
            }

            if (_accessBoundary == null)
            {
                if (formatJson)
                    Console.Out.WriteLine("{\"ok\":false,\"error\":\"Access boundary not registered\"}");
                else
                    Console.Error.WriteLine("Access boundary not registered.");
                return Task.FromResult(1);
            }

            if (string.IsNullOrWhiteSpace(packId))
            {
                if (formatJson)
                    Console.Out.WriteLine("{\"ok\":false,\"error\":\"pack id is required\"}");
                else
                    Console.Error.WriteLine("pack id is required");
                return Task.FromResult(1);
            }

            var pack = _policyPackRegistry.GetById(packId.Trim());
            if (pack == null)
            {
                if (formatJson)
                    Console.Out.WriteLine($"{{\"ok\":false,\"error\":\"unknown pack '{packId.Trim()}'\"}}");
                else
                    Console.Error.WriteLine($"unknown pack '{packId.Trim()}'");
                return Task.FromResult(1);
            }

            var previousActive = _accessBoundary.GetActivePolicyPack();
            var previousPackDefinition = previousActive is null
                ? null
                : _policyPackRegistry.GetById(previousActive.Id);

            _accessBoundary.ApplyPolicyPack(pack);

            ActiveTrustPolicyPack activated;
            try
            {
                activated = _policyPackRegistry.ActivateAsync(pack.Id, ct).GetAwaiter().GetResult();
            }
            catch
            {
                // Keep runtime boundary aligned with persisted registry state on activation failures.
                if (previousPackDefinition is not null)
                    _accessBoundary.ApplyPolicyPack(previousPackDefinition);
                else
                    _accessBoundary.Reset();
                throw;
            }
            if (formatJson)
            {
                Console.Out.WriteLine(System.Text.Json.JsonSerializer.Serialize(
                    new
                    {
                        ok = true,
                        applied = new { activated.Id, activated.Version, activated.ActivatedAtUtc, pack.DisplayName }
                    },
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                Console.Out.WriteLine($"Applied trust policy pack {activated.Id}@{activated.Version}.");
            }

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trust pack apply failed");
            if (formatJson)
                Console.Out.WriteLine($"{{\"ok\":false,\"error\":\"{ex.Message}\"}}");
            else
                Console.Error.WriteLine(ex.Message);
            return Task.FromResult(1);
        }
    }
}
