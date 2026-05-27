using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nexo.Core.Application.Trust.Models;
using Nexo.Core.Application.Trust.Ports;

namespace GameDirector.Domain;

/// <summary>
/// Records Game Director tool/brick decisions into the Nexo adaptation audit store.
/// </summary>
public static class GameDirectorAudit
{
    public static string NewAuditId() => Guid.NewGuid().ToString("N");

    public static string HashInput(object input)
    {
        var json = JsonSerializer.Serialize(input);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }

    public static void LogToolExecution(
        IDataDecisionAuditLog auditLog,
        string auditId,
        string tool,
        string inputHash,
        string modelId,
        string trustPolicy,
        string outputSummary,
        IReadOnlyList<string>? sanitizationEvents = null)
    {
        var reason = JsonSerializer.Serialize(new
        {
            audit_id = auditId,
            tool,
            input_hash = inputHash,
            model_id = modelId,
            trust_policy = trustPolicy,
            output_summary = outputSummary,
            sanitization_events = sanitizationEvents ?? []
        });

        auditLog.LogClassification("game-director", tool, reason);
    }
}

public sealed record AuditRecord(
    string AuditId,
    DateTimeOffset Timestamp,
    string Tool,
    string InputHash,
    string ModelId,
    string TrustPolicy,
    string OutputSummary,
    IReadOnlyList<string> SanitizationEvents);

public static class AuditRecordParser
{
    public static bool TryParse(DataDecisionAuditEntry entry, out AuditRecord? record)
    {
        record = null;
        if (!string.Equals(entry.DataType, "game-director", StringComparison.OrdinalIgnoreCase))
            return false;

        if (string.IsNullOrWhiteSpace(entry.Reason))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(entry.Reason);
            var root = doc.RootElement;
            record = new AuditRecord(
                root.TryGetProperty("audit_id", out var id) ? id.GetString() ?? "" : "",
                entry.Timestamp,
                root.TryGetProperty("tool", out var tool) ? tool.GetString() ?? entry.LevelName ?? "" : entry.LevelName ?? "",
                root.TryGetProperty("input_hash", out var hash) ? hash.GetString() ?? "" : "",
                root.TryGetProperty("model_id", out var model) ? model.GetString() ?? "deterministic" : "deterministic",
                root.TryGetProperty("trust_policy", out var policy) ? policy.GetString() ?? "unknown" : "unknown",
                root.TryGetProperty("output_summary", out var summary) ? summary.GetString() ?? "" : "",
                root.TryGetProperty("sanitization_events", out var events) && events.ValueKind == JsonValueKind.Array
                    ? events.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => s.Length > 0).ToList()
                    : []);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static IReadOnlyList<AuditRecord> Query(
        IDataDecisionAuditLog auditLog,
        DateTimeOffset from,
        DateTimeOffset to,
        string? tool = null,
        string? assetId = null)
    {
        var entries = auditLog.GetRecent(5000, since: from, until: to, eventType: "Classification");
        var records = new List<AuditRecord>();
        foreach (var entry in entries)
        {
            if (!TryParse(entry, out var record) || record is null)
                continue;
            if (!string.IsNullOrWhiteSpace(tool) &&
                !string.Equals(record.Tool, tool, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!string.IsNullOrWhiteSpace(assetId) &&
                !record.OutputSummary.Contains(assetId, StringComparison.OrdinalIgnoreCase))
                continue;
            records.Add(record);
        }

        return records.OrderByDescending(r => r.Timestamp).ToList();
    }
}
