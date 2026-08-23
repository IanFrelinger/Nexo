using FluentAssertions;
using Ashlar.Tests.Infrastructure.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.VirtualProduction;

/// <summary>
/// The shipped hosts declare exactly one target framework, so the commands the docs print can be run
/// as printed.
/// </summary>
/// <remarks>
/// `Ashlar.API` multi-targeted for a while, purely so the `net8.0` leg of this test project could link
/// it. The visible cost was that `dotnet run --project application/src/Ashlar.API` refused to start —
/// "Your project targets multiple frameworks" — which is the hero command of `docs/TesterQuickstart.md`
/// and the first thing a tester runs (#350). Everything downstream of it was unreachable.
///
/// Documentation alone cannot hold this: the pages now pass `-f net10.0`, which keeps working whether
/// the host multi-targets or not, so re-adding a framework would break nobody's build and nobody's
/// docs check — it would only quietly restore the trap. This asserts the shape instead.
/// </remarks>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "ProdStyle")]
public sealed class ShippedHostFrameworkProdStyleTests
{
    [Theory(Timeout = TestTimeouts.HostTouching)]
    [InlineData("application/src/Ashlar.API/Ashlar.API.csproj")]
    [InlineData("application/src/Ashlar.CLI/Ashlar.CLI.csproj")]
    public async Task Shipped_hosts_declare_exactly_one_target_framework(string relativeCsproj)
    {
        var csproj = Path.Combine(TestPaths.FindRepoRoot(), relativeCsproj.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(csproj).Should().BeTrue($"{relativeCsproj} is a shipped host and must exist");

        var xml = await File.ReadAllTextAsync(csproj);

        xml.Should().NotContain(
            "<TargetFrameworks>",
            "a shipped host that multi-targets cannot be started by the documented `dotnet run --project …` "
            + "command; that is exactly how the tester quickstart's hero command broke. If a second "
            + "framework is genuinely needed, update the docs in the same change and delete this test.");

        xml.Should().Contain(
            "<TargetFramework>",
            "the host must state the single framework it ships on");
    }
}
