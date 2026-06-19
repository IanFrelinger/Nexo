using Nexo.Spike.S1;
using Nexo.Spike.S1.IntentDensity;
using Nexo.Spike.S1.Reporting;

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

var harness = new EscapeRateHarness();
var report = await harness.RunAsync(new EscapeRateHarnessOptions
{
    Seeds = parsed.Seeds,
    MutationSample = parsed.MutationSample,
    BudgetMinutes = parsed.BudgetMinutes,
    OutputDirectory = parsed.OutputDirectory
}).ConfigureAwait(false);

var densityAnalyzer = new IntentDensityAnalyzer();
var densityReport = await densityAnalyzer.AnalyzeAsync(new IntentDensityOptions
{
    Seeds = parsed.Seeds,
    CertificationThreshold = parsed.CertificationThreshold
}).ConfigureAwait(false);

var equivalence = CertificationEquivalence.Evaluate(densityReport, report);
densityReport = densityReport with
{
    Equivalence = new CertificationEquivalenceReport(
        equivalence.DensityEqualsOne,
        equivalence.EscapesEqualsZero,
        equivalence.EquivalenceHolds,
        equivalence.IntentDensity,
        equivalence.WrongImplEscapes,
        equivalence.Scope)
};

NegativeControlReport? negativeControl = null;
if (parsed.RunNegativeControl)
{
    var negativeIntent = ResolveNegativeControlIntent();
    var ncHarness = await harness.RunAsync(new EscapeRateHarnessOptions
    {
        Seeds = parsed.Seeds,
        MutationSample = 0,
        BudgetMinutes = 1,
        OutputDirectory = Path.Combine(parsed.OutputDirectory, "negative-control"),
        IntentPath = negativeIntent
    }).ConfigureAwait(false);

    var ncDensity = await densityAnalyzer.AnalyzeAsync(new IntentDensityOptions
    {
        Seeds = parsed.Seeds,
        CertificationThreshold = parsed.CertificationThreshold,
        IntentPath = negativeIntent
    }).ConfigureAwait(false);

    var certificationRevoked =
        ncDensity.IntentDensity < 1.0 - 1e-9 && ncHarness.WrongImpl.Escapes > 0;
    negativeControl = new NegativeControlReport(
        true,
        NegativeControlStripping.RemovedRelation,
        ncDensity.IntentDensity,
        ncHarness.WrongImpl.Escapes,
        certificationRevoked);

    densityReport = densityReport with { NegativeControl = negativeControl };
}

var jsonPath = Path.Combine(parsed.OutputDirectory, "escape-rate-report.json");
var densityJsonPath = Path.Combine(parsed.OutputDirectory, "intent-density-report.json");
var mdPath = Path.Combine(parsed.OutputDirectory, "findings.md");

await EscapeRateReportWriter.WriteJsonAsync(report, jsonPath).ConfigureAwait(false);
await IntentDensityReportWriter.WriteJsonAsync(densityReport, densityJsonPath).ConfigureAwait(false);

var findings = EscapeRateReportWriter.RenderFindings(report)
               + "\n"
               + IntentDensityReportWriter.RenderFindingsSection(densityReport);
await File.WriteAllTextAsync(mdPath, findings).ConfigureAwait(false);

Console.WriteLine($"Wrote {jsonPath}");
Console.WriteLine($"Wrote {densityJsonPath}");
Console.WriteLine($"Wrote {mdPath}");
Console.WriteLine($"Catalog version: {report.CatalogVersion}");
Console.WriteLine(
    $"Wrong-impl escape rate: {report.WrongImpl.EscapeRate:P1} ({report.WrongImpl.Escapes}/{report.WrongImpl.Escapes + report.WrongImpl.Caught})");
