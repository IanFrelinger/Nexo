using Nexo.Spike.S3;
using Nexo.Spike.S3.Models;
using Nexo.Spike.S3.Certification;
using Nexo.Spike.S3.Generation;
using Nexo.Spike.S3.Loop;
using Nexo.Spike.S3.Registry;
using Nexo.Spike.S3.Reporting;

if (args.Length > 0 && string.Equals(args[0], "live", StringComparison.OrdinalIgnoreCase))
{
    return await RunLiveAsync(args[1..]).ConfigureAwait(false);
}

return await RunRecordedLoopAsync(args).ConfigureAwait(false);

static async Task<int> RunRecordedLoopAsync(string[] args)
{
    var parsed = RecordedHarnessCli.Parse(args);
    if (parsed.ShowHelp)
    {
        Console.WriteLine(RecordedHarnessCli.HelpText);
        return parsed.IsError ? 2 : 0;
    }

    if (parsed.IsError)
    {
        Console.Error.WriteLine(parsed.ErrorMessage);
        Console.Error.WriteLine(RecordedHarnessCli.HelpText);
        return 2;
    }

    var registryRoot = Path.Combine(parsed.ArtifactsRoot, RegistryPaths.RegistryFolderName);
    if (parsed.ResetRegistry && Directory.Exists(registryRoot))
        Directory.Delete(registryRoot, recursive: true);

    var registry = new SkillRegistry(registryRoot);
    var generator = SkillGeneratorFactory.Create();
    var certification = new SkillCertificationHarness(
        mutationThresholdPercent: parsed.MutationThreshold,
        requireMutation: parsed.RequireMutation,
        densitySeeds: parsed.DensitySeeds,
        escapeSeeds: parsed.EscapeSeeds);
    var loop = new SkillReuseLoop(registry, generator, certification);

    var report = await SkillLoopDemonstration.RunAsync(loop, registry, generator.BackendName)
        .ConfigureAwait(false);

    var reportPath = await SkillLoopReportWriter.WriteAsync(
        report,
        Path.Combine(parsed.ArtifactsRoot, "skill-loop-report.json")).ConfigureAwait(false);

    Console.WriteLine($"Wrote skill loop report: {reportPath}");
    Console.WriteLine($"Generator backend: {report.GeneratorBackend} (recorded — offline)");
    Console.WriteLine($"Registry root: {registryRoot}");
    Console.WriteLine($"Generation calls: {loop.GenerationCallCount}");
    Console.WriteLine($"Certification invocations: {loop.CertificationInvocationCount}");

    var success = report.Calls.Count == 4
                  && report.Calls.Any(c => c.Outcome == EnsureSkillOutcome.Generated)
                  && report.Calls.Any(c => c.Outcome == EnsureSkillOutcome.Reused && c.Scenario == "reused-same-intent")
                  && report.Calls.Any(c => c.Outcome == EnsureSkillOutcome.Reused && c.Scenario == "reused-other-context")
                  && report.Calls.Any(c => c.Outcome == EnsureSkillOutcome.Rejected)
                  && loop.GenerationCallCount == 4
                  && loop.CertificationInvocationCount == 4;

    return success ? 0 : 1;
}

static async Task<int> RunLiveAsync(string[] args)
{
    var parsed = LiveHarnessCli.Parse(args);
    if (parsed.ShowHelp)
    {
        Console.WriteLine(LiveHarnessCli.HelpText);
        return parsed.IsError ? 2 : 0;
    }

    if (parsed.IsError)
    {
        Console.Error.WriteLine(parsed.ErrorMessage);
        Console.Error.WriteLine(LiveHarnessCli.HelpText);
        return 2;
    }

    try
    {
        return await LiveGenerateRunner.RunAsync(new LiveGenerateRunner.LiveGenerateOptions(
            parsed.ArtifactsRoot,
            parsed.IntentCount,
            parsed.MaxAttempts,
            parsed.ResetRegistry,
            parsed.MutationThreshold,
            parsed.RequireMutation,
            parsed.DensitySeeds,
            parsed.EscapeSeeds)).ConfigureAwait(false);
    }
    catch (InvalidOperationException ex)
    {
        Console.Error.WriteLine(ex.Message);
        return 1;
    }
}

internal static class RecordedHarnessCli
{
    public const string HelpText = """
        Nexo Spike S3 — recorded skill loop (default, offline)

        Usage:
          dotnet run --project src/Nexo.Spike.S3 -- [--out path] [--reset-registry]

        Options:
          --out path                 Artifacts root (default: artifacts/s3)
          --reset-registry           Delete registry before run
          --no-reset-registry        Keep existing registry entries
          --mutation-threshold P     Mutation score threshold percent (default: 80)
          --require-mutation         Fail certification if dotnet-stryker unavailable (default: true)
          --no-require-mutation      Allow skipping MutationGate when stryker missing
          --density-seeds N          Intent density seeds (default: 4)
          --escape-seeds N           Escape-rate seeds (default: 2)
          --help                     Show help
        """;

    public static CommonHarnessCli.ParsedArgs Parse(string[] args) => CommonHarnessCli.Parse(args, resetRegistryDefault: true);
}

