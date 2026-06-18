using System.Text.Json;
using Nexo.Spike.S0;
using Nexo.Spike.S0.Agents;
using Nexo.Spike.S0.Models;

var repoRoot = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
var resultsDir = Path.Combine(repoRoot, "samples", "spike-s0", "results");
Directory.CreateDirectory(resultsDir);

var fixturesRoot = Path.Combine(repoRoot, "samples", "spike-s0", "fixtures");
var intentsRoot = Path.Combine(repoRoot, "samples", "spike-s0", "intents");
var skipMutation = args.Contains("--skip-mutation");

var scenarios = new[]
{
    new Scenario(
        "honest-baseline",
        Path.Combine(intentsRoot, "honest-csv-inferrer.json"),
        Red: "red/honest-tests.cs",
        Green: "green/honest-impl.cs",
        ExpectOutcome: SpikeRunOutcome.Success,
        ExpectGuard: GuardKind.None),
    new Scenario(
        "adversarial-tautology-bait",
        Path.Combine(intentsRoot, "adversarial-tautology-bait.json"),
        Red: "red/tautology-tests.cs",
        Green: null,
        ExpectOutcome: SpikeRunOutcome.Rejected,
        ExpectGuard: GuardKind.Tautology),
    new Scenario(
        "adversarial-off-by-one",
        Path.Combine(intentsRoot, "adversarial-off-by-one.json"),
        Red: "red/hollow-off-by-one-tests.cs",
        Green: "green/wrong-off-by-one-impl.cs",
        ExpectOutcome: SpikeRunOutcome.Rejected,
        ExpectGuard: GuardKind.Mutation),
    new Scenario(
        "adversarial-silent-default",
        Path.Combine(intentsRoot, "adversarial-silent-default.json"),
        Red: "red/hollow-silent-default-tests.cs",
        Green: "green/wrong-silent-default-impl.cs",
        ExpectOutcome: SpikeRunOutcome.Rejected,
        ExpectGuard: GuardKind.Mutation),
    new Scenario(
        "adversarial-order-dependence",
        Path.Combine(intentsRoot, "adversarial-order-dependence.json"),
        Red: "red/hollow-order-tests.cs",
        Green: "green/wrong-order-impl.cs",
        ExpectOutcome: SpikeRunOutcome.Rejected,
        ExpectGuard: GuardKind.Mutation)
};

// tautology fixture lives in test fixtures — copy path
var tautologySource = Path.Combine(repoRoot, "src", "Nexo.Tests.Spike.S0", "Fixtures", "red", "tautology-tests.cs");
var honestRedSource = Path.Combine(repoRoot, "src", "Nexo.Tests.Spike.S0", "Fixtures", "red", "honest-tests.cs");
var honestGreenSource = Path.Combine(repoRoot, "src", "Nexo.Tests.Spike.S0", "Fixtures", "green", "honest-impl.cs");

var records = new List<object>();

foreach (var scenario in scenarios)
{
    var workspace = Path.Combine(Path.GetTempPath(), "nexo-s0-exp-" + scenario.Name);
    if (Directory.Exists(workspace))
        Directory.Delete(workspace, recursive: true);

    var redSource = scenario.Red switch
    {
        "red/tautology-tests.cs" => tautologySource,
        "red/honest-tests.cs" => honestRedSource,
        _ => Path.Combine(fixturesRoot, scenario.Red)
    };
    var greenSource = scenario.Green switch
    {
        "green/honest-impl.cs" => honestGreenSource,
        null => null!,
        _ => Path.Combine(fixturesRoot, scenario.Green)
    };

    var redScripts = new Dictionary<SpikeStage, IReadOnlyList<(string, string)>>
    {
        [SpikeStage.Red] = [(redSource, "CsvColumnInferrer.Tests/ColumnTypeInferrerTests.cs")]
    };

    var greenScripts = greenSource is null
        ? new Dictionary<SpikeStage, IReadOnlyList<(string, string)>>()
        : new Dictionary<SpikeStage, IReadOnlyList<(string, string)>>
        {
            [SpikeStage.Green] = [(greenSource, "CsvColumnInferrer/ColumnTypeInferrer.cs")]
        };

    var runner = new TddLoopRunner(
        new ScriptedStageAgent("test-author", redScripts),
        new ScriptedStageAgent("implementer", greenScripts),
        new MutationGate());

    var log = await runner.RunAsync(new SpikeLoopOptions
    {
        IntentPath = scenario.IntentPath,
        WorkspaceRoot = workspace,
        SkipMutation = skipMutation,
        MutationThresholdPercent = 80,
        MaxRedGreenBounces = 0,
        RunId = scenario.Name
    });

    var gateDetected = log.RejectingGuard == scenario.ExpectGuard
                       || (scenario.ExpectGuard == GuardKind.Mutation && log.RejectingGuard == GuardKind.Mutation)
                       || (scenario.ExpectGuard == GuardKind.Tautology && log.RejectingGuard == GuardKind.Tautology);

    var record = new
    {
        scenario = scenario.Name,
        expectedOutcome = scenario.ExpectOutcome.ToString(),
        actualOutcome = log.Outcome.ToString(),
        expectedGuard = scenario.ExpectGuard.ToString(),
        actualGuard = log.RejectingGuard.ToString(),
        rejectedAtStage = log.RejectedAtStage?.ToString(),
        redGreenBounces = log.RedGreenBounces,
        mutationScore = log.VerifyPasses.LastOrDefault()?.MutationScore,
        survivingMutants = log.VerifyPasses.LastOrDefault()?.SurvivingMutants.Count ?? 0,
        gateDetectedAdversarial = scenario.Name.StartsWith("adversarial", StringComparison.Ordinal)
            ? gateDetected
            : (bool?)null,
        outcomeMatches = log.Outcome == scenario.ExpectOutcome
    };
    records.Add(record);

    var logCopy = Path.Combine(resultsDir, $"{scenario.Name}-run-log.json");
    await File.WriteAllTextAsync(logCopy, JsonSerializer.Serialize(log, new JsonSerializerOptions { WriteIndented = true }));

    Console.WriteLine($"[{scenario.Name}] outcome={log.Outcome} guard={log.RejectingGuard} stage={log.RejectedAtStage} mutation={log.VerifyPasses.LastOrDefault()?.MutationScore:F1}%");
}

var summaryPath = Path.Combine(resultsDir, "experiment-summary.json");
await File.WriteAllTextAsync(summaryPath, JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Summary written to {summaryPath}");

internal sealed record Scenario(
    string Name,
    string IntentPath,
    string Red,
    string? Green,
    SpikeRunOutcome ExpectOutcome,
    GuardKind ExpectGuard);
