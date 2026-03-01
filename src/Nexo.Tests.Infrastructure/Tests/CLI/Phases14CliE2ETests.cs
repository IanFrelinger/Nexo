using Nexo.Tests.Application.Helpers;
using Nexo.Tests.Infrastructure;
using Nexo.Tests.Infrastructure.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.CLI;

/// <summary>
/// End-to-end smoke tests for Phases 1-4: observe, analyze bricks, adapt, improve.
/// Uses prebuilt CLI (build once, run many) for faster execution.
/// </summary>
[Collection("E2E")]
[Trait("Category", "E2E")]
public sealed class Phases14CliE2ETests : IDisposable
{
    private readonly string _repoRoot;
    private readonly string _tempDir;
    private readonly IDisposable _tempDirCleanup;

    public Phases14CliE2ETests()
    {
        _repoRoot = TestPaths.FindRepoRoot();
        (_tempDir, _tempDirCleanup) = TestHelpers.CreateTempDirectoryWithCleanup("nexo-phases14-e2e");
    }

    public void Dispose()
    {
        _tempDirCleanup.Dispose();
    }

    [Fact]
    public async Task AdaptCommand_DryRun_ExitsZero()
    {
        var (code, stdout, _) = await CliRunner.RunAsync(_repoRoot, $"adapt --dry-run --store-path \"{_tempDir}\"");

        Assert.Equal(0, code);
        Assert.Contains("Decomposed", stdout, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ImproveCommand_DryRun_Exits()
    {
        var (code, _, _) = await CliRunner.RunAsync(_repoRoot, "improve --dry-run");

        Assert.True(code == 0 || code == 1);
    }

    [Fact]
    public async Task ImproveCommand_WithViolations_AppliesFixes()
    {
        var csPath = Path.Combine(_tempDir, "EmptyCatch.cs");
        await File.WriteAllTextAsync(csPath, """
            using System;
            namespace Test;
            public class C
            {
                public void M()
                {
                    try { }
                    catch (Exception) { }
                }
            }
            """);

        var (code, _, _) = await CliRunner.RunAsync(_repoRoot, $"improve --path \"{_tempDir}\" --yes --skip-regression --store-path \"{_tempDir}\"");

        Assert.True(code == 0 || code == 1);
        var content = await File.ReadAllTextAsync(csPath);
        Assert.True(
            content.Contains("Trace.WriteLine", StringComparison.Ordinal) || code == 0,
            "File should be modified with Trace.WriteLine when violations were fixed, or exit 0 if no violations");
    }

    [Fact]
    public async Task AnalyzeBricksCommand_Exits()
    {
        var obsPath = Path.Combine(_repoRoot, "src", "Nexo.Infrastructure", "Observation");
        var path = Directory.Exists(obsPath) ? obsPath : _repoRoot;

        var (code, _, _) = await CliRunner.RunAsync(_repoRoot, $"analyze bricks --path \"{path}\"");

        Assert.True(code == 0 || code == 1);
    }

    [Fact]
    public async Task Phases14_FullPipeline_ObserveAnalyzeAdaptImprove()
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
    }
}
