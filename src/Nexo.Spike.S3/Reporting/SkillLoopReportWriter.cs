using System.Text.Json;
using System.Text.Json.Serialization;
using Nexo.Spike.S3.Certification;
using Nexo.Spike.S3.Generation;
using Nexo.Spike.S3.Loop;
using Nexo.Spike.S3.Models;
using Nexo.Spike.S3.Registry;
using Nexo.Spike.S3.Reporting;

namespace Nexo.Spike.S3.Reporting;

public static class SkillLoopReportWriter
{
    public const string DefaultReportPath = "artifacts/s3/skill-loop-report.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static async Task<string> WriteAsync(SkillLoopReport report, string? outputPath = null)
    {
        var path = outputPath ?? ResolveDefaultReportPath();
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(report, SerializerOptions)).ConfigureAwait(false);
        return path;
    }

    public static string ResolveDefaultReportPath()
    {
        var candidates = new List<string>();
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            candidates.Add(Path.Combine(dir.FullName, DefaultReportPath));
            dir = dir.Parent;
        }

        candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), DefaultReportPath));
        foreach (var candidate in candidates)
        {
            var parent = Path.GetDirectoryName(candidate);
            if (parent is not null)
                return candidate;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), DefaultReportPath);
    }
}

public static class SkillLoopDemonstration
{
    public static async Task<SkillLoopReport> RunAsync(
        SkillReuseLoop loop,
        SkillRegistry registry,
        string generatorBackend,
        CancellationToken ct = default)
    {
        var calls = new List<SkillLoopCallRecord>();
        var scenarios = BuildScenarios();

        foreach (var scenario in scenarios)
        {
            ct.ThrowIfCancellationRequested();
            var before = registry.ListEntries().Count;
            var result = await loop.EnsureSkillAsync(scenario.Intent, scenario.ContextLabel, ct)
                .ConfigureAwait(false);
            var after = registry.ListEntries().Count;

            calls.Add(new SkillLoopCallRecord(
                scenario.Name,
                scenario.ContextLabel,
                result.Outcome,
                result.GenerationRan,
                result.CertificationRan,
                result.Reused,
                result.GeneratorBackend,
                result.Entry?.EntryId,
                result.RejectionReason,
                before,
                after,
                result.IsolationEnforced,
                result.ModelId,
                result.AttemptsUsed,
                result.Entry?.Provenance.Temperature));
        }

        var version = string.Equals(generatorBackend, SkillGeneratorFactory.ClaudeMode, StringComparison.OrdinalIgnoreCase)
            ? "s3.2-claude-v1"
            : "s3.1-recorded-v1";

        return new SkillLoopReport(
            Version: version,
            GeneratorBackend: generatorBackend,
            RunAt: DateTimeOffset.UtcNow,
            Calls: calls,
            RegistryEntryIds: registry.ListEntries().Select(e => e.EntryId).OrderBy(id => id).ToList());
    }

    private static IReadOnlyList<DemoScenario> BuildScenarios() =>
    [
        new(
            "generated-and-admitted",
            new IntentDescriptor(
                "csv-column-type-inference",
                ["core", "offline"],
                CapabilityKey: "csv-type-inference"),
            "initial-discovery"),
        new(
            "reused-same-intent",
            new IntentDescriptor(
                "csv-column-type-inference",
                ["core", "offline"],
                CapabilityKey: "csv-type-inference"),
            "repeat-request"),
        new(
            "reused-other-context",
            new IntentDescriptor(
                "etl-pipeline-csv-inference",
                ["etl", "batch"],
                CapabilityKey: "csv-type-inference"),
            "etl-pipeline-stage-3"),
        new(
            "rejected-failed-certification",
            new IntentDescriptor(
                "csv-constant-type-guess",
                ["constant-guess", "offline"],
                CapabilityKey: "csv-constant-guess"),
            "bad-candidate-intent")
    ];

    private sealed record DemoScenario(string Name, IntentDescriptor Intent, string ContextLabel);
}
