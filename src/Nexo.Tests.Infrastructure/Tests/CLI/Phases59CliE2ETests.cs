using Nexo.Tests.Application.Helpers;
using Nexo.Tests.Infrastructure;
using Nexo.Tests.Infrastructure.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.CLI;

/// <summary>
/// End-to-end smoke tests for Phases 5-9: self-context, compose, test parallel, mesh.
/// Uses prebuilt CLI via CliRunner.
/// </summary>
[Collection("E2E")]
[Trait("Category", "E2E")]
public sealed class Phases59CliE2ETests : IDisposable
{
    private readonly string _repoRoot;
    private readonly IDisposable _tempDirCleanup;

    public Phases59CliE2ETests()
    {
        _repoRoot = TestPaths.FindRepoRoot();
        (_, _tempDirCleanup) = TestHelpers.CreateTempDirectoryWithCleanup("nexo-phases59-e2e");
    }

    public void Dispose()
    {
        _tempDirCleanup.Dispose();
    }

    [Fact]
    public async Task SelfContextCommand_Exits()
    {
        var (code, _, _) = await CliRunner.RunAsync(_repoRoot, "self-context --lookback 1h");
        Assert.Equal(0, code);
    }

    [Fact]
    public async Task ComposeCommand_Exits()
    {
        var (code, stdout, _) = await CliRunner.RunAsync(_repoRoot, "compose --problem \"test Nexo CLI\"");
        Assert.Equal(0, code);
        Assert.Contains("Pipeline", stdout, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TestParallelCommand_Exits()
    {
        var (code, _, _) = await CliRunner.RunAsync(_repoRoot, "test parallel --count 1");
        Assert.True(code == 0 || code == 1);
    }

    [Fact]
    public async Task MeshCommand_Discover_Exits()
    {
        var (code, _, _) = await CliRunner.RunAsync(_repoRoot, "mesh --discover");
        Assert.Equal(0, code);
    }

    [Fact]
    public async Task MeshCommand_Advertise_Exits()
    {
        var (code, _, _) = await CliRunner.RunAsync(_repoRoot, "mesh --advertise");
        Assert.Equal(0, code);
    }
}
