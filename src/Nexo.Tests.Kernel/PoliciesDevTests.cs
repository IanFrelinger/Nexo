using System.Text.Json;
using FluentAssertions;
using Moq;
using Nexo.Abstractions;
using Nexo.Policies.Dev;
using Xunit;

namespace Nexo.Tests.Kernel;

public class BuildMustPassBeforeCommitTests
{
    private static ToolCall Commit() => new("repo.git.commit", JsonDocument.Parse("{}").RootElement);
    private static ToolCall Other() => new("repo.fs.write", JsonDocument.Parse("{}").RootElement);

    [Fact]
    public void Allows_non_commit_calls_without_state()
    {
        var p = new BuildMustPassBeforeCommit();
        p.Approve(Other(), new WorldSnapshot(0, new Dictionary<string, object?>()), out var reason).Should().BeTrue();
        reason.Should().Be("OK");
    }

    [Fact]
    public void Rejects_commit_when_build_or_tests_missing()
    {
        var p = new BuildMustPassBeforeCommit();
        p.Approve(Commit(), new WorldSnapshot(0, new Dictionary<string, object?>()), out var r1).Should().BeFalse();
        r1.Should().Contain("build/tests not green");

        p.Approve(Commit(), new WorldSnapshot(0, new Dictionary<string, object?>
        {
            ["LastBuildOk"] = true,
        }), out _).Should().BeFalse();

        p.Approve(Commit(), new WorldSnapshot(0, new Dictionary<string, object?>
        {
            ["LastBuildOk"] = false,
            ["LastTestsOk"] = true,
        }), out _).Should().BeFalse();
    }

    [Fact]
    public void Allows_commit_when_build_and_tests_pass()
    {
        var p = new BuildMustPassBeforeCommit();
        var snap = new WorldSnapshot(0, new Dictionary<string, object?>
        {
            ["LastBuildOk"] = true,
            ["LastTestsOk"] = true,
        });
        p.Approve(Commit(), snap, out var reason).Should().BeTrue();
        reason.Should().Be("OK");
    }
}

[Collection("EnvironmentSensitive")]
public class BuildTestBudgetTests
{
    private static ToolCall Call(string id) => new(id, JsonDocument.Parse("{}").RootElement);
    private static readonly WorldSnapshot Snap = new(0, new Dictionary<string, object?>());

    [Fact]
    public void Default_constructor_uses_constants_when_env_unset()
    {
        EnvVar.Run("NEXO_BUILD_BUDGET", null, () =>
        EnvVar.Run("NEXO_TEST_BUDGET", null, () =>
        {
            var p = new BuildTestBudget();
            p.Approve(Call("dotnet.build"), Snap, out var r).Should().BeTrue();
            r.Should().Be("OK");
            p.Approve(Call("dotnet.build"), Snap, out var r2).Should().BeFalse();
            r2.Should().Contain("Build budget exceeded");
        }));
    }

    [Fact]
    public void Reads_environment_variables_when_set()
    {
        EnvVar.Run("NEXO_BUILD_BUDGET", " 3 ", () =>
        {
            var p = new BuildTestBudget(testBudget: 99);
            p.Approve(Call("dotnet.build"), Snap, out _).Should().BeTrue();
            p.Approve(Call("dotnet.build"), Snap, out _).Should().BeTrue();
            p.Approve(Call("dotnet.build"), Snap, out _).Should().BeTrue();
            p.Approve(Call("dotnet.build"), Snap, out var reason).Should().BeFalse();
            reason.Should().Contain("Build budget exceeded");
        });
    }

    [Fact]
    public void Falls_back_when_environment_variable_is_invalid()
    {
        EnvVar.Run("NEXO_TEST_BUDGET", "not-a-number", () =>
        {
            var p = new BuildTestBudget(buildBudget: 99);
            for (var i = 0; i < BuildTestBudget.DefaultTestBudget; i++)
                p.Approve(Call("dotnet.test"), Snap, out _).Should().BeTrue();
            p.Approve(Call("dotnet.test"), Snap, out var reason).Should().BeFalse();
            reason.Should().Contain("Test budget exceeded");
        });
    }

    [Fact]
    public void Allows_unrelated_tool_calls_indefinitely()
    {
        var p = new BuildTestBudget(0, 0);
        for (var i = 0; i < 10; i++)
        {
            p.Approve(Call("repo.fs.write"), Snap, out var r).Should().BeTrue();
            r.Should().Be("OK");
        }
    }

