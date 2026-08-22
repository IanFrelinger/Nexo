using System.Text.Json;
using System.Text.RegularExpressions;

namespace Ashlar.Spikes.FirstFlight;

/// <summary>
/// A proposal produced by a model AT FLIGHT TIME. The run script called the local
/// provider (ollama), recorded the raw exchange to the repo's recordings directory, and
/// mounted the recording read-only at <c>/ashlar-live</c>; this loader extracts the
/// proposed source from the model's raw response.
///
/// <para>Extraction is deliberately mechanical (fenced code block, else first
/// <c>using</c> to last brace) and does NO validation beyond finding a class name — the
/// certification gate is the judge of the proposal, and pre-filtering here would blur
/// whose verdict counts. A recording so garbled that no source can even be located never
/// reaches the gate and fails the flight loudly.</para>
/// </summary>
public sealed record LiveProposal(
    string Provider,
    string Model,
    string Signature,
    double DurationSeconds,
    string Source,
    string TypeName)
{
    /// <summary>Loads and extracts the proposal; null (with a printed reason) when unusable.</summary>
    public static LiveProposal? Load(string recordingPath)
    {
        if (!File.Exists(recordingPath))
        {
            Console.WriteLine($"LIVE PROPOSAL: no recording at {recordingPath} (was -Live passed to the run script?)");
            return null;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(recordingPath));
        var root = doc.RootElement;
        var response = root.GetProperty("response").GetString() ?? "";
        var provider = root.TryGetProperty("provider", out var p) ? p.GetString() ?? "?" : "?";
        var model = root.TryGetProperty("model", out var m) ? m.GetString() ?? "?" : "?";
        var signature = root.TryGetProperty("signature", out var s) ? s.GetString() ?? "model:unknown:live" : "model:unknown:live";
        var duration = root.TryGetProperty("durationSeconds", out var d) ? d.GetDouble() : 0;

        var source = ExtractSource(response);
        if (source is null)
        {
            Console.WriteLine("LIVE PROPOSAL: no C# source could be located in the model response; "
                + "the raw recording is retained as evidence of the attempt");
            return null;
        }

        var typeName = DeriveTypeName(source);
        if (typeName is null)
        {
            Console.WriteLine("LIVE PROPOSAL: no class declaration found in the extracted source");
            return null;
        }

        return new LiveProposal(provider, model, signature, duration, source, typeName);
    }

    private static string? ExtractSource(string response)
    {
        // Preferred: a fenced code block (```csharp or bare ```).
        var fence = Regex.Match(response, "```(?:csharp|cs|c#)?\\s*\\n(.*?)```", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (fence.Success)
        {
            var block = fence.Groups[1].Value.Trim();
            if (block.Length > 0)
                return block;
        }

        // Fallback: first `using` directive through the last closing brace.
        var start = response.IndexOf("using ", StringComparison.Ordinal);
        var end = response.LastIndexOf('}');
        if (start >= 0 && end > start)
            return response[start..(end + 1)].Trim();

        return null;
    }

    private static string? DeriveTypeName(string source)
    {
        var ns = Regex.Match(source, @"namespace\s+([\w.]+)");
        var cls = Regex.Match(source, @"class\s+(\w+)");
        if (!cls.Success)
            return null;
        return ns.Success ? $"{ns.Groups[1].Value}.{cls.Groups[1].Value}" : cls.Groups[1].Value;
    }
}
