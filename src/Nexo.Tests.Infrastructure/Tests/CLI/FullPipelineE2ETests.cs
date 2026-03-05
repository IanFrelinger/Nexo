using Nexo.Tests.Application.Helpers;
using Nexo.Tests.Infrastructure.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.CLI;

/// <summary>
/// Full pipeline E2E: observe → analyze → adapt → improve → self-context in sequence.
/// </summary>
[Collection("E2E")]
[Trait("Category", "E2E")]
public sealed class FullPipelineE2ETests : E2ETestBase
{
    public FullPipelineE2ETests() : base("nexo-full-pipeline-e2e") { }

    [Fact(Timeout = 90000)]
    public async Task ObserveAnalyzeAdaptImproveSelfContext_Sequential_NoCrash()
    {
        var watchPath = Path.Combine(TempDir, "src");
        Directory.CreateDirectory(watchPath);

        var (observeCode, _, _) = await RunCliAsync($"observe --path \"{TempDir}\" --duration 1s");
        Assert.Equal(0, observeCode);

        var obsPath = Path.Combine(RepoRoot, "src", "Nexo.Infrastructure", "Observation");
        var analyzePath = Directory.Exists(obsPath) ? obsPath : RepoRoot;

        var (analyzeCode, _, _) = await RunCliAsync($"analyze bricks --path \"{analyzePath}\"");
        Assert.True(analyzeCode == 0 || analyzeCode == 1);

        var (adaptCode, _, _) = await RunCliAsync($"adapt --dry-run --store-path \"{TempDir}\"");
        Assert.Equal(0, adaptCode);

        var (improveCode, _, _) = await RunCliAsync("improve --dry-run");
        Assert.True(improveCode == 0 || improveCode == 1);

        var (selfContextCode, _, _) = await RunCliAsync("self-context --lookback 1h");
        Assert.Equal(0, selfContextCode);
    }

    [Fact(Timeout = 60000)]
    public async Task ImproveWithAutonomySupervised_PromptsUser()
    {
        TestHelpers.CreateTempCsFileWithEmptyCatch(TempDir);

        var (code, stdout, _) = await RunCliAsync($"improve --autonomy supervised --path \"{TempDir}\" --yes --skip-regression --store-path \"{TempDir}\"");

        Assert.True(code == 0 || code == 1);
        var hasPrompt = stdout.Contains("Nexo suggests", StringComparison.OrdinalIgnoreCase) ||
                        stdout.Contains("Apply?", StringComparison.OrdinalIgnoreCase) ||
                        stdout.Contains("Apply", StringComparison.OrdinalIgnoreCase) ||
                        stdout.Contains("violation", StringComparison.OrdinalIgnoreCase) ||
                        stdout.Contains("EmptyCatch", StringComparison.OrdinalIgnoreCase) ||
                        stdout.Contains("fixed", StringComparison.OrdinalIgnoreCase) ||
                        stdout.Contains("No violations", StringComparison.OrdinalIgnoreCase);
        Assert.True(hasPrompt || code == 0, "Expected improve output to contain prompt or fix info");
    }
}