    [Fact]
    public void Reset_clears_counters()
    {
        var p = new BuildTestBudget(1, 1);
        p.Approve(Call("forge.build"), Snap, out _).Should().BeTrue();
        p.Approve(Call("forge.build"), Snap, out _).Should().BeFalse();
        p.Reset();
        p.Approve(Call("forge.build"), Snap, out var reason).Should().BeTrue();
        reason.Should().Be("OK");
    }

    [Fact]
    public void Test_budget_enforces_separate_counter()
    {
        var p = new BuildTestBudget(0, 1);
        p.Approve(Call("forge.test"), Snap, out _).Should().BeTrue();
        p.Approve(Call("dotnet.test"), Snap, out var reason).Should().BeFalse();
        reason.Should().Contain("Test budget exceeded");
    }
}

public class ForgeMediatedWritesPolicyTests
{
    private static ToolCall Write(string path) =>
        new("repo.fs.write", JsonDocument.Parse(JsonSerializer.Serialize(new Dictionary<string, string> { ["path"] = path })).RootElement);
    private static ToolCall SearchReplace(string path) =>
        new("repo.fs.search_replace", JsonDocument.Parse(JsonSerializer.Serialize(new Dictionary<string, string> { ["path"] = path })).RootElement);
    private static ToolCall Other(string id) => new(id, JsonDocument.Parse("{}").RootElement);

    private static readonly WorldSnapshot Snap = new(0, new Dictionary<string, object?>());

    [Fact]
    public void Constructor_rejects_null_arguments()
    {
        Assert.Throws<ArgumentNullException>(() => new ForgeMediatedWritesPolicy((IAggressivenessModeStore)null!));
        Assert.Throws<ArgumentNullException>(() => new ForgeMediatedWritesPolicy((Func<BackgroundAgentAggressivenessMode>)null!));
    }

    [Fact]
    public void Non_write_calls_always_pass()
    {
        var p = new ForgeMediatedWritesPolicy(() => BackgroundAgentAggressivenessMode.Passive);
        p.Approve(Other("dotnet.build"), Snap, out var r).Should().BeTrue();
        r.Should().Be("OK");
    }

    [Theory]
    [InlineData(BackgroundAgentAggressivenessMode.Active)]
    [InlineData(BackgroundAgentAggressivenessMode.Ambient)]
    public void High_trust_modes_are_no_ops(BackgroundAgentAggressivenessMode mode)
    {
        var p = new ForgeMediatedWritesPolicy(() => mode);
        p.Approve(Write("src/Foo.cs"), Snap, out var r).Should().BeTrue();
        r.Should().Be("OK");
    }

    [Theory]
    [InlineData(BackgroundAgentAggressivenessMode.Passive)]
    [InlineData(BackgroundAgentAggressivenessMode.SemiActive)]
    public void Low_trust_modes_block_source_writes(BackgroundAgentAggressivenessMode mode)
    {
        var p = new ForgeMediatedWritesPolicy(() => mode);
        p.Approve(Write("src/Foo.cs"), Snap, out var r).Should().BeFalse();
        r.Should().Contain("forge-mediated writes");
        p.Approve(SearchReplace("tests/Bar.cs"), Snap, out var r2).Should().BeFalse();
        r2.Should().Contain("forge-mediated writes");
    }

    [Fact]
    public void Low_trust_modes_allow_non_source_paths()
    {
        var p = new ForgeMediatedWritesPolicy(() => BackgroundAgentAggressivenessMode.Passive);
        p.Approve(Write("docs/readme.md"), Snap, out _).Should().BeTrue();
        p.Approve(Write(".nexo/state.json"), Snap, out _).Should().BeTrue();
    }

    [Fact]
    public void Empty_or_missing_path_passes()
    {
        var p = new ForgeMediatedWritesPolicy(() => BackgroundAgentAggressivenessMode.Passive);
        var emptyPath = new ToolCall("repo.fs.write", JsonDocument.Parse("""{"path":""}""").RootElement);
        var missingPath = new ToolCall("repo.fs.write", JsonDocument.Parse("""{"other":"x"}""").RootElement);
        var notObject = new ToolCall("repo.fs.write", JsonDocument.Parse("[]").RootElement);
        var nonStringPath = new ToolCall("repo.fs.write", JsonDocument.Parse("""{"path":123}""").RootElement);
        p.Approve(emptyPath, Snap, out _).Should().BeTrue();
        p.Approve(missingPath, Snap, out _).Should().BeTrue();
        p.Approve(notObject, Snap, out _).Should().BeTrue();
        p.Approve(nonStringPath, Snap, out _).Should().BeTrue();
    }