internal static class LiveHarnessCli
{
    public const string HelpText = """
        Nexo Spike S3 — live Claude generation (LOCAL ONLY)

        Usage:
          NEXO_S3_GENERATOR=claude ANTHROPIC_API_KEY=... \
            dotnet run --project src/Nexo.Spike.S3 -- live [--out path]

        Options:
          --out path                 Artifacts root (default: artifacts/s3)
          --intents N                Intents to generate (default: 1, max catalog size)
          --max-attempts N           Attempt cap per intent (default: 3)
          --reset-registry           Delete registry before run (default)
          --no-reset-registry        Keep registry entries
          --mutation-threshold P     Mutation threshold percent (default: 80)
          --no-require-mutation      Skip mutation when stryker missing
          --density-seeds N          Intent density seeds (default: 4)
          --escape-seeds N           Escape-rate seeds (default: 2)
          --help                     Show help

        Never run in CI/cloud. Transcripts land in artifacts/s3/generate-live/.
        """;

    public static LiveParsedArgs Parse(string[] args)
    {
        var intents = 1;
        var maxAttempts = 3;
        var commonArgs = new List<string>();

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--intents":
                    if (!CommonHarnessCli.TryReadInt(args, ref i, out intents) || intents <= 0)
                        return LiveParsedArgs.Error("Invalid --intents value");
                    break;
                case "--max-attempts":
                    if (!CommonHarnessCli.TryReadInt(args, ref i, out maxAttempts) || maxAttempts <= 0)
                        return LiveParsedArgs.Error("Invalid --max-attempts value");
                    break;
                default:
                    commonArgs.Add(args[i]);
                    break;
            }
        }

        var common = CommonHarnessCli.Parse(commonArgs.ToArray(), resetRegistryDefault: true);
        if (common.IsError)
            return LiveParsedArgs.FromError(common.ErrorMessage!);

        return new LiveParsedArgs(
            false,
            common.ShowHelp,
            common.ArtifactsRoot,
            intents,
            maxAttempts,
            common.ResetRegistry,
            common.MutationThreshold,
            common.RequireMutation,
            common.DensitySeeds,
            common.EscapeSeeds,
            null);
    }

    internal sealed record LiveParsedArgs(
        bool IsError,
        bool ShowHelp,
        string ArtifactsRoot,
        int IntentCount,
        int MaxAttempts,
        bool ResetRegistry,
        double MutationThreshold,
        bool RequireMutation,
        int DensitySeeds,
        int EscapeSeeds,
        string? ErrorMessage)
    {
        public static LiveParsedArgs Error(string message) =>
            new(true, false, string.Empty, 1, 3, true, 80, true, 4, 2, message);

        public static LiveParsedArgs FromError(string message) => Error(message);
    }
}

internal static class CommonHarnessCli
{
    public static ParsedArgs Parse(string[] args, bool resetRegistryDefault)
    {
        var artifactsRoot = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "s3");
        var resetRegistry = resetRegistryDefault;
        var mutationThreshold = 80.0;
        var requireMutation = true;
        var densitySeeds = 4;
        var escapeSeeds = 2;
        var showHelp = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--help" or "-h":
                    showHelp = true;
                    break;
                case "--out":
                    if (!TryReadString(args, ref i, out var outPath))
                        return ParsedArgs.Error("Missing value for --out");
                    artifactsRoot = Path.GetFullPath(outPath);
                    break;
                case "--reset-registry":
                    resetRegistry = true;
                    break;
                case "--no-reset-registry":
                    resetRegistry = false;
                    break;
                case "--mutation-threshold":
                    if (!TryReadDouble(args, ref i, out mutationThreshold) || mutationThreshold <= 0)
                        return ParsedArgs.Error("Invalid --mutation-threshold value");
                    break;
                case "--require-mutation":
                    requireMutation = true;
                    break;
                case "--no-require-mutation":
                    requireMutation = false;
                    break;
                case "--density-seeds":
                    if (!TryReadInt(args, ref i, out densitySeeds) || densitySeeds <= 0)
                        return ParsedArgs.Error("Invalid --density-seeds value");
                    break;
                case "--escape-seeds":
                    if (!TryReadInt(args, ref i, out escapeSeeds) || escapeSeeds <= 0)
                        return ParsedArgs.Error("Invalid --escape-seeds value");
                    break;
                default:
                    if (args[i].StartsWith("--", StringComparison.Ordinal))
                        return ParsedArgs.Error($"Unknown argument: {args[i]}");
                    break;
            }
        }

        return new ParsedArgs(false, showHelp, artifactsRoot, resetRegistry, mutationThreshold, requireMutation, densitySeeds, escapeSeeds, null);
    }

    public static bool TryReadInt(string[] args, ref int index, out int value)
    {
        value = 0;
        if (index + 1 >= args.Length || !int.TryParse(args[index + 1], out value))
            return false;
        index++;
        return true;
    }

    private static bool TryReadDouble(string[] args, ref int index, out double value)
    {
        value = 0;
        if (index + 1 >= args.Length || !double.TryParse(args[index + 1], out value))
            return false;
        index++;
        return true;
    }

    private static bool TryReadString(string[] args, ref int index, out string value)
    {
        value = string.Empty;
        if (index + 1 >= args.Length)
            return false;
        value = args[index + 1];
        index++;
        return true;
    }

    internal sealed record ParsedArgs(
        bool IsError,
        bool ShowHelp,
        string ArtifactsRoot,
        bool ResetRegistry,
        double MutationThreshold,
        bool RequireMutation,
        int DensitySeeds,
        int EscapeSeeds,
        string? ErrorMessage)
    {
        public static ParsedArgs Error(string message) =>
            new(true, false, string.Empty, true, 80, true, 4, 2, message);
    }
}
