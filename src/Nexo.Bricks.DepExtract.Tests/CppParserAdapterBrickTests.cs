using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Bricks.DepExtract.Profile;
using Nexo.Core.Application.Generation;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Bricks.Ports;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;
using Nexo.Agents.TestKit;
using Xunit;

namespace Nexo.Bricks.DepExtract.Tests;

/// <summary>
/// Full pipeline: extract (Docker) then adapt via GenerativeArtifactBrick
/// target=cpp-evtx (local Ollama). Replaces the retired CppParserAdapterBrick path.
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

    private static FakeContext Ctx() => new();

    [LiveModelFact]
    public async Task Pipeline_extract_then_adapt_produces_draft_referencing_real_api()
    {
        var extractInput = new BrickInput(new Dictionary<string, object>
        {
            ["srcDir"] = _fixtureDir,
            ["outDir"] = _outDir,
            ["entries"] = new[] { "LegacyRadarLog.hpp", "LegacyRadarLog.cpp" }
        });
        var extractOutput = await _extractor.ExecuteAsync(extractInput, ImplementationType.Deterministic, Ctx());
        var duplicatePath = extractOutput.Get<string>("duplicatePath");
        Directory.Exists(duplicatePath).Should().BeTrue();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IProviderFactory>(new ProviderFactory(NullLogger<ProviderFactory>.Instance));
        services.AddNexoCppEvtxAgentProfile();
        services.AddSingleton<GenerativeArtifactBrick>();
        await using var sp = services.BuildServiceProvider();

        var brick = sp.GetRequiredService<GenerativeArtifactBrick>();
        var grounding = CppAdapterPrompt.AssembleGrounding(
            duplicatePath, new[] { "LegacyRadarLog.hpp", "LegacyRadarLog.cpp" });
        var input = new BrickInput();
        input.Set("target", CppEvtxProfileFactory.TargetId);
        input.Set("grounding", grounding);
        input.Set("preferDeterministic", false);
        input.Set("outputPath", Path.Combine(_outDir, "adapted_reader.hpp"));
        input.Set("overrides", new Dictionary<string, object>
        {
            ["entryInclude"] = "LegacyRadarLog.hpp",
            ["entryFiles"] = new[] { "LegacyRadarLog.hpp", "LegacyRadarLog.cpp" }
        });

        var adaptOutput = await brick.ExecuteAsync(input, ImplementationType.Agentic, Ctx());
        var code = adaptOutput.Get<GeneratedArtifact>("artifact").Content;
        await File.WriteAllTextAsync(Path.Combine(_outDir, "adapted_reader.hpp"), code);

        File.Exists(Path.Combine(_outDir, "adapted_reader.hpp")).Should().BeTrue();
        code.Should().NotBeNullOrWhiteSpace();
        code.Should().Contain("CustomEventReader");

        var mentionsRealApi =
            code.Contains("readNextContact", StringComparison.OrdinalIgnoreCase) ||
            code.Contains("seekToByteOffset", StringComparison.OrdinalIgnoreCase) ||
            code.Contains("LegacyRadarLog", StringComparison.OrdinalIgnoreCase);
        if (string.Equals(Environment.GetEnvironmentVariable("REQUIRE_OLLAMA_QUALITY"), "1", StringComparison.Ordinal))
        {
            mentionsRealApi.Should().BeTrue(
                "REQUIRE_OLLAMA_QUALITY=1: draft must reference the proprietary API — got:\n" + code);
        }
    }

    [Fact]
    public async Task Deterministic_scaffold_path_is_available_via_preferDeterministic()
    {
        var grounding = """
            class SimpleStream {
            public:
                explicit SimpleStream(const std::string& path);
                bool advance();
            };
            """;
        var registry = new AgentProfileRegistry();
        registry.Register(new CppEvtxProfileFactory().Create(new ServiceCollection().BuildServiceProvider()));
        // Factory may omit validators when no sandbox runner — fine for this check.
        var brick = new GenerativeArtifactBrick(registry);
        var input = new BrickInput();
        input.Set("target", "cpp-evtx");
        input.Set("grounding", grounding);
        input.Set("preferDeterministic", true);
        input.Set("overrides", new Dictionary<string, object> { ["entryInclude"] = "SimpleStream.hpp" });

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, Ctx());
        output.Get<GeneratedArtifact>("artifact").Content.Should().Contain("CustomEventReader");
        output.Get<string>("generationStrategy").Should().Be("Templated");
    }

    private sealed class FakeContext : IExecutionContext
    {
        public string AgentId => "t";
        public string BehaviorId => "t";
        public bool IsAirGapped => true;
        public bool AuditMode => false;
        public string Provider => "ollama";
        public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();
    }
}