    [Fact]
    public void Mode_store_is_consulted_on_each_call()
    {
        var store = new Mock<IAggressivenessModeStore>();
        store.SetupSequence(s => s.GetMode())
            .Returns(BackgroundAgentAggressivenessMode.Active)
            .Returns(BackgroundAgentAggressivenessMode.Passive);

        var p = new ForgeMediatedWritesPolicy(store.Object);
        p.Approve(Write("src/A.cs"), Snap, out _).Should().BeTrue();
        p.Approve(Write("src/A.cs"), Snap, out var r).Should().BeFalse();
        r.Should().Contain("forge-mediated writes");
    }
}

public class MaxWriteSizeTests
{
    [Fact]
    public void Allows_non_write_calls()
    {
        var p = new MaxWriteSize(100);
        p.Approve(new ToolCall("dotnet.build", JsonDocument.Parse("{}").RootElement),
            new WorldSnapshot(0, new Dictionary<string, object?>()), out var r).Should().BeTrue();
        r.Should().Be("OK");
    }

    [Fact]
    public void Allows_write_when_arguments_not_object()
    {
        var p = new MaxWriteSize(100);
        p.Approve(new ToolCall("repo.fs.write", JsonDocument.Parse("[]").RootElement),
            new WorldSnapshot(0, new Dictionary<string, object?>()), out _).Should().BeTrue();
    }

    [Fact]
    public void Rejects_write_missing_content()
    {
        var p = new MaxWriteSize(100);
        var call = new ToolCall("repo.fs.write", JsonDocument.Parse("""{"path":"x"}""").RootElement);
        p.Approve(call, new WorldSnapshot(0, new Dictionary<string, object?>()), out var r).Should().BeFalse();
        r.Should().Contain("content is required");
    }

    [Fact]
    public void Rejects_write_null_content()
    {
        var p = new MaxWriteSize(100);
        var call = new ToolCall("repo.fs.write", JsonDocument.Parse("""{"content":null}""").RootElement);
        p.Approve(call, new WorldSnapshot(0, new Dictionary<string, object?>()), out var r).Should().BeFalse();
        r.Should().Contain("cannot be null");
    }

    [Fact]
    public void Rejects_write_exceeding_size()
    {
        var p = new MaxWriteSize(5);
        var big = new string('a', 10);
        var call = new ToolCall("repo.fs.write", JsonDocument.Parse(JsonSerializer.Serialize(new Dictionary<string, string> { ["content"] = big })).RootElement);
        p.Approve(call, new WorldSnapshot(0, new Dictionary<string, object?>()), out var r).Should().BeFalse();
        r.Should().Contain("Write too large");
    }

    [Fact]
    public void Allows_write_within_size()
    {
        var p = new MaxWriteSize(100);
        var call = new ToolCall("repo.fs.write", JsonDocument.Parse("""{"content":"hi"}""").RootElement);
        p.Approve(call, new WorldSnapshot(0, new Dictionary<string, object?>()), out var r).Should().BeTrue();
        r.Should().Be("OK");
    }

    [Fact]
    public void Default_max_size_is_200KB()
    {
        var p = new MaxWriteSize();
        var json = JsonSerializer.Serialize(new Dictionary<string, string> { ["content"] = new string('x', 200_000) });
        var call = new ToolCall("repo.fs.write", JsonDocument.Parse(json).RootElement);
        p.Approve(call, new WorldSnapshot(0, new Dictionary<string, object?>()), out _).Should().BeTrue();
    }
}

[Collection("EnvironmentSensitive")]
public class PathAllowlistTests
{
    private static ToolCall Write(string path) =>
        new("repo.fs.write", JsonDocument.Parse(JsonSerializer.Serialize(new Dictionary<string, string> { ["path"] = path })).RootElement);
    private static ToolCall WriteWithNullPath() =>
        new("repo.fs.write", JsonDocument.Parse("""{"path":null}""").RootElement);
    private static readonly WorldSnapshot EmptySnap = new(0, new Dictionary<string, object?>());

    [Fact]
    public void Non_write_calls_pass()
    {
        var p = new PathAllowlist();
        p.Approve(new ToolCall("dotnet.test", JsonDocument.Parse("{}").RootElement), EmptySnap, out var r).Should().BeTrue();
        r.Should().Be("OK");
    }

    [Fact]
    public void Write_without_arguments_object_passes()
    {
        var p = new PathAllowlist();
        var call = new ToolCall("repo.fs.write", JsonDocument.Parse("[]").RootElement);
        p.Approve(call, EmptySnap, out _).Should().BeTrue();
    }

