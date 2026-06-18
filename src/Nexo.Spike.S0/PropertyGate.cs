namespace Nexo.Spike.S0;

public sealed record PropertyGateResult(
    bool Passed,
    int FailedTests,
    string RawOutput);

/// <summary>
/// Runs immutable spec-derived and metamorphic property tests at VERIFY.
/// </summary>
public sealed class PropertyGate
{
    public const string PropertyProject = "CsvColumnInferrer.Properties/CsvColumnInferrer.Properties.csproj";

    public async Task<PropertyGateResult> RunAsync(string workspaceRoot, CancellationToken ct = default)
    {
        var projectPath = Path.Combine(workspaceRoot, PropertyProject);
        if (!File.Exists(projectPath))
        {
            return new PropertyGateResult(
                false,
                0,
                "CsvColumnInferrer.Properties project missing from workspace");
        }

        var (buildCode, buildOut, buildErr, buildTimedOut) = await SpikeDotnetRunner.RunAsync(
            workspaceRoot,
            $"build \"{PropertyProject}\" -c Release",
            TimeSpan.FromMinutes(10),
            ct).ConfigureAwait(false);

        if (buildCode != 0 || buildTimedOut)
            return new PropertyGateResult(false, 0, $"{buildOut}\n{buildErr}");

        var (testCode, testOut, testErr, testTimedOut) = await SpikeDotnetRunner.RunAsync(
            workspaceRoot,
            $"test \"{PropertyProject}\" -c Release --no-build --filter Category=SpikeProperty --verbosity minimal",
            TimeSpan.FromMinutes(10),
            ct).ConfigureAwait(false);

        if (testTimedOut)
            return new PropertyGateResult(false, 0, "Property tests timed out");

        var combined = $"{testOut}\n{testErr}";
        var failed = CountFailedTests(combined);
        return new PropertyGateResult(testCode == 0, failed, combined);
    }

    private static int CountFailedTests(string output)
    {
        var match = System.Text.RegularExpressions.Regex.Match(output, @"Failed:\s+(\d+)");
        return match.Success && int.TryParse(match.Groups[1].Value, out var n) ? n : 0;
    }
}
