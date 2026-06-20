using Nexo.Spike.S3.Models;
using Nexo.Spike.S3.Certification;
using Nexo.Spike.S3.Generation;
using Nexo.Spike.S3.Loop;
using Nexo.Spike.S3.Registry;
using Nexo.Spike.S3.Reporting;

var parsed = HarnessCli.Parse(args);
if (parsed.ShowHelp)
{
    Console.WriteLine(HarnessCli.HelpText);
    return parsed.IsError ? 2 : 0;
}

if (parsed.IsError)
{
    Console.Error.WriteLine(parsed.ErrorMessage);
    Console.Error.WriteLine(HarnessCli.HelpText);
    return 2;
}

var registryRoot = Path.Combine(parsed.ArtifactsRoot, RegistryPaths.RegistryFolderName);
if (parsed.ResetRegistry && Directory.Exists(registryRoot))
{
    Directory.Delete(registryRoot, recursive: true);
}

var registry = new SkillRegistry(registryRoot);
var generator = new ScriptedStandInSkillGenerator();
var certification = new SkillCertificationHarness(
    mutationThresholdPercent: parsed.MutationThreshold,
    requireMutation: parsed.RequireMutation,
    densitySeeds: parsed.DensitySeeds,
    escapeSeeds: parsed.EscapeSeeds);
var loop = new SkillReuseLoop(registry, generator, certification);

var report = await SkillLoopDemonstration.RunAsync(
    loop,
    registry,
    ScriptedStandInSkillGenerator.BackendLabel).ConfigureAwait(false);

var reportPath = await SkillLoopReportWriter.WriteAsync(
    report,
    Path.Combine(parsed.ArtifactsRoot, "skill-loop-report.json")).ConfigureAwait(false);

Console.WriteLine($"Wrote skill loop report: {reportPath}");
Console.WriteLine($"Generator backend: {report.GeneratorBackend} (StandIn — not a model)");
Console.WriteLine($"Registry root: {registryRoot}");
Console.WriteLine($"Registry entries: {string.Join(", ", report.RegistryEntryIds)}");
Console.WriteLine($"Generation count: {loop.GenerationCount}");
Console.WriteLine($"Certification invocations: {loop.CertificationInvocationCount}");

foreach (var call in report.Calls)
{
    Console.WriteLine(
        $"  [{call.Scenario}] outcome={call.Outcome} reused={call.Reused} " +
        $"generationRan={call.GenerationRan} certificationRan={call.CertificationRan} " +
        $"registry={call.RegistryEntryCountBefore}->{call.RegistryEntryCountAfter}");
}

var success = report.Calls.Count == 4
              && report.Calls.Any(c => c.Outcome == EnsureSkillOutcome.Generated)
              && report.Calls.Any(c => c.Outcome == EnsureSkillOutcome.Reused && c.Scenario == "reused-same-intent")
              && report.Calls.Any(c => c.Outcome == EnsureSkillOutcome.Reused && c.Scenario == "reused-other-context")
              && report.Calls.Any(c => c.Outcome == EnsureSkillOutcome.Rejected)
              && loop.GenerationCount == 2;

return success ? 0 : 1;

internal static class HarnessCli
{
    public const string HelpText = """
        Nexo Spike S3 — skill registry + capability lookup + reuse loop (stand-in generation)

        Usage:
          dotnet run --project src/Nexo.Spike.S3 -- [--out path] [--reset-registry]

        Options:
          --out path                 Artifacts root (default: artifacts/s3)
          --reset-registry           Delete registry before run (demo default: on)
          --no-reset-registry        Keep existing registry entries
          --mutation-threshold P     Mutation score threshold percent (default: 80)
          --require-mutation         Fail certification if dotnet-stryker unavailable (default: true)
          --no-require-mutation      Allow skipping MutationGate when stryker missing
          --density-seeds N          Intent density seeds (default: 4)
          --escape-seeds N           Escape-rate seeds (default: 2)
          --help                     Show help

        Stand-in generation only — offline, deterministic, labeled scripted-standin.
        """;

    public static ParsedArgs Parse(string[] args)
    {
        var artifactsRoot = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "s3");
        var resetRegistry = true;
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
                    return ParsedArgs.Error($"Unknown argument: {args[i]}");
            }
        }

        return new ParsedArgs(false, showHelp, artifactsRoot, resetRegistry, mutationThreshold, requireMutation, densitySeeds, escapeSeeds, null);
    }

    private static bool TryReadInt(string[] args, ref int index, out int value)
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
