using FluentAssertions;
using Nexo.BackgroundAgents.HostRunners;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.HostRunners;

/// <summary>
/// Tests for the planner scratchpad — verify append/load round-trip, tail truncation,
/// and that missing files / failed I/O degrade silently.
/// </summary>
public sealed class PlannerScratchpadTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _path;

    public PlannerScratchpadTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "nexo-scratchpad-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _path = Path.Combine(_tempDir, "planner-notes.md");
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public void LoadTail_returns_empty_for_missing_file()
    {
        PlannerScratchpad.LoadTail(_path).Should().BeEmpty();
    }

    [Fact]
    public void Append_creates_directory_and_writes_markdown_line()
    {
        var nestedPath = Path.Combine(_tempDir, "a", "b", "c", "notes.md");
        PlannerScratchpad.Append(nestedPath, NewEntry(rationale: "first cycle"));

        File.Exists(nestedPath).Should().BeTrue();
        var content = File.ReadAllText(nestedPath);
        content.Should().StartWith("- **").And.Contain("`runtime-planner`")
            .And.Contain("iter=2").And.Contain("first cycle");
    }

    [Fact]
    public void Append_then_LoadTail_round_trips_in_order()
    {
        PlannerScratchpad.Append(_path, NewEntry(rationale: "alpha"));
        PlannerScratchpad.Append(_path, NewEntry(rationale: "beta"));
        PlannerScratchpad.Append(_path, NewEntry(rationale: "gamma"));

        var tail = PlannerScratchpad.LoadTail(_path);
        var alphaIdx = tail.IndexOf("alpha", StringComparison.Ordinal);
        var betaIdx = tail.IndexOf("beta", StringComparison.Ordinal);
        var gammaIdx = tail.IndexOf("gamma", StringComparison.Ordinal);
        alphaIdx.Should().BeGreaterThanOrEqualTo(0);
        betaIdx.Should().BeGreaterThan(alphaIdx);
        gammaIdx.Should().BeGreaterThan(betaIdx);
    }

    [Fact]
    public void LoadTail_truncates_to_max_bytes_at_utf8_boundary()
    {
        // Write a long entry, then ask for fewer bytes than its size.
        var longRationale = new string('x', 4_000);
        PlannerScratchpad.Append(_path, NewEntry(rationale: longRationale));

        var info = new FileInfo(_path);
        info.Length.Should().BeGreaterThan(1_000);

        var tail = PlannerScratchpad.LoadTail(_path, maxBytes: 256);
        tail.Length.Should().BeLessThanOrEqualTo(256);
        // Should never throw on decode — tail must be valid UTF-8.
        var bytes = System.Text.Encoding.UTF8.GetBytes(tail);
        System.Text.Encoding.UTF8.GetString(bytes).Should().Be(tail);
    }

    [Fact]
    public void Write_paths_are_rendered_in_markdown()
    {
        PlannerScratchpad.Append(_path, NewEntry(
            rationale: "wrote some files",
            paths: new[] { "src/foo.cs", "tests/bar.cs" }));

        var tail = PlannerScratchpad.LoadTail(_path);
        tail.Should().Contain("`src/foo.cs`").And.Contain("`tests/bar.cs`");
    }

    private static ScratchpadEntry NewEntry(string? rationale = null, IReadOnlyList<string>? paths = null) =>
        new(
            DateTimeOffset.UtcNow,
            "runtime-planner",
            Iterations: 2,
            ToolsExecuted: 3,
            ToolsDenied: 0,
            StoppedReason: "empty",
            Rationale: rationale,
            WritePaths: paths);
}
