using System.Text.Json;
using FluentAssertions;
using Nexo.Abstractions;
using Nexo.Tools.Dev;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Tools;

/// <summary>
/// Tests for <see cref="RepoFsReadTool"/> — the agent observation primitive that
/// reads a text file under the repo root.
///
/// Particular attention to security boundaries: path traversal and absolute paths
/// must be rejected with a structured error rather than silently leaking content
/// from outside the configured root.
/// </summary>
public sealed class RepoFsReadToolTests : IDisposable
{
    private readonly string _root;
    private readonly RepoFsReadTool _tool = new();

    public RepoFsReadToolTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "nexo-readtool-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public async Task Reads_existing_file_within_root()
    {
        var rel = "hello.txt";
        var contents = "hello, world";
        await File.WriteAllTextAsync(Path.Combine(_root, rel), contents);

        var result = await Invoke(rel);
        var payload = ParsePayload(result);

        result.Delta.Log.Should().ContainSingle().Which.Should().Contain("read:hello.txt");
        payload.GetProperty("exists").GetBoolean().Should().BeTrue();
        payload.GetProperty("truncated").GetBoolean().Should().BeFalse();
        payload.GetProperty("content").GetString().Should().Be(contents);
        payload.GetProperty("bytes").GetInt32().Should().Be(contents.Length);
    }

    [Fact]
    public async Task Returns_exists_false_for_missing_file()
    {
        var result = await Invoke("nope.txt");
        var payload = ParsePayload(result);
        payload.GetProperty("exists").GetBoolean().Should().BeFalse();
        result.Delta.Log.Single().Should().Contain("not_found");
    }

    [Fact]
    public async Task Rejects_parent_traversal()
    {
        var result = await Invoke("../escape.txt");
        var payload = ParsePayload(result);
        payload.GetProperty("exists").GetBoolean().Should().BeFalse();
        payload.GetProperty("error").GetString().Should().Contain("traversal");
        result.Delta.Log.Single().Should().Contain("REJECTED");
    }

    [Fact]
    public async Task Rejects_absolute_path()
    {
        var abs = OperatingSystem.IsWindows() ? @"C:\windows\system32\drivers\etc\hosts" : "/etc/passwd";
        var result = await Invoke(abs);
        var payload = ParsePayload(result);
        payload.GetProperty("exists").GetBoolean().Should().BeFalse();
        payload.GetProperty("error").GetString().Should().Contain("absolute");
        result.Delta.Log.Single().Should().Contain("REJECTED");
    }

    [Fact]
    public async Task Truncates_when_file_exceeds_max_bytes()
    {
        var rel = "big.txt";
        var contents = new string('x', 1024);
        await File.WriteAllTextAsync(Path.Combine(_root, rel), contents);

        var result = await Invoke(rel, maxBytes: 64);
        var payload = ParsePayload(result);
        payload.GetProperty("truncated").GetBoolean().Should().BeTrue();
        payload.GetProperty("bytes").GetInt32().Should().Be(1024);
        payload.GetProperty("content").GetString()!.Length.Should().Be(64);
    }

    [Fact]
    public async Task Rejects_whitespace_only_path()
    {
        var result = await Invoke("   ");
        var payload = ParsePayload(result);
        payload.GetProperty("exists").GetBoolean().Should().BeFalse();
        payload.GetProperty("error").GetString().Should().Contain("required");
    }

    [Fact]
    public async Task Rejects_missing_root()
    {
        var args = JsonSerializer.SerializeToElement(new { root = "  ", path = "file.txt" });
        var result = await _tool.InvokeAsync(new ToolCall(_tool.Id, args), WorldSnapshot.ForRepo(_root, _root), CancellationToken.None);
        ParsePayload(result).GetProperty("error").GetString().Should().Contain("root and path are required");
    }

    private async Task<ToolResult> Invoke(string relPath, int? maxBytes = null)
    {
        var argsObj = maxBytes is null
            ? (object)new { root = _root, path = relPath }
            : new { root = _root, path = relPath, max_bytes = maxBytes.Value };
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
