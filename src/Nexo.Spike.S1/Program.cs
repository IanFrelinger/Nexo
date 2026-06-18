using Nexo.Spike.S1;
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

var jsonPath = Path.Combine(parsed.OutputDirectory, "escape-rate-report.json");
var mdPath = Path.Combine(parsed.OutputDirectory, "findings.md");

await EscapeRateReportWriter.WriteJsonAsync(report, jsonPath).ConfigureAwait(false);
await EscapeRateReportWriter.WriteFindingsMarkdownAsync(report, mdPath).ConfigureAwait(false);

Console.WriteLine($"Wrote {jsonPath}");
Console.WriteLine($"Wrote {mdPath}");
Console.WriteLine(
    $"Wrong-impl escape rate: {report.WrongImpl.EscapeRate:P1} ({report.WrongImpl.Escapes}/{report.WrongImpl.Escapes + report.WrongImpl.Caught})");
Console.WriteLine($"Weak-test dimension: {report.WeakTest.Status}");

return 0;

internal static class HarnessCli
{
    public const string HelpText = """
        Nexo Spike S1 — gate escape-rate harness

        Usage:
          dotnet run --project src/Nexo.Spike.S1 -- --seeds N --mutation-sample M --budget-minutes T [--out path]

        Options:
          --seeds N              Number of deterministic seeds (default: 8)
          --mutation-sample M    Weak-test candidates to run (0 skips mutation; default: 0)
          --budget-minutes T     Wall-clock budget for mutation dimension (default: 30)
          --out path             Output directory (default: artifacts/s1)
          --help                 Show help
        """;

    public static ParsedArgs Parse(string[] args)
    {
        var seeds = 8;
        var mutationSample = 0;
        var budgetMinutes = 30;
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
                case "--out":
                    if (!TryReadString(args, ref i, out var outPath))
                        return ParsedArgs.Error("Missing value for --out");
                    output = Path.GetFullPath(outPath);
                    break;
                default:
                    return ParsedArgs.Error($"Unknown argument: {arg}");
            }
        }

        return new ParsedArgs(false, showHelp, seeds, mutationSample, budgetMinutes, output, null);
    }

    private static bool TryReadInt(string[] args, ref int index, out int value)
    {
        value = 0;
        if (index + 1 >= args.Length || !int.TryParse(args[index + 1], out value))
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
        string OutputDirectory,
        string? ErrorMessage)
    {
        public static ParsedArgs Error(string message) =>
            new(true, false, 0, 0, 0, string.Empty, message);
    }
}
