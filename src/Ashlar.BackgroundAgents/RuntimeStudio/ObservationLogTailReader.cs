using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ashlar.BackgroundAgents.Observations;

namespace Ashlar.BackgroundAgents.RuntimeStudio;

/// <summary>
/// Best-effort tail read of <c>observations.jsonl</c> for operator metrics (no full-file scan).
/// </summary>
public static class ObservationLogTailReader
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Reads up to <paramref name="maxTailBytes"/> from the end of the file, counts non-empty lines,
    /// and returns the newest successfully parsed <see cref="RuntimeObservation.ts"/> (scanning tail to head).
    /// </summary>
    public static (int TailNonEmptyLineCount, DateTimeOffset? LastTimestamp) ReadTail(string observationsPath, int maxTailBytes = 65_536)
    {
        if (string.IsNullOrWhiteSpace(observationsPath) || !File.Exists(observationsPath))
            return (0, null);

        try
        {
            using var fs = new FileStream(observationsPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var len = fs.Length;
            if (len == 0)
                return (0, null);

            var take = (int)Math.Min(len, maxTailBytes);
            fs.Seek(-take, SeekOrigin.End);
            using var sr = new StreamReader(fs, Encoding.UTF8);
            var chunk = sr.ReadToEnd();
            var lines = chunk.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (lines.Length == 0)
                return (0, null);

            DateTimeOffset? lastTs = null;
            for (var i = lines.Length - 1; i >= 0; i--)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                try
                {
                    var obs = JsonSerializer.Deserialize<RuntimeObservation>(line, Json);
                    if (obs is not null)
                    {
                        lastTs = obs.ts;
                        break;
                    }
                }
                catch
                {
                    try
                    {
                        using var d = JsonDocument.Parse(line);
                        if (d.RootElement.TryGetProperty("ts", out var tsEl) &&
                            tsEl.ValueKind == JsonValueKind.String &&
                            DateTimeOffset.TryParse(
                                tsEl.GetString(),
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                                out var parsed))
                        {
                            lastTs = parsed;
                            break;
                        }
                    }
                    catch
                    {
                        /* skip */
                    }
                }
            }

            return (lines.Length, lastTs);
        }
        catch
        {
            return (0, null);
        }
    }
}
