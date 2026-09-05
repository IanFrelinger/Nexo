using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.BackgroundAgents.Forge;
using Ashlar.CLI.Commands.BackgroundAgent;
using Xunit;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>Tests for proposals background agent command.</summary>
public class ProposalsBackgroundAgentCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _repoRoot;
    private readonly ChangeProposalStore _store;

    public ProposalsBackgroundAgentCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ashlar-prop-cli-" + Guid.NewGuid().ToString("N"));
        _repoRoot = Path.Combine(_tempDir, "repo");
        Directory.CreateDirectory(Path.Combine(_repoRoot, "src"));
        _store = new ChangeProposalStore(Path.Combine(_tempDir, "forge"));
    }

    /// <summary>Dispose.</summary>
    public void Dispose() { try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ } }

    /// <summary>New cmd.</summary>
    private ProposalsBackgroundAgentCommand NewCmd() =>
        new(_store, NullLogger<ProposalsBackgroundAgentCommand>.Instance);

    private void WriteMinimalConsoleAppAtRepoRoot()
    {
        File.WriteAllText(
            Path.Combine(_repoRoot, "Tiny.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(_repoRoot, "Program.cs"), "System.Console.WriteLine(1);");
    }

    private void WriteMinimalXunitTestProjectAtRepoRoot()
    {
        File.WriteAllText(
            Path.Combine(_repoRoot, "Smoke.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <IsPackable>false</IsPackable>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
                <PackageReference Include="xunit" Version="2.6.6" />
                <PackageReference Include="xunit.runner.visualstudio" Version="2.5.6">
                  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
                  <PrivateAssets>all</PrivateAssets>
                </PackageReference>
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(
            Path.Combine(_repoRoot, "UnitTest1.cs"),
            """
            using Xunit;

            /// <summary>Tests for unit test1.</summary>
            public class UnitTest1
            {
                /// <summary>Ok.</summary>
                [Fact]
                public void Ok() => Assert.True(true);
            }
            """);
    }

    [Fact]
    public async Task BuildAsync_runs_dotnet_build_from_repo_root()
    {
        /// <summary>Write minimal console app at repo root.</summary>
        WriteMinimalConsoleAppAtRepoRoot();
        var cmd = NewCmd();
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var rc = await cmd.BuildAsync(_repoRoot, formatJson: false, stdout, stderr, default);
        rc.Should().Be(0);
        stdout.ToString().Should().Contain("Build succeeded");
    }

    [Fact]
    public async Task Apply_with_verify_build_returns_4_when_tree_no_longer_builds_but_file_is_written()
    {
        /// <summary>Write minimal console app at repo root.</summary>
        WriteMinimalConsoleAppAtRepoRoot();
        _store.Add(new ChangeProposal
        {
            Id = "break-build",
            TargetPath = "Program.cs",
            NewContent = "this is not valid csharp",
            Summary = "break"
        });
        _store.Approve("break-build");

        var cmd = NewCmd();
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var rc = await cmd.ApplyAsync("break-build", _repoRoot, force: false, formatJson: false, verifyBuild: true, verifyTest: false, stdout, stderr);
        rc.Should().Be(4);
        File.ReadAllText(Path.Combine(_repoRoot, "Program.cs")).Should().Be("this is not valid csharp");
        _store.Find("break-build")!.Status.Should().Be(ChangeProposalStatus.Applied);
    }

    [Fact]
    public async Task TestAsync_fails_closed_when_project_has_no_tests()
    {
        WriteMinimalConsoleAppAtRepoRoot();
        var cmd = NewCmd();
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var rc = await cmd.TestAsync(_repoRoot, formatJson: true, stdout, stderr, default);
        rc.Should().Be(1);
        using var json = JsonDocument.Parse(stdout.ToString());
        json.RootElement.GetProperty("ok").GetBoolean().Should().BeFalse();
        json.RootElement.GetProperty("test").GetProperty("ok").GetBoolean().Should().BeFalse();
        json.RootElement.GetProperty("test").GetProperty("exit_code").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task TestAsync_builds_then_tests_minimal_xunit_project()
    {
        /// <summary>Write minimal xunit test project at repo root.</summary>
        WriteMinimalXunitTestProjectAtRepoRoot();
        var cmd = NewCmd();
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var rc = await cmd.TestAsync(_repoRoot, formatJson: false, stdout, stderr, default);
        rc.Should().Be(0);
        stdout.ToString().Should().Contain("Tests succeeded");
    }

    [Fact]
    public async Task Apply_with_verify_test_returns_5_when_tests_fail_after_apply()
    {
        /// <summary>Write minimal xunit test project at repo root.</summary>
        WriteMinimalXunitTestProjectAtRepoRoot();
        _store.Add(new ChangeProposal
        {
            Id = "break-test",
            TargetPath = "UnitTest1.cs",
            NewContent =
                """
                using Xunit;

                /// <summary>Tests for unit test1.</summary>
                public class UnitTest1
                {
                    /// <summary>Bad.</summary>
                    [Fact]
                    public void Bad() => Assert.True(false);
                }
                """,
            Summary = "break tests"
        });
        _store.Approve("break-test");

        var cmd = NewCmd();
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var rc = await cmd.ApplyAsync("break-test", _repoRoot, force: false, formatJson: false, verifyBuild: false, verifyTest: true, stdout, stderr);
        rc.Should().Be(5);
        _store.Find("break-test")!.Status.Should().Be(ChangeProposalStatus.Applied);
    }

    [Fact]
    public async Task Approve_then_apply_writes_file_and_marks_applied()
    {
        var saved = _store.Add(new ChangeProposal
        {
            Id = "to-apply",
            TargetPath = "src/A.cs",
            NewContent = "// hello",
            Summary = "first"
        });

        var cmd = NewCmd();
        var stdout = new StringWriter(); var stderr = new StringWriter();
        (await cmd.ApproveAsync(saved.Id, "alice", "lgtm", formatJson: false)).Should().Be(0);

        var rc = await cmd.ApplyAsync(saved.Id, _repoRoot, force: false, formatJson: false, verifyBuild: false, verifyTest: false, stdout, stderr);
        rc.Should().Be(0);
        File.ReadAllText(Path.Combine(_repoRoot, "src", "A.cs")).Should().Be("// hello");
        _store.Find(saved.Id)!.Status.Should().Be(ChangeProposalStatus.Applied);
    }

    [Fact]
    public async Task Apply_refuses_when_base_sha_drifted_unless_forced()
    {
        var path = Path.Combine(_repoRoot, "src", "B.cs");
        File.WriteAllText(path, "// original");
        var sha = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("// original")));

        _store.Add(new ChangeProposal
        {
            Id = "drift",
            TargetPath = "src/B.cs",
            NewContent = "// rewritten",
            Summary = "rewrite",
            BaseSha256 = sha
        });
        _store.Approve("drift");

        File.WriteAllText(path, "// drifted by someone else");
        var cmd = NewCmd();
        var stdout = new StringWriter(); var stderr = new StringWriter();
        var rc = await cmd.ApplyAsync("drift", _repoRoot, force: false, formatJson: false, verifyBuild: false, verifyTest: false, stdout, stderr);
        rc.Should().Be(3);
        stderr.ToString().Should().Contain("Drift");

        var rcForce = await cmd.ApplyAsync("drift", _repoRoot, force: true, formatJson: false, verifyBuild: false, verifyTest: false, new StringWriter(), new StringWriter());
        rcForce.Should().Be(0);
        File.ReadAllText(path).Should().Be("// rewritten");
    }

    [Fact]
    public async Task Janitor_stales_old_proposals_and_reports_ids()
    {
        // Add an Approved proposal and force its UpdatedAt to be 100 hours ago.
        _store.Add(new ChangeProposal { Id = "stale-me", TargetPath = "src/C.cs", NewContent = "x", Summary = "old" });
        _store.Approve("stale-me");
        var approvedPath = Path.Combine(_tempDir, "forge", "approved", "stale-me.json");
        var doc = JsonSerializer.Deserialize<ChangeProposal>(File.ReadAllText(approvedPath))!;
        File.WriteAllText(approvedPath, JsonSerializer.Serialize(doc with { UpdatedAt = DateTimeOffset.UtcNow.AddHours(-100) }, new JsonSerializerOptions { WriteIndented = true }));

        var cmd = NewCmd();
        var stdout = new StringWriter(); var stderr = new StringWriter();
        var rc = await cmd.JanitorAsync(proposedTtlHours: null, approvedTtlHours: 24, formatJson: true, stdout, stderr);
        rc.Should().Be(0);

        using var json = JsonDocument.Parse(stdout.ToString());
        json.RootElement.GetProperty("count").GetInt32().Should().Be(1);
        _store.Find("stale-me")!.Status.Should().Be(ChangeProposalStatus.Stale);
    }

    [Fact]
    public async Task List_filters_by_status_and_target_prefix()
    {
        _store.Add(new ChangeProposal { Id = "a", TargetPath = "src/A.cs", NewContent = "x", Summary = "a" });
        _store.Add(new ChangeProposal { Id = "b", TargetPath = "tests/B.cs", NewContent = "x", Summary = "b" });
        _store.Approve("b");

        var cmd = NewCmd();
        var stdout = new StringWriter(); var stderr = new StringWriter();
        var rc = await cmd.ListAsync(status: "Approved", targetPrefix: null, formatJson: true, stdout, stderr);
        rc.Should().Be(0);

        using var json = JsonDocument.Parse(stdout.ToString());
        json.RootElement.GetProperty("count").GetInt32().Should().Be(1);
        json.RootElement.GetProperty("proposals")[0].GetProperty("id").GetString().Should().Be("b");
    }
}
