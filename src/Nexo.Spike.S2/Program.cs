using Nexo.Spike.S2;
using Nexo.Spike.S2.Adversary;
using Nexo.Spike.S2.Adversary.Llm;
using Nexo.Spike.S2.Reporting;

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

var adversaryMode = AdaptiveAdversaryFactory.ResolveMode();
var runStartedAt = DateTimeOffset.UtcNow;
var runDirectory = AdaptiveEscapeReportPublisher.PlanRunDirectory(
    parsed.ArtifactsRoot,
    adversaryMode,
    runStartedAt);

if (string.Equals(adversaryMode, AdaptiveAdversaryFactory.LlmMode, StringComparison.OrdinalIgnoreCase))
{
    try
    {
        LlmRunPreflight.EnsureProviderConfigured();
    }
    catch (InvalidOperationException ex)
    {
        var stub = await AdaptiveEscapeReportPublisher.PublishStubLlmFailureAsync(
            parsed.ArtifactsRoot,
            ex.Message,
            Environment.GetEnvironmentVariable("NEXO_S2_LLM_MODEL")).ConfigureAwait(false);

        Console.Error.WriteLine("LLM adversary preflight failed (stub/unconfigured provider).");
        Console.Error.WriteLine(ex.Message);
        Console.Error.WriteLine($"Invalid run marker: {Path.Combine(stub.RunDirectory, AdaptiveEscapeReportPublisher.InvalidMarkerFileName)}");
        return 1;
    }
}

AdaptiveEscapeReport report;
try
{
    var harness = new AdaptiveEscapeHarness();
    report = await harness.RunAsync(new AdaptiveEscapeHarnessOptions
    {
        Intents = parsed.Intents,
        AttemptsPerIntent = parsed.Attempts,
        OutputDirectory = runDirectory,
        IntentPath = parsed.IntentPath,
        ReferenceOraclePath = parsed.ReferenceOraclePath,
        MutationThresholdPercent = parsed.MutationThreshold,
        RequireMutation = parsed.RequireMutation
    }).ConfigureAwait(false);
}
catch (Exception ex) when (string.Equals(adversaryMode, AdaptiveAdversaryFactory.LlmMode, StringComparison.OrdinalIgnoreCase)
                           && LlmRunPreflight.IsProviderMissingError(ex))
{
    var stub = await AdaptiveEscapeReportPublisher.PublishStubLlmFailureAsync(
        parsed.ArtifactsRoot,
        ex.Message,
        Environment.GetEnvironmentVariable("NEXO_S2_LLM_MODEL")).ConfigureAwait(false);

    Console.Error.WriteLine("LLM adversary run failed before completion (stub/unconfigured provider).");
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine($"Invalid run marker: {Path.Combine(stub.RunDirectory, AdaptiveEscapeReportPublisher.InvalidMarkerFileName)}");
    return 1;
}

var published = await AdaptiveEscapeReportPublisher.PublishRunAsync(
    report,
    parsed.ArtifactsRoot,
    runDirectory,
    attemptCanonicalPromotion: true).ConfigureAwait(false);

Console.WriteLine($"Wrote run report: {Path.Combine(published.RunDirectory, AdaptiveEscapeReportPublisher.RunReportFileName)}");
Console.WriteLine($"Wrote run findings: {Path.Combine(published.RunDirectory, AdaptiveEscapeReportPublisher.RunFindingsFileName)}");
if (published.PromotedToCanonical)
{
    Console.WriteLine($"Promoted canonical: {published.CanonicalJsonPath}");
    Console.WriteLine($"Promoted canonical: {published.CanonicalFindingsPath}");
}
else if (!report.NonVacuityProven)
{
    Console.WriteLine(
        $"Canonical report NOT updated (status={published.Status}). " +
        $"See {Path.Combine(published.RunDirectory, AdaptiveEscapeReportPublisher.InvalidMarkerFileName)}");
}

Console.WriteLine($"Adversary backend: {report.AdversaryBackend}");
Console.WriteLine($"True-escape rate: {report.TrueEscapeRate:P1} ({report.TrueEscapeCount}/{report.Attempts.Count})");
Console.WriteLine($"Benign-pass rate: {report.BenignPassRate:P1} ({report.BenignPassCount}/{report.Attempts.Count})");
Console.WriteLine($"Rejection rate: {report.RejectionRate:P1} ({report.RejectedCount}/{report.Attempts.Count})");
Console.WriteLine($"Non-vacuity proven: {report.NonVacuityProven}");
Console.WriteLine(report.ScopeNote);

