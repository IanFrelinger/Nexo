using System.Text.Json;
using FluentAssertions;
using Nexo.Abstractions;
using Nexo.Tools.Dev;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Tools;

/// <summary>
/// Tests for <see cref="RepoFsListTool"/> — the agent discovery primitive that
/// enumerates files/dirs under the repo root, with sane defaults for prompt-bounded output.
/// </summary>
public sealed class RepoFsListToolTests : IDisposable
{
    private readonly string _root;
    private readonly RepoFsListTool _tool = new();

    public RepoFsListToolTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "nexo-listtool-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public async Task Lists_top_level_entries()
    {
        await File.WriteAllTextAsync(Path.Combine(_root, "a.txt"), "a");
        Directory.CreateDirectory(Path.Combine(_root, "subdir"));

        var result = await Invoke();
        var payload = ParsePayload(result);

        payload.GetProperty("exists").GetBoolean().Should().BeTrue();
        var entries = payload.GetProperty("entries").EnumerateArray().ToList();
        entries.Should().HaveCount(2);
        entries.Should().ContainSingle(e => e.GetProperty("kind").GetString() == "dir" &&
                                            e.GetProperty("path").GetString() == "subdir");
        entries.Should().ContainSingle(e => e.GetProperty("kind").GetString() == "file" &&
                                            e.GetProperty("path").GetString() == "a.txt");
    }

    [Fact]
    public async Task Skips_artifact_directories()
    {
        Directory.CreateDirectory(Path.Combine(_root, "bin"));
        Directory.CreateDirectory(Path.Combine(_root, "obj"));
        Directory.CreateDirectory(Path.Combine(_root, ".git"));
        Directory.CreateDirectory(Path.Combine(_root, "src"));
        await File.WriteAllTextAsync(Path.Combine(_root, "bin", "should-not-show.dll"), "");
        await File.WriteAllTextAsync(Path.Combine(_root, "src", "kept.cs"), "");

        var result = await Invoke(recursive: true);
        var payload = ParsePayload(result);
        var entries = payload.GetProperty("entries").EnumerateArray().ToList();

        entries.Should().NotContain(e => e.GetProperty("path").GetString()!.StartsWith("bin", StringComparison.Ordinal));
        entries.Should().NotContain(e => e.GetProperty("path").GetString()!.StartsWith("obj", StringComparison.Ordinal));
        entries.Should().NotContain(e => e.GetProperty("path").GetString()!.StartsWith(".git", StringComparison.Ordinal));
        entries.Should().Contain(e => e.GetProperty("path").GetString() == "src");
        entries.Should().Contain(e => e.GetProperty("path").GetString() == "src/kept.cs");
    }

    [Fact]
    public async Task Caps_results_at_max_entries()
    {
        for (var i = 0; i < 50; i++)
            await File.WriteAllTextAsync(Path.Combine(_root, $"f{i:D2}.txt"), "");

        var result = await Invoke(maxEntries: 10);
        var payload = ParsePayload(result);

        payload.GetProperty("truncated").GetBoolean().Should().BeTrue();
        payload.GetProperty("entries").GetArrayLength().Should().Be(10);
    }

    [Fact]
    public async Task Returns_exists_false_for_missing_directory()
    {
        var result = await Invoke(subpath: "no-such-dir");
        var payload = ParsePayload(result);
        payload.GetProperty("exists").GetBoolean().Should().BeFalse();
        result.Delta.Log.Single().Should().Contain("not_found");
    }

    [Fact]
    public async Task Rejects_parent_traversal()
    {
        var result = await Invoke(subpath: "../escape");
        var payload = ParsePayload(result);
        payload.GetProperty("error").GetString().Should().Contain("traversal");
    }

    [Fact]
    public async Task Caps_recursion_depth()
    {
        // Create a chain deeper than MaxRecursionDepth (6) to verify the walker stops
        // descending instead of blowing the stack on a pathological tree.
        var current = _root;
        for (var i = 0; i < 10; i++)
        {
            current = Path.Combine(current, $"d{i}");
            Directory.CreateDirectory(current);
        }
        await File.WriteAllTextAsync(Path.Combine(current, "deep.txt"), "");

        var result = await Invoke(recursive: true, maxEntries: 1000);
        var payload = ParsePayload(result);
        var paths = payload.GetProperty("entries").EnumerateArray()
            .Select(e => e.GetProperty("path").GetString()!).ToList();

        // The deep file is past depth 6 so it must not appear.
        paths.Should().NotContain(p => p.EndsWith("/deep.txt", StringComparison.Ordinal));
        // But shallow directories are present.
        paths.Should().Contain("d0");
    }

    private async Task<ToolResult> Invoke(string? subpath = null, bool recursive = false, int? maxEntries = null)
    {
        var argsObj = new Dictionary<string, object?>
        {
            ["root"] = _root,
            ["recursive"] = recursive,
        };
        if (!string.IsNullOrEmpty(subpath)) argsObj["path"] = subpath;
        if (maxEntries is not null) argsObj["max_entries"] = maxEntries.Value;
        var args = JsonSerializer.SerializeToElement(argsObj);
        var snapshot = WorldSnapshot.ForRepo(_root, _root);
        return await _tool.InvokeAsync(new ToolCall(_tool.Id, args), snapshot, CancellationToken.None);
    }

    private static JsonElement ParsePayload(ToolResult result)
    {
        var json = JsonSerializer.Serialize(result.Payload);
        return JsonDocument.Parse(json).RootElement;
    }
}
