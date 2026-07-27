using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Bricks.DepExtract;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Xunit;

namespace Nexo.Bricks.DepExtract.Tests;

public sealed class PathValidationAndErrorTests : IDisposable
{
    private readonly string _fixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "basic");
    private readonly string _outDir = Path.Combine(Path.GetTempPath(), "dep-extract-path-" + Guid.NewGuid());
    private readonly CppDependencyExtractorBrick _brick = new(NullLogger<CppDependencyExtractorBrick>.Instance);

    public void Dispose()
    {
        if (Directory.Exists(_outDir)) Directory.Delete(_outDir, recursive: true);
    }

    [Fact]
    public async Task ExecuteAsync_rejects_empty_entries()
    {
        var act = () => _brick.ExecuteAsync(new BrickInput(new Dictionary<string, object>
        {
            ["srcDir"] = _fixtureDir,
            ["outDir"] = _outDir,
            ["entries"] = Array.Empty<string>()
        }), ImplementationType.Deterministic, new AirGappedTestContext());

        var ex = await act.Should().ThrowAsync<DepExtractException>();
        ex.Which.Code.Should().Be("invalid_input");
        ex.Which.SuggestedStatusCode.Should().Be(400);
    }

    [Fact]
    public async Task ExecuteAsync_rejects_traversal_entry()
    {
        var act = () => _brick.ExecuteAsync(new BrickInput(new Dictionary<string, object>
        {
            ["srcDir"] = _fixtureDir,
            ["outDir"] = _outDir,
            ["entries"] = new[] { "../outside.hpp" }
        }), ImplementationType.Deterministic, new AirGappedTestContext());

        var ex = await act.Should().ThrowAsync<DepExtractException>();
        ex.Which.Code.Should().Be("invalid_input");
        ex.Which.Message.Should().Contain("..");
    }

    [Fact]
    public async Task ExecuteAsync_rejects_absolute_entry()
    {
        var abs = Path.Combine(_fixtureDir, "parser", "Parser.hpp");
        var act = () => _brick.ExecuteAsync(new BrickInput(new Dictionary<string, object>
        {
            ["srcDir"] = _fixtureDir,
            ["outDir"] = _outDir,
            ["entries"] = new[] { abs }
        }), ImplementationType.Deterministic, new AirGappedTestContext());

        var ex = await act.Should().ThrowAsync<DepExtractException>();
        ex.Which.Code.Should().Be("invalid_input");
        ex.Which.Message.Should().Contain("absolute");
    }

    [Fact]
    public async Task ExecuteAsync_rejects_missing_entry_file()
    {
        var act = () => _brick.ExecuteAsync(new BrickInput(new Dictionary<string, object>
        {
            ["srcDir"] = _fixtureDir,
            ["outDir"] = _outDir,
            ["entries"] = new[] { "parser/DoesNotExist.hpp" }
        }), ImplementationType.Deterministic, new AirGappedTestContext());

        var ex = await act.Should().ThrowAsync<DepExtractException>();
        ex.Which.Code.Should().Be("not_found");
        ex.Which.SuggestedStatusCode.Should().Be(404);
    }

    [Fact]
    public async Task ExecuteAsync_rejects_bad_define_format()
    {
        var act = () => _brick.ExecuteAsync(new BrickInput(new Dictionary<string, object>
        {
            ["srcDir"] = _fixtureDir,
            ["outDir"] = _outDir,
            ["entries"] = new[] { "parser/Parser.hpp" },
            ["defines"] = new[] { "NOT_A_VALID_DEFINE" }
        }), ImplementationType.Deterministic, new AirGappedTestContext());

        var ex = await act.Should().ThrowAsync<DepExtractException>();
        ex.Which.Code.Should().Be("invalid_input");
        ex.Which.Message.Should().Contain("KEY=VAL");
    }

    [Fact]
    public async Task ExecuteAsync_rejects_missing_srcDir()
    {
        var act = () => _brick.ExecuteAsync(new BrickInput(new Dictionary<string, object>
        {
            ["srcDir"] = Path.Combine(_outDir, "missing"),
            ["outDir"] = _outDir,
            ["entries"] = new[] { "a.hpp" }
        }), ImplementationType.Deterministic, new AirGappedTestContext());

        var ex = await act.Should().ThrowAsync<DepExtractException>();
        ex.Which.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task ExecuteAsync_rejects_srcDir_outside_workspace_root_when_configured()
    {
        var root = Path.Combine(Path.GetTempPath(), "ws-root-" + Guid.NewGuid());
        Directory.CreateDirectory(root);
        Environment.SetEnvironmentVariable("WORKSPACE_ROOT", root);
        try
        {
            var outside = Path.Combine(Path.GetTempPath(), "outside-" + Guid.NewGuid());
            Directory.CreateDirectory(outside);
            File.WriteAllText(Path.Combine(outside, "a.hpp"), "#pragma once\n");
            var act = () => _brick.ExecuteAsync(new BrickInput(new Dictionary<string, object>
            {
                ["srcDir"] = outside,
                ["outDir"] = Path.Combine(root, "out"),
                ["entries"] = new[] { "a.hpp" }
            }), ImplementationType.Deterministic, new AirGappedTestContext());

            var ex = await act.Should().ThrowAsync<DepExtractException>();
            ex.Which.Code.Should().Be("invalid_input");
            ex.Which.Message.Should().Contain("WORKSPACE_ROOT");
        }
        finally
        {
            Environment.SetEnvironmentVariable("WORKSPACE_ROOT", null);
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
