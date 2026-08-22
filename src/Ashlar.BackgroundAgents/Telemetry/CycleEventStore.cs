using System.Text;
using System.Text.Json;

namespace Ashlar.BackgroundAgents.Telemetry;

/// <summary>
/// Append-only structured log of background-agent execution cycles, written as
/// JSONL (one JSON object per line) for trivial post-hoc analysis.
///
/// <para>This complements the human-friendly Markdown scratchpad — the scratchpad
/// is for the agent to read; this file is for operators (and the
/// <c>ashlar background-agent stats</c> CLI) to read. Each entry captures enough
/// detail to compute aggregate metrics (avg duration, tool throughput, denial
/// rates, model attribution) without needing to scrape live logs.</para>
///
/// <para>Storage location is configurable; the daemon defaults to
/// <c>&lt;repo&gt;/.ashlar/runtime-studio/cycles.jsonl</c>. Parent directories are
/// created lazily; I/O failures are swallowed so telemetry can never crash a cycle.</para>
/// </summary>
public sealed class CycleEventStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Default repo-relative location for the cycle event log.
    /// </summary>
    public const string DefaultRelativePath = ".ashlar/runtime-studio/cycles.jsonl";

    private readonly string _path;
    private readonly object _gate = new();

    /// <summary>
    /// Construct a store that writes to <see cref="DefaultRelativePath"/> under the
    /// current working directory (typically the repo root for the daemon).
    /// </summary>
    public CycleEventStore()
        : this(System.IO.Path.Combine(Directory.GetCurrentDirectory(), DefaultRelativePath))
    {
    }

    public CycleEventStore(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Cycle log path must be non-empty.", nameof(path));
        _path = path;
    }

    /// <summary>
    /// Path the store writes to. Exposed for diagnostics and the stats command.
    /// </summary>
    public string Path => _path;

    /// <summary>
    /// Append a single cycle event. Best-effort: serialization or I/O errors are
    /// swallowed so telemetry can never destabilise a successful agent cycle.
    /// </summary>
    public void Append(CycleEvent evt)
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(evt, SerializerOptions);
            lock (_gate)
            {
                File.AppendAllText(_path, json + "\n", Encoding.UTF8);
            }
        }
        catch
        {
            // Swallow — this store is purely observational.
        }
    }

    /// <summary>
    /// Stream events from the log in append order. Yields nothing if the file
    /// does not exist or contains only malformed lines.
    /// </summary>
    public IEnumerable<CycleEvent> Read()
    {
        if (!File.Exists(_path))
            yield break;

        using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            CycleEvent? evt = null;
            try
            {
                evt = JsonSerializer.Deserialize<CycleEvent>(line, SerializerOptions);
            }
            catch
            {
                // Skip malformed lines (e.g. partial write on prior crash).
            }
            if (evt is not null)
                yield return evt;
        }
    }
}
