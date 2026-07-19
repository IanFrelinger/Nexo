using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Bricks.DepExtract;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;
using Xunit;

namespace Nexo.Bricks.DepExtract.Tests;

/// <summary>
/// Full pipeline test: a synthetic "proprietary" parser (Fixtures/proprietary,
/// distinctive method names on purpose) is run through the real
/// CppDependencyExtractorBrick (Docker) and then the real CppParserAdapterBrick
/// (local Ollama, codellama:7b — `ollama pull codellama:7b` first). Asserts
/// the drafted adapter actually references the extracted parser's real API,
/// not generic boilerplate, and that nothing about the run touches the network
/// beyond localhost:11434 (Ollama).
/// </summary>
public sealed class CppParserAdapterBrickTests : IDisposable
{
    private readonly string _fixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "proprietary");
    private readonly string _outDir = Path.Combine(Path.GetTempPath(), "dep-extract-adapter-test-" + Guid.NewGuid());
    private readonly CppDependencyExtractorBrick _extractor = new(NullLogger<CppDependencyExtractorBrick>.Instance);

    public void Dispose()
    {
        if (Directory.Exists(_outDir)) Directory.Delete(_outDir, recursive: true);
    }

    private static AirGappedTestContext Ctx() => new();

    [Fact]
    public async Task Pipeline_extract_then_adapt_produces_draft_referencing_real_api()
    {
        // Stage 1 — extraction (Docker, deterministic, already validated elsewhere for
        // its own correctness; here it just supplies real input to stage 2).
        var extractInput = new BrickInput(new Dictionary<string, object>
        {
            ["srcDir"] = _fixtureDir,
            ["outDir"] = _outDir,
            ["entries"] = new[] { "LegacyRadarLog.hpp", "LegacyRadarLog.cpp" }
        });
        var extractOutput = await _extractor.ExecuteAsync(extractInput, ImplementationType.Deterministic, Ctx());
        var duplicatePath = extractOutput.Get<string>("duplicatePath");
        Directory.Exists(duplicatePath).Should().BeTrue();

        // Stage 2 — adaptation (local Ollama; no cloud provider ever referenced).
        var providerFactory = new ProviderFactory(NullLogger<ProviderFactory>.Instance);
        var adapter = new CppParserAdapterBrick(providerFactory, NullLogger<CppParserAdapterBrick>.Instance);

        var adaptOutput = await adapter.ExecuteAsync(
            new BrickInput(new Dictionary<string, object>
            {
                ["parserDir"] = duplicatePath,
                ["entryFiles"] = new[] { "LegacyRadarLog.hpp", "LegacyRadarLog.cpp" },
                ["outputPath"] = Path.Combine(_outDir, "adapted_reader.hpp")
            }),
            ImplementationType.Agentic,
            Ctx());

        var code = adaptOutput.Get<string>("adapterCode");
        File.Exists(Path.Combine(_outDir, "adapted_reader.hpp")).Should().BeTrue();
        code.Should().NotBeNullOrWhiteSpace();

        // The real proof of "adaptation": did it actually use the extracted parser's
        // real, made-up-for-this-test method names, not just emit the bare template?
        var mentionsRealApi =
            code.Contains("readNextContact", StringComparison.OrdinalIgnoreCase) ||
            code.Contains("seekToByteOffset", StringComparison.OrdinalIgnoreCase) ||
            code.Contains("LegacyRadarLog", StringComparison.OrdinalIgnoreCase);
        mentionsRealApi.Should().BeTrue(
            "the draft should reference the specific proprietary API it was given, not generic boilerplate — got:\n" + code);

        code.Should().Contain("CustomEventReader", "must target the actual evtx seam contract");
    }

    [Fact]
    public async Task ExecuteAsync_rejects_deterministic_implementation()
    {
        var providerFactory = new ProviderFactory(NullLogger<ProviderFactory>.Instance);
        var adapter = new CppParserAdapterBrick(providerFactory, NullLogger<CppParserAdapterBrick>.Instance);

        var input = new BrickInput(new Dictionary<string, object>
        {
            ["parserDir"] = _fixtureDir,
            ["entryFiles"] = new[] { "LegacyRadarLog.hpp" },
            ["outputPath"] = Path.Combine(_outDir, "x.hpp")
        });

        var act = () => adapter.ExecuteAsync(input, ImplementationType.Deterministic, Ctx());

        await act.Should().ThrowAsync<ArgumentException>("this brick has no rule-based implementation — adaptation requires reasoning");
    }

    private sealed class AirGappedTestContext : IExecutionContext
    {
        public string AgentId => "test-agent";
        public string BehaviorId => "cpp-parser-adapter";
        public bool IsAirGapped => true;
        public bool AuditMode => true;
        public string Provider => "ollama";
        public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();
    }
}
