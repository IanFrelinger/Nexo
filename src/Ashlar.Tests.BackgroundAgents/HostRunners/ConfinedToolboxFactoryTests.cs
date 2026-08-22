using System.Text.Json;
using FluentAssertions;
using Ashlar.Abstractions;
using Ashlar.BackgroundAgents.HostRunners;
using Ashlar.Core.Application.Execution.Ports;
using Xunit;

namespace Ashlar.Tests.BackgroundAgents.HostRunners;

/// <summary>
/// Confinement-derived write policy (extension spec Part B): when a
/// <see cref="ProposerConfinement"/> is supplied, the toolbox's path allowlist is EXACTLY
/// the declared writable prefixes — the historical defaults and the environment-variable
/// widening must not apply, because the declaration is the single source that also derives
/// the sandbox mounts.
/// </summary>
public sealed class ConfinedToolboxFactoryTests
{
    private const string EnvVar = "ASHLAR_PATH_ALLOWLIST_EXTRA";

    private static ToolCall WriteCall(string path) => new(
        "repo.fs.write",
        JsonSerializer.SerializeToElement(new { path, content = "x" }));

    private static WorldSnapshot Snapshot() => new(0, new Dictionary<string, object?>());

    [Fact]
    public void WithConfinement_TheAllowlistIsExactlyTheDeclaration()
    {
        var confinement = new ProposerConfinement { WritablePrefixes = new[] { "src/Generated/" } };
        var previous = Environment.GetEnvironmentVariable(EnvVar);
        Environment.SetEnvironmentVariable(EnvVar, "secrets/");
        try
        {
            var (_, policies, _) = RepoFsToolboxFactory.CreateWithBuildTest(confinement: confinement);

            policies.Approve(WriteCall("src/Generated/NewBrick.cs"), Snapshot(), out _)
                .Should().BeTrue("the declared surface is writable");
            policies.Approve(WriteCall("docs/notes.md"), Snapshot(), out var docsReason)
                .Should().BeFalse("historical defaults must not widen a confinement declaration");
            docsReason.Should().Contain("not allowed");
            policies.Approve(WriteCall("secrets/creds.txt"), Snapshot(), out _)
                .Should().BeFalse("environment widening must not apply to a confinement declaration");
            policies.Approve(WriteCall("src/Other/File.cs"), Snapshot(), out _)
                .Should().BeFalse("sibling paths outside the declared prefix stay read-only");
        }
        finally
        {
            Environment.SetEnvironmentVariable(EnvVar, previous);
        }
    }

    [Fact]
    public void WithoutConfinement_TheHistoricalDefaultAllowlistStillApplies()
    {
        var (_, policies, _) = RepoFsToolboxFactory.CreateWithBuildTest();

        policies.Approve(WriteCall("docs/notes.md"), Snapshot(), out _)
            .Should().BeTrue("null confinement preserves existing behavior");
    }
}
