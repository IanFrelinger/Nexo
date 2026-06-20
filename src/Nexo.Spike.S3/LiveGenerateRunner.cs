using Nexo.Spike.S3.Certification;
using Nexo.Spike.S3.Generation;
using Nexo.Spike.S3.Generation.Claude;
using Nexo.Spike.S3.Loop;
using Nexo.Spike.S3.Models;
using Nexo.Spike.S3.Registry;
using Nexo.Spike.S3.Reporting;

namespace Nexo.Spike.S3;

public static class LiveGenerateRunner
{
    public sealed record LiveGenerateOptions(
        string ArtifactsRoot,
        int IntentCount,
        int MaxAttemptsPerIntent,
        bool ResetRegistry,
        double MutationThreshold,
        bool RequireMutation,
        int DensitySeeds,
        int EscapeSeeds);

    public static async Task<int> RunAsync(LiveGenerateOptions options, CancellationToken ct = default)
    {
        SkillGeneratorFactory.EnsureLiveModeConfigured();

        var mode = SkillGeneratorFactory.ResolveMode();
        if (!string.Equals(mode, SkillGeneratorFactory.ClaudeMode, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Live generation requires NEXO_S3_GENERATOR=claude. CI/cloud must use the recorded backend.");
        }

        var config = ClaudeRuntimeConfig.LoadFromEnvironment();
        var estimatedCalls = options.IntentCount * options.MaxAttemptsPerIntent;
        Console.WriteLine("Live Claude generation (LOCAL ONLY — costs credits)");
        Console.WriteLine($"Model: {config.Model}");
        Console.WriteLine($"Temperature: {config.Temperature}");
        Console.WriteLine($"Isolation enforced: true");
        Console.WriteLine($"Estimated Anthropic calls (worst case): {estimatedCalls}");
        Console.WriteLine("API key: read from ANTHROPIC_API_KEY at call time only — never persisted.");

        var registryRoot = Path.Combine(options.ArtifactsRoot, RegistryPaths.RegistryFolderName);
        var generateRoot = Path.Combine(options.ArtifactsRoot, "generate-live");
        if (options.ResetRegistry && Directory.Exists(registryRoot))
            Directory.Delete(registryRoot, recursive: true);
        Directory.CreateDirectory(generateRoot);

        var registry = new SkillRegistry(registryRoot);
        var generator = SkillGeneratorFactory.Create(generateRoot);
        var certification = new SkillCertificationHarness(
            mutationThresholdPercent: options.MutationThreshold,
            requireMutation: options.RequireMutation,
            densitySeeds: options.DensitySeeds,
            escapeSeeds: options.EscapeSeeds);
        var loop = new SkillReuseLoop(
            registry,
            generator,
            certification,
            new SkillReuseLoopOptions
            {
                MaxAttemptsPerIntent = options.MaxAttemptsPerIntent,
                GenerationWorkRoot = generateRoot
            });

        var intents = BuildLiveIntents().Take(Math.Max(1, options.IntentCount)).ToList();
        var calls = new List<SkillLoopCallRecord>();

        foreach (var scenario in intents)
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
                result.ModelId ?? config.Model,
                result.AttemptsUsed,
                config.Temperature));

            if (result.Outcome == EnsureSkillOutcome.Generated)
            {
                var reuse = await loop.EnsureSkillAsync(scenario.Intent, "reuse-proof", ct)
                    .ConfigureAwait(false);
                calls.Add(new SkillLoopCallRecord(
                    $"{scenario.Name}-reuse-proof",
                    "reuse-proof",
                    reuse.Outcome,
                    reuse.GenerationRan,
                    reuse.CertificationRan,
                    reuse.Reused,
                    reuse.GeneratorBackend,
                    reuse.Entry?.EntryId,
                    reuse.RejectionReason,
                    registry.ListEntries().Count,
                    registry.ListEntries().Count,
                    reuse.IsolationEnforced,
                    reuse.ModelId ?? config.Model,
                    reuse.AttemptsUsed,
                    config.Temperature));
            }
        }

        var report = new SkillLoopReport(
            Version: "s3.2-claude-v1",
            GeneratorBackend: generator.BackendName,
            RunAt: DateTimeOffset.UtcNow,
            Calls: calls,
            RegistryEntryIds: registry.ListEntries().Select(e => e.EntryId).OrderBy(id => id).ToList());

        var reportPath = await SkillLoopReportWriter.WriteAsync(
            report,
            Path.Combine(options.ArtifactsRoot, "skill-loop-report.json")).ConfigureAwait(false);

        Console.WriteLine($"Wrote live report: {reportPath}");
        Console.WriteLine($"Transcripts: {generateRoot}");
        Console.WriteLine($"Generation calls: {loop.GenerationCallCount}");
        Console.WriteLine($"Certification invocations: {loop.CertificationInvocationCount}");

        foreach (var call in calls)
        {
            Console.WriteLine(
                $"  [{call.Scenario}] outcome={call.Outcome} isolation={call.IsolationEnforced} " +
                $"generationRan={call.GenerationRan} attempts={call.AttemptsUsed}");
        }

        var hasOutcome = calls.Any(c => c.Outcome is EnsureSkillOutcome.Generated or EnsureSkillOutcome.Rejected);
        var reuseProof = calls.Any(c => c.Scenario.EndsWith("-reuse-proof", StringComparison.Ordinal)
                                         && c.Outcome == EnsureSkillOutcome.Reused
                                         && !c.GenerationRan);

        return hasOutcome && (reuseProof || calls.All(c => c.Outcome == EnsureSkillOutcome.Rejected)) ? 0 : 1;
    }

    private static IReadOnlyList<LiveScenario> BuildLiveIntents() =>
    [
        new(
            "claude-csv-type-inference",
            new IntentDescriptor(
                "csv-column-type-inference",
                ["live", "claude"],
                CapabilityKey: "csv-type-inference"),
            "live-primary"),
        new(
            "claude-null-safe-inference",
            new IntentDescriptor(
                "csv-null-safe-type-inference",
                ["live", "claude"],
                CapabilityKey: "csv-null-safe-inference"),
            "live-secondary")
    ];

    private sealed record LiveScenario(string Name, IntentDescriptor Intent, string ContextLabel);
}
