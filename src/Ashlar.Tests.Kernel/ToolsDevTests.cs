using System.Text.Json;
using System.Diagnostics;
using FluentAssertions;
using Moq;
using Ashlar.Abstractions;
using Ashlar.Core.Application.Maintenance.Models;
using Ashlar.Core.Application.Maintenance.Ports;
using Ashlar.Tools.Dev;
using Ashlar.Tools.Dev.Deltas;
using Xunit;

namespace Ashlar.Tests.Kernel;

/// <summary>Tests for tools dev.</summary>
public class ToolsDevTests
{
    /// <summary>Call.</summary>
    /// <param name="id">Id.</param>
    /// <param name="args">Args.</param>
    private static ToolCall Call(string id, object args) =>
        new(id, JsonDocument.Parse(JsonSerializer.Serialize(args)).RootElement);

    /// <summary>
    /// True when <paramref name="dir"/> can actually be enumerated, whatever its mode bits
    /// claim. Detects running with privileges that bypass permission checks (root), where a
    /// "locked directory" test has no premise to stand on.
    /// </summary>
    private static bool CanEnumerate(string dir)
    {
        try
        {
            _ = Directory.EnumerateFileSystemEntries(dir).GetEnumerator().MoveNext();
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    [Fact]
    public void FileEdit_and_RepoDelta_track_edits_and_logs()
    {
        var edit = new FileEdit("/a.txt", "before", "after", 3, 1);
        edit.Path.Should().Be("/a.txt");
        edit.BeforeSha.Should().Be("before");
        edit.AfterSha.Should().Be("after");
        edit.Added.Should().Be(3);
        edit.Removed.Should().Be(1);

        var delta = new RepoDelta { TickFrom = 1, TickTo = 2 };
        delta.AddLog("changed");
        delta.AddEdit(edit);
        delta.Log.Should().ContainSingle();
        delta.Edits.Should().ContainSingle();
        delta.Signature = new byte[] { 1 };
        delta.Signature.Should().NotBeNull();
    }

    [Fact]
    public void CleanArtifactsTool_constructor_rejects_null_service()
    {
        var act = () => new CleanArtifactsTool(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task CleanArtifactsTool_takes_its_repo_root_from_the_sandbox_not_the_arguments()
    {
        // Renamed from CleanArtifactsTool_uses_explicit_repo_root_...: the tool no longer
        // accepts an explicit repoRoot. It used to prefer the model's over the snapshot's
        // (`args.repoRoot ?? snapshot`), which pointed a delete operation wherever the model
        // liked. The argument below is left in deliberately, to assert it is ignored.
        var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "ashlar-clean-" + Guid.NewGuid().ToString("N")));

        var cleanup = new Mock<IArtifactCleanupService>();
        cleanup.Setup(s => s.CleanAsync(
                "incomplete-blobs",
                It.Is<ArtifactCleanupContext?>(c => c!.RepoRoot == root),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArtifactCleanupResult(
                "incomplete-blobs",
                5,
                Array.Empty<string>(),
                new[] { "locked file" }));

        var tool = new CleanArtifactsTool(cleanup.Object);
        tool.Schema.Id.Should().Be(CleanArtifactsTool.IdConstant);
        tool.Schema.Description.Should().Contain("Clean disk");

        var result = await tool.InvokeAsync(
            Call(CleanArtifactsTool.IdConstant, new { strategyId = "incomplete-blobs", repoRoot = "/explicit/root" }),
            WorldSnapshot.ForRepo(root),
            CancellationToken.None);

        result.Delta.Log.Should().ContainSingle().Which.Should().Contain("reclaimed=5");
    }

    [Fact]
    public async Task CleanArtifactsTool_invokes_cleanup_service()
    {
        var cleanup = new Mock<IArtifactCleanupService>();
        cleanup.Setup(s => s.CleanAsync(
                It.IsAny<string?>(),
                It.IsAny<ArtifactCleanupContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArtifactCleanupResult(
                StrategyId: "test-artifacts",
                BytesReclaimed: 42,
                PathsDeleted: new[] { "/tmp/x" },
                Errors: Array.Empty<string>()));

        var tool = new CleanArtifactsTool(cleanup.Object);
        tool.Id.Should().Be(CleanArtifactsTool.IdConstant);

        var snap = WorldSnapshot.ForRepo("/repo");
        var result = await tool.InvokeAsync(
            Call(CleanArtifactsTool.IdConstant, new { strategyId = "test-artifacts" }),
            snap,
            CancellationToken.None);

        result.Delta.Log.Should().ContainSingle().Which.Should().Contain("clean_artifacts");
    }

    [Fact]
    public async Task CleanArtifactsTool_handles_invalid_json_arguments()
    {
        var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "ashlar-clean-bad-" + Guid.NewGuid().ToString("N")));
        var cleanup = new Mock<IArtifactCleanupService>();
        cleanup.Setup(s => s.CleanAsync(
                null,
                It.IsAny<ArtifactCleanupContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArtifactCleanupResult("all", 0, Array.Empty<string>(), Array.Empty<string>()));

        var tool = new CleanArtifactsTool(cleanup.Object);
        var call = new ToolCall(CleanArtifactsTool.IdConstant, JsonDocument.Parse("[]").RootElement);
        var result = await tool.InvokeAsync(call, WorldSnapshot.ForRepo(root), CancellationToken.None);
        result.Delta.Log.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RepoFsListTool_lists_repo_entries()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-list-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "readme.txt"), "hi");
        try
        {
            var tool = new RepoFsListTool();
            var snap = WorldSnapshot.ForRepo(root);
            var result = await tool.InvokeAsync(
                Call("repo.fs.list", new { root, path = ".", recursive = false }),
                snap,
                CancellationToken.None);
            result.Payload.Should().NotBeNull();
            result.Delta.Log.Should().ContainSingle().Which.Should().Contain("list");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RepoFsListTool_rejects_invalid_subpath()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-list-bad-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var tool = new RepoFsListTool();
            var result = await tool.InvokeAsync(
                Call("repo.fs.list", new { root, path = "../outside" }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            result.Delta.Log.Should().ContainSingle().Which.Should().Contain("REJECTED");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RepoFsReadTool_reads_and_truncates_large_files()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-read-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "big.txt");
        await File.WriteAllTextAsync(path, new string('x', 100));
        try
        {
            var tool = new RepoFsReadTool();
            tool.Id.Should().Be("repo.fs.read");
            tool.Schema.Id.Should().Be("repo.fs.read");
            var result = await tool.InvokeAsync(
                Call("repo.fs.read", new { root, path = "big.txt", max_bytes = 10 }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            result.Delta.Log.Should().ContainSingle();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RepoFsWriteTool_writes_file_and_records_edit_delta()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-write-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var tool = new RepoFsWriteTool();
            var result = await tool.InvokeAsync(
                Call("repo.fs.write", new { root, path = "sub/out.txt", content = "line1\nline2" }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            var full = Path.Combine(root, "sub/out.txt");
            File.Exists(full).Should().BeTrue();
            (await File.ReadAllTextAsync(full)).Should().Contain("line1");
            result.Delta.Log.Should().ContainSingle().Which.Should().Contain("write:");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RepoFsEnsureFileTool_creates_or_skips_existing()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-ensure-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var tool = new RepoFsEnsureFileTool();
            tool.Id.Should().Be("repo.fs.ensure_file");
            tool.Schema.Id.Should().Be("repo.fs.ensure_file");
            tool.Schema.Description.Should().Contain("Create file");
            var created = await tool.InvokeAsync(
                Call("repo.fs.ensure_file", new { root, path = "nested/deep/new.txt", content = "hello" }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            created.Delta.Log.Should().ContainSingle().Which.Should().Contain("created");
            File.Exists(Path.Combine(root, "nested", "deep", "new.txt")).Should().BeTrue();

            var exists = await tool.InvokeAsync(
                Call("repo.fs.ensure_file", new { root, path = "nested/deep/new.txt", content = "other" }),
                WorldSnapshot.ForRepo(root, tick: 1),
                CancellationToken.None);
            exists.Delta.Log.Should().ContainSingle().Which.Should().Contain("exists");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RepoFsSearchReplaceTool_replaces_content()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-sr-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "file.txt");
        await File.WriteAllTextAsync(path, "foo bar foo");
        try
        {
            var tool = new RepoFsSearchReplaceTool();
            var result = await tool.InvokeAsync(
                Call("repo.fs.search_replace", new { root, path = "file.txt", find = "foo", replace = "baz" }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            (await File.ReadAllTextAsync(path)).Should().Be("baz bar baz");
            result.Delta.Log.Should().ContainSingle().Which.Should().Contain("s&r:");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task DotnetRunTool_executes_dotnet_version()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-run-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var result = await new DotnetRunTool().InvokeAsync(
                Call("dotnet.run", new { root, args = "--version", timeoutSeconds = 30 }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            result.Delta.Log.Should().Contain(l => l.Contains("dotnet:exit="));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task DotnetTestTool_runs_on_minimal_project()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "Empty.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <IsPackable>false</IsPackable>
              </PropertyGroup>
            </Project>
            """);
        try
        {
            await new DotnetBuildTool().InvokeAsync(
                Call("dotnet.build", new { root }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            var result = await new DotnetTestTool().InvokeAsync(
                Call("dotnet.test", new { root }),
                WorldSnapshot.ForRepo(root, tick: 1),
                CancellationToken.None);
            result.Delta.Log.Should().Contain(l => l.Contains("test:exit="));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task DotnetBuildTool_builds_minimal_project()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-build-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "Mini.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
        try
        {
            var result = await new DotnetBuildTool().InvokeAsync(
                Call("dotnet.build", new { root }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            result.Delta.Log.Should().Contain(l => l.Contains("build:exit="));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task DotnetBuildTool_uses_preferred_solution_filter_when_root_has_multiple_build_files()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-build-filter-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "Mini.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(Path.Combine(root, "Other.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        try
        {
            /// <summary>Run dotnet async.</summary>
            /// <param name="sln"">Sln".</param>
            await RunDotnetAsync(root, "new sln --name Ashlar --format sln");
            /// <summary>Run dotnet async.</summary>
            /// <param name="Mini.csproj"">Mini.csproj".</param>
            await RunDotnetAsync(root, "sln Ashlar.sln add Mini.csproj");
            await File.WriteAllTextAsync(Path.Combine(root, "Ashlar.LocalDevCore.slnf"), """
                {
                  "solution": {
                    "path": "Ashlar.sln",
                    "projects": [
                      "Mini.csproj"
                    ]
                  }
                }
                """);

            var result = await new DotnetBuildTool().InvokeAsync(
                Call("dotnet.build", new { root }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);

            result.Delta.Log.Should().Contain(l => l.Contains("build:exit=0"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ForgeBuildTool_builds_minimal_project()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-forge-build-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "Forge.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
        try
        {
            var tool = new ForgeBuildTool();
            tool.Id.Should().Be("forge.build");
            var result = await tool.InvokeAsync(
                Call("forge.build", new { root }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            result.Delta.Log.Should().Contain(l => l.Contains("forge.build:exit="));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ForgeTestTool_runs_on_built_minimal_project()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-forge-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "ForgeTest.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <IsPackable>false</IsPackable>
              </PropertyGroup>
            </Project>
            """);
        try
        {
            await new DotnetBuildTool().InvokeAsync(
                Call("dotnet.build", new { root }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            var tool = new ForgeTestTool();
            tool.Id.Should().Be("forge.test");
            var result = await tool.InvokeAsync(
                Call("forge.test", new { root }),
                WorldSnapshot.ForRepo(root, tick: 1),
                CancellationToken.None);
            result.Delta.Log.Should().Contain(l => l.Contains("forge.test:exit="));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RepoGitCommitTool_appends_pseudo_commit_log()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-commit-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var tool = new RepoGitCommitTool();
            tool.Id.Should().Be("repo.git.commit");
            tool.Schema.Id.Should().Be("repo.git.commit");
            tool.Schema.Description.Should().Contain("pseudo-commit");
            var result = await tool.InvokeAsync(
                Call("repo.git.commit", new { root, message = "feat: kernel coverage" }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);

            var logPath = Path.Combine(root, "COMMIT_LOG.txt");
            File.Exists(logPath).Should().BeTrue();
            (await File.ReadAllTextAsync(logPath)).Should().Contain("feat: kernel coverage");
            result.Delta.Log.Should().ContainSingle().Which.Should().Contain("commit:feat: kernel coverage");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task DocsUpdateTool_appends_changelog_entry()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-docs-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var tool = new DocsUpdateTool();
            tool.Id.Should().Be("docs.update");
            tool.Schema.Id.Should().Be("docs.update");
            tool.Schema.Description.Should().Contain("CHANGELOG");
            var snap = WorldSnapshot.ForRepo(root);
            var result = await tool.InvokeAsync(
                Call("docs.update", new { root, entry = "ship feature" }),
                snap,
                CancellationToken.None);

            var changelog = Path.Combine(root, "CHANGELOG.md");
            File.Exists(changelog).Should().BeTrue();
            (await File.ReadAllTextAsync(changelog)).Should().Contain("ship feature");
            result.Delta.Log.Should().Contain("docs:update");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RoslynAnalyzeTool_reports_missing_file_and_rule_violations()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-roslyn-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var sourcePath = Path.Combine(root, "Cmd.cs");
        await File.WriteAllTextAsync(sourcePath, """
            namespace Wrong;
            /// <summary>Not sealed.</summary>
            class NotSealed { }
            """);
        try
        {
            var tool = new RoslynAnalyzeTool();
            var missing = await tool.InvokeAsync(
                Call("roslyn.analyze", new
                {
                    root,
                    files = new[] { "missing.cs", "Cmd.cs" },
                    rules = new
                    {
                        requiredNamespace = "Expected",
                        requireFileScopedNamespace = true,
                        requirePublic = true,
                        requireSealed = true,
                        requiredClassName = "Cmd",
                    },
                }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);

            missing.Payload.Should().NotBeNull();
            missing.Delta.Log.Should().Contain(l => l.Contains("violations="));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }


    [Fact]
    public async Task DotnetRunTool_logs_stderr_and_timeout()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-run-err-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var invalid = await new DotnetRunTool().InvokeAsync(
                Call("dotnet.run", new { root, args = "not-a-real-subcommand" }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            invalid.Delta.Log.Should().Contain(l => l.Contains("dotnet:exit="));
            invalid.Delta.Log.Should().Contain(l => l.Contains("dotnet:stderr"));

            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
            var timedOut = await new DotnetRunTool().InvokeAsync(
                Call("dotnet.run", new { root, args = "restore", timeoutSeconds = 1 }),
                WorldSnapshot.ForRepo(root, tick: 1),
                cts.Token);
            timedOut.Delta.Log.Should().Contain(l => l.Contains("dotnet:exit=") || l.Contains("dotnet:timeout"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }


    [Fact]
    public async Task CleanArtifactsTool_uses_repo_root_from_snapshot()
    {
        var cleanup = new Mock<IArtifactCleanupService>();
        cleanup.Setup(s => s.CleanAsync(
                "incomplete-blobs",
                It.Is<ArtifactCleanupContext?>(c => c!.RepoRoot == "/repo/root"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArtifactCleanupResult("incomplete-blobs", 10, Array.Empty<string>(), Array.Empty<string>()));

        var tool = new CleanArtifactsTool(cleanup.Object);
        var snap = new WorldSnapshot(0, new Dictionary<string, object?> { ["RepoRoot"] = "/repo/root" });
        var result = await tool.InvokeAsync(
            Call(CleanArtifactsTool.IdConstant, new { strategyId = "incomplete-blobs" }),
            snap,
            CancellationToken.None);

        result.Delta.Log.Should().ContainSingle().Which.Should().Contain("clean_artifacts");
    }

    [Fact]
    public async Task DocsUpdateTool_appends_to_existing_changelog()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-docs-existing-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "CHANGELOG.md"), "- existing entry\n");

        try
        {
            var result = await new DocsUpdateTool().InvokeAsync(
                Call("docs.update", new { root, entry = "second entry" }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);

            var text = await File.ReadAllTextAsync(Path.Combine(root, "CHANGELOG.md"));
            text.Should().Contain("existing entry");
            text.Should().Contain("second entry");
            result.Delta.Log.Should().Contain("docs:update");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RepoFsReadTool_reports_missing_file()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-read-miss-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var result = await new RepoFsReadTool().InvokeAsync(
                Call("repo.fs.read", new { root, path = "missing.txt" }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            result.Delta.Log.Should().ContainSingle().Which.Should().Contain("not_found");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RepoFsListTool_lists_recursively_with_truncation()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-list-rec-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "sub"));
        await File.WriteAllTextAsync(Path.Combine(root, "sub", "nested.txt"), "nested");
        try
        {
            var result = await new RepoFsListTool().InvokeAsync(
                Call("repo.fs.list", new { root, path = ".", recursive = true, max_entries = 5 }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            result.Delta.Log.Should().ContainSingle().Which.Should().Contain("list");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RoslynAnalyzeTool_passes_when_rules_satisfied()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-roslyn-ok-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "Cmd.cs"), """
            namespace Expected;

            /// <summary>Cmd.</summary>
            public sealed class Cmd
            {
            }
            """);

        try
        {
            var result = await new RoslynAnalyzeTool().InvokeAsync(
                Call("roslyn.analyze", new
                {
                    root,
                    files = new[] { "Cmd.cs" },
                    rules = new
                    {
                        requiredNamespace = "Expected",
                        requireFileScopedNamespace = true,
                        requirePublic = true,
                        requireSealed = true,
                        requiredClassName = "Cmd",
                    },
                }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);

            result.Delta.Log.Should().Contain(l => l.Contains("violations=0"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void DotnetRunTool_exposes_schema_and_id()
    {
        var tool = new DotnetRunTool();
        tool.Id.Should().Be("dotnet.run");
        tool.Schema.Id.Should().Be("dotnet.run");
        tool.Schema.Description.Should().Contain("dotnet");
    }



    [Fact]
    public async Task RoslynAnalyzeTool_checks_base_type_and_command_name()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-roslyn-cmd-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "Cmd.cs"), """
            namespace Expected;

            /// <summary>Cmd.</summary>
            public sealed class Cmd : BaseCmd
            {
                public Cmd() : base("expected-name") { }
            }

            /// <summary>Base cmd.</summary>
            public class BaseCmd
            {
                /// <summary>Base cmd.</summary>
                /// <param name="name">Name.</param>
                protected BaseCmd(string name) { }
            }
            """);

        try
        {
            var ok = await new RoslynAnalyzeTool().InvokeAsync(
                Call("roslyn.analyze", new
                {
                    root,
                    files = new[] { "Cmd.cs" },
                    rules = new
                    {
                        requiredNamespace = "Expected",
                        requireFileScopedNamespace = true,
                        requirePublic = true,
                        requireSealed = true,
                        requiredClassName = "Cmd",
                        requiredBaseType = "BaseCmd",
                        requiredCommandName = "expected-name",
                    },
                }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            ok.Delta.Log.Should().Contain(l => l.Contains("violations=0"));

            await File.WriteAllTextAsync(Path.Combine(root, "Bad.cs"), """
                namespace Expected;
                /// <summary>Bad.</summary>
                class Bad { }
                """);
            var bad = await new RoslynAnalyzeTool().InvokeAsync(
                Call("roslyn.analyze", new
                {
                    root,
                    files = new[] { "Bad.cs" },
                    rules = new { requireFileScopedNamespace = false, requirePublic = true, requireSealed = true },
                }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            bad.Delta.Log.Should().Contain(l => l.Contains("violations=") && !l.Contains("violations=0"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RepoFsListTool_respects_max_recursion_depth()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-list-deep-" + Guid.NewGuid().ToString("N"));
        var current = root;
        for (var i = 0; i < 8; i++)
        {
            current = Path.Combine(current, $"level{i}");
            Directory.CreateDirectory(current);
        }
        await File.WriteAllTextAsync(Path.Combine(current, "leaf.txt"), "deep");

        try
        {
            var result = await new RepoFsListTool().InvokeAsync(
                Call("repo.fs.list", new { root, path = ".", recursive = true, max_entries = 500 }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);

            result.Delta.Log.Should().Contain(l => l.Contains("entries="));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RepoFsListTool_reports_missing_directory_and_rejects_bad_paths()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-list-miss-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var tool = new RepoFsListTool();
            tool.Id.Should().Be("repo.fs.list");
            tool.Schema.Id.Should().Be("repo.fs.list");
            tool.Schema.Description.Should().Contain("List files");

            var missing = await tool.InvokeAsync(
                Call("repo.fs.list", new { root, path = "missing-dir" }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            missing.Delta.Log.Should().Contain(l => l.Contains("not_found"));

            // Was: root = "" in the arguments must be rejected. See the equivalent note in
            // RepoFsReadTool_rejects_invalid_paths_and_empty_arguments.
            var noSandboxRoot = await tool.InvokeAsync(
                Call("repo.fs.list", new { path = "." }),
                new WorldSnapshot(0, new Dictionary<string, object?>()),
                CancellationToken.None);
            noSandboxRoot.Delta.Log.Should().Contain(l => l.Contains("REJECTED"));

            var absolute = await tool.InvokeAsync(
                Call("repo.fs.list", new { root, path = "/etc" }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            absolute.Delta.Log.Should().Contain(l => l.Contains("REJECTED"));

            var traversal = await tool.InvokeAsync(
                Call("repo.fs.list", new { root, path = "../outside" }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            traversal.Delta.Log.Should().Contain(l => l.Contains("REJECTED"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RepoFsListTool_truncates_when_max_entries_exceeded()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-list-trunc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        for (var i = 0; i < 12; i++)
            await File.WriteAllTextAsync(Path.Combine(root, $"file{i:D2}.txt"), "x");

        try
        {
            var result = await new RepoFsListTool().InvokeAsync(
                Call("repo.fs.list", new { root, path = ".", max_entries = 5 }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);

            result.Delta.Log.Should().ContainSingle().Which.Should().Contain("(truncated)");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RepoFsListTool_honors_cancellation()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-list-cancel-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var act = () => new RepoFsListTool().InvokeAsync(
                Call("repo.fs.list", new { root, path = "." }),
                WorldSnapshot.ForRepo(root),
                cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RepoFsListTool_skips_build_artifacts_and_lists_files()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-list-skip-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "bin"));
        Directory.CreateDirectory(Path.Combine(root, "src"));
        await File.WriteAllTextAsync(Path.Combine(root, "src", "Program.cs"), "// code");
        await File.WriteAllTextAsync(Path.Combine(root, "bin", "junk.dll"), "x");
        try
        {
            var result = await new RepoFsListTool().InvokeAsync(
                Call("repo.fs.list", new { root, path = ".", recursive = true, max_entries = 10 }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);

            result.Delta.Log.Should().Contain(l => l.Contains("entries="));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RepoFsReadTool_rejects_invalid_paths_and_empty_arguments()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-read-reject-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var tool = new RepoFsReadTool();

            // Was: root = "" in the arguments must be rejected. The tools no longer accept a
            // root at all, so the equivalent invariant is that a snapshot WITHOUT a RepoRoot
            // fails closed rather than guessing one.
            var noSandboxRoot = await tool.InvokeAsync(
                Call("repo.fs.read", new { path = "x.txt" }),
                new WorldSnapshot(0, new Dictionary<string, object?>()),
                CancellationToken.None);
            noSandboxRoot.Delta.Log.Should().Contain(l => l.Contains("REJECTED"));

            var traversal = await tool.InvokeAsync(
                Call("repo.fs.read", new { root, path = "../secret.txt" }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            traversal.Delta.Log.Should().Contain(l => l.Contains("REJECTED"));

            var absolute = await tool.InvokeAsync(
                Call("repo.fs.read", new { root, path = "/etc/passwd" }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            absolute.Delta.Log.Should().Contain(l => l.Contains("REJECTED"));

            tool.Id.Should().Be("repo.fs.read");
            tool.Schema.Id.Should().Be("repo.fs.read");

            var emptyPath = await tool.InvokeAsync(
                Call("repo.fs.read", new { root, path = "   " }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            emptyPath.Delta.Log.Should().Contain(l => l.Contains("REJECTED"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RepoFsReadTool_honors_custom_max_bytes()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-read-max-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "big.txt"), new string('a', 200));
        try
        {
            var result = await new RepoFsReadTool().InvokeAsync(
                Call("repo.fs.read", new { root, path = "big.txt", max_bytes = 50 }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);

            result.Delta.Log.Should().ContainSingle().Which.Should().Contain("(truncated)");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }


    [Fact]
    public void RoslynAnalyzeTool_exposes_schema_and_id()
    {
        var tool = new RoslynAnalyzeTool();
        tool.Id.Should().Be("roslyn.analyze");
        tool.Schema.Id.Should().Be("roslyn.analyze");
        tool.Schema.Description.Should().Contain("Roslyn");
    }

    [Fact]
    public async Task RoslynAnalyzeTool_reports_individual_rule_violations()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-roslyn-viol-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        async Task<int> ViolationCount(string fileName, string source, object rules)
        {
            await File.WriteAllTextAsync(Path.Combine(root, fileName), source);
            var result = await new RoslynAnalyzeTool().InvokeAsync(
                Call("roslyn.analyze", new { root, files = new[] { fileName }, rules }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            var log = result.Delta.Log.First(l => l.StartsWith("roslyn:violations="));
            return int.Parse(log.Split('=')[1]);
        }

        try
        {
            (await ViolationCount("Ns.cs", """
                namespace Wrong;
                /// <summary>Ns.</summary>
                public sealed class Ns { }
                """, new { requiredNamespace = "Expected", requireFileScopedNamespace = true })).Should().BeGreaterThan(0);

            (await ViolationCount("BlockNs.cs", """
                namespace Blocked {
                    /// <summary>Block ns.</summary>
                    public sealed class BlockNs { }
                }
                """, new { requireFileScopedNamespace = true })).Should().BeGreaterThan(0);

            (await ViolationCount("NoClass.cs", "// empty", new { })).Should().BeGreaterThan(0);

            (await ViolationCount("Name.cs", """
                namespace Expected;
                /// <summary>Other.</summary>
                public sealed class Other { }
                """, new { requiredClassName = "ExpectedName", requireFileScopedNamespace = true })).Should().BeGreaterThan(0);

            (await ViolationCount("Internal.cs", """
                namespace Expected;
                /// <summary>Internal.</summary>
                sealed class Internal { }
                """, new { requirePublic = true, requireFileScopedNamespace = true })).Should().BeGreaterThan(0);

            (await ViolationCount("Open.cs", """
                namespace Expected;
                /// <summary>Open.</summary>
                public class Open { }
                """, new { requireSealed = true, requireFileScopedNamespace = true })).Should().BeGreaterThan(0);

            (await ViolationCount("Base.cs", """
                namespace Expected;
                /// <summary>Base.</summary>
                public sealed class Base { }
                """, new { requiredBaseType = "MissingBase", requireFileScopedNamespace = true })).Should().BeGreaterThan(0);

            (await ViolationCount("Cmd.cs", """
                namespace Expected;
                /// <summary>Cmd.</summary>
                public sealed class Cmd : BaseCmd
                {
                    public Cmd() : base("wrong-name") { }
                }
                /// <summary>Base cmd.</summary>
                public class BaseCmd
                {
                    /// <summary>Base cmd.</summary>
                    /// <param name="name">Name.</param>
                    protected BaseCmd(string name) { }
                }
                """, new { requiredCommandName = "right-name", requireFileScopedNamespace = true })).Should().BeGreaterThan(0);

            var missing = await new RoslynAnalyzeTool().InvokeAsync(
                Call("roslyn.analyze", new { root, files = new[] { "ghost.cs" }, rules = new { } }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            missing.Delta.Log.Should().Contain(l => l.Contains("violations=1"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }




    [Fact]
    public async Task RepoFsListTool_reports_enumeration_failure_for_unreadable_subdirectory()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
            return;

        var root = Path.Combine(Path.GetTempPath(), "ashlar-list-locked-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var locked = Path.Combine(root, "locked");
        Directory.CreateDirectory(locked);
        await File.WriteAllTextAsync(Path.Combine(locked, "secret.txt"), "x");

        try
        {
            File.SetUnixFileMode(locked, UnixFileMode.None);

            // Root bypasses DAC permission checks, so a mode-000 directory is still
            // enumerable and this test's premise cannot hold. The dev container runs as root
            // (scripts/handoff/devbox.sh, deliberately, to avoid a UID mismatch on the bind
            // mount), so this has been failing there unnoticed — the host cannot execute
            // tests at all, and nothing ran the full Tests.Kernel suite until the game-layer
            // extraction. Probe for the premise rather than assuming it.
            //
            // NB: bails with `return`, matching the OS guard above, which xUnit reports as
            // PASSED rather than skipped. A green run under root is not evidence this path
            // works. Same treatment as
            // the equivalent write-permission test in the extracted game layer.
            if (CanEnumerate(locked))
            {
                return;
            }

            var result = await new RepoFsListTool().InvokeAsync(
                Call("repo.fs.list", new { root, path = ".", recursive = true }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);

            result.Delta.Log.Should().Contain(l => l.Contains("FAILED"));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                try { File.SetUnixFileMode(locked, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute); } catch { /* best effort */ }
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RepoFsReadTool_honors_cancellation()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-read-cancel-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "note.txt"), "hello");
        try
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var act = () => new RepoFsReadTool().InvokeAsync(
                Call("repo.fs.read", new { root, path = "note.txt" }),
                WorldSnapshot.ForRepo(root),
                cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }



    [Fact]
    public async Task RepoFsReadTool_exposes_schema_and_reads_successfully()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-read-ok-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "hello.txt"), "hello");
        try
        {
            var tool = new RepoFsReadTool();
            tool.Id.Should().Be("repo.fs.read");
            tool.Schema.Id.Should().Be("repo.fs.read");

            var result = await tool.InvokeAsync(
                Call("repo.fs.read", new { root, path = "hello.txt" }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);

            result.Delta.Log.Should().ContainSingle().Which.Should().Contain("bytes=5");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static async Task RunDotnetAsync(string workingDirectory, string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo("dotnet", arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        }) ?? throw new InvalidOperationException("Failed to start dotnet.");

        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        process.ExitCode.Should().Be(0, $"dotnet {arguments} should succeed. stdout={stdout} stderr={stderr}");
    }
}
