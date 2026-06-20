using System.Text.Json;
using Nexo.Spike.S0;
using Nexo.Spike.S0.Models;
using Nexo.Spike.S3.Models;

namespace Nexo.Spike.S3.Generation;

public static class GenerationRequestSealer
{
    public const string RequestFileName = "request.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static async Task<(GenerationRequest Request, string IntentSpecPath)> BuildAsync(
        IntentDescriptor intent,
        IReadOnlyList<GateVerdictSummary>? priorVerdicts,
        int attemptIndex,
        CancellationToken ct = default)
    {
        var intentSpecPath = ResolveIntentSpecPath();
        var spec = await BrickSpecLoader.LoadAsync(intentSpecPath, ct).ConfigureAwait(false);
        var sealedSpec = ToSealedSpec(spec);
        var request = new GenerationRequest(
            intent,
            sealedSpec,
            priorVerdicts ?? [],
            attemptIndex);

        return (request, intentSpecPath);
    }

    public static SealedIntentSpec ToSealedSpec(BrickSpec spec) =>
        new(
            spec.IntentId,
            spec.Title,
            spec.Description,
            spec.Inputs,
            spec.Outputs,
            spec.Invariants);

    public static void Persist(GenerationRequest request, string requestPath)
    {
        var json = JsonSerializer.Serialize(request, SerializerOptions);
        Directory.CreateDirectory(Path.GetDirectoryName(requestPath)!);
        File.WriteAllText(requestPath, json);
    }

    public static GenerationRequest Load(string requestPath)
    {
        var json = File.ReadAllText(requestPath);
        return JsonSerializer.Deserialize<GenerationRequest>(json, SerializerOptions)
               ?? throw new InvalidOperationException($"Could not deserialize generation request: {requestPath}");
    }

    internal static string ResolveIntentSpecPath()
    {
        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "samples", "spike-s0", "intents", "honest-csv-inferrer.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "Nexo.Spike.S1", "Fixtures", "honest-csv-inferrer.json"),
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "honest-csv-inferrer.json")
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
                return path;
        }

        throw new FileNotFoundException("honest-csv-inferrer intent fixture not found");
    }
}
