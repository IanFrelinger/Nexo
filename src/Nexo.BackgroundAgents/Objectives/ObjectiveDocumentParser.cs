#pragma warning disable NEXOEXP001 // Parses the experimental tiering fields (source, touch-set) of an objective by design; see docs/SdkCompatibilityPolicy.md.
using System.Globalization;
using System.Text;
using Nexo.Core.Application.Autonomy;

namespace Nexo.BackgroundAgents.Objectives;

/// <summary>
/// Parses (and serializes) the markdown-with-frontmatter format used by
/// <see cref="ObjectiveDocument"/>. Implemented in-house rather than pulling
/// a YAML library because:
/// 1. The schema is tiny and intentionally constrained — we only support a flat
///    string→string map plus a single bracket-list (<c>tags</c>).
/// 2. Adding YamlDotNet would balloon the Background Agents footprint for one
///    feature and bring versioning headaches across net8/net9 targets.
///
/// <para>The parser is deliberately tolerant: missing fields fall back to defaults,
/// unknown keys are ignored, and a missing or malformed frontmatter block makes
/// the whole file the body (with <see cref="ObjectiveDocument.Id"/> derived from
/// the supplied filename and a Pending status). This means the planner — or an
/// operator — can drop a one-line markdown file into <c>pending/</c> and the
/// store will treat it as a valid objective.</para>
/// </summary>
public static class ObjectiveDocumentParser
{
    /// <summary>
    /// Parse a single objective document. The <paramref name="fallbackId"/> is used
    /// when the frontmatter doesn't supply an explicit id.
    /// </summary>
    public static ObjectiveDocument Parse(string text, string fallbackId, ObjectiveStatus fallbackStatus = ObjectiveStatus.Pending)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new ObjectiveDocument
            {
                Id = fallbackId,
                Title = fallbackId,
                Status = fallbackStatus,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                Body = string.Empty
            };
        }

        // Normalize line endings to \n for easier slicing without losing data.
        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');

        var (frontmatter, body) = SplitFrontmatter(normalized);
        var fields = ParseFrontmatter(frontmatter);

        var id = GetString(fields, "id") ?? fallbackId;
        var title = GetString(fields, "title") ?? id;
        var status = ParseEnum<ObjectiveStatus>(GetString(fields, "status")) ?? fallbackStatus;
        var owner = GetString(fields, "owner");
        var priority = ParseInt(GetString(fields, "priority")) ?? 100;
        var attempts = ParseInt(GetString(fields, "attempts")) ?? 0;
        var blockedReason = GetString(fields, "blocked_reason");
        var tags = ParseList(GetString(fields, "tags"));
        // Objective files are operator-authored artifacts, so a missing source parses as
        // Human (the trust root); machine intake stamps its source explicitly (R1.1).
        var source = ParseEnum<ObjectiveSource>(GetString(fields, "source")) ?? ObjectiveSource.Human;
        var touchPaths = ParseList(GetString(fields, "touch_paths"));
        var touchNamespaces = ParseList(GetString(fields, "touch_namespaces"));
        var touchCapabilities = ParseList(GetString(fields, "touch_capabilities"));
        var touch = touchPaths.Count > 0 || touchNamespaces.Count > 0 || touchCapabilities.Count > 0
            ? new TouchSet
            {
                PathPrefixes = touchPaths,
                Namespaces = touchNamespaces,
                Capabilities = touchCapabilities,
            }
            : null;
        var now = DateTimeOffset.UtcNow;
        var createdAt = ParseTimestamp(GetString(fields, "created_at")) ?? now;
        var updatedAt = ParseTimestamp(GetString(fields, "updated_at")) ?? createdAt;

        return new ObjectiveDocument
        {
            Id = id,
            Title = title,
            Status = status,
            Owner = owner,
            Priority = priority,
            Attempts = attempts,
            BlockedReason = blockedReason,
            Tags = tags,
            Source = source,
            Touch = touch,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            Body = body.Trim()
        };
    }

    private static (string frontmatter, string body) SplitFrontmatter(string text)
    {
        // Frontmatter must start with --- on the very first line and be terminated
        // by a second --- on its own line. Anything else means "no frontmatter".
        if (!text.StartsWith("---\n", StringComparison.Ordinal) && text != "---")
            return (string.Empty, text);

        // Skip the opening fence.
        var rest = text.AsSpan(4); // length of "---\n"
        var endRel = rest.IndexOf("\n---", StringComparison.Ordinal);
        if (endRel < 0) return (string.Empty, text);

        var fm = rest.Slice(0, endRel).ToString();
        var afterFenceIdx = endRel + 4; // past "\n---"
        // Consume the optional newline after the closing fence.
        var bodySpan = rest.Slice(Math.Min(afterFenceIdx, rest.Length));
        if (bodySpan.Length > 0 && bodySpan[0] == '\n')
            bodySpan = bodySpan.Slice(1);
        return (fm, bodySpan.ToString());
    }

    private static IReadOnlyDictionary<string, string> ParseFrontmatter(string frontmatter)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(frontmatter))
            return dict;

        foreach (var rawLine in frontmatter.Split('\n'))
        {
            var line = rawLine.TrimEnd();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                continue;
            var colon = line.IndexOf(':');
            if (colon <= 0) continue;
            var key = line.Substring(0, colon).Trim();
            var value = line.Substring(colon + 1).Trim();
            value = Unquote(value);
            dict[key] = value;
        }
        return dict;
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2 &&
            ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
        {
            return value.Substring(1, value.Length - 2)
                        .Replace("\\\"", "\"")
                        .Replace("\\\\", "\\");
        }
        return value;
    }

    private static string? GetString(IReadOnlyDictionary<string, string> fields, string key)
        => fields.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : null;

    private static int? ParseInt(string? raw)
        => int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : null;

    private static T? ParseEnum<T>(string? raw) where T : struct
        => Enum.TryParse<T>(raw, ignoreCase: true, out var v) ? v : null;

    private static DateTimeOffset? ParseTimestamp(string? raw)
        => DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var ts)
            ? ts
            : null;

    private static IReadOnlyList<string> ParseList(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<string>();
        // Accept both `[a, b]` flow form and `a, b` bare form.
        var trimmed = raw.Trim();
        if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            trimmed = trimmed.Substring(1, trimmed.Length - 2);
        return trimmed
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Unquote)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();
    }
}
