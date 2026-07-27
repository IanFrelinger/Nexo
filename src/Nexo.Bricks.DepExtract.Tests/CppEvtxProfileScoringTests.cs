using FluentAssertions;
using Nexo.Bricks.DepExtract.Profile;
using Nexo.Core.Application.Generation;
using Nexo.Core.Application.Generation.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Bricks.Ports;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;
using Xunit;

namespace Nexo.Bricks.DepExtract.Tests;

/// <summary>
/// Migrated from CppParserAdapterBrickRetryAndScoringTests — drives
/// GenerativeArtifactBrick with target=cpp-evtx and a fake model client.
/// </summary>
public sealed class CppEvtxProfileScoringTests : IDisposable
{
    private readonly string _fixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "proprietary");
    private readonly string _outDir = Path.Combine(Path.GetTempPath(), "dep-extract-score-" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_outDir)) Directory.Delete(_outDir, recursive: true);
    }

    private async Task<BrickOutput> RunAsync(ICppModelClient model, string? grounding = null)
    {
        Directory.CreateDirectory(_outDir);
        grounding ??= CppAdapterPrompt.AssembleGrounding(
            _fixtureDir, new[] { "LegacyRadarLog.hpp", "LegacyRadarLog.cpp" });

        var registry = new AgentProfileRegistry();
        registry.Register(new AgentProfile
        {
            TargetId = "cpp-evtx",
            Drafter = new CppArtifactDrafter(model, new CppArtifactDrafterOptions()),
            DeterministicDrafter = new CppDeterministicDrafter(),
            Validators = Array.Empty<object>(),
            Tunables = new GenerationTunables { PreferDeterministic = false, MaxRepairAttempts = 1 },
            Capabilities = new AgentProfileCapabilities { SupportsDeterministic = true },
            Knowledge = new DomainKnowledge(),
            Llm = new LLMConfig { Model = "fake", SystemPrompt = CppAdapterPrompt.BuildSystemPrompt() }
        });

        var brick = new GenerativeArtifactBrick(registry);
        var input = new BrickInput();
        input.Set("target", "cpp-evtx");
        input.Set("grounding", grounding);
        input.Set("preferDeterministic", false);
        input.Set("outputPath", Path.Combine(_outDir, "adapted_reader.hpp"));
        return await brick.ExecuteAsync(input, ImplementationType.Agentic, new FakeContext(), CancellationToken.None);
    }

    [Fact]
    public async Task ModelClient_retries_transient_failures_and_succeeds()
    {
        var fake = new RetryingModel(failuresBeforeSuccess: 2, response: GoodResponse);
        var output = await RunAsync(fake);
        fake.CallCount.Should().Be(3);
        output.Get<GeneratedArtifact>("artifact").Content.Should().Contain("readNextContact");
    }

    [Fact]
    public async Task ModelClient_gives_up_after_maxRetries()
    {
        var fake = new RetryingModel(failuresBeforeSuccess: 10, response: GoodResponse, maxRetries: 3);
        var act = () => RunAsync(fake);
        await act.Should().ThrowAsync<ModelUnavailableException>();
        fake.CallCount.Should().Be(3);
    }

    [Fact]
    public async Task ModelClient_does_not_retry_non_transient_errors()
    {
        var fake = new RetryingModel(failuresBeforeSuccess: 0, response: GoodResponse, throwNonTransient: true);
        var act = () => RunAsync(fake);
        await act.Should().ThrowAsync<InvalidOperationException>();
        fake.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Draft_flags_bare_template_echo_via_provenance()
    {
        var output = await RunAsync(new FixedModel(BareTemplateResponse));
        var artifact = output.Get<GeneratedArtifact>("artifact");
        var prov = artifact.Provenance ?? output.Get<GenerativeProvenance>(GenerativeBrick.ProvenanceOutputKey);
        prov.Should().NotBeNull();
        prov!.Warnings.Should().Contain(w => w.Contains("low source coverage", StringComparison.OrdinalIgnoreCase));
        CppAdapterPrompt.SourceApiCoverage(artifact.Content, CppAdapterPrompt.AssembleGrounding(
            _fixtureDir, new[] { "LegacyRadarLog.hpp", "LegacyRadarLog.cpp" })).Should().BeLessThan(0.15);
    }

    [Fact]
    public async Task Draft_flags_invented_symbols_as_unsupported_references()
    {
        var grounding = CppAdapterPrompt.AssembleGrounding(
            _fixtureDir, new[] { "LegacyRadarLog.hpp", "LegacyRadarLog.cpp" });
        var output = await RunAsync(new FixedModel(HallucinatingResponse), grounding);
        var prov = output.Get<GeneratedArtifact>("artifact").Provenance
            ?? output.Get<GenerativeProvenance>(GenerativeBrick.ProvenanceOutputKey);
        prov!.UnsupportedReferences.Should().Contain("MAX_CHUNKS");
    }

    [Fact]
    public async Task Draft_does_not_flag_real_extracted_methods()
    {
        var grounding = CppAdapterPrompt.AssembleGrounding(
            _fixtureDir, new[] { "LegacyRadarLog.hpp", "LegacyRadarLog.cpp" });
        var output = await RunAsync(new FixedModel(GoodResponse), grounding);
        var prov = output.Get<GeneratedArtifact>("artifact").Provenance
            ?? output.Get<GenerativeProvenance>(GenerativeBrick.ProvenanceOutputKey);
        prov!.UnsupportedReferences.Should().BeEmpty();
    }

    [Fact]
    public async Task Draft_empty_model_response_is_not_a_cpp_adapter()
    {
        var output = await RunAsync(new FixedModel("   "));
        var artifact = output.Get<GeneratedArtifact>("artifact");
        artifact.Content.Trim().Should().BeEmpty();
        var prov = artifact.Provenance
            ?? output.Get<GenerativeProvenance>(GenerativeBrick.ProvenanceOutputKey);
        prov!.Warnings.Should().Contain(w => w.Contains("did not look like", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AssembleGrounding_rejects_entry_traversal()
    {
        var act = () => CppAdapterPrompt.AssembleGrounding(
            _fixtureDir, new[] { "../proprietary/LegacyRadarLog.hpp" });
        act.Should().Throw<InvalidOperationException>();
    }

    private sealed class FakeContext : IExecutionContext
    {
        public string AgentId => "t";
        public string BehaviorId => "t";
        public bool IsAirGapped => true;
        public bool AuditMode => false;
        public string Provider => "mock";
        public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();
    }

    private sealed class FixedModel : ICppModelClient
    {
        private readonly string _response;
        public FixedModel(string response) => _response = response;
        public Task<string> CompleteAsync(
            string systemPrompt, string userPrompt, LLMConfig config, CancellationToken cancellationToken = default) =>
            Task.FromResult(_response);
    }

    private sealed class RetryingModel : ICppModelClient
    {
        private readonly int _failuresBeforeSuccess;
        private readonly string _response;
        private readonly bool _throwNonTransient;
        private readonly int _maxRetries;
        public int CallCount { get; private set; }

        public RetryingModel(int failuresBeforeSuccess, string response, bool throwNonTransient = false, int maxRetries = 3)
        {
            _failuresBeforeSuccess = failuresBeforeSuccess;
            _response = response;
            _throwNonTransient = throwNonTransient;
            _maxRetries = maxRetries;
        }

        public async Task<string> CompleteAsync(
            string systemPrompt, string userPrompt, LLMConfig config, CancellationToken cancellationToken = default)
        {
            // Mirror ProviderFactoryCppModelClient retry so tests stay focused on draft quality.
            var client = new ProviderFactoryCppModelClient(
                new CountingProvider(this),
                maxRetries: _maxRetries);
            return await client.CompleteAsync(systemPrompt, userPrompt, config, cancellationToken);
        }

        private sealed class CountingProvider : IProviderFactory
        {
            private readonly RetryingModel _outer;
            public CountingProvider(RetryingModel outer) => _outer = outer;
            public bool IsProviderAvailable(string provider) => true;
            public Task<string> ExecuteLLMAsync(
                string provider, string systemPrompt, string userPrompt, object config, CancellationToken cancellationToken = default)
            {
                _outer.CallCount++;
                if (_outer._throwNonTransient)
                    throw new InvalidOperationException("simulated non-transient failure (should not be retried)");
                if (_outer.CallCount <= _outer._failuresBeforeSuccess)
                    throw new ModelUnavailableException("simulated transient Ollama unavailability");
                return Task.FromResult(_outer._response);
            }
            public Task<string> ExecuteVisionAsync(string provider, string systemPrompt, string userPrompt, byte[] imageBytes, object config, CancellationToken cancellationToken = default)
                => throw new NotImplementedException();
            public Task<string> ExecuteVisionMultiFrameAsync(string provider, string systemPrompt, string userPrompt, IReadOnlyList<byte[]> frameBytes, object config, CancellationToken cancellationToken = default)
                => throw new NotImplementedException();
            public Task<string> ExecuteVideoAsync(string systemPrompt, string userPrompt, IReadOnlyList<byte[]> frameBytes, object config, CancellationToken cancellationToken = default)
                => throw new NotImplementedException();
            public Task EnsureOllamaReachableAsync(bool requireVisionModel, CancellationToken cancellationToken = default)
                => Task.CompletedTask;
        }
    }

    private const string GoodResponse = """
        ```cpp
        #pragma once
        #include "processing.hpp"
        namespace fileproc {
        class CustomEventReader : public IEventReader {
        public:
            explicit CustomEventReader(const std::filesystem::path& path) : reader_(path.string()) {}
            void set_requested_fields(const std::vector<std::string>& names) override {}
            bool decodes_named_fields() const override { return true; }
            bool next(Event& out, size_t max_depth) override {
                RadarContact c;
                if (!reader_.readNextContact(c)) return false;
                out.t = c.timestampSeconds;
                return true;
            }
            bool can_resume() const override { return true; }
            uint64_t position() const override { return reader_.currentByteOffset(); }
            void resume_at(uint64_t off) override { reader_.seekToByteOffset(off); }
        private:
            LegacyRadarLog reader_;
        };
        } // namespace fileproc
        ```
        """;

    private const string BareTemplateResponse = """
        ```cpp
        #pragma once
        #include "processing.hpp"
        namespace fileproc {
        class CustomEventReader : public IEventReader {
        public:
            explicit CustomEventReader(const std::filesystem::path& path) { (void)path; }
            void set_requested_fields(const std::vector<std::string>& names) override {}
            bool decodes_named_fields() const override { return true; }
            bool next(Event& out, size_t max_depth) override { (void)out; (void)max_depth; return false; }
            bool can_resume() const override { return true; }
            uint64_t position() const override { return 0; }
            void resume_at(uint64_t off) override { (void)off; }
        };
        } // namespace fileproc
        ```
        """;

    private const string HallucinatingResponse = """
        ```cpp
        #pragma once
        #include "processing.hpp"
        namespace fileproc {
        class CustomEventReader : public IEventReader {
        public:
            explicit CustomEventReader(const std::filesystem::path& path) : reader_(path.string()) {}
            void set_requested_fields(const std::vector<std::string>& names) override {}
            bool decodes_named_fields() const override { return true; }
            bool next(Event& out, size_t max_depth) override {
                RadarContact c;
                if (!reader_.readNextContact(c)) return false;
                out.t = c.timestampSeconds;
                return true;
            }
            bool can_resume() const override { return reader_.currentByteOffset() < fileproc::MAX_CHUNKS; }
            uint64_t position() const override { return reader_.currentByteOffset(); }
            void resume_at(uint64_t off) override { reader_.seekToByteOffset(off); }
        private:
            LegacyRadarLog reader_;
        };
        } // namespace fileproc
        ```
        """;
}
