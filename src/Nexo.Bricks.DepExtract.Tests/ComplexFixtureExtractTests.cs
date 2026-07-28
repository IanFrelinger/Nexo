using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Bricks.DepExtract;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Xunit;

namespace Nexo.Bricks.DepExtract.Tests;

/// <summary>
/// Deterministic extraction against the more complex fixtures (T11–T14):
/// multi-root includes, platform #ifdef factories, GUI-warning facades,
/// and deep relative-include graphs.
/// Adaptation (Ollama) is covered by StressTest; this suite hard-asserts
/// extraction correctness without waiting on a model.
/// </summary>
public sealed class ComplexFixtureExtractTests : IDisposable
{
    private readonly string _stress = Path.Combine(AppContext.BaseDirectory, "Fixtures", "stress");
    private readonly string _outDir = Path.Combine(Path.GetTempPath(), "dep-extract-complex-" + Guid.NewGuid());
    private readonly CppDependencyExtractorBrick _brick = new(NullLogger<CppDependencyExtractorBrick>.Instance);

    public void Dispose()
    {
        if (Directory.Exists(_outDir)) Directory.Delete(_outDir, recursive: true);
    }

    [Fact]
    public async Task T11_multi_root_includes_need_extra_include_dirs()
    {
        var src = Path.Combine(_stress, "T11_MultiRootIncludes");
        var output = await _brick.ExecuteAsync(new BrickInput(new Dictionary<string, object>
        {
            ["srcDir"] = src,
            ["outDir"] = Path.Combine(_outDir, "t11"),
            ["entries"] = new[] { "src/MultiRootLog.cpp", "src/MultiRootLog.hpp" },
            ["includeDirs"] = new[] { "include", "vendor/byteio" },
            ["manifestOnly"] = true
        }), ImplementationType.Deterministic, new AirGappedTestContext());

        var lower = output.Get<string[]>("lower");
        lower.Should().Contain(p => p.Replace('\\', '/').Contains("RadarTypes.hpp"));
        lower.Should().Contain(p => p.Replace('\\', '/').Contains("ByteCursor.hpp"));
        output.Get<string>("manifestText").Should().Contain("MultiRootLog");
    }

    [Fact]
    public async Task T12_default_platform_resolves_A_not_B()
    {
        var src = Path.Combine(_stress, "T12_PlatformFactory");
        var output = await _brick.ExecuteAsync(new BrickInput(new Dictionary<string, object>
        {
            ["srcDir"] = src,
            ["outDir"] = Path.Combine(_outDir, "t12a"),
            ["entries"] = new[] { "Core/EventFactory.cpp", "Core/EventFactory.hpp" },
            ["manifestOnly"] = true
        }), ImplementationType.Deterministic, new AirGappedTestContext());

        var lowerJoined = string.Join('\n', output.Get<string[]>("lower"));
        lowerJoined.Should().Contain("StreamA.hpp");
        lowerJoined.Should().NotContain("StreamB.hpp");
    }

    [Fact]
    public async Task T12_define_switches_to_platform_B()
    {
        var src = Path.Combine(_stress, "T12_PlatformFactory");
        var output = await _brick.ExecuteAsync(new BrickInput(new Dictionary<string, object>
        {
            ["srcDir"] = src,
            ["outDir"] = Path.Combine(_outDir, "t12b"),
            ["entries"] = new[] { "Core/EventFactory.cpp", "Core/EventFactory.hpp" },
            ["defines"] = new[] { "USE_PLATFORM_B=1" },
            ["manifestOnly"] = true
        }), ImplementationType.Deterministic, new AirGappedTestContext());

        var lowerJoined = string.Join('\n', output.Get<string[]>("lower"));
        lowerJoined.Should().Contain("StreamB.hpp");
        lowerJoined.Should().NotContain("StreamA.hpp");
    }

    [Fact]
    public async Task T13_warning_facade_duplicate_contains_entry_sources()
    {
        var src = Path.Combine(_stress, "T13_GuiWarningFacade");
        var output = await _brick.ExecuteAsync(new BrickInput(new Dictionary<string, object>
        {
            ["srcDir"] = src,
            ["outDir"] = Path.Combine(_outDir, "t13"),
            ["entries"] = new[] { "WarningFacade.cpp", "WarningFacade.hpp" }
        }), ImplementationType.Deterministic, new AirGappedTestContext());

        var dup = output.Get<string>("duplicatePath");
        Directory.Exists(dup).Should().BeTrue();
        File.Exists(Path.Combine(dup, "WarningFacade.hpp")).Should().BeTrue();
        File.Exists(Path.Combine(dup, "WarningFacade.cpp")).Should().BeTrue();
    }

    [Fact]
    public async Task T14_nested_relative_graph_pulls_plugins_and_endian()
    {
        var src = Path.Combine(_stress, "T14_NestedRelativeGraph");
        var output = await _brick.ExecuteAsync(new BrickInput(new Dictionary<string, object>
        {
            ["srcDir"] = src,
            ["outDir"] = Path.Combine(_outDir, "t14"),
            ["entries"] = new[] { "engine/core/GraphReader.cpp", "engine/core/GraphReader.hpp" },
            ["manifestOnly"] = true
        }), ImplementationType.Deterministic, new AirGappedTestContext());

        var lowerJoined = string.Join('\n', output.Get<string[]>("lower")).Replace('\\', '/');
        lowerJoined.Should().Contain("PluginBridge.hpp");
        lowerJoined.Should().Contain("Endian.hpp");
        lowerJoined.Should().Contain("Cursor.hpp");
        lowerJoined.Should().Contain("Buffer.hpp");
    }
}
