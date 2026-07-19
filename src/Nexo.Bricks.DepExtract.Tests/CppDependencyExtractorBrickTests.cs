using FluentAssertions;
using Nexo.Bricks.DepExtract;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Nexo.Bricks.DepExtract.Tests;

/// <summary>
/// Exercises CppDependencyExtractorBrick against the real `dep-extract` Docker
/// image (built from tools/dep_extract in the evtx toolkit: `docker build -t
/// dep-extract tools/dep_extract`) and a known-answer fixture bundled with this
/// test project, so the expected lower/upper closure is asserted exactly, not
/// just "it ran without throwing".
/// </summary>
public sealed class CppDependencyExtractorBrickTests : IDisposable
{
    private readonly string _fixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "basic");
    private readonly string _outDir = Path.Combine(Path.GetTempPath(), "dep-extract-brick-test-" + Guid.NewGuid());
    private readonly CppDependencyExtractorBrick _brick = new(NullLogger<CppDependencyExtractorBrick>.Instance);

    public void Dispose()
    {
        if (Directory.Exists(_outDir)) Directory.Delete(_outDir, recursive: true);
    }

    [Fact]
    public async Task ExecuteAsync_computes_exact_lower_and_upper_closure()
    {
        var input = new BrickInput(new Dictionary<string, object>
        {
            ["srcDir"] = _fixtureDir,
            ["outDir"] = _outDir,
            ["entries"] = new[] { "parser/Parser.cpp", "parser/Parser.hpp" }
        });

        var output = await _brick.ExecuteAsync(input, ImplementationType.Deterministic, new AirGappedTestContext());

        output.Get<string[]>("lower").Should().BeEquivalentTo(["Common/Types.hpp", "Common/Utils.hpp"]);
        output.Get<string[]>("upper").Should().BeEquivalentTo(["App/Main.cpp", "Facade/Facade.cpp", "Facade/Facade.hpp"]);
        output.Get<string[]>("external").Should().BeEmpty();
        output.Get<string>("manifestText").Should().Contain("parser/Parser.cpp");
        output.Summary.Should().Contain("lower=2").And.Contain("upper=3");
    }

    [Fact]
    public async Task ExecuteAsync_duplicate_contains_exactly_entry_plus_lower()
    {
        var input = new BrickInput(new Dictionary<string, object>
        {
            ["srcDir"] = _fixtureDir,
            ["outDir"] = _outDir,
            ["entries"] = new[] { "parser/Parser.cpp", "parser/Parser.hpp" }
        });

        var output = await _brick.ExecuteAsync(input, ImplementationType.Deterministic, new AirGappedTestContext());

        var duplicatePath = output.Get<string>("duplicatePath");
        Directory.Exists(duplicatePath).Should().BeTrue("the brick should physically produce a duplicate by default");

        var copied = Directory.EnumerateFiles(duplicatePath, "*", SearchOption.AllDirectories)
            .Select(p => Path.GetRelativePath(duplicatePath, p).Replace('\\', '/'))
            .OrderBy(p => p);

        copied.Should().BeEquivalentTo([
            "parser/Parser.cpp", "parser/Parser.hpp", "Common/Types.hpp", "Common/Utils.hpp"
        ]);

        // and the fixture itself must be untouched — this brick only ever reads srcDir
        File.Exists(Path.Combine(_fixtureDir, "parser", "Parser.cpp")).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_manifestOnly_skips_the_duplicate()
    {
        var input = new BrickInput(new Dictionary<string, object>
        {
            ["srcDir"] = _fixtureDir,
            ["outDir"] = _outDir,
            ["entries"] = new[] { "parser/Parser.hpp" },
            ["manifestOnly"] = true
        });

        var output = await _brick.ExecuteAsync(input, ImplementationType.Deterministic, new AirGappedTestContext());

        output.ToDictionary().Should().NotContainKey("duplicatePath");
        Directory.Exists(Path.Combine(_outDir, "duplicate")).Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_includeUpper_widens_the_duplicate()
    {
        var input = new BrickInput(new Dictionary<string, object>
        {
            ["srcDir"] = _fixtureDir,
            ["outDir"] = _outDir,
            ["entries"] = new[] { "parser/Parser.hpp" },
            ["includeUpper"] = true
        });

        var output = await _brick.ExecuteAsync(input, ImplementationType.Deterministic, new AirGappedTestContext());

        var duplicatePath = output.Get<string>("duplicatePath");
        var copied = Directory.EnumerateFiles(duplicatePath, "*", SearchOption.AllDirectories)
            .Select(p => Path.GetRelativePath(duplicatePath, p).Replace('\\', '/'));

        copied.Should().Contain("App/Main.cpp", "includeUpper should pull dependents into the duplicate too");
    }

    [Fact]
    public async Task ExecuteAsync_rejects_agentic_implementation()
    {
        var input = new BrickInput(new Dictionary<string, object>
        {
            ["srcDir"] = _fixtureDir,
            ["outDir"] = _outDir,
            ["entries"] = new[] { "parser/Parser.hpp" }
        });

        var act = () => _brick.ExecuteAsync(input, ImplementationType.Agentic, new AirGappedTestContext());

        await act.Should().ThrowAsync<ArgumentException>("this brick has no LLM-backed implementation");
    }

    /// <summary>Matches the real deployment scenario this brick was built for: no network access.</summary>
    private sealed class AirGappedTestContext : IExecutionContext
    {
        public string AgentId => "test-agent";
        public string BehaviorId => "cpp-dependency-extractor";
        public bool IsAirGapped => true;
        public bool AuditMode => true;
        public string Provider => "none";
        public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();
    }
}