    [Fact]
    public void Write_without_path_property_passes()
    {
        var p = new PathAllowlist();
        var call = new ToolCall("repo.fs.write", JsonDocument.Parse("""{"content":"x"}""").RootElement);
        p.Approve(call, EmptySnap, out _).Should().BeTrue();
    }

    [Theory]
    [InlineData("src/Foo.cs")]
    [InlineData("tests/Bar.cs")]
    [InlineData("docs/readme.md")]
    [InlineData("application/foo")]
    [InlineData(".nexo/state.json")]
    public void Default_allowlist_permits_known_prefixes(string path)
    {
        var p = new PathAllowlist();
        p.Approve(Write(path), EmptySnap, out var r).Should().BeTrue();
        r.Should().Be("OK");
    }

    [Fact]
    public void Rejects_empty_or_null_path()
    {
        var p = new PathAllowlist();
        p.Approve(Write(""), EmptySnap, out var r).Should().BeFalse();
        r.Should().Contain("empty or null path");
        p.Approve(WriteWithNullPath(), EmptySnap, out var r2).Should().BeFalse();
        r2.Should().Contain("empty or null path");
    }

    [Fact]
    public void Rejects_disallowed_relative_path()
    {
        var p = new PathAllowlist();
        p.Approve(Write("etc/passwd"), EmptySnap, out var r).Should().BeFalse();
        r.Should().Contain("Path not allowed");
    }

    [Fact]
    public void Rejects_path_traversal()
    {
        var p = new PathAllowlist();
        p.Approve(Write("src/../etc/x"), EmptySnap, out var r).Should().BeFalse();
        r.Should().Contain("path traversal not permitted");
    }

    [Fact]
    public void Rejects_absolute_path_without_sandbox_root()
    {
        var p = new PathAllowlist();
        var abs = OperatingSystem.IsWindows() ? @"C:\foo\bar" : "/foo/bar";
        p.Approve(Write(abs), EmptySnap, out var r).Should().BeFalse();
        r.Should().Contain("absolute path not permitted");
    }

