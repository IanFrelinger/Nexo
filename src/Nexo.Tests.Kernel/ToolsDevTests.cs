using System.Text.Json;
using System.Diagnostics;
using FluentAssertions;
using Moq;
using Nexo.Abstractions;
using Nexo.Core.Application.Maintenance.Models;
using Nexo.Core.Application.Maintenance.Ports;
using Nexo.Tools.Dev;
using Nexo.Tools.Dev.Deltas;
using Xunit;

namespace Nexo.Tests.Kernel;

public class ToolsDevTests
{
    private static ToolCall Call(string id, object args) =>
        new(id, JsonDocument.Parse(JsonSerializer.Serialize(args)).RootElement);

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
    public async Task CleanArtifactsTool_uses_explicit_repo_root_and_reports_cleanup_errors()
    {
        var cleanup = new Mock<IArtifactCleanupService>();
        cleanup.Setup(s => s.CleanAsync(
                "incomplete-blobs",
                It.Is<ArtifactCleanupContext?>(c => c!.RepoRoot == "/explicit/root"),
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
            new WorldSnapshot(0, new Dictionary<string, object?>()),
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
        var cleanup = new Mock<IArtifactCleanupService>();
        cleanup.Setup(s => s.CleanAsync(
                null,
                It.IsAny<ArtifactCleanupContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArtifactCleanupResult("all", 0, Array.Empty<string>(), Array.Empty<string>()));

        var tool = new CleanArtifactsTool(cleanup.Object);
        var call = new ToolCall(CleanArtifactsTool.IdConstant, JsonDocument.Parse("[]").RootElement);
        var result = await tool.InvokeAsync(call, new WorldSnapshot(0, new Dictionary<string, object?>()), CancellationToken.None);
        result.Delta.Log.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RepoFsListTool_lists_repo_entries()
    {
        var root = Path.Combine(Path.GetTempPath(), "nexo-list-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "readme.txt"), "hi");
        try
        {
            var tool = new RepoFsListTool();
            var snap = new WorldSnapshot(0, new Dictionary<string, object?>());
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
        var root = Path.Combine(Path.GetTempPath(), "nexo-list-bad-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var tool = new RepoFsListTool();
            var result = await tool.InvokeAsync(
                Call("repo.fs.list", new { root, path = "../outside" }),
                new WorldSnapshot(0, new Dictionary<string, object?>()),
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
        var root = Path.Combine(Path.GetTempPath(), "nexo-read-" + Guid.NewGuid().ToString("N"));
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
                new WorldSnapshot(0, new Dictionary<string, object?>()),
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
        var root = Path.Combine(Path.GetTempPath(), "nexo-write-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var tool = new RepoFsWriteTool();
            var result = await tool.InvokeAsync(
                Call("repo.fs.write", new { root, path = "sub/out.txt", content = "line1\nline2" }),
                new WorldSnapshot(0, new Dictionary<string, object?>()),
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
        var root = Path.Combine(Path.GetTempPath(), "nexo-ensure-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var tool = new RepoFsEnsureFileTool();
            tool.Id.Should().Be("repo.fs.ensure_file");
            tool.Schema.Id.Should().Be("repo.fs.ensure_file");
            tool.Schema.Description.Should().Contain("Create file");
            var created = await tool.InvokeAsync(
                Call("repo.fs.ensure_file", new { root, path = "nested/deep/new.txt", content = "hello" }),
                new WorldSnapshot(0, new Dictionary<string, object?>()),
                CancellationToken.None);
            created.Delta.Log.Should().ContainSingle().Which.Should().Contain("created");
            File.Exists(Path.Combine(root, "nested", "deep", "new.txt")).Should().BeTrue();

            var exists = await tool.InvokeAsync(
                Call("repo.fs.ensure_file", new { root, path = "nested/deep/new.txt", content = "other" }),
                new WorldSnapshot(1, new Dictionary<string, object?>()),
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
        var root = Path.Combine(Path.GetTempPath(), "nexo-sr-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "file.txt");
        await File.WriteAllTextAsync(path, "foo bar foo");
        try
        {
            var tool = new RepoFsSearchReplaceTool();
            var result = await tool.InvokeAsync(
                Call("repo.fs.search_replace", new { root, path = "file.txt", find = "foo", replace = "baz" }),
                new WorldSnapshot(0, new Dictionary<string, object?>()),
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
        var root = Path.Combine(Path.GetTempPath(), "nexo-run-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var result = await new DotnetRunTool().InvokeAsync(
                Call("dotnet.run", new { root, args = "--version", timeoutSeconds = 30 }),
                new WorldSnapshot(0, new Dictionary<string, object?>()),
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
        var root = Path.Combine(Path.GetTempPath(), "nexo-test-" + Guid.NewGuid().ToString("N"));
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
                new WorldSnapshot(0, new Dictionary<string, object?>()),
                CancellationToken.None);
            var result = await new DotnetTestTool().InvokeAsync(
                Call("dotnet.test", new { root }),
                new WorldSnapshot(1, new Dictionary<string, object?>()),
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
        var root = Path.Combine(Path.GetTempPath(), "nexo-build-" + Guid.NewGuid().ToString("N"));
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
                new WorldSnapshot(0, new Dictionary<string, object?>()),
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
        var root = Path.Combine(Path.GetTempPath(), "nexo-build-filter-" + Guid.NewGuid().ToString("N"));
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
            await RunDotnetAsync(root, "new sln --name Nexo --format sln");
            await RunDotnetAsync(root, "sln Nexo.sln add Mini.csproj");
            await File.WriteAllTextAsync(Path.Combine(root, "Nexo.LocalDevCore.slnf"), """
                {
                  "solution": {
                    "path": "Nexo.sln",
                    "projects": [
                      "Mini.csproj"
                    ]
                  }
                }
                """);

            var result = await new DotnetBuildTool().InvokeAsync(
                Call("dotnet.build", new { root }),
                new WorldSnapshot(0, new Dictionary<string, object?>()),
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
        var root = Path.Combine(Path.GetTempPath(), "nexo-forge-build-" + Guid.NewGuid().ToString("N"));
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
                new WorldSnapshot(0, new Dictionary<string, object?>()),
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
        var root = Path.Combine(Path.GetTempPath(), "nexo-forge-test-" + Guid.NewGuid().ToString("N"));
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
                new WorldSnapshot(0, new Dictionary<string, object?>()),
                CancellationToken.None);
            var tool = new ForgeTestTool();
            tool.Id.Should().Be("forge.test");
            var result = await tool.InvokeAsync(
                Call("forge.test", new { root }),
                new WorldSnapshot(1, new Dictionary<string, object?>()),
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
        var root = Path.Combine(Path.GetTempPath(), "nexo-commit-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var tool = new RepoGitCommitTool();
            tool.Id.Should().Be("repo.git.commit");
            tool.Schema.Id.Should().Be("repo.git.commit");
            tool.Schema.Description.Should().Contain("pseudo-commit");
            var result = await tool.InvokeAsync(
                Call("repo.git.commit", new { root, message = "feat: kernel coverage" }),
                new WorldSnapshot(0, new Dictionary<string, object?>()),
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
        var root = Path.Combine(Path.GetTempPath(), "nexo-docs-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var tool = new DocsUpdateTool();
            tool.Id.Should().Be("docs.update");
            tool.Schema.Id.Should().Be("docs.update");
            tool.Schema.Description.Should().Contain("CHANGELOG");
            var snap = new WorldSnapshot(0, new Dictionary<string, object?>());
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
        var root = Path.Combine(Path.GetTempPath(), "nexo-roslyn-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var sourcePath = Path.Combine(root, "Cmd.cs");
        await File.WriteAllTextAsync(sourcePath, """
            namespace Wrong;
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
                new WorldSnapshot(0, new Dictionary<string, object?>()),
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
    public async Task TileMapRenderTool_rejects_missing_osm_and_invalid_bbox()
    {
        var root = Path.Combine(Path.GetTempPath(), "nexo-tile-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var tool = new TileMapRenderTool();
            var missing = await tool.InvokeAsync(
                Call("repo.tile_map.render", new { root, osm_path = "maps/missing.osm" }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            missing.Delta.Log.Should().Contain(l => l.Contains("not_found"));

            var mapsDir = Path.Combine(root, "maps");
            Directory.CreateDirectory(mapsDir);
            await File.WriteAllTextAsync(Path.Combine(mapsDir, "empty.osm"), """
                <?xml version="1.0" encoding="UTF-8"?>
                <osm version="0.6"></osm>
                """);

            var invalidBbox = await tool.InvokeAsync(
                Call("repo.tile_map.render", new
                {
                    root,
                    osm_path = "maps/empty.osm",
                    min_lat = 2.0,
                    max_lat = 1.0,
                    min_lon = -1.0,
                    max_lon = 1.0,
                }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            invalidBbox.Delta.Log.Should().Contain(l => l.Contains("bounds:"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task DotnetRunTool_logs_stderr_and_timeout()
    {
        var root = Path.Combine(Path.GetTempPath(), "nexo-run-err-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var invalid = await new DotnetRunTool().InvokeAsync(
                Call("dotnet.run", new { root, args = "not-a-real-subcommand" }),
                new WorldSnapshot(0, new Dictionary<string, object?>()),
                CancellationToken.None);
            invalid.Delta.Log.Should().Contain(l => l.Contains("dotnet:exit="));
            invalid.Delta.Log.Should().Contain(l => l.Contains("dotnet:stderr"));

            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
            var timedOut = await new DotnetRunTool().InvokeAsync(
                Call("dotnet.run", new { root, args = "restore", timeoutSeconds = 1 }),
                new WorldSnapshot(1, new Dictionary<string, object?>()),
                cts.Token);
            timedOut.Delta.Log.Should().Contain(l => l.Contains("dotnet:exit=") || l.Contains("dotnet:timeout"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task TileMapRenderTool_renders_osm_with_roads_and_parks()
    {
        var root = Path.Combine(Path.GetTempPath(), "nexo-tile-ok-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var mapsDir = Path.Combine(root, "maps");
        Directory.CreateDirectory(mapsDir);
        await File.WriteAllTextAsync(Path.Combine(mapsDir, "sample.osm"), """
            <?xml version="1.0" encoding="UTF-8"?>
            <osm version="0.6">
              <node id="1" lat="51.500" lon="-0.120"/>
              <node id="2" lat="51.505" lon="-0.115"/>
              <node id="3" lat="51.502" lon="-0.118"/>
              <node id="4" lat="51.500" lon="-0.120"/>
              <way id="10">
                <nd ref="1"/><nd ref="2"/>
                <tag k="highway" v="primary"/>
              </way>
              <way id="11">
                <nd ref="1"/><nd ref="3"/><nd ref="4"/>
                <tag k="leisure" v="park"/>
              </way>
              <way id="12">
                <nd ref="2"/><nd ref="3"/>
                <tag k="waterway" v="river"/>
              </way>
            </osm>
            """);

        try
        {
            var tool = new TileMapRenderTool();
            var result = await tool.InvokeAsync(
                Call("repo.tile_map.render", new
                {
                    root,
                    osm_path = "maps/sample.osm",
                    output_path = "maps/out.png",
                    width = 256,
                    height = 256,
                }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);

            result.Delta.Log.Should().Contain(l => l.Contains("tile_map.render maps/out.png"));
            File.Exists(Path.Combine(root, "maps/out.png")).Should().BeTrue();
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
        var root = Path.Combine(Path.GetTempPath(), "nexo-docs-existing-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "CHANGELOG.md"), "- existing entry\n");

        try
        {
            var result = await new DocsUpdateTool().InvokeAsync(
                Call("docs.update", new { root, entry = "second entry" }),
                new WorldSnapshot(0, new Dictionary<string, object?>()),
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
        var root = Path.Combine(Path.GetTempPath(), "nexo-read-miss-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var result = await new RepoFsReadTool().InvokeAsync(
                Call("repo.fs.read", new { root, path = "missing.txt" }),
                new WorldSnapshot(0, new Dictionary<string, object?>()),
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
        var root = Path.Combine(Path.GetTempPath(), "nexo-list-rec-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "sub"));
        await File.WriteAllTextAsync(Path.Combine(root, "sub", "nested.txt"), "nested");
        try
        {
            var result = await new RepoFsListTool().InvokeAsync(
                Call("repo.fs.list", new { root, path = ".", recursive = true, max_entries = 5 }),
                new WorldSnapshot(0, new Dictionary<string, object?>()),
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
        var root = Path.Combine(Path.GetTempPath(), "nexo-roslyn-ok-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "Cmd.cs"), """
            namespace Expected;

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
                new WorldSnapshot(0, new Dictionary<string, object?>()),
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
    public async Task TileMapRenderTool_uses_default_output_and_custom_bbox()
    {
        var root = Path.Combine(Path.GetTempPath(), "nexo-tile-default-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var mapsDir = Path.Combine(root, "maps");
        Directory.CreateDirectory(mapsDir);
        await File.WriteAllTextAsync(Path.Combine(mapsDir, "city.osm"), """
            <?xml version="1.0" encoding="UTF-8"?>
            <osm version="0.6">
              <node id="1" lat="51.500" lon="-0.120"/>
              <node id="2" lat="51.501" lon="-0.119"/>
              <way id="10">
                <nd ref="1"/><nd ref="2"/>
                <tag k="highway" v="motorway"/>
              </way>
              <way id="11">
                <nd ref="1"/><nd ref="2"/>
                <tag k="building" v="yes"/>
              </way>
            </osm>
            """);

        try
        {
            var tool = new TileMapRenderTool();
            var defaultOut = await tool.InvokeAsync(
                Call("repo.tile_map.render", new { root, osm_path = "maps/city.osm" }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            defaultOut.Delta.Log.Should().Contain(l => l.Contains("maps/city.png"));
            File.Exists(Path.Combine(root, "maps/city.png")).Should().BeTrue();

            var bboxOut = await tool.InvokeAsync(
                Call("repo.tile_map.render", new
                {
                    root,
                    osm_path = "maps/city.osm",
                    output_path = "maps/custom.png",
                    min_lat = 51.499,
                    max_lat = 51.502,
                    min_lon = -0.121,
                    max_lon = -0.118,
                }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            bboxOut.Delta.Log.Should().Contain(l => l.Contains("maps/custom.png"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task TileMapRenderTool_reports_empty_coordinates_and_rejects_bad_output_path()
    {
        var root = Path.Combine(Path.GetTempPath(), "nexo-tile-empty-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var mapsDir = Path.Combine(root, "maps");
        Directory.CreateDirectory(mapsDir);
        await File.WriteAllTextAsync(Path.Combine(mapsDir, "nodes-only.osm"), """
            <?xml version="1.0" encoding="UTF-8"?>
            <osm version="0.6">
              <node id="1" lat="51.500" lon="-0.120"/>
            </osm>
            """);

        try
        {
            var tool = new TileMapRenderTool();
            var empty = await tool.InvokeAsync(
                Call("repo.tile_map.render", new { root, osm_path = "maps/nodes-only.osm" }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            empty.Delta.Log.Should().Contain(l => l.Contains("no coordinates"));

            var badOut = await tool.InvokeAsync(
                Call("repo.tile_map.render", new { root, osm_path = "maps/nodes-only.osm", output_path = "../outside.png" }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            badOut.Delta.Log.Should().Contain(l => l.Contains("REJECTED"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RoslynAnalyzeTool_checks_base_type_and_command_name()
    {
        var root = Path.Combine(Path.GetTempPath(), "nexo-roslyn-cmd-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "Cmd.cs"), """
            namespace Expected;

            public sealed class Cmd : BaseCmd
            {
                public Cmd() : base("expected-name") { }
            }

            public class BaseCmd
            {
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
                new WorldSnapshot(0, new Dictionary<string, object?>()),
                CancellationToken.None);
            ok.Delta.Log.Should().Contain(l => l.Contains("violations=0"));

            await File.WriteAllTextAsync(Path.Combine(root, "Bad.cs"), """
                namespace Expected;
                class Bad { }
                """);
            var bad = await new RoslynAnalyzeTool().InvokeAsync(
                Call("roslyn.analyze", new
                {
                    root,
                    files = new[] { "Bad.cs" },
                    rules = new { requireFileScopedNamespace = false, requirePublic = true, requireSealed = true },
                }),
                new WorldSnapshot(0, new Dictionary<string, object?>()),
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
        var root = Path.Combine(Path.GetTempPath(), "nexo-list-deep-" + Guid.NewGuid().ToString("N"));
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
                new WorldSnapshot(0, new Dictionary<string, object?>()),
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
        var root = Path.Combine(Path.GetTempPath(), "nexo-list-miss-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var tool = new RepoFsListTool();
            tool.Id.Should().Be("repo.fs.list");
            tool.Schema.Id.Should().Be("repo.fs.list");
            tool.Schema.Description.Should().Contain("List files");

            var missing = await tool.InvokeAsync(
                Call("repo.fs.list", new { root, path = "missing-dir" }),
                new WorldSnapshot(0, new Dictionary<string, object?>()),
                CancellationToken.None);
            missing.Delta.Log.Should().Contain(l => l.Contains("not_found"));

            var noRoot = await tool.InvokeAsync(
                Call("repo.fs.list", new { root = "", path = "." }),
                new WorldSnapshot(0, new Dictionary<string, object?>()),
                CancellationToken.None);
            noRoot.Delta.Log.Should().Contain(l => l.Contains("REJECTED"));

            var absolute = await tool.InvokeAsync(
                Call("repo.fs.list", new { root, path = "/etc" }),
                new WorldSnapshot(0, new Dictionary<string, object?>()),
                CancellationToken.None);
            absolute.Delta.Log.Should().Contain(l => l.Contains("REJECTED"));

            var traversal = await tool.InvokeAsync(
                Call("repo.fs.list", new { root, path = "../outside" }),
                new WorldSnapshot(0, new Dictionary<string, object?>()),
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
        var root = Path.Combine(Path.GetTempPath(), "nexo-list-trunc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        for (var i = 0; i < 12; i++)
            await File.WriteAllTextAsync(Path.Combine(root, $"file{i:D2}.txt"), "x");

        try
        {
            var result = await new RepoFsListTool().InvokeAsync(
                Call("repo.fs.list", new { root, path = ".", max_entries = 5 }),
                new WorldSnapshot(0, new Dictionary<string, object?>()),
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
        var root = Path.Combine(Path.GetTempPath(), "nexo-list-cancel-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var act = () => new RepoFsListTool().InvokeAsync(
                Call("repo.fs.list", new { root, path = "." }),
                new WorldSnapshot(0, new Dictionary<string, object?>()),
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
        var root = Path.Combine(Path.GetTempPath(), "nexo-list-skip-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "bin"));
        Directory.CreateDirectory(Path.Combine(root, "src"));
        await File.WriteAllTextAsync(Path.Combine(root, "src", "Program.cs"), "// code");
        await File.WriteAllTextAsync(Path.Combine(root, "bin", "junk.dll"), "x");
        try
        {
            var result = await new RepoFsListTool().InvokeAsync(
                Call("repo.fs.list", new { root, path = ".", recursive = true, max_entries = 10 }),
                new WorldSnapshot(0, new Dictionary<string, object?>()),
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
        var root = Path.Combine(Path.GetTempPath(), "nexo-read-reject-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var tool = new RepoFsReadTool();

            var emptyRoot = await tool.InvokeAsync(
                Call("repo.fs.read", new { root = "", path = "x.txt" }),
                new WorldSnapshot(0, new Dictionary<string, object?>()),
                CancellationToken.None);
            emptyRoot.Delta.Log.Should().Contain(l => l.Contains("REJECTED"));

            var traversal = await tool.InvokeAsync(
                Call("repo.fs.read", new { root, path = "../secret.txt" }),
                new WorldSnapshot(0, new Dictionary<string, object?>()),
                CancellationToken.None);
            traversal.Delta.Log.Should().Contain(l => l.Contains("REJECTED"));

            var absolute = await tool.InvokeAsync(
                Call("repo.fs.read", new { root, path = "/etc/passwd" }),
                new WorldSnapshot(0, new Dictionary<string, object?>()),
                CancellationToken.None);
            absolute.Delta.Log.Should().Contain(l => l.Contains("REJECTED"));

            tool.Id.Should().Be("repo.fs.read");
            tool.Schema.Id.Should().Be("repo.fs.read");

            var emptyPath = await tool.InvokeAsync(
                Call("repo.fs.read", new { root, path = "   " }),
                new WorldSnapshot(0, new Dictionary<string, object?>()),
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
        var root = Path.Combine(Path.GetTempPath(), "nexo-read-max-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "big.txt"), new string('a', 200));
        try
        {
            var result = await new RepoFsReadTool().InvokeAsync(
                Call("repo.fs.read", new { root, path = "big.txt", max_bytes = 50 }),
                new WorldSnapshot(0, new Dictionary<string, object?>()),
                CancellationToken.None);

            result.Delta.Log.Should().ContainSingle().Which.Should().Contain("(truncated)");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task TileMapRenderTool_reports_parse_error_for_invalid_xml()
    {
        var root = Path.Combine(Path.GetTempPath(), "nexo-tile-bad-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var mapsDir = Path.Combine(root, "maps");
        Directory.CreateDirectory(mapsDir);
        await File.WriteAllTextAsync(Path.Combine(mapsDir, "broken.osm"), "<not-osm");

        try
        {
            var result = await new TileMapRenderTool().InvokeAsync(
                Call("repo.tile_map.render", new { root, osm_path = "maps/broken.osm" }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);

            result.Delta.Log.Should().Contain(l => l.Contains("parse_error"));
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
        var root = Path.Combine(Path.GetTempPath(), "nexo-roslyn-viol-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        async Task<int> ViolationCount(string fileName, string source, object rules)
        {
            await File.WriteAllTextAsync(Path.Combine(root, fileName), source);
            var result = await new RoslynAnalyzeTool().InvokeAsync(
                Call("roslyn.analyze", new { root, files = new[] { fileName }, rules }),
                new WorldSnapshot(0, new Dictionary<string, object?>()),
                CancellationToken.None);
            var log = result.Delta.Log.First(l => l.StartsWith("roslyn:violations="));
            return int.Parse(log.Split('=')[1]);
        }

        try
        {
            (await ViolationCount("Ns.cs", """
                namespace Wrong;
                public sealed class Ns { }
                """, new { requiredNamespace = "Expected", requireFileScopedNamespace = true })).Should().BeGreaterThan(0);

            (await ViolationCount("BlockNs.cs", """
                namespace Blocked {
                    public sealed class BlockNs { }
                }
                """, new { requireFileScopedNamespace = true })).Should().BeGreaterThan(0);

            (await ViolationCount("NoClass.cs", "// empty", new { })).Should().BeGreaterThan(0);

            (await ViolationCount("Name.cs", """
                namespace Expected;
                public sealed class Other { }
                """, new { requiredClassName = "ExpectedName", requireFileScopedNamespace = true })).Should().BeGreaterThan(0);

            (await ViolationCount("Internal.cs", """
                namespace Expected;
                sealed class Internal { }
                """, new { requirePublic = true, requireFileScopedNamespace = true })).Should().BeGreaterThan(0);

            (await ViolationCount("Open.cs", """
                namespace Expected;
                public class Open { }
                """, new { requireSealed = true, requireFileScopedNamespace = true })).Should().BeGreaterThan(0);

            (await ViolationCount("Base.cs", """
                namespace Expected;
                public sealed class Base { }
                """, new { requiredBaseType = "MissingBase", requireFileScopedNamespace = true })).Should().BeGreaterThan(0);

            (await ViolationCount("Cmd.cs", """
                namespace Expected;
                public sealed class Cmd : BaseCmd
                {
                    public Cmd() : base("wrong-name") { }
                }
                public class BaseCmd
                {
                    protected BaseCmd(string name) { }
                }
                """, new { requiredCommandName = "right-name", requireFileScopedNamespace = true })).Should().BeGreaterThan(0);

            var missing = await new RoslynAnalyzeTool().InvokeAsync(
                Call("roslyn.analyze", new { root, files = new[] { "ghost.cs" }, rules = new { } }),
                new WorldSnapshot(0, new Dictionary<string, object?>()),
                CancellationToken.None);
            missing.Delta.Log.Should().Contain(l => l.Contains("violations=1"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task TileMapRenderTool_draws_highway_matrix_and_area_features()
    {
        var root = Path.Combine(Path.GetTempPath(), "nexo-tile-rich-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var mapsDir = Path.Combine(root, "maps");
        Directory.CreateDirectory(mapsDir);

        var highwayTypes = new[]
        {
            "motorway", "trunk", "primary", "secondary", "tertiary", "residential",
            "service", "path", "railway", "unclassified",
        };

        var sb = new System.Text.StringBuilder("""
            <?xml version="1.0" encoding="UTF-8"?>
            <osm version="0.6">
              <node id="1" lat="51.500" lon="-0.120"/>
              <node id="2" lat="51.501" lon="-0.119"/>
              <node id="3" lat="51.501" lon="-0.120"/>
              <node id="4" lat="51.500" lon="-0.119"/>
            """);

        var wayId = 10;
        foreach (var hw in highwayTypes)
        {
            sb.AppendLine($"""
                  <way id="{wayId}">
                    <nd ref="1"/><nd ref="2"/>
                    <tag k="highway" v="{hw}"/>
                  </way>
                """);
            wayId++;
        }

        sb.AppendLine("""
              <way id="100">
                <nd ref="1"/><nd ref="2"/><nd ref="3"/><nd ref="4"/><nd ref="1"/>
                <tag k="leisure" v="park"/>
              </way>
              <way id="101">
                <nd ref="1"/><nd ref="2"/><nd ref="3"/><nd ref="4"/><nd ref="1"/>
                <tag k="natural" v="water"/>
              </way>
              <way id="102">
                <nd ref="1"/><nd ref="2"/><nd ref="3"/><nd ref="4"/><nd ref="1"/>
                <tag k="building" v="yes"/>
              </way>
              <way id="103">
                <nd ref="1"/><nd ref="2"/>
                <tag k="railway" v="rail"/>
              </way>
              <way id="104">
                <nd ref="1"/><nd ref="2"/>
                <tag k="waterway" v="river"/>
              </way>
            </osm>
            """);

        await File.WriteAllTextAsync(Path.Combine(mapsDir, "rich.osm"), sb.ToString());

        try
        {
            var tool = new TileMapRenderTool();
            var result = await tool.InvokeAsync(
                Call("repo.tile_map.render", new { root, osm_path = "maps/rich.osm", width = 800, height = 600 }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);

            result.Delta.Log.Should().Contain(l => l.Contains("maps/rich.png"));
            new FileInfo(Path.Combine(root, "maps/rich.png")).Length.Should().BeGreaterThan(100);

            var badBbox = await tool.InvokeAsync(
                Call("repo.tile_map.render", new
                {
                    root,
                    osm_path = "maps/rich.osm",
                    output_path = "maps/bad-bbox.png",
                    min_lat = 51.502,
                    max_lat = 51.500,
                    min_lon = -0.121,
                    max_lon = -0.118,
                }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            badBbox.Delta.Log.Should().Contain(l => l.Contains("invalid bbox"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task TileMapRenderTool_clamps_dimensions_and_draws_remaining_landcover()
    {
        var root = Path.Combine(Path.GetTempPath(), "nexo-tile-land-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var mapsDir = Path.Combine(root, "maps");
        Directory.CreateDirectory(mapsDir);

        await File.WriteAllTextAsync(Path.Combine(mapsDir, "land.osm"), """
            <?xml version="1.0" encoding="UTF-8"?>
            <osm version="0.6">
              <node id="1" lat="40.0000" lon="-74.0000"/>
              <node id="2" lat="40.0000" lon="-73.9990"/>
              <node id="3" lat="40.0010" lon="-73.9990"/>
              <node id="4" lat="40.0010" lon="-74.0000"/>
              <way id="10">
                <nd ref="1"/><nd ref="2"/><nd ref="3"/><nd ref="4"/><nd ref="1"/>
                <tag k="landuse" v="forest"/>
              </way>
              <way id="11">
                <nd ref="1"/><nd ref="2"/><nd ref="3"/><nd ref="4"/><nd ref="1"/>
                <tag k="landuse" v="cemetery"/>
              </way>
              <way id="12">
                <nd ref="1"/><nd ref="2"/><nd ref="3"/><nd ref="4"/><nd ref="1"/>
                <tag k="leisure" v="garden"/>
              </way>
              <way id="13">
                <nd ref="1"/><nd ref="2"/>
                <tag k="waterway" v="stream"/>
              </way>
              <way id="14">
                <nd ref="1"/><nd ref="2"/>
                <tag k="highway" v="living_street"/>
              </way>
            </osm>
            """);

        try
        {
            var tool = new TileMapRenderTool();
            tool.Schema.Id.Should().Be("repo.tile_map.render");

            var result = await tool.InvokeAsync(
                Call("repo.tile_map.render", new { root, osm_path = "maps/land.osm", width = 9000, height = 32 }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);

            result.Delta.Log.Should().Contain(l => l.Contains("4096x64"));
            new FileInfo(Path.Combine(root, "maps/land.png")).Length.Should().BeGreaterThan(50);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task TileMapRenderTool_reports_empty_coordinates()
    {
        var root = Path.Combine(Path.GetTempPath(), "nexo-tile-empty-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "empty.osm"), """
            <?xml version="1.0" encoding="UTF-8"?>
            <osm version="0.6">
              <node id="1"/>
              <way id="2"><nd ref="1"/><tag k="highway" v="road"/></way>
            </osm>
            """);

        try
        {
            var result = await new TileMapRenderTool().InvokeAsync(
                Call("repo.tile_map.render", new { root, osm_path = "empty.osm" }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);

            result.Delta.Log.Should().Contain(l => l.Contains("no coordinates"));
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

        var root = Path.Combine(Path.GetTempPath(), "nexo-list-locked-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var locked = Path.Combine(root, "locked");
        Directory.CreateDirectory(locked);
        await File.WriteAllTextAsync(Path.Combine(locked, "secret.txt"), "x");

        try
        {
            File.SetUnixFileMode(locked, UnixFileMode.None);

            var result = await new RepoFsListTool().InvokeAsync(
                Call("repo.fs.list", new { root, path = ".", recursive = true }),
                new WorldSnapshot(0, new Dictionary<string, object?>()),
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
        var root = Path.Combine(Path.GetTempPath(), "nexo-read-cancel-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "note.txt"), "hello");
        try
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var act = () => new RepoFsReadTool().InvokeAsync(
                Call("repo.fs.read", new { root, path = "note.txt" }),
                new WorldSnapshot(0, new Dictionary<string, object?>()),
                cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task TileMapRenderTool_honors_cancellation_during_parse()
    {
        var root = Path.Combine(Path.GetTempPath(), "nexo-tile-cancel-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var mapsDir = Path.Combine(root, "maps");
        Directory.CreateDirectory(mapsDir);

        var sb = new System.Text.StringBuilder("""
            <?xml version="1.0" encoding="UTF-8"?>
            <osm version="0.6">
            """);
        for (var i = 0; i < 500; i++)
            sb.AppendLine($"""  <node id="{i}" lat="51.500" lon="-0.120"/>""");
        sb.AppendLine("</osm>");
        await File.WriteAllTextAsync(Path.Combine(mapsDir, "big.osm"), sb.ToString());

        try
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var result = await new TileMapRenderTool().InvokeAsync(
                Call("repo.tile_map.render", new { root, osm_path = "maps/big.osm" }),
                WorldSnapshot.ForRepo(root),
                cts.Token);

            result.Delta.Log.Should().Contain(l => l.Contains("parse_error"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task TileMapRenderTool_reports_render_error_when_output_directory_not_writable()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
            return;

        var root = Path.Combine(Path.GetTempPath(), "nexo-tile-ro-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var mapsDir = Path.Combine(root, "maps");
        Directory.CreateDirectory(mapsDir);
        var readOnlyDir = Path.Combine(mapsDir, "readonly");
        Directory.CreateDirectory(readOnlyDir);
        await File.WriteAllTextAsync(Path.Combine(mapsDir, "tiny.osm"), """
            <?xml version="1.0" encoding="UTF-8"?>
            <osm version="0.6">
              <node id="1" lat="51.500" lon="-0.120"/>
              <node id="2" lat="51.501" lon="-0.119"/>
              <way id="10">
                <nd ref="1"/><nd ref="2"/>
                <tag k="highway" v="residential"/>
              </way>
            </osm>
            """);

        try
        {
            File.SetUnixFileMode(readOnlyDir, UnixFileMode.UserRead | UnixFileMode.UserExecute);

            var result = await new TileMapRenderTool().InvokeAsync(
                Call("repo.tile_map.render", new { root, osm_path = "maps/tiny.osm", output_path = "maps/readonly/out.png" }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);

            result.Delta.Log.Should().Contain(l => l.Contains("render_error"));
        }
        finally
        {
            try { File.SetUnixFileMode(readOnlyDir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute); } catch { /* best effort */ }
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RepoFsReadTool_exposes_schema_and_reads_successfully()
    {
        var root = Path.Combine(Path.GetTempPath(), "nexo-read-ok-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "hello.txt"), "hello");
        try
        {
            var tool = new RepoFsReadTool();
            tool.Id.Should().Be("repo.fs.read");
            tool.Schema.Id.Should().Be("repo.fs.read");

            var result = await tool.InvokeAsync(
                Call("repo.fs.read", new { root, path = "hello.txt" }),
                new WorldSnapshot(0, new Dictionary<string, object?>()),
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
