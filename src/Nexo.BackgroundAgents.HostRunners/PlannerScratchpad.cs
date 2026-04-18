using System.Globalization;
using System.Text;

namespace Nexo.BackgroundAgents.HostRunners;

/// <summary>
/// Tiny append-only persistence layer for planner-style background agents.
///
/// <para>The agent has no memory between cycles by default — every <c>RunCycleAsync</c>
/// starts the model with a blank slate, so the planner can endlessly re-derive the
/// same starting context ("what's in this repo?") without ever building on its own
/// past reasoning. The scratchpad exists to break that amnesia: each cycle appends a
/// short Markdown note ("rationale", tools_executed, write_paths) and the next cycle's
/// snapshot embeds the tail of the file as <c>RecentNotes</c>.</para>
///
/// <para>Storage format is plain Markdown so a human can read or edit the file
/// alongside the agent. Writes are atomic enough for a single-writer scenario
/// (the daemon executes one cycle at a time per agent); concurrent multi-writer
/// is intentionally out of scope.</para>
/// </summary>
public static class PlannerScratchpad
{
    /// <summary>
    /// Default cap on how much of the scratchpad tail is loaded back into the next
    /// snapshot. Sized to fit comfortably in a typical 8K-ish prompt window even
    /// alongside tool schemas and the world state.
    /// </summary>
    public const int DefaultTailBytes = 8 * 1024;

    /// <summary>
    /// Returns the trailing <paramref name="maxBytes"/> of the scratchpad as a string,
    /// truncated at a UTF-8 character boundary. Empty if the file does not yet exist.
    /// </summary>
    public static string LoadTail(string scratchpadPath, int maxBytes = DefaultTailBytes)
    {
        if (string.IsNullOrWhiteSpace(scratchpadPath) || !File.Exists(scratchpadPath))
            return string.Empty;

        try
        {
            var info = new FileInfo(scratchpadPath);
            if (info.Length == 0)
                return string.Empty;

            using var fs = new FileStream(scratchpadPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var take = (int)Math.Min(info.Length, maxBytes);
            var offset = info.Length - take;
            fs.Seek(offset, SeekOrigin.Begin);

            var buffer = new byte[take];
            var read = fs.Read(buffer, 0, take);
            // Trim a leading partial UTF-8 sequence so we always hand back valid text.
            var startIdx = 0;
            while (startIdx < read && (buffer[startIdx] & 0xC0) == 0x80)
                startIdx++;
            return Encoding.UTF8.GetString(buffer, startIdx, read - startIdx);
        }
        catch
        {
            // Scratchpad is best-effort context; never break a cycle if the file is locked.
            return string.Empty;
        }
    }

    /// <summary>
    /// Appends one cycle entry to the scratchpad. Creates the parent directory and file
    /// on first use. Best-effort: I/O errors are swallowed so a transient FS hiccup
    /// cannot crash a cycle that already produced useful work.
    /// </summary>
    public static void Append(string scratchpadPath, ScratchpadEntry entry)
    {
        if (string.IsNullOrWhiteSpace(scratchpadPath))
            return;

        try
        {
            var dir = Path.GetDirectoryName(scratchpadPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var sb = new StringBuilder();
            sb.Append("- **").Append(entry.Timestamp.ToString("u", CultureInfo.InvariantCulture)).Append("** ");
            sb.Append('`').Append(entry.AgentName).Append('`').Append(" — ");
            sb.Append("iter=").Append(entry.Iterations);
            sb.Append(", exec=").Append(entry.ToolsExecuted);
            sb.Append(", denied=").Append(entry.ToolsDenied);
            sb.Append(", stopped=").Append(entry.StoppedReason ?? "?");
            if (!string.IsNullOrWhiteSpace(entry.Rationale))
                sb.Append(" — _").Append(entry.Rationale!.Replace("\n", " ").Replace("_", "\\_")).Append('_');
            if (entry.WritePaths is { Count: > 0 })
            {
                sb.Append(" — paths: ");
                sb.Append(string.Join(", ", entry.WritePaths.Select(p => $"`{p}`")));
            }
            sb.AppendLine();

            File.AppendAllText(scratchpadPath, sb.ToString(), Encoding.UTF8);
        }
        catch
        {
            // Same rationale as LoadTail: never let scratchpad I/O crash a cycle.
        }
    }
}

/// <summary>
/// One Markdown line of planner history. Captured at the end of each ReAct cycle.
/// </summary>
public sealed record ScratchpadEntry(
    DateTimeOffset Timestamp,
    string AgentName,
    int Iterations,
    int ToolsExecuted,
    int ToolsDenied,
    string? StoppedReason,
    string? Rationale,
    IReadOnlyList<string>? WritePaths);
