using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Nexo.Bricks.DepExtract.Profile;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Core.Application.Generation;
using Nexo.Core.Application.Generation.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Bricks.Ports;
using Nexo.Infrastructure.Execution;
using Nexo.Infrastructure.Execution.Scratch;

namespace Nexo.Bricks.DepExtract;

/// <summary>
/// Full C++/EVTX <see cref="AgentProfile"/> factory. Drives
/// <c>GenerativeArtifactBrick</c> with target <see cref="TargetId"/> — zero
/// edits to that brick for language support.
/// </summary>
public sealed class CppEvtxProfileFactory : IAgentProfileFactory
{
    /// <summary>Stable target id for the C++ EVTX adapter profile.</summary>
    public const string TargetId = "cpp-evtx";

    /// <inheritdoc />
    public AgentProfile Create(IServiceProvider services)
    {
        var poco = Environment.GetEnvironmentVariable("POCO_CONTEXT");
        var requireCompile = string.Equals(
            Environment.GetEnvironmentVariable("COMPILE_GATE"), "1", StringComparison.Ordinal);

        var runner = services.GetService<ISandboxedCommandRunner>();
        var scratch = services.GetService<IScratchSpace>() ?? new FileScratchSpace();
        var files = services.GetService<IManagedFileSet>() ?? new SnapshotManagedFileSet();
        var flight = services.GetService<ISingleFlightGuard>() ?? new SingleFlightGuard();
        var ops = services.GetService<IPocoDeploymentOps>() ?? new UnconfiguredPocoDeploymentOps();
        var providers = services.GetService<IProviderFactory>();
        var modelClient = services.GetService<ICppModelClient>()
            ?? (providers is null
                ? new UnavailableCppModelClient()
                : new ProviderFactoryCppModelClient(
                    providers,
                    maxRetries: 3,
                    logger: services.GetService<ILogger<ProviderFactoryCppModelClient>>()));

        var gxxOptions = new GxxCompileValidatorOptions
        {
            PocoContext = poco,
            RequireSuccess = requireCompile,
            Image = "dep-extract:latest",
            Entrypoint = "g++"
        };
        var validators = runner is null
            ? Array.Empty<object>()
            : new object[]
            {
                new GxxCompileValidator(
                    runner,
                    scratch,
                    gxxOptions,
                    services.GetService<ILogger<GxxCompileValidator>>())
            };

        var llm = new LLMConfig
        {
            Model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "qwen2.5-coder:7b",
            SystemPrompt = CppAdapterPrompt.BuildSystemPrompt(),
            Temperature = 0.1,
            MaxTokens = 4000
        };

        return new AgentProfile
        {
            TargetId = TargetId,
            Drafter = new CppArtifactDrafter(modelClient, new CppArtifactDrafterOptions
            {
                PocoContext = poco,
                Llm = llm
            }),
            DeterministicDrafter = new CppDeterministicDrafter(),
            Validators = validators,
            Sandbox = new DockerSandboxProvider(new DockerSandboxProviderOptions
            {
                PocoContext = poco,
                Image = "dep-extract:latest",
                Entrypoint = "g++"
            }),
            Deployment = new PocoInstallTarget(
                files,
                flight,
                ops,
                new PocoInstallTargetOptions
                {
                    PocoContext = poco,
                    RebuildImage = true,
                    RecreateEvtx = true,
                    Strategy = "scaffold"
                }),
            Knowledge = new DomainKnowledge
            {
                Standards = new[] { "IEventReader / fileproc contract", "air-gapped local model draft" },
                ReferenceData = new Dictionary<string, string>
                {
                    ["contractVocabulary"] = string.Join(",", CppAdapterPrompt.KnownContractVocabulary.OrderBy(x => x))
                }
            },
            Llm = llm,
            Tunables = new GenerationTunables
            {
                PreferDeterministic = true,
                MaxRepairAttempts = 3
            },
            Capabilities = new AgentProfileCapabilities
            {
                SupportsDeterministic = true,
                SupportsSandbox = true,
                SupportsDeployment = true
            }
        };
    }

    private sealed class UnconfiguredPocoDeploymentOps : IPocoDeploymentOps
    {
        public void EnsureStackIdentity() =>
            throw new InvalidOperationException(
                "IPocoDeploymentOps is not registered — call AddNexoCppEvtxAgentProfile and register stack ops.");

        public Task RetagImageAsync(string fromTag, string toTag, CancellationToken cancellationToken) =>
            Task.FromException(new InvalidOperationException("IPocoDeploymentOps not registered"));

        public Task<(bool Ok, string Log)> BuildCustomImageAsync(
            string pocoContext, string imageTag, CancellationToken cancellationToken) =>
            Task.FromResult((false, "IPocoDeploymentOps not registered"));

        public Task<(bool Ok, string Log)> RecreateServiceAsync(
            string service, bool waitHealthy, CancellationToken cancellationToken) =>
            Task.FromResult((false, "IPocoDeploymentOps not registered"));

        public Task<(bool Ok, string Detail)> SmokeAsync(
            string? extractFile, CancellationToken cancellationToken) =>
            Task.FromResult((false, "IPocoDeploymentOps not registered"));

        public void UpsertEnvVar(string key, string value) { }
    }
}

/// <summary>Fallback model client when no provider factory is registered.</summary>
internal sealed class UnavailableCppModelClient : ICppModelClient
{
    /// <inheritdoc />
    public Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        LLMConfig config,
        CancellationToken cancellationToken = default) =>
        throw new ModelUnavailableException(
            "No IProviderFactory/ICppModelClient registered for cpp-evtx model drafting.");
}

/// <summary>DI helpers for the C++ EVTX agent profile plugin.</summary>
public static class CppEvtxProfileServiceCollectionExtensions
{
    /// <summary>
    /// Registers the cpp-evtx agent profile. Pair with
    /// <c>AddNexoGenerativeAgents()</c> from Nexo.Authoring so
    /// <see cref="IAgentProfileRegistry"/> loads this source.
    /// </summary>
    public static IServiceCollection AddNexoCppEvtxAgentProfile(this IServiceCollection services)
    {
        services.TryAddSingleton<IScratchSpace, FileScratchSpace>();
        services.TryAddSingleton<IManagedFileSet, SnapshotManagedFileSet>();
        services.TryAddSingleton<ISingleFlightGuard, SingleFlightGuard>();
        services.TryAddSingleton<ICppModelClient>(sp =>
        {
            var providers = sp.GetService<IProviderFactory>();
            if (providers is null)
                return new UnavailableCppModelClient();
            return new ProviderFactoryCppModelClient(
                providers,
                maxRetries: 3,
                logger: sp.GetService<ILogger<ProviderFactoryCppModelClient>>());
        });
        services.TryAddSingleton<IAgentProfileRegistry>(sp =>
            new AgentProfileRegistry(sp.GetServices<IAgentProfileSource>(), sp));
        services.AddSingleton<CppEvtxProfileFactory>();
        services.AddSingleton<IAgentProfileSource>(sp => new CppEvtxProfileSource(sp));
        return services;
    }

    private sealed class CppEvtxProfileSource : IAgentProfileSource
    {
        private readonly IServiceProvider _sp;
        public CppEvtxProfileSource(IServiceProvider sp) => _sp = sp;
        public AgentProfile Create(IServiceProvider services) =>
            services.GetRequiredService<CppEvtxProfileFactory>().Create(services);
    }
}
