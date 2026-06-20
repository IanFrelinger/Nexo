using Nexo.Spike.S0;
using Nexo.Spike.S3.Models;

namespace Nexo.Spike.S3.Generation;

/// <summary>
/// Deterministic scripted stand-in generator. Clearly labeled StandIn — not a model.
/// </summary>
public sealed class ScriptedStandInSkillGenerator : IStandInSkillGenerator
{
    public const string BackendLabel = "scripted-standin";

    private int _generationCount;

    string IStandInSkillGenerator.BackendName => BackendLabel;

    public int GenerationCount => _generationCount;

    public SkillCandidate Generate(IntentDescriptor intent)
    {
        _generationCount++;
        var catalog = StandInSkillCatalog.Resolve(intent);
        return new SkillCandidate(
            catalog.CandidateId,
            intent,
            ResolveIntentSpecPath(),
            catalog.ImplementationSource,
            BackendLabel,
            catalog.Hypothesis);
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
