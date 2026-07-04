using System.Text.Json;
using Nexo.Core.Application.Trust.Models;

namespace Nexo.Infrastructure.Trust;

/// <summary>
/// Shared export logic for UserKnowledgeLogStore implementations.
/// </summary>
internal static class UserKnowledgeLogExportHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>To json.</summary>
    public static string ToJson(IReadOnlyList<UserKnowledgeLogEntry> entries)
    {
        var export = new
        {
            exportedAt = DateTimeOffset.UtcNow,
            count = entries.Count,
            entries = entries.Select(e => new
            {
                e.Id,
                e.DataType,
                e.Content,
                e.SourceObservationIds,
                e.Version,
                e.CreatedAt,
                e.UpdatedAt,
            }).ToList(),
        };
        return JsonSerializer.Serialize(export, JsonOptions);
    }

    /// <summary>To markdown.</summary>
    public static string ToMarkdown(IReadOnlyList<UserKnowledgeLogEntry> entries)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# Nexo User Knowledge Log");
        sb.AppendLine();
        sb.AppendLine($"Exported: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Entries: {entries.Count}");
        sb.AppendLine();

        foreach (var e in entries)
        {
            sb.AppendLine($"## {e.Id}");
            sb.AppendLine();
            sb.AppendLine($"- **DataType:** {e.DataType}");
            sb.AppendLine($"- **Version:** {e.Version}");
            sb.AppendLine($"- **Created:** {e.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine($"- **Updated:** {e.UpdatedAt:yyyy-MM-dd HH:mm:ss} UTC");
            if (e.SourceObservationIds is { Count: > 0 })
            {
                sb.AppendLine("- **Provenance:**");
                foreach (var sid in e.SourceObservationIds)
                    sb.AppendLine($"  - {sid}");
            }
            sb.AppendLine();
            sb.AppendLine(e.Content);
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
