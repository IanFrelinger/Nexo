using System.CommandLine;
using FluentAssertions;
using Xunit;

namespace Nexo.Tests.CLI.Tests.Commands;

/// <summary>
/// Guards the root command registration list in Program.CommandRegistration.cs.
/// A command can be built (BuildXxxCommand) and then silently never added to the root;
/// that is how `nexo trust` disappeared in #162 while README/GettingStarted and the
/// Tier C security gate kept invoking it.
/// </summary>
[Trait("Category", "CLI")]
public sealed class RootCommandRegistrationTests
{
    [Fact(Timeout = 15000)]
    public async Task RootCommand_RegistersTrustCommand()
    {
        await Task.CompletedTask;
        var root = Nexo.CLI.Program.BuildRootCommand();
        var subcommands = root.Subcommands.Select(s => s.Name).ToList();

        subcommands.Should().Contain("trust");
    }

    [Fact(Timeout = 15000)]
    public async Task TrustCommand_HasDocumentedSubcommands()
    {
        await Task.CompletedTask;
        var root = Nexo.CLI.Program.BuildRootCommand();
        var trust = root.Subcommands.Single(s => s.Name == "trust");
        var subcommands = trust.Subcommands.Select(s => s.Name).ToList();

        // Named by README.md, docs/GettingStarted.md and scripts/security-gate-tier-c.sh.
        subcommands.Should().Contain(new[] { "dashboard", "boundary", "pause", "resume", "pack" });
        trust.Subcommands.Single(s => s.Name == "pack").Subcommands.Select(s => s.Name)
            .Should().Contain("apply");
    }
}
