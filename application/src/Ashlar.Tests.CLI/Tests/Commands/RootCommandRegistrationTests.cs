using System.CommandLine;
using Ashlar.CLI.Commands;
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
    public async Task GlobalFormatJson_isReachableByAlias_notByOptionName()
    {
        await Task.CompletedTask;
        var root = Ashlar.CLI.Program.BuildRootCommand();

        // System.CommandLine strips the leading "--" from Option.Name. Every `o.Name ==
        // "--format-json"` lookup in Commands/ therefore matches nothing and silently reports "no
        // JSON asked for" — the flag is read by code that can never see it, which is the same silent
        // acceptance this wave is fixing. Pin the two facts a correct lookup depends on.
        var formatJson = root.Options.Single(o => o.HasAlias("--format-json"));
        formatJson.Name.Should().Be("format-json", "the prefix is stripped — never match on Name alone");

        CommandExecutionSupport.WantsJson(root.Parse("doctor --format-json")).Should().BeTrue();
        CommandExecutionSupport.WantsJson(root.Parse("doctor")).Should().BeFalse();
        CommandExecutionSupport.WantsJson(root.Parse("test portable --format-json")).Should().BeTrue();
        CommandExecutionSupport.WantsJson(root.Parse("test multi-env --format-json")).Should().BeTrue();
        CommandExecutionSupport.WantsJson(root.Parse("docker ps --format-json")).Should().BeTrue();

        var verbose = root.Options.Single(o => o.HasAlias("--verbose"));
        verbose.Name.Should().Be("verbose", "the prefix is stripped — never match on Name alone");
        CommandExecutionSupport.WantsVerbose(root.Parse("test portable --verbose")).Should().BeTrue();
        CommandExecutionSupport.WantsVerbose(root.Parse("test portable")).Should().BeFalse();
        CommandExecutionSupport.WantsVerbose(root.Parse("test multi-env --verbose")).Should().BeTrue();
        CommandExecutionSupport.WantsVerbose(root.Parse("docker ps --verbose")).Should().BeTrue();
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