    [Fact]
    public void Allows_absolute_path_within_SandboxRoot_in_snapshot()
    {
        var p = new PathAllowlist();
        var sandbox = Path.Combine(Path.GetTempPath(), "sb_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sandbox);
        try
        {
            var inside = Path.Combine(sandbox, "inside.txt");
            var snap = new WorldSnapshot(0, new Dictionary<string, object?> { ["SandboxRoot"] = sandbox });
            p.Approve(Write(inside), snap, out var r).Should().BeTrue();
            r.Should().Be("OK");

            // Also verify the candidate-equals-root edge case.
            p.Approve(Write(sandbox), snap, out _).Should().BeTrue();
        }
        finally { Directory.Delete(sandbox, recursive: true); }
    }

    [Fact]
    public void Rejects_absolute_path_outside_SandboxRoot()
    {
        var p = new PathAllowlist();
        var sandbox = Path.Combine(Path.GetTempPath(), "sb_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sandbox);
        try
        {
            var outside = Path.Combine(Path.GetTempPath(), "outside_" + Guid.NewGuid().ToString("N"));
            var snap = new WorldSnapshot(0, new Dictionary<string, object?> { ["SandboxRoot"] = sandbox });
            p.Approve(Write(outside), snap, out var r).Should().BeFalse();
            r.Should().Contain("outside SandboxRoot");
        }
        finally { Directory.Delete(sandbox, recursive: true); }
    }

    [Fact]
    public void Falls_back_to_environment_SANDBOX_ROOT_when_state_lacks_it()
    {
        var sandbox = Path.Combine(Path.GetTempPath(), "envsb_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sandbox);
        try
        {
            EnvVar.Run("NEXO_SANDBOX_ROOT", sandbox, () =>
            {
                var p = new PathAllowlist();
                var inside = Path.Combine(sandbox, "x.txt");
                p.Approve(Write(inside), EmptySnap, out var r).Should().BeTrue();
                r.Should().Be("OK");
            });
        }
        finally
        {
            Directory.Delete(sandbox, recursive: true);
        }
    }

    [Fact]
    public void Constructor_merges_extra_prefixes_with_normalization_and_dedup()
    {
        var p = new PathAllowlist(new[]
        {
            "extras/",
            "extras/",
            "libs",
            @"plugins\sub\",
            "",
            "  ",
            "../bad",
            "..bad",
        });

        p.Approve(Write("extras/x"), EmptySnap, out _).Should().BeTrue();
        p.Approve(Write("libs/x"), EmptySnap, out _).Should().BeTrue();
        p.Approve(Write("plugins/sub/x"), EmptySnap, out _).Should().BeTrue();
        p.Approve(Write("bad/x"), EmptySnap, out var rBad).Should().BeFalse();
        rBad.Should().Contain("Path not allowed");
    }

    [Fact]
    public void Empty_extras_environment_value_is_ignored()
    {
        EnvVar.Run("NEXO_PATH_ALLOWLIST_EXTRA", "   ", () =>
        {
            var p = new PathAllowlist();
            p.Approve(Write("etc/passwd"), EmptySnap, out var reason).Should().BeFalse();
            reason.Should().Contain("Path not allowed");
        });
    }

    [Fact]
    public void Extras_environment_value_adds_comma_separated_prefixes()
    {
        EnvVar.Run("NEXO_PATH_ALLOWLIST_EXTRA", "libs/, plugins ,dupe", () =>
        {
            var p = new PathAllowlist();
            p.Approve(Write("libs/x"), EmptySnap, out var r1).Should().BeTrue(r1);
            p.Approve(Write("plugins/x"), EmptySnap, out var r2).Should().BeTrue(r2);
            p.Approve(Write("dupe/x"), EmptySnap, out var r3).Should().BeTrue(r3);
        });
    }

    [Fact]
    public void NormalizePrefix_drops_entries_that_are_only_slashes()
    {
        var p = new PathAllowlist(new[] { "/", "//", "///" });
        p.Approve(Write("etc/passwd"), EmptySnap, out var reason).Should().BeFalse();
        reason.Should().Contain("Path not allowed");
    }

    [Fact]
    public void ResolveSandboxRoot_returns_null_when_path_invalid()
    {
        var prev = Environment.GetEnvironmentVariable("NEXO_SANDBOX_ROOT");
        try
        {
            Environment.SetEnvironmentVariable("NEXO_SANDBOX_ROOT", "\0bad\0");
            var p = new PathAllowlist();
            var abs = OperatingSystem.IsWindows() ? @"C:\foo\bar" : "/foo/bar";
            p.Approve(Write(abs), EmptySnap, out var reason).Should().BeFalse();
            reason.Should().Contain("absolute path not permitted");
        }
        finally
        {
            Environment.SetEnvironmentVariable("NEXO_SANDBOX_ROOT", prev);
        }
    }

    [Fact]
    public void Non_string_path_property_is_treated_as_empty()
    {
        var p = new PathAllowlist();
        var call = new ToolCall("repo.fs.write", JsonDocument.Parse("""{"path":123}""").RootElement);
        p.Approve(call, EmptySnap, out var reason).Should().BeFalse();
        reason.Should().Contain("empty or null path");
    }

    [Fact]
    public void Invalid_SandboxRoot_in_snapshot_falls_back_to_absolute_path_denial()
    {
        var snap = new WorldSnapshot(0, new Dictionary<string, object?> { ["SandboxRoot"] = "\0bad\0" });
        var p = new PathAllowlist();
        var abs = OperatingSystem.IsWindows() ? @"C:\foo\bar" : "/foo/bar";
        p.Approve(Write(abs), snap, out var reason).Should().BeFalse();
        reason.Should().Contain("absolute path not permitted");
    }

    [Fact]
    public void Search_replace_uses_same_rules_as_write()
    {
        var p = new PathAllowlist();
        var call = new ToolCall("repo.fs.search_replace",
            JsonDocument.Parse(JsonSerializer.Serialize(new Dictionary<string, string> { ["path"] = "etc/x" })).RootElement);
        p.Approve(call, EmptySnap, out var r).Should().BeFalse();
        r.Should().Contain("Path not allowed");
    }

    [Fact]
    public void Invalid_absolute_path_returns_invalid_path_reason()
    {
        var p = new PathAllowlist();
        var sandbox = Path.Combine(Path.GetTempPath(), "isb_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sandbox);
        try
        {
            var snap = new WorldSnapshot(0, new Dictionary<string, object?> { ["SandboxRoot"] = sandbox });
            var bad = "/" + new string('\0', 1) + new string(Path.GetInvalidPathChars()) + "bad";
            var call = new ToolCall("repo.fs.write",
                JsonDocument.Parse(JsonSerializer.Serialize(new Dictionary<string, string> { ["path"] = bad })).RootElement);
            p.Approve(call, snap, out var r).Should().BeFalse();
            r.Should().Contain("invalid absolute path");
        }
        finally { Directory.Delete(sandbox, recursive: true); }
    }
}
