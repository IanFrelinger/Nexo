using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Bricks.DepExtract;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;
using Xunit;

namespace Nexo.Bricks.DepExtract.Tests;

/// <summary>
/// Unit tests for the two things the stress test report called out: retry on
/// transient Ollama failures, and the two quality heuristics (template-echo
/// detection, hallucination detection). Uses a fake IProviderFactory instead of
/// real Ollama so failure injection is deterministic and fast — the real-Ollama
/// happy path is already covered by CppParserAdapterBrickTests.
/// </summary>
public sealed class CppParserAdapterBrickRetryAndScoringTests : IDisposable
{
    private readonly string _fixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "proprietary");
    private readonly string _outDir = Path.Combine(Path.GetTempPath(), "dep-extract-retry-test-" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_outDir)) Directory.Delete(_outDir, recursive: true);
    }

    private BrickInput Input(string? code = null) => new(new Dictionary<string, object>
    {
        ["parserDir"] = _fixtureDir,
        ["entryFiles"] = new[] { "LegacyRadarLog.hpp", "LegacyRadarLog.cpp" },
        ["outputPath"] = Path.Combine(_outDir, "adapted_reader.hpp"),
        ["maxRetries"] = 3,
    });

    private static AirGappedTestContext Ctx() => new();

    [Fact]
    public async Task ExecuteAsync_retries_transient_failures_and_succeeds()
    {
        var fake = new FakeProviderFactory(failuresBeforeSuccess: 2, response: GoodResponse);
        var adapter = new CppParserAdapterBrick(fake, NullLogger<CppParserAdapterBrick>.Instance);

        var output = await adapter.ExecuteAsync(Input(), ImplementationType.Agentic, Ctx());

        fake.CallCount.Should().Be(3, "it should fail twice, then succeed on the third attempt");
        output.Get<string>("adapterCode").Should().Contain("readNextContact");
    }

    [Fact]
    public async Task ExecuteAsync_gives_up_after_maxRetries_transient_failures()
    {
        var fake = new FakeProviderFactory(failuresBeforeSuccess: 10, response: GoodResponse); // more failures than maxRetries=3
        var adapter = new CppParserAdapterBrick(fake, NullLogger<CppParserAdapterBrick>.Instance);

        var act = () => adapter.ExecuteAsync(Input(), ImplementationType.Agentic, Ctx());

        await act.Should().ThrowAsync<ModelUnavailableException>();
        fake.CallCount.Should().Be(3, "it should stop retrying once maxRetries attempts are used up");
    }

    [Fact]
    public async Task ExecuteAsync_does_not_retry_non_transient_errors()
    {
        var fake = new FakeProviderFactory(failuresBeforeSuccess: 0, response: GoodResponse, throwNonTransient: true);
        var adapter = new CppParserAdapterBrick(fake, NullLogger<CppParserAdapterBrick>.Instance);

        var act = () => adapter.ExecuteAsync(Input(), ImplementationType.Agentic, Ctx());

        await act.Should().ThrowAsync<InvalidOperationException>();
        fake.CallCount.Should().Be(1, "a non-transient error should propagate immediately, not burn retries");
    }

    [Fact]
    public async Task ExecuteAsync_flags_bare_template_echo_as_low_confidence()
    {
        var fake = new FakeProviderFactory(failuresBeforeSuccess: 0, response: BareTemplateResponse);
        var adapter = new CppParserAdapterBrick(fake, NullLogger<CppParserAdapterBrick>.Instance);

        var output = await adapter.ExecuteAsync(Input(), ImplementationType.Agentic, Ctx());

        output.Get<bool>("looksLikeTemplateEcho").Should().BeTrue();
        output.Get<double>("sourceApiCoverage").Should().BeLessThan(0.15);
        output.Summary.Should().Contain("WARNING");
    }

    [Fact]
    public async Task ExecuteAsync_does_not_flag_a_real_integration_as_template_echo()
    {
        var fake = new FakeProviderFactory(failuresBeforeSuccess: 0, response: GoodResponse);
        var adapter = new CppParserAdapterBrick(fake, NullLogger<CppParserAdapterBrick>.Instance);

        var output = await adapter.ExecuteAsync(Input(), ImplementationType.Agentic, Ctx());

        output.Get<bool>("looksLikeTemplateEcho").Should().BeFalse();
        output.Get<double>("sourceApiCoverage").Should().BeGreaterThan(0.15);
    }

    [Fact]
    public async Task ExecuteAsync_flags_invented_symbols_as_possible_hallucinations()
    {
        var fake = new FakeProviderFactory(failuresBeforeSuccess: 0, response: HallucinatingResponse);
        var adapter = new CppParserAdapterBrick(fake, NullLogger<CppParserAdapterBrick>.Instance);

        var output = await adapter.ExecuteAsync(Input(), ImplementationType.Agentic, Ctx());

        var hallucinations = output.Get<string[]>("possibleHallucinations");
        hallucinations.Should().Contain("MAX_CHUNKS", "it's referenced via fileproc:: but doesn't exist in the source or the contract");
        output.Summary.Should().Contain("WARNING");
    }

    [Fact]
    public async Task ExecuteAsync_does_not_flag_real_extracted_methods_as_hallucinations()
    {
        var fake = new FakeProviderFactory(failuresBeforeSuccess: 0, response: GoodResponse);
        var adapter = new CppParserAdapterBrick(fake, NullLogger<CppParserAdapterBrick>.Instance);

        var output = await adapter.ExecuteAsync(Input(), ImplementationType.Agentic, Ctx());

        output.Get<string[]>("possibleHallucinations").Should().BeEmpty();
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
            explicit CustomEventReader(const std::filesystem::path& path) {
                // TODO: open `path` using the proprietary parser's real constructor/open call
            }
            void set_requested_fields(const std::vector<std::string>& names) override {}
            bool decodes_named_fields() const override { return true; }
            bool next(Event& out, size_t max_depth) override {
                // TODO: read ONE record using the proprietary parser's real "read next" call.
                return false;
            }
            bool can_resume() const override { return true; }
            uint64_t position() const override { return pos_; }
            void resume_at(uint64_t off) override { /* TODO: seek */ }
        private:
            uint64_t pos_ = 0;
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

    /// <summary>Fails a fixed number of times with a transient (or a non-transient) error, then succeeds.</summary>
    private sealed class FakeProviderFactory : IProviderFactory
    {
        private readonly int _failuresBeforeSuccess;
        private readonly string _response;
        private readonly bool _throwNonTransient;
        public int CallCount { get; private set; }

        public FakeProviderFactory(int failuresBeforeSuccess, string response, bool throwNonTransient = false)
        {
            _failuresBeforeSuccess = failuresBeforeSuccess;
            _response = response;
            _throwNonTransient = throwNonTransient;
        }

        public bool IsProviderAvailable(string provider) => true;

        public Task<string> ExecuteLLMAsync(string provider, string systemPrompt, string userPrompt, object config, CancellationToken cancellationToken = default)
        {
            CallCount++;
            if (_throwNonTransient)
                throw new InvalidOperationException("simulated non-transient failure (should not be retried)");
            if (CallCount <= _failuresBeforeSuccess)
                throw new ModelUnavailableException("simulated transient Ollama unavailability");
            return Task.FromResult(_response);
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
