using Ashlar.Tests.Infrastructure.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.CLI;

/// <summary>
/// End-to-end smoke tests for Phases 5-9: self-context, compose, test parallel, mesh.
/// Uses prebuilt CLI via CliRunner with process timeout.
/// </summary>
[Collection("E2E")]
[Trait("Category", "E2E")]
public sealed class Phases59CliE2ETests : E2ETestBase
{
    public Phases59CliE2ETests() : base("ashlar-phases59-e2e") { }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task SelfContextCommand_Exits()
    {
        var (code, _, _) = await RunCliAsync("self-context --lookback 1h");
        Assert.Equal(0, code);
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task ComposeCommand_Exits()
    {
        var (code, stdout, _) = await RunCliAsync("compose --problem \"test Ashlar CLI\"");
        Assert.Equal(0, code);
        Assert.Contains("Pipeline", stdout, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task TestParallelCommand_Exits()
    {
        var (code, _, _) = await RunCliAsync("test parallel --count 1 --filter \"FullyQualifiedName~BaseFrameworkSmokeTests\"");
        Assert.True(code == 0 || code == 1);
    }

    [Fact(Timeout = TestTimeouts.Mesh)]
    public async Task MeshCommand_Discover_Exits()
    {
        var (code, _, _) = await RunCliAsync("mesh --discover");
        Assert.Equal(0, code);
    }

    [Fact(Timeout = TestTimeouts.Mesh)]
    public async Task MeshCommand_Advertise_Exits()
    {
        var (code, _, _) = await RunCliAsync("mesh --advertise");
        Assert.Equal(0, code);
    }

    [Fact(Timeout = TestTimeouts.Mesh)]
    public async Task MeshCommand_Sync_Exits()
    {
        var sharedPath = Path.Combine(TempDir, "shared");
        Directory.CreateDirectory(sharedPath);
        var env = new Dictionary<string, string?> { ["ASHLAR_SHARED_ADAPTATIONS_PATH"] = sharedPath };
        var (code, _, _) = await RunCliAsync("mesh sync", env);
        Assert.Equal(0, code);
    }

    [Fact(Timeout = TestTimeouts.Fast)]
    public async Task MeshCommand_Capabilities_Exits()
    {
        var (code, stdout, _) = await RunCliAsync("mesh capabilities");
        Assert.Equal(0, code);
        Assert.Contains("Supported formats", stdout, StringComparison.OrdinalIgnoreCase);
    }
}
