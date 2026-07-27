using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Bricks.DepExtract.Gui;
using Nexo.Infrastructure.Execution;
using Xunit;

namespace Nexo.Bricks.DepExtract.Gui.Tests;

/// <summary>
/// Phase C (adapt-cycle failure loops): proves the AdaptInstallLoop retry orchestration
/// itself — build-fail-then-succeed, extract-smoke-fail-then-succeed, and the SKIP_OLLAMA=1
/// short-circuit — without needing real Docker or a live Ollama. Uses a fake IInstallExecutor
/// (the "test-only hook" substitute for a flaky live-LLM build/extract fixture) plus a fake
/// IProviderFactory for the forced model path on retry. The scaffold/compile happy path is
/// already covered live (Phase A/B); the Ollama-transient-failure retry inside the adapter
/// itself is covered by CppEvtxProfileScoringTests.
/// </summary>
public sealed class AdaptInstallLoopFailureLoopTests : IDisposable
{
    private readonly string _root;
    private readonly string _srcDir;
    private readonly string? _originalTmpDir;

    public AdaptInstallLoopFailureLoopTests()
    {
        _originalTmpDir = Environment.GetEnvironmentVariable("TMPDIR");
        _root = Path.Combine(Path.GetTempPath(), "adapt-loop-test-" + Guid.NewGuid());
        _srcDir = Path.Combine(_root, "proprietary-demo", "T1_TrivialCounter");
        Directory.CreateDirectory(_srcDir);
        var fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "stress", "T1_TrivialCounter", "Counter.hpp");
        File.Copy(fixture, Path.Combine(_srcDir, "Counter.hpp"));

