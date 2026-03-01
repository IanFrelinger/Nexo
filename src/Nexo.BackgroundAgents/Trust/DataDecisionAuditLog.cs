using System.Collections.Concurrent;
using System.Text.Json;
using Nexo.Core.Application.Trust.Models;
using Nexo.Core.Application.Trust.Ports;

namespace Nexo.BackgroundAgents.Trust;

/// <summary>
/// Unified in-memory audit log for sanitization, boundary changes, and classification.
/// Implements both IDataDecisionAuditLog and ISanitizationAuditLog.
/// </summary>
public sealed class DataDecisionAuditLog : IDataDecisionAuditLog, ISanitizationAuditLog
{
    private readonly ConcurrentQueue<DataDecisionAuditEntry> _entries = new();
    private const int MaxEntries = 50_000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    /// <inheritdoc />
    public void LogSanitization(SanitizationAuditEntryDto entry)
    {
        Append(new DataDecisionAuditEntry
        {
            EventType = "Sanitization",
            Timestamp = entry.Timestamp,
            RuleVersion = entry.RuleVersion,
            FieldOrType = entry.FieldOrType,
            Disposition = entry.Disposition,
            Reason = entry.Reason,
        });
    }

    /// <inheritdoc />
    public void LogRedaction(DateTimeOffset timestamp, string ruleVersion, string fieldOrType, string disposition, string? reason)
    {
        LogSanitization(new SanitizationAuditEntryDto(timestamp, ruleVersion, fieldOrType, disposition, reason));
    }

    /// <inheritdoc />
    IReadOnlyList<SanitizationAuditEntry> ISanitizationAuditLog.GetRecent(int maxCount, DateTimeOffset? since)
    {
        var all = GetRecentInternal(maxCount * 3, since);
        return all
            .Where(e => e.EventType == "Sanitization")
            .Take(maxCount)
            .Select(e => new SanitizationAuditEntry
            {
                Timestamp = e.Timestamp,
                RuleVersion = e.RuleVersion ?? "",
                FieldOrType = e.FieldOrType ?? "",
                Disposition = e.Disposition ?? "",
                Reason = e.Reason,
            })
            .ToList();
    }

    /// <inheritdoc />
    public void LogBoundaryChange(BoundaryChangeEvent evt)
    {
        Append(new DataDecisionAuditEntry
        {
            EventType = "BoundaryChange",
            Timestamp = evt.Timestamp,
            ChangeType = evt.ChangeType,
            Category = evt.Category,
            SourceId = evt.SourceId,
            ProjectPath = evt.ProjectPath,
            PreviousState = evt.PreviousState,
            NewState = evt.NewState,
        });
    }

    /// <inheritdoc />
    public void LogClassification(string dataType, string levelName, string? reason)
    {
        Append(new DataDecisionAuditEntry
        {
            EventType = "Classification",
            Timestamp = DateTimeOffset.UtcNow,
            DataType = dataType,
            LevelName = levelName,
            Reason = reason,
        });
    }

    /// <inheritdoc />
    public IReadOnlyList<DataDecisionAuditEntry> GetRecent(int maxCount, DateTimeOffset? since = null) =>
        GetRecentInternal(maxCount, since);

    /// <inheritdoc />
    public string ExportToJson(int maxCount = 1000, DateTimeOffset? since = null)
    {
        var entries = GetRecentInternal(maxCount, since);
        var export = new
        {
            exportedAt = DateTimeOffset.UtcNow,
            count = entries.Count,
            since = since?.ToString("o"),
            entries = entries.Select(e => new
            {
                e.EventType,
                e.Timestamp,
                e.RuleVersion,
                e.FieldOrType,
                e.Disposition,
                e.Reason,
                e.ChangeType,
                e.Category,
                e.SourceId,
                e.ProjectPath,
                e.PreviousState,
                e.NewState,
                e.DataType,
                e.LevelName,
            }).ToList(),
        };
        return JsonSerializer.Serialize(export, JsonOptions);
    }

    /// <inheritdoc />
    public string ExportToMarkdown(int maxCount = 1000, DateTimeOffset? since = null)
    {
        var entries = GetRecentInternal(maxCount, since);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# Data Decision Audit Log");
        sb.AppendLine();
        sb.AppendLine($"Exported: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Entries: {entries.Count}");
        if (since.HasValue)
            sb.AppendLine($"Since: {since.Value:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();

        foreach (var e in entries)
        {
            sb.AppendLine($"## {e.EventType} @ {e.Timestamp:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            if (e.EventType == "Sanitization")
            {
                sb.AppendLine($"- **Rule:** {e.RuleVersion} | **Field:** {e.FieldOrType} | **Disposition:** {e.Disposition}");
                if (!string.IsNullOrEmpty(e.Reason))
                    sb.AppendLine($"- **Reason:** {e.Reason}");
            }
            else if (e.EventType == "BoundaryChange")
            {
                sb.AppendLine($"- **Change:** {e.ChangeType} | {e.PreviousState} → {e.NewState}");
                if (!string.IsNullOrEmpty(e.Category))
                    sb.AppendLine($"- **Category:** {e.Category}");
                if (!string.IsNullOrEmpty(e.SourceId))
                    sb.AppendLine($"- **Source:** {e.SourceId}");
                if (!string.IsNullOrEmpty(e.ProjectPath))
                    sb.AppendLine($"- **Project:** {e.ProjectPath}");
            }
            else if (e.EventType == "Classification")
            {
                sb.AppendLine($"- **DataType:** {e.DataType} | **Level:** {e.LevelName}");
                if (!string.IsNullOrEmpty(e.Reason))
                    sb.AppendLine($"- **Reason:** {e.Reason}");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private void Append(DataDecisionAuditEntry entry)
    {
        _entries.Enqueue(entry);
        while (_entries.Count > MaxEntries && _entries.TryDequeue(out _)) { }
    }

    private IReadOnlyList<DataDecisionAuditEntry> GetRecentInternal(int maxCount, DateTimeOffset? since)
    {
        var list = _entries.ToArray();
        var filtered = since.HasValue ? list.Where(e => e.Timestamp >= since.Value).ToArray() : list;
        return filtered.OrderByDescending(e => e.Timestamp).Take(maxCount).ToList();
    }
}
