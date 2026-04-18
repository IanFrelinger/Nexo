using System.Text;

namespace Nexo.BackgroundAgents.Objectives;

/// <summary>
/// In-memory representation of a single backlog item — the first-class document
/// that replaces the planner's single-string <c>Objective</c> parameter.
///
/// <para>An objective lives on disk as a markdown file with a YAML-ish frontmatter
/// block (parsed by <see cref="ObjectiveDocumentParser"/>). Storing them as plain
/// markdown makes the backlog operator-editable, diff-friendly, and trivially
/// auditable — operators can grep, edit, or hand-author objectives without ever
/// running the daemon.</para>
///
/// <para>The frontmatter carries the lifecycle (status, owner, priority, attempts
/// counter, blocked reason); the body is free-form markdown the planner uses as
/// the high-signal goal description.</para>
/// </summary>
public sealed record ObjectiveDocument
{
    /// <summary>Stable unique slug — typically the filename without the <c>.md</c> extension.</summary>
    public required string Id { get; init; }

    /// <summary>Human-readable title (one line).</summary>
    public required string Title { get; init; }

    /// <summary>Lifecycle status. Drives which folder the file lives in on disk.</summary>
    public ObjectiveStatus Status { get; init; } = ObjectiveStatus.Pending;

    /// <summary>Optional owner agent id (typically "runtime-planner").</summary>
    public string? Owner { get; init; }

    /// <summary>
    /// Numeric priority. Lower number = higher priority (1 is highest). The store
    /// uses this as the secondary sort after status.
    /// </summary>
    public int Priority { get; init; } = 100;

    /// <summary>
    /// Number of times the planner has attempted this objective. Used by the
    /// auto-block heuristic — see <see cref="ObjectiveStore"/> docs.
    /// </summary>
    public int Attempts { get; init; }

    /// <summary>If <see cref="Status"/> is Blocked, the reason that was recorded.</summary>
    public string? BlockedReason { get; init; }

    /// <summary>Optional free-form tags used for grouping and reporting.</summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>UTC timestamp when the objective was first created (file mtime fallback).</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>UTC timestamp of the last lifecycle transition.</summary>
    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>The free-form markdown body the planner reads as its goal text.</summary>
    public string Body { get; init; } = string.Empty;

    /// <summary>
    /// Render this document back to its canonical markdown-with-frontmatter form.
    /// The output is stable so round-tripping (load → save → load) is a no-op,
    /// which keeps git diffs minimal when only one field is updated.
    /// </summary>
    public string Serialize()
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.Append("id: ").AppendLine(Id);
        sb.Append("title: ").AppendLine(EscapeYamlString(Title));
        sb.Append("status: ").AppendLine(Status.ToString());
        if (!string.IsNullOrWhiteSpace(Owner))
            sb.Append("owner: ").AppendLine(Owner);
        sb.Append("priority: ").AppendLine(Priority.ToString(System.Globalization.CultureInfo.InvariantCulture));
        sb.Append("attempts: ").AppendLine(Attempts.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (!string.IsNullOrWhiteSpace(BlockedReason))
            sb.Append("blocked_reason: ").AppendLine(EscapeYamlString(BlockedReason!));
        if (Tags.Count > 0)
            sb.Append("tags: [").Append(string.Join(", ", Tags.Select(EscapeYamlString))).AppendLine("]");
        sb.Append("created_at: ").AppendLine(CreatedAt.ToString("u", System.Globalization.CultureInfo.InvariantCulture));
        sb.Append("updated_at: ").AppendLine(UpdatedAt.ToString("u", System.Globalization.CultureInfo.InvariantCulture));
        sb.AppendLine("---");
        sb.AppendLine();
        sb.Append(Body.TrimEnd());
        sb.AppendLine();
        return sb.ToString();
    }

    private static string EscapeYamlString(string s)
    {
        // Quote on whitespace, colon, or special chars to keep the parser unambiguous.
        if (s.Length == 0) return "\"\"";
        var needsQuote = s.IndexOfAny(new[] { ':', '#', '\'', '"', '\n', '\r', '\t' }) >= 0
                         || s.StartsWith(' ') || s.EndsWith(' ');
        if (!needsQuote) return s;
        return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}

/// <summary>
/// Lifecycle states for an <see cref="ObjectiveDocument"/>. The store maps
/// each status to a folder under <c>.nexo/runtime-studio/objectives/</c> so the
/// physical layout matches the logical state and operators can move files
/// around manually if needed.
/// </summary>
public enum ObjectiveStatus
{
    Pending,
    InProgress,
    Done,
    Blocked
}
