using System.CommandLine;
using FluentAssertions;
using Xunit;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>
/// Guards the root command registration list in Program.CommandRegistration.cs.
/// A command can be built (BuildXxxCommand) and then silently never added to the root;
/// that is how `ashlar trust` disappeared in #162 while README/GettingStarted and the
/// Tier C security gate kept invoking it.
/// </summary>
[Trait("Category", "CLI")]
public sealed class RootCommandRegistrationTests
{
    [Fact(Timeout = 15000)]
    public async Task RootCommand_RegistersTrustCommand()
    {
        await Task.CompletedTask;
        var root = Ashlar.CLI.Program.BuildRootCommand();
        var subcommands = root.Subcommands.Select(s => s.Name).ToList();

        subcommands.Should().Contain("trust");
    }

    [Fact(Timeout = 15000)]
    public async Task RootCommand_RegistersInitCommand()
    {
        await Task.CompletedTask;
        var root = Ashlar.CLI.Program.BuildRootCommand();
        var subcommands = root.Subcommands.Select(s => s.Name).ToList();

        // `ashlar init` is the front door of the product loop (init -> verify -> run ->
        // deploy); docs and the quickstart lead with it, so it must never silently
        // disappear from the root the way `trust` did in #162.
        subcommands.Should().Contain("init");
    }

    [Fact(Timeout = 15000)]
    public async Task RootCommand_RegistersVerifyCommand()
    {
        await Task.CompletedTask;
        var root = Ashlar.CLI.Program.BuildRootCommand();
        var subcommands = root.Subcommands.Select(s => s.Name).ToList();

        // The wall. Same guard as init: the loop's verbs stay registered.
        subcommands.Should().Contain("verify");
    }

    [Fact(Timeout = 15000)]
    public async Task RootCommand_RegistersGatesCommand()
    {
        await Task.CompletedTask;
        var root = Ashlar.CLI.Program.BuildRootCommand();
        var subcommands = root.Subcommands.Select(s => s.Name).ToList();

        // The human half of admission. Same guard as init and verify.
        subcommands.Should().Contain("gates");
    }

    [Fact(Timeout = 15000)]
    public async Task RootCommand_RegistersRunCommand()
    {
        await Task.CompletedTask;
        var root = Ashlar.CLI.Program.BuildRootCommand();
        var subcommands = root.Subcommands.Select(s => s.Name).ToList();

        // The loop's fourth verb. Same guard as init, verify, and gates.
        subcommands.Should().Contain("run");
    }


    [Fact(Timeout = 15000)]
    public async Task RootCommand_RegistersKeysCommand()
    {
        await Task.CompletedTask;
        var root = Ashlar.CLI.Program.BuildRootCommand();
        var keys = root.Subcommands.SingleOrDefault(s => s.Name == "keys");

        // The operator's signing identity (SPEC-006). Same guard as the loop's verbs: it must
        // never silently vanish from the root — a project whose `keys init` disappeared would
        // fall back to unsigned verdicts with no signal that signing was ever available.
        keys.Should().NotBeNull("`ashlar keys` must stay registered");
        keys!.Subcommands.Select(s => s.Name).Should().Contain(new[] { "init", "show" });
    }

    [Fact(Timeout = 15000)]
    public async Task RootCommand_RegistersPkgCommand()
    {
        await Task.CompletedTask;
        var root = Ashlar.CLI.Program.BuildRootCommand();
        var pkg = root.Subcommands.SingleOrDefault(s => s.Name == "pkg");

        // Certified extension packages — admissions that travel. Same guard as the other verbs.
        pkg.Should().NotBeNull("`ashlar pkg` must stay registered");
        pkg!.Subcommands.Select(s => s.Name).Should().Contain(new[] { "export", "import", "show", "publish", "pull", "share" });
    }

    [Fact(Timeout = 15000)]
    public async Task RootCommand_RegistersExportCommand()
    {
        await Task.CompletedTask;
        var root = Ashlar.CLI.Program.BuildRootCommand();
        var export = root.Subcommands.SingleOrDefault(s => s.Name == "export");

        // Turning a certified project into a portable, self-proving download. Same guard as the
        // other verbs: it must never silently vanish.
        export.Should().NotBeNull("`ashlar export` must stay registered");
        export!.Subcommands.Select(s => s.Name).Should().Contain(new[] { "native", "aws", "azure" });
    }

    [Fact(Timeout = 15000)]
    public async Task TrustCommand_HasDocumentedSubcommands()
    {
        await Task.CompletedTask;
        var root = Ashlar.CLI.Program.BuildRootCommand();
        var trust = root.Subcommands.Single(s => s.Name == "trust");
        var subcommands = trust.Subcommands.Select(s => s.Name).ToList();

        // Named by README.md, docs/GettingStarted.md and scripts/security-gate-tier-c.sh.
        subcommands.Should().Contain(new[] { "dashboard", "boundary", "pause", "resume", "pack" });
        trust.Subcommands.Single(s => s.Name == "pack").Subcommands.Select(s => s.Name)
            .Should().Contain("apply");
    }
}