return report.NonVacuityProven || parsed.AllowIncompleteNonVacuity ? 0 : 1;

internal static class HarnessCli
{
    public const string HelpText = """
        Nexo Spike S2 — adaptive adversary escape rate vs independent oracle

        Usage:
          dotnet run --project src/Nexo.Spike.S2 -- --intents K --attempts N [--out path]

        Options:
          --intents K              Number of intent runs (default: 1)
          --attempts N             Attempts per intent (default: 3 for mock sequence)
          --mutation-threshold P   Mutation score threshold percent (default: 80)
          --require-mutation       Fail if dotnet-stryker unavailable (default: true)
          --no-require-mutation    Allow skipping MutationGate when stryker missing
          --intent path            Brick intent JSON (default: S1 honest fixture)
          --oracle path            Reference oracle JSON (default: artifacts/s2/reference-oracle.json)
          --out path               Artifacts root directory (default: artifacts/s2)
          --allow-incomplete-non-vacuity  Exit 0 even if non-vacuity not proven (local dev)
          --help                   Show help

        Run outputs land in artifacts/s2/runs/<backend>-<version>/.
        Canonical artifacts/s2/adaptive-escape-report.json is updated ONLY for valid mock runs.

        Local LLM run (never in CI):
          export NEXO_S2_ADVERSARY=llm
          export OPENAI_API_KEY=...
          export NEXO_S2_LLM_MODEL=gpt-4o
        """;

    public static ParsedArgs Parse(string[] args)
    {
        var intents = 1;
        var attempts = 3;
        var mutationThreshold = 80.0;
        var requireMutation = true;
        var allowIncomplete = false;
        var artifactsRoot = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "s2");
        var intent = ResolveDefaultIntent();
        var oracle = ResolveDefaultOracle();
        var showHelp = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--help" or "-h":
                    showHelp = true;
                    break;
                case "--intents":
                    if (!TryReadInt(args, ref i, out intents) || intents <= 0)
                        return ParsedArgs.Error("Invalid --intents value");
                    break;
                case "--attempts":
                    if (!TryReadInt(args, ref i, out attempts) || attempts <= 0)
                        return ParsedArgs.Error("Invalid --attempts value");
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
                case "--allow-incomplete-non-vacuity":
                    allowIncomplete = true;
                    break;
                case "--out":
                    if (!TryReadString(args, ref i, out var outPath))
                        return ParsedArgs.Error("Missing value for --out");
                    artifactsRoot = Path.GetFullPath(outPath);
                    break;
                case "--intent":
                    if (!TryReadString(args, ref i, out var intentPath))
                        return ParsedArgs.Error("Missing value for --intent");
                    intent = Path.GetFullPath(intentPath);
                    break;
                case "--oracle":
                    if (!TryReadString(args, ref i, out var oraclePath))
                        return ParsedArgs.Error("Missing value for --oracle");
                    oracle = Path.GetFullPath(oraclePath);
                    break;
                default:
                    return ParsedArgs.Error($"Unknown argument: {arg}");
            }
        }

        return new ParsedArgs(false, showHelp, intents, attempts, mutationThreshold, requireMutation, allowIncomplete, artifactsRoot, intent, oracle, null);
    }

    private static string ResolveDefaultIntent()
    {
        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "src", "Nexo.Spike.S1", "Fixtures", "honest-csv-inferrer.json"),
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "honest-csv-inferrer.json"),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "samples", "spike-s0", "intents", "honest-csv-inferrer.json"))
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
                return path;
        }

        throw new FileNotFoundException("honest-csv-inferrer.json fixture not found");
    }

    private static string ResolveDefaultOracle()
    {
        var candidate = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "s2", "reference-oracle.json");
        if (File.Exists(candidate))
            return candidate;

        var found = AdversaryAccessPolicy.FindRepoFile("artifacts/s2/reference-oracle.json");
        return found ?? candidate;
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
        int Intents,
        int Attempts,
        double MutationThreshold,
        bool RequireMutation,
        bool AllowIncompleteNonVacuity,
        string ArtifactsRoot,
        string IntentPath,
        string ReferenceOraclePath,
        string? ErrorMessage)
    {
        public static ParsedArgs Error(string message) =>
            new(true, false, 0, 0, 80, true, false, string.Empty, string.Empty, string.Empty, message);
    }
}