        // Stack-identity preflight (ComposePipeline.EnsureStackIdentity) needs a per-project
        // env file under WORKSPACE_ROOT matching the default COMPOSE_PROJECT.
        var envDir = Path.Combine(_root, ".parser-pipeline");
        Directory.CreateDirectory(envDir);
        File.WriteAllText(Path.Combine(envDir, "nexo-parser-pipeline.env"), "COMPOSE_PROJECT=nexo-parser-pipeline\n");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    private WebApplicationFactory<Program> MakeFactory(IInstallExecutor installer, IProviderFactory? provider = null)
    {
        Environment.SetEnvironmentVariable("WORKSPACE_ROOT", _root);
        var tmpDir = Path.Combine(_root, ".tmp");
        Directory.CreateDirectory(tmpDir);
        Environment.SetEnvironmentVariable("TMPDIR", tmpDir);
        Environment.SetEnvironmentVariable("COMPILE_GATE", "0");
        return new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.ConfigureServices(services =>
        {
            services.AddSingleton(installer);
            if (provider is not null)
                services.AddSingleton(provider);
        }));
    }

    private async Task<string> CreatePlanAsync(HttpClient client, bool preferScaffold = true)
    {
        var before = Directory.Exists(PlanSessionStore.ResolvePlansDirectory())
            ? Directory.GetFiles(PlanSessionStore.ResolvePlansDirectory(), "*.json").ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var resp = await client.PostAsJsonAsync("/api/plan", new
        {
            srcDir = _srcDir,
            entries = new[] { "Counter.hpp" },
            preferScaffold,
            requireCompile = false,
        });

        // ASP.NET 8 TestHost + System.Text.Json 10 can fail Results.Json with
        // PipeWriter.UnflushedBytes after the plan is already persisted — recover planId from disk.
        if (resp.StatusCode == HttpStatusCode.OK)
        {
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var planId = doc.RootElement.GetProperty("planId").GetString();
            planId.Should().NotBeNullOrWhiteSpace();
            return planId!;
        }

        var created = Directory.GetFiles(PlanSessionStore.ResolvePlansDirectory(), "*.json")
            .Where(f => !before.Contains(f))
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
        created.Should().NotBeNull(
            "expected /api/plan to persist a plan even when TestHost JSON write fails: "
            + await resp.Content.ReadAsStringAsync());
        return Path.GetFileNameWithoutExtension(created!);
    }

    [Fact]
    public async Task AdaptCycle_retries_after_build_failure_and_succeeds_on_second_attempt()
    {
        var fake = new FakeInstallExecutor(n => n == 1
            ? Fail("docker build failed (companion compile gate): undefined symbol readNextContact",
                buildLog: "collect2: error: ld returned 1 exit status")
            : Ok(smokeRan: true, smokeOk: true, smokeDetail: "health+extract ok rows=5"));
        using var factory = MakeFactory(fake, new FakeProviderFactory(GoodResponse));
        var client = factory.CreateClient();
        try
        {
            var planId = await CreatePlanAsync(client);

            var resp = await client.PostAsJsonAsync("/api/adapt-cycle", new
            {
                planId,
                rebuildImage = true,
                recreateEvtx = true,
                maxBuildRetries = 2,
                requireCompile = false,
                confirmModelReview = true,
            });
            resp.StatusCode.Should().Be(HttpStatusCode.OK, await resp.Content.ReadAsStringAsync());
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            doc.RootElement.GetProperty("ok").GetBoolean().Should().BeTrue();
            var attempts = doc.RootElement.GetProperty("attempts").EnumerateArray().ToArray();
            attempts.Should().HaveCount(2, "attempt 1 fails the build, attempt 2 must succeed");
            attempts[0].GetProperty("installOk").GetBoolean().Should().BeFalse();
            attempts[1].GetProperty("installOk").GetBoolean().Should().BeTrue();
            fake.CallCount.Should().Be(2);
        }
        finally
        {
            Environment.SetEnvironmentVariable("WORKSPACE_ROOT", null);
            Environment.SetEnvironmentVariable("TMPDIR", _originalTmpDir);
            Environment.SetEnvironmentVariable("COMPILE_GATE", null);
        }
    }

    [Fact]
    public async Task AdaptCycle_scaffold_smoke_failure_surfaces_oracle_error_without_model_retry()
    {
        var fake = new FakeInstallExecutor(_ =>
            Fail("Install completed but smoke failed: extract produced no data rows for tiny_smoke.evtq",
                smokeRan: true, smokeOk: false, smokeDetail: "extract produced no data rows for tiny_smoke.evtq"));
        using var factory = MakeFactory(fake, new FakeProviderFactory(GoodResponse));
        var client = factory.CreateClient();
        try
        {
            var planId = await CreatePlanAsync(client);

            var resp = await client.PostAsJsonAsync("/api/adapt-cycle", new
            {
                planId,
                rebuildImage = true,
                recreateEvtx = true,
                smokeExtractFile = "tiny_smoke.evtq",
                maxBuildRetries = 2,
                requireCompile = false,
            });
            resp.StatusCode.Should().Be(HttpStatusCode.BadGateway, await resp.Content.ReadAsStringAsync());
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            doc.RootElement.GetProperty("error").GetString().Should().Be("adapt_cycle_failed");
            doc.RootElement.GetProperty("detail").GetString().Should().Contain("not retrying via the local model");
            var attempts = doc.RootElement.GetProperty("extras").GetProperty("attempts").EnumerateArray().ToArray();
            attempts.Should().HaveCount(1, "a deterministic scaffold cannot learn a proprietary wire format from model retries");
            attempts[0].GetProperty("smokeDetail").GetString().Should().Contain("no data rows");
            fake.CallCount.Should().Be(1);
        }
        finally
        {
            Environment.SetEnvironmentVariable("WORKSPACE_ROOT", null);
            Environment.SetEnvironmentVariable("TMPDIR", _originalTmpDir);
            Environment.SetEnvironmentVariable("COMPILE_GATE", null);
        }
    }

    [Fact]
    public async Task AdaptCycle_model_draft_retries_after_extract_smoke_failure_and_succeeds()
    {
        var fake = new FakeInstallExecutor(n => n == 1
            ? Fail("Install completed but smoke failed: extract produced no data rows for tiny_smoke.evtq",
                smokeRan: true, smokeOk: false, smokeDetail: "extract produced no data rows for tiny_smoke.evtq")
            : Ok(smokeRan: true, smokeOk: true, smokeDetail: "health+extract ok rows=3"));
        using var factory = MakeFactory(fake, new FakeProviderFactory(GoodResponse));
        var client = factory.CreateClient();
        try
        {
            var planId = await CreatePlanAsync(client, preferScaffold: false);

            var resp = await client.PostAsJsonAsync("/api/adapt-cycle", new
            {
                planId,
                rebuildImage = true,
                recreateEvtx = true,
                smokeExtractFile = "tiny_smoke.evtq",
                maxBuildRetries = 2,
                preferScaffold = false,
                requireCompile = false,
                confirmModelReview = true,
            });
            resp.StatusCode.Should().Be(HttpStatusCode.OK, await resp.Content.ReadAsStringAsync());
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            doc.RootElement.GetProperty("ok").GetBoolean().Should().BeTrue();
            var attempts = doc.RootElement.GetProperty("attempts").EnumerateArray().ToArray();
            attempts.Should().HaveCount(2, "model-authored drafts may use smoke feedback for a corrected second draft");
            attempts[0].GetProperty("installOk").GetBoolean().Should().BeFalse();
            attempts[1].GetProperty("installOk").GetBoolean().Should().BeTrue();
            fake.CallCount.Should().Be(2);
        }
        finally
        {
            Environment.SetEnvironmentVariable("WORKSPACE_ROOT", null);
            Environment.SetEnvironmentVariable("TMPDIR", _originalTmpDir);
            Environment.SetEnvironmentVariable("COMPILE_GATE", null);
        }
    }

    [Fact]
    public async Task AdaptCycle_with_SKIP_OLLAMA_does_not_retry_after_first_failure()
    {
        var fake = new FakeInstallExecutor(_ =>
            Fail("docker build failed (companion compile gate): undefined symbol readNextContact",
                buildLog: "collect2: error: ld returned 1 exit status"));
        using var factory = MakeFactory(fake, new FakeProviderFactory(GoodResponse));
        var client = factory.CreateClient();
        Environment.SetEnvironmentVariable("SKIP_OLLAMA", "1");
        try
        {
            var planId = await CreatePlanAsync(client);

            var resp = await client.PostAsJsonAsync("/api/adapt-cycle", new
            {
                planId,
                rebuildImage = true,
                recreateEvtx = true,
                maxBuildRetries = 2,
                requireCompile = false,
            });
            resp.StatusCode.Should().Be(HttpStatusCode.BadGateway, await resp.Content.ReadAsStringAsync());
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            doc.RootElement.GetProperty("error").GetString().Should().Be("adapt_cycle_failed");
            doc.RootElement.GetProperty("detail").GetString().Should().Contain("SKIP_OLLAMA=1 blocks model re-adapt");
            var attempts = doc.RootElement.GetProperty("extras").GetProperty("attempts").EnumerateArray().ToArray();
            attempts.Should().HaveCount(1, "SKIP_OLLAMA=1 must fail fast after the first install failure, not burn a model retry");
            fake.CallCount.Should().Be(1, "no second install call — no model re-adapt happened under SKIP_OLLAMA=1");
        }
        finally
        {
            Environment.SetEnvironmentVariable("WORKSPACE_ROOT", null);
            Environment.SetEnvironmentVariable("TMPDIR", _originalTmpDir);
            Environment.SetEnvironmentVariable("COMPILE_GATE", null);
            Environment.SetEnvironmentVariable("SKIP_OLLAMA", null);
        }
    }

    private static InstallResult Fail(
        string error, string? buildLog = null, bool smokeRan = false, bool? smokeOk = null, string? smokeDetail = null) =>
        new(false, error, "", null, buildLog, "http://127.0.0.1:18080/", error,
            Array.Empty<string>(), null, smokeRan, smokeOk, smokeDetail, Array.Empty<string>());

    private static InstallResult Ok(bool smokeRan, bool? smokeOk, string? smokeDetail) =>
        new(true, null, "/tmp/adapted_reader.hpp", "evtx:custom", "build ok", "http://127.0.0.1:18080/",
            "Adapter installed. Open the Poco parser GUI to run extractions.",
            Array.Empty<string>(), null, smokeRan, smokeOk, smokeDetail, Array.Empty<string>());

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
                long v;
                if (!reader_.advance(v)) return false;
                out.t = static_cast<double>(v);
                return true;
            }
            bool can_resume() const override { return true; }
            uint64_t position() const override { return 0; }
            void resume_at(uint64_t off) override {}
        private:
            SimpleCounter reader_;
        };
        } // namespace fileproc
        ```
        """;

    /// <summary>Deterministic drop-in for the real InstallExecutor — no Docker/Poco touched.</summary>
    private sealed class FakeInstallExecutor : IInstallExecutor
    {
        private readonly Func<int, InstallResult> _resultFactory;
        public int CallCount { get; private set; }

        public FakeInstallExecutor(Func<int, InstallResult> resultFactory) => _resultFactory = resultFactory;

        public Task<InstallResult> RunSyncAsync(
            InstallWorkRequest req, Func<string, PlanSession?>? resolvePlan, CancellationToken ct)
        {
            CallCount++;
            return Task.FromResult(_resultFactory(CallCount));
        }
    }

    /// <summary>Deterministic drop-in for Ollama — always returns the same good draft.</summary>
    private sealed class FakeProviderFactory : IProviderFactory
    {
        private readonly string _response;
        public FakeProviderFactory(string response) => _response = response;

        public bool IsProviderAvailable(string provider) => true;

        public Task<string> ExecuteLLMAsync(
            string provider, string systemPrompt, string userPrompt, object config,
            CancellationToken cancellationToken = default) => Task.FromResult(_response);

        public Task<string> ExecuteVisionAsync(
            string provider, string systemPrompt, string userPrompt, byte[] imageBytes, object config,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<string> ExecuteVisionMultiFrameAsync(
            string provider, string systemPrompt, string userPrompt, IReadOnlyList<byte[]> frameBytes, object config,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<string> ExecuteVideoAsync(
            string systemPrompt, string userPrompt, IReadOnlyList<byte[]> frameBytes, object config,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task EnsureOllamaReachableAsync(bool requireVisionModel, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
