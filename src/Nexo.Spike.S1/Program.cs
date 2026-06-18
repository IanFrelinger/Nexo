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

var options = new EscapeRateHarnessOptions
{
    Seeds = parsed.Seeds,
    MutationSample = parsed.MutationSample,
    BudgetMinutes = parsed.BudgetMinutes,
    OutputDirectory = parsed.OutputDirectory
};

var harness = new EscapeRateHarness();
var report = await harness.RunAsync(options).ConfigureAwait(false);

var densityAnalyzer = new IntentDensityAnalyzer();
var densityReport = await densityAnalyzer.AnalyzeAsync(parsed.CertificationThreshold).ConfigureAwait(false);

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

return 0;

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
                case "--out":
                    if (!TryReadString(args, ref i, out var outPath))
                        return ParsedArgs.Error("Missing value for --out");
                    output = Path.GetFullPath(outPath);
                    break;
                default:
                    return ParsedArgs.Error($"Unknown argument: {arg}");
            }
        }

        return new ParsedArgs(false, showHelp, seeds, mutationSample, budgetMinutes, certificationThreshold, output, null);
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
        string? ErrorMessage)
    {
        public static ParsedArgs Error(string message) =>
            new(true, false, 0, 0, 0, 0.95, string.Empty, message);
    }
}