Console.WriteLine($"Distinct wrong-impl trials: {report.DistinctWrongImplTrials} ({report.TotalWrongImplCandidates} total runs)");
Console.WriteLine($"Weak-test dimension: {report.WeakTest.Status} (escape rate: {report.WeakTest.EscapeRate:P1})");
Console.WriteLine($"Intent density: {densityReport.IntentDensity:P1} — certification: {densityReport.Certification.Verdict}");
Console.WriteLine($"Equivalence (density==1.0 <=> escapes==0): {equivalence.EquivalenceHolds} — {CertificationEquivalence.ScopeNote}");
if (negativeControl is not null)
    Console.WriteLine($"Negative control: density={negativeControl.IntentDensity:P1}, escapes={negativeControl.WrongImplEscapes}, broken={negativeControl.EquivalenceBroken}");

return equivalence.EquivalenceHolds ? 0 : 1;

static string ResolveNegativeControlIntent()
{
    var candidates = new[]
    {
        Path.Combine(Directory.GetCurrentDirectory(), "src", "Nexo.Spike.S1", "Fixtures", "honest-csv-inferrer-negative-control.json"),
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "honest-csv-inferrer-negative-control.json")
    };

    foreach (var path in candidates)
    {
        if (File.Exists(path))
            return path;
    }

    throw new FileNotFoundException("honest-csv-inferrer-negative-control.json fixture not found");
}

internal static class HarnessCli
{
    public const string HelpText = """
        Nexo Spike S1 — gate escape-rate harness

        Usage:
          dotnet run --project src/Nexo.Spike.S1 -- --seeds N --mutation-sample M --budget-minutes T [--out path]

        Options:
          --seeds N              Number of deterministic seeds (default: 8)
          --mutation-sample M    Weak-test candidates to run (0 skips mutation; default: 4)
          --budget-minutes T     Wall-clock budget for mutation dimension (default: 30)
          --certification-threshold P  Intent-density certification threshold (default: 0.95)
          --negative-control     Run stripped-spec negative control (proves equivalence not vacuous)
          --out path             Output directory (default: artifacts/s1)
          --help                 Show help
        """;

    public static ParsedArgs Parse(string[] args)
    {
        var seeds = 8;
        var mutationSample = 4;
        var budgetMinutes = 30;
        var certificationThreshold = 0.95;
        var output = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "s1");
        var showHelp = false;
        var runNegativeControl = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--help" or "-h":
                    showHelp = true;
                    break;
                case "--seeds":
                    if (!TryReadInt(args, ref i, out seeds) || seeds < 0)
                        return ParsedArgs.Error("Invalid --seeds value");
                    break;
                case "--mutation-sample":
                    if (!TryReadInt(args, ref i, out mutationSample) || mutationSample < 0)
                        return ParsedArgs.Error("Invalid --mutation-sample value");
                    break;
                case "--budget-minutes":
                    if (!TryReadInt(args, ref i, out budgetMinutes) || budgetMinutes <= 0)
                        return ParsedArgs.Error("Invalid --budget-minutes value");
                    break;
                case "--certification-threshold":
                    if (!TryReadDouble(args, ref i, out certificationThreshold) || certificationThreshold <= 0 || certificationThreshold > 1)
                        return ParsedArgs.Error("Invalid --certification-threshold value");
                    break;
                case "--negative-control":
                    runNegativeControl = true;
                    break;
                case "--out":
                    if (!TryReadString(args, ref i, out var outPath))
                        return ParsedArgs.Error("Missing value for --out");
                    output = Path.GetFullPath(outPath);
                    break;
                default:
                    return ParsedArgs.Error($"Unknown argument: {arg}");
            }
        }

        return new ParsedArgs(false, showHelp, seeds, mutationSample, budgetMinutes, certificationThreshold, output, runNegativeControl, null);
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
        int Seeds,
        int MutationSample,
        int BudgetMinutes,
        double CertificationThreshold,
        string OutputDirectory,
        bool RunNegativeControl,
        string? ErrorMessage)
    {
        public static ParsedArgs Error(string message) =>
            new(true, false, 0, 0, 0, 0.95, string.Empty, false, message);
    }
}
