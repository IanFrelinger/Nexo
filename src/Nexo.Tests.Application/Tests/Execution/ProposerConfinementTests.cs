using FluentAssertions;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Infrastructure.Execution.Sandbox;
using Xunit;

namespace Nexo.Tests.Application.Tests.Execution;

/// <summary>
/// Proposer confinement (extension spec Part B): ONE declaration derives both the tool
/// write-allowlist and the sandbox bind mounts, invalid or overlapping declarations are
/// refused loudly, and the network mode a backend cannot realize is refused fail-closed
/// rather than degraded.
/// </summary>
public sealed class ProposerConfinementTests
{
    // --- single source, two derivations ----------------------------------------------------

    [Fact]
    public void BothDerivations_ComeFromTheSameDeclaration()
    {
        var confinement = new ProposerConfinement { WritablePrefixes = new[] { "src/", ".nexo" } };

        var allowlist = confinement.ToPathAllowlistPrefixes();
        var mounts = confinement.ToMounts("C:\\repo");

        allowlist.Should().Equal("src/", ".nexo/");
        // First mount: the whole repo, read-only context.
        mounts[0].Should().Be(new Mount("C:/repo", "/workspace", ReadOnly: true));
        // Then exactly one read-write mount per allowlisted prefix — same source, same order.
        mounts.Skip(1).Select(m => m.ReadOnly).Should().OnlyContain(ro => ro == false);
        mounts.Skip(1).Select(m => m.ContainerPath).Should().Equal("/workspace/src", "/workspace/.nexo");
        mounts.Skip(1).Select(m => m.HostPath).Should().Equal("C:/repo/src", "C:/repo/.nexo");
    }

    [Fact]
    public void Normalization_HandlesBackslashesAndMissingTrailingSlash()
    {
        var confinement = new ProposerConfinement { WritablePrefixes = new[] { "\\src\\Generated", "docs" } };

        confinement.ToPathAllowlistPrefixes().Should().Equal("src/Generated/", "docs/");
    }

    // --- refusals --------------------------------------------------------------------------

    [Fact]
    public void EmptyDeclaration_IsAWiringBug_NotAStricterSandbox()
    {
        var confinement = new ProposerConfinement { WritablePrefixes = Array.Empty<string>() };

        var act = () => confinement.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*no writable prefixes*");
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("src/../../etc")]
    [InlineData("C:/absolute")]
    public void RootedOrTraversingPrefixes_AreRefused(string prefix)
    {
        var confinement = new ProposerConfinement { WritablePrefixes = new[] { prefix } };

        var act = () => confinement.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*not a clean repo-relative path*");
    }

    [Theory]
    [InlineData("src/", "src/Generated/")] // nested
    [InlineData("src/", "src/")]           // duplicate
    public void OverlappingWritablePrefixes_AreRefused_BecauseMountOrderWouldDecidePermissions(
        string first, string second)
    {
        var confinement = new ProposerConfinement { WritablePrefixes = new[] { first, second } };

        var act = () => confinement.ToMounts("/repo");

        act.Should().Throw<InvalidOperationException>().WithMessage("*overlap*");
    }

    // --- network modes ---------------------------------------------------------------------

    [Fact]
    public void AllowedEndpoints_DefaultEmpty_AndTravelOnTheSpec()
    {
        var spec = new SandboxSpec("img", Array.Empty<Mount>(), NetworkAccess.HostServicesOnly, new[] { "sleep" });

        spec.AllowedEndpoints.Should().BeEmpty();
        (spec with { AllowedEndpoints = new[] { "host.docker.internal:11434" } })
            .AllowedEndpoints.Should().ContainSingle().Which.Should().Be("host.docker.internal:11434");
    }

    [Fact]
    public void DockerBackends_RefuseHostServicesOnly_FailClosed()
    {
        var spec = new SandboxSpec(
            "img", Array.Empty<Mount>(), NetworkAccess.HostServicesOnly, new[] { "sleep", "infinity" });

        var oneShot = () => DockerSandboxedCommandRunner.BuildDockerArguments(spec);
        var session = () => DockerSandboxedSessionRunner.BuildStartArguments(spec, "nexo-session-x", 0);

        oneShot.Should().Throw<NotSupportedException>().WithMessage("*fail-closed*");
        session.Should().Throw<NotSupportedException>().WithMessage("*fail-closed*",
            "a session believing itself contained while holding open egress is worse than no session");
    }

    [Fact]
    public void ConfinementMounts_FlowIntoASessionSpec_Unchanged()
    {
        var confinement = new ProposerConfinement { WritablePrefixes = new[] { "src/" } };
        var spec = new SandboxSpec(
            "proposer:latest",
            confinement.ToMounts("/repo"),
            NetworkAccess.None,
            new[] { "sleep", "infinity" });

        var args = DockerSandboxedSessionRunner.BuildStartArguments(spec, "nexo-session-x", 42);

        args.Should().Contain("/repo:/workspace:ro", "context is read-only");
        args.Should().Contain("/repo/src:/workspace/src", "the declared surface is the only writable mount");
    }
}
