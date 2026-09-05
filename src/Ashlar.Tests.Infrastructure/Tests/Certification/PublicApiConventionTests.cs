using FluentAssertions;
using Ashlar.Core.Application.Paths;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// PublicAPI baselines must declare the surface, not suppress the analyzer.
/// Five <c>Ashlar.Brick.Contracts</c> authoring-port files used
/// <c>#pragma warning disable RS0016</c>, so an undeclared addition compiled
/// clean. The symbols now live in <c>PublicAPI.Unshipped.txt</c>.
/// </summary>
[Trait("Category", "Certification")]
public sealed class PublicApiConventionTests
{
    [Fact]
    public void BrickContractsPorts_DoNotSuppressRs0016()
    {
        var root = RepoPathResolver.FindRepoRoot();
        var ports = Path.Combine(root, "src", "Ashlar.Brick.Contracts", "Authoring", "Ports");
        Directory.Exists(ports).Should().BeTrue(ports);

        var offenders = Directory.EnumerateFiles(ports, "*.cs")
            .Where(path => File.ReadAllText(path).Contains("#pragma warning disable RS0016", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        offenders.Should().BeEmpty(
            "RS0016 suppressions hide undeclared public surface. Add the symbol to "
            + "PublicAPI.Unshipped.txt instead. Offenders: {0}",
            string.Join(", ", offenders));
    }

    [Fact]
    public void BrickContractsUnshipped_DeclaresAuthoringPorts()
    {
        var root = RepoPathResolver.FindRepoRoot();
        var unshipped = File.ReadAllLines(
            Path.Combine(root, "src", "Ashlar.Brick.Contracts", "PublicAPI.Unshipped.txt"));
        var names = unshipped
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .ToHashSet(StringComparer.Ordinal);

        names.Should().Contain("Ashlar.Core.Domain.Bricks.Ports.AgentProfile");
        names.Should().Contain("Ashlar.Core.Domain.Bricks.Ports.IAcceptanceEvaluator");
        names.Should().Contain("Ashlar.Core.Domain.Bricks.Ports.IArtifactDrafter");
        names.Should().Contain("Ashlar.Core.Domain.Bricks.Ports.BrickConstraintManifest");
        names.Should().Contain("Ashlar.Core.Domain.Bricks.Ports.GenerationRequest");
        names.Count.Should().BeGreaterThanOrEqualTo(147);
    }
}
