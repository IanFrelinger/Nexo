using System.Text;
using System.Text.Json;

namespace Nexo.BackgroundAgents.Observations;

/// <summary>
/// File-backed implementation of <see cref="IObservationStore"/> that writes one
/// JSON object per line to <c>.nexo/runtime-studio/observations.jsonl</c>
/// (or a caller-supplied path).
///
/// <para>Mirrors the design of <see cref="Telemetry.CycleEventStore"/>: append-only,
/// best-effort I/O, lock-protected writes, malformed-line-tolerant reads. The
/// shared format choice (JSONL) means operators can grep, jq, and tail both
/// files with the same toolchain.</para>
/// </summary>
public sealed class JsonlObservationStore : IObservationStore
{
    /// <summary>Default repo-relative location for the observations log.</summary>
    public const string DefaultRelativePath = ".nexo/runtime-studio/observations.jsonl";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _path;
    private readonly object _gate = new();

    /// <summary>
    /// Construct a store at <see cref="DefaultRelativePath"/> under the current
    /// working directory (typically the repo root for the daemon).
    /// </summary>
    public JsonlObservationStore()
        : this(System.IO.Path.Combine(Directory.GetCurrentDirectory(), DefaultRelativePath))
    {
    }

    public JsonlObservationStore(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Observation log path must be non-empty.", nameof(path));
        _path = path;
    }

    public string Location => _path;

    public void Append(RuntimeObservation observation)
    {
        if (observation is null)
            throw new ArgumentNullException(nameof(observation));
        try
        {
            var dir = System.IO.Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(observation, SerializerOptions);
            lock (_gate)
            {
                File.AppendAllText(_path, json + "\n", Encoding.UTF8);
            }
        }
        catch
        {
            // Swallow — observations must never destabilise a cycle.
        }
    }

    public IEnumerable<RuntimeObservation> ReadSince(
        DateTimeOffset? since = null,
        ObservationKind? kind = null,
        int? limit = null)
    {
        if (!File.Exists(_path))
            yield break;

        var emitted = 0;
        using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            RuntimeObservation? obs = null;
            try
            {
                obs = JsonSerializer.Deserialize<RuntimeObservation>(line, SerializerOptions);
            }
            catch
            {
                // Skip malformed lines (e.g. partial write on prior crash).
            }
            if (obs is null)
                continue;
            if (since is not null && obs.ts < since.Value)
                continue;
            if (kind is not null && obs.kind != kind.Value)
                continue;

            yield return obs;
            emitted++;
            if (limit is not null && emitted >= limit.Value)
                yield break;
        }
    }
}

