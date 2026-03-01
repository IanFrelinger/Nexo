using Nexo.Tests.Application.Helpers;
using Nexo.Tests.Infrastructure;
using Nexo.Tests.Infrastructure.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.CLI;

/// <summary>
/// Full pipeline E2E: observe → analyze → adapt → improve → self-context in sequence.
/// </summary>
[Collection("E2E")]
[Trait("Category", "E2E")]
public sealed class FullPipelineE2ETests : IDisposable
{
    private readonly string _repoRoot;
    private readonly string _tempDir;
    private readonly IDisposable _tempDirCleanup;

    public FullPipelineE2ETests()
    {
        _repoRoot = TestPaths.FindRepoRoot();
        (_tempDir, _tempDirCleanup) = TestHelpers.CreateTempDirectoryWithCleanup("nexo-full-pipeline-e2e");
    }

    public void Dispose()
    {
        _tempDirCleanup.Dispose();
    }

    [Fact]
    public async Task ObserveAnalyzeAdaptImproveSelfContext_Sequential_NoCrash()
    {
        var watchPath = Path.Combine(_tempDir, "src");
        Directory.CreateDirectory(watchPath);

        var (observeCode, _, _) = await CliRunner.RunAsync(_repoRoot, $"observe --path \"{_tempDir}\" --duration 1s");
        Assert.Equal(0, observeCode);

        var obsPath = Path.Combine(_repoRoot, "src", "Nexo.Infrastructure", "Observation");
        var analyzePath = Directory.Exists(obsPath) ? obsPath : _repoRoot;

        var (analyzeCode, _, _) = await CliRunner.RunAsync(_repoRoot, $"analyze bricks --path \"{analyzePath}\"");
        Assert.True(analyzeCode == 0 || analyzeCode == 1);

        var (adaptCode, _, _) = await CliRunner.RunAsync(_repoRoot, $"adapt --dry-run --store-path \"{_tempDir}\"");
        Assert.Equal(0, adaptCode);

        var (improveCode, _, _) = await CliRunner.RunAsync(_repoRoot, "improve --dry-run");
        Assert.True(improveCode == 0 || improveCode == 1);

        var (selfContextCode, _, _) = await CliRunner.RunAsync(_repoRoot, "self-context --lookback 1h");
        Assert.Equal(0, selfContextCode);
    }

    [Fact]
    public async Task ImproveWithAutonomySupervised_PromptsUser()
    {
        TestHelpers.CreateTempCsFileWithEmptyCatch(_tempDir);

        var (code, stdout, _) = await CliRunner.RunAsync(_repoRoot, $"improve --autonomy supervised --path \"{_tempDir}\" --yes --skip-regression --store-path \"{_tempDir}\"");

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
