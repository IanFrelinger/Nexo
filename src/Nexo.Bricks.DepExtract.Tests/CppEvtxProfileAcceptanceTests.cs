using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Bricks.DepExtract.Profile;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Core.Application.Generation;
using Nexo.Core.Application.Generation.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Bricks.Ports;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution.Scratch;
using Xunit;

namespace Nexo.Bricks.DepExtract.Tests;

/// <summary>
/// Acceptance: target=cpp-evtx drives GenerativeArtifactBrick end-to-end
/// (deterministic scaffold, model+compile-repair, rollback-on-failed-install).
/// </summary>
public sealed class CppEvtxProfileAcceptanceTests
{
    [Fact]
    public async Task CppEvtx_DeterministicScaffold_WithCompileGateOff()
    {
        var previous = Environment.GetEnvironmentVariable("COMPILE_GATE");
        Environment.SetEnvironmentVariable("COMPILE_GATE", "0");
        try
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IScratchSpace, FileScratchSpace>();
            services.AddSingleton<IManagedFileSet, SnapshotManagedFileSet>();
            services.AddSingleton<ISingleFlightGuard, SingleFlightGuard>();
            services.AddSingleton<ICppModelClient>(new ScriptedModel(_ => ""));
            services.AddSingleton<IPocoDeploymentOps, FakeOps>();
            // Soft compile validator (no docker) — RequireSuccess false.
            services.AddSingleton<ISandboxedCommandRunner>(new FakeSandboxRunner(
                (_, _) => Task.FromResult(new ProcessCommandResult(0, "ok", ""))));
            services.AddNexoCppEvtxAgentProfile();
            services.AddSingleton<GenerativeArtifactBrick>();
            await using var sp = services.BuildServiceProvider();

            var brick = sp.GetRequiredService<GenerativeArtifactBrick>();
            var grounding = """
                class SimpleStream {
                public:
                    explicit SimpleStream(const std::string& path);
                    bool advance();
                    long last_type() const;
                };
                """;
            var input = new BrickInput();
            input.Set("target", CppEvtxProfileFactory.TargetId);
            input.Set("grounding", grounding);
            input.Set("preferDeterministic", true);
            input.Set("overrides", new Dictionary<string, object>
            {
                ["entryInclude"] = "SimpleStream.hpp"
            });

            var output = await brick.ExecuteAsync(
                input,
                ImplementationType.Deterministic,
                new FakeContext(),
                CancellationToken.None);

            var artifact = output.Get<GeneratedArtifact>("artifact");
            artifact.Content.Should().Contain("CustomEventReader");
            artifact.Content.Should().Contain("SimpleStream");
            output.Get<string>("generationStrategy").Should().Be("Templated");
        }
        finally
        {
            Environment.SetEnvironmentVariable("COMPILE_GATE", previous);
        }
    }

    [Fact]
    public async Task CppEvtx_ModelPath_RepairFromCompileFeedback()
    {
        var calls = 0;
        var model = new ScriptedModel(_ =>
        {
            calls++;
            if (calls == 1)
            {
                return """
                    #pragma once
                    #include "processing.hpp"
                    namespace fileproc {
                    class CustomEventReader : public IEventReader {
                    public:
                      explicit CustomEventReader(const std::filesystem::path& p) { (void)p; }
                      void set_requested_fields(const std::vector<std::string>& n) override { (void)n; }
                      bool decodes_named_fields() const override { return false; }
                      bool next(Event& out, size_t d) override { (void)d; broken_call(); return false; }
                      bool can_resume() const override { return false; }
                      uint64_t position() const override { return 0; }
                      void resume_at(uint64_t o) override { (void)o; }
                    };
                    inline void use_custom_reader() {}
                    }
                    """;
            }

            return """
                #pragma once
                #include "processing.hpp"
                namespace fileproc {
                class CustomEventReader : public IEventReader {
                public:
                  explicit CustomEventReader(const std::filesystem::path& p) { (void)p; }
                  void set_requested_fields(const std::vector<std::string>& n) override { (void)n; }
                  bool decodes_named_fields() const override { return false; }
                  bool next(Event& out, size_t d) override { (void)d; out.data.clear(); return false; }
                  bool can_resume() const override { return false; }
                  uint64_t position() const override { return 0; }
                  void resume_at(uint64_t o) override { (void)o; }
                };
                inline void use_custom_reader() {}
                }
                """;
        });

        var sandboxCalls = 0;
        var runner = new FakeSandboxRunner((_, _) =>
        {
            sandboxCalls++;
            if (sandboxCalls == 1)
                return Task.FromResult(new ProcessCommandResult(1, "", "error: 'broken_call' was not declared"));
            return Task.FromResult(new ProcessCommandResult(0, "ok", ""));
        });

        var registry = new AgentProfileRegistry();
        registry.Register(new AgentProfile
        {
            TargetId = "cpp-evtx",
            Drafter = new CppArtifactDrafter(model, new CppArtifactDrafterOptions()),
            DeterministicDrafter = new CppDeterministicDrafter(),
            Validators = new object[]
            {
                new GxxCompileValidator(
                    runner,
                    new FileScratchSpace(),
                    new GxxCompileValidatorOptions { RequireSuccess = true, PocoContext = CreateMinimalPoco() })
            },
            Tunables = new GenerationTunables { PreferDeterministic = false, MaxRepairAttempts = 3 },
            Capabilities = new AgentProfileCapabilities
            {
                SupportsDeterministic = true,
                SupportsSandbox = true
            },
            Knowledge = new DomainKnowledge(),
            Llm = new LLMConfig { Model = "fake", SystemPrompt = CppAdapterPrompt.BuildSystemPrompt() }
        });

        var brick = new GenerativeArtifactBrick(registry);
        var input = new BrickInput();
        input.Set("target", "cpp-evtx");
        input.Set("grounding", "class P { void advance(); };");
        input.Set("preferDeterministic", false);

        var output = await brick.ExecuteAsync(
            input, ImplementationType.Agentic, new FakeContext(), CancellationToken.None);

        output.Get<bool>("verified").Should().BeTrue();
        calls.Should().Be(2);
        sandboxCalls.Should().Be(2);
        output.Get<GeneratedArtifact>("artifact").Content.Should().NotContain("broken_call");
    }

    [Fact]
    public async Task CppEvtx_Deployment_RollsBackManagedFiles_OnBuildFailure()
    {
        var poco = CreateMinimalPoco();
        await File.WriteAllTextAsync(Path.Combine(poco, "common", "adapted_reader.hpp"), "PRIOR");

        var files = new SnapshotManagedFileSet();
        var ops = new FakeOps { BuildOk = false };
        var deploy = new PocoInstallTarget(
            files,
            new SingleFlightGuard(),
            ops,
            new PocoInstallTargetOptions
            {
                PocoContext = poco,
                RebuildImage = true,
                RecreateEvtx = true,
                Strategy = "scaffold",
                CompileGatePassed = true
            });

        var registry = new AgentProfileRegistry();
        registry.Register(new AgentProfile
        {
            TargetId = "cpp-evtx",
            Drafter = new CppArtifactDrafter(new ScriptedModel(_ => "#pragma once\nclass CustomEventReader {};\ninline void use_custom_reader(){}\n"),
                new CppArtifactDrafterOptions()),
            DeterministicDrafter = new FixedDetDrafter("#pragma once\nclass CustomEventReader {};\ninline void use_custom_reader(){}\n"),
            Validators = Array.Empty<object>(),
            Deployment = deploy,
            Tunables = new GenerationTunables { PreferDeterministic = true, MaxRepairAttempts = 1 },
            Capabilities = new AgentProfileCapabilities
            {
                SupportsDeterministic = true,
                SupportsDeployment = true
            },
            Knowledge = new DomainKnowledge(),
            Llm = new LLMConfig { Model = "none" }
        });

        // Force verified + no human review so deployment runs.
        var brick = new GenerativeArtifactBrick(registry);
        // Use a profile whose deterministic draft has RequiresHumanReview=false via FixedDetDrafter
        var input = new BrickInput();
        input.Set("target", "cpp-evtx");
        input.Set("grounding", "x");

        var output = await brick.ExecuteAsync(
            input, ImplementationType.Deterministic, new FakeContext { AuditMode = false }, CancellationToken.None);

        var deployment = output.Get<DeploymentApplyResult>("deploymentResult");
        deployment.Should().NotBeNull();
        deployment!.Succeeded.Should().BeFalse();
        (await File.ReadAllTextAsync(Path.Combine(poco, "common", "adapted_reader.hpp"))).Should().Be("PRIOR");
    }

    private static string CreateMinimalPoco()
    {
        var root = Directory.CreateDirectory(
            Path.Combine(Path.GetTempPath(), "cpp-acc-" + Guid.NewGuid().ToString("n"))).FullName;
        Directory.CreateDirectory(Path.Combine(root, "common"));
        Directory.CreateDirectory(Path.Combine(root, "poco"));
        File.WriteAllText(Path.Combine(root, "common", "processing.hpp"), "#pragma once\nstruct Event{};\nstruct FileInfo{};\n");
        File.WriteAllText(Path.Combine(root, "poco", "main.cpp"), "int main(){return 0;}\n");
        File.WriteAllText(Path.Combine(root, "Dockerfile.custom"), "FROM scratch\n");
        return root;
    }

    private sealed class FixedDetDrafter : IDeterministicDrafter
    {
        private readonly string _code;
        public FixedDetDrafter(string code) => _code = code;
        public bool TryDraft(GenerationRequest request, out GeneratedArtifact? artifact)
        {
            artifact = new GeneratedArtifact
            {
                Content = _code,
                Provenance = GenerativeProvenance.Create(
                    GenerationStrategy.Templated,
                    verified: true,
                    requiresHumanReview: false)
            };
            return true;
        }
    }

    private sealed class ScriptedModel : ICppModelClient
    {
        private readonly Func<int, string> _script;
        private int _n;
        public ScriptedModel(Func<int, string> script) => _script = script;
        public Task<string> CompleteAsync(
            string systemPrompt, string userPrompt, LLMConfig config, CancellationToken cancellationToken = default)
        {
            _n++;
            return Task.FromResult(_script(_n));
        }
    }

    private sealed class FakeSandboxRunner : ISandboxedCommandRunner
    {
        private readonly Func<SandboxSpec, CancellationToken, Task<ProcessCommandResult>> _h;
        public FakeSandboxRunner(Func<SandboxSpec, CancellationToken, Task<ProcessCommandResult>> h) => _h = h;
        public Task<ProcessCommandResult> RunAsync(SandboxSpec spec, CancellationToken cancellationToken = default) =>
            _h(spec, cancellationToken);
    }

    private sealed class FakeOps : IPocoDeploymentOps
    {
        public bool BuildOk { get; set; } = true;
        public void EnsureStackIdentity() { }
        public Task RetagImageAsync(string fromTag, string toTag, CancellationToken cancellationToken) =>
            Task.CompletedTask;
        public Task<(bool Ok, string Log)> BuildCustomImageAsync(
            string pocoContext, string imageTag, CancellationToken cancellationToken) =>
            Task.FromResult((BuildOk, BuildOk ? "ok" : "build failed"));
        public Task<(bool Ok, string Log)> RecreateServiceAsync(
            string service, bool waitHealthy, CancellationToken cancellationToken) =>
            Task.FromResult((true, "ok"));
        public Task<(bool Ok, string Detail)> SmokeAsync(
            string? extractFile, CancellationToken cancellationToken) =>
            Task.FromResult((true, "ok"));
        public void UpsertEnvVar(string key, string value) { }
    }

    private sealed class FakeContext : IExecutionContext
    {
        public string AgentId => "t";
        public string BehaviorId => "t";
        public bool IsAirGapped => false;
        public bool AuditMode { get; init; }
        public string Provider => "mock";
        public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();
    }
}
