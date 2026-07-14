using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Nexo.AI.Pipeline.Clients;
using Nexo.AI.Pipeline.Governance;
using Nexo.AI.Pipeline.Routing;

namespace Nexo.AI.Pipeline;

/// <summary>
/// DI registration for the MEAI chat pipeline (feature-flagged).
/// </summary>
public static class MeaiPipelineServiceCollectionExtensions
{
    /// <summary>
    /// Returns true when the MEAI pipeline should be registered.
    /// Resolution order: explicit <paramref name="explicitEnable"/> →
    /// <c>Nexo:UseMeaiPipeline</c> config → <c>NEXO_USE_MEAI_PIPELINE</c> env → false.
    /// </summary>
    public static bool IsMeaiPipelineEnabled(IConfiguration? configuration, bool? explicitEnable = null)
    {
        if (explicitEnable.HasValue)
        {
            return explicitEnable.Value;
        }

        if (configuration is not null)
        {
            var flagged = configuration[MeaiPipelineOptions.FeatureFlagKey];
            if (!string.IsNullOrWhiteSpace(flagged)
                && bool.TryParse(flagged, out var parsed))
            {
                return parsed;
            }

            if (string.Equals(flagged, "1", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        var env = Environment.GetEnvironmentVariable(MeaiPipelineOptions.FeatureFlagEnvVar);
        return string.Equals(env, "1", StringComparison.OrdinalIgnoreCase)
               || string.Equals(env, "true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Registers keyed <see cref="IChatClient"/> pipelines for <c>local:ollama</c> and <c>local:onnx</c>
    /// with the fixed Nexo governance stack, plus a default auditing <see cref="RoutingChatClient"/>.
    /// Raw provider clients are never registered in DI.
    /// </summary>
    public static IServiceCollection AddNexoMeaiPipeline(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<MeaiPipelineOptions>? configure = null,
        Func<IServiceProvider, IChatClient>? ollamaInnerFactory = null,
        Func<IServiceProvider, IChatClient>? onnxInnerFactory = null,
        bool registerDefaultRouter = true)
    {
        var options = new MeaiPipelineOptions();
        if (configuration is not null)
        {
            BindSection(configuration.GetSection(MeaiPipelineOptions.SectionName), options);
        }

        configure?.Invoke(options);
        services.AddSingleton(Options.Create(options));

        RegisterGovernanceDefaults(services);
        RegisterRoutingDefaults(services);

        Func<IServiceProvider, IChatClient> defaultOllama = sp =>
            new OllamaHttpChatClient(sp.GetRequiredService<IOptions<MeaiPipelineOptions>>());
        Func<IServiceProvider, IChatClient> defaultOnnx = sp =>
            new LlamaSharpChatClient(sp.GetRequiredService<IOptions<MeaiPipelineOptions>>());

        services.AddKeyedChatClient(
                MeaiTargetKeys.LocalOllama,
                sp => (ollamaInnerFactory ?? defaultOllama)(sp))
            .UseNexoGovernance(MeaiTargetKeys.LocalOllama);

        services.AddKeyedChatClient(
                MeaiTargetKeys.LocalOnnx,
                sp => (onnxInnerFactory ?? defaultOnnx)(sp))
            .UseNexoGovernance(MeaiTargetKeys.LocalOnnx);

        if (registerDefaultRouter)
        {
            // Router sits outside per-target stacks; wrap only with Auditing (plan Phase 3).
            services.AddChatClient(sp =>
            {
                var router = ActivatorUtilities.CreateInstance<RoutingChatClient>(sp);
                var auditor = sp.GetRequiredService<IChatInvocationAuditor>();
                return new AuditingChatClient(router, auditor, RoutingChatClient.RouterTargetKey);
            });
        }

        return services;
    }

    /// <summary>
    /// Registers a governed keyed <see cref="IChatClient"/> for an additional target (e.g. cloud stubs in tests).
    /// </summary>
    public static ChatClientBuilder AddNexoGovernedChatClient(
        this IServiceCollection services,
        string targetKey,
        Func<IServiceProvider, IChatClient> innerFactory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetKey);
        RegisterGovernanceDefaults(services);
        return services.AddKeyedChatClient(targetKey, innerFactory)
            .UseNexoGovernance(targetKey);
    }

    /// <summary>
    /// Registers default governance services if not already present.
    /// </summary>
    public static IServiceCollection RegisterGovernanceDefaults(this IServiceCollection services)
    {
        services.TryAddSingleton<IChatTargetAccessPolicy, DefaultChatTargetAccessPolicy>();
        services.TryAddSingleton<IChatMessageSanitizer, DefaultChatMessageSanitizer>();
        services.TryAddSingleton<ITargetSanitizePolicy, DefaultTargetSanitizePolicy>();
        services.TryAddSingleton<IChatInvocationAuditor, InMemoryChatInvocationAuditor>();
        return services;
    }

    /// <summary>
    /// Registers default routing services if not already present.
    /// </summary>
    public static IServiceCollection RegisterRoutingDefaults(this IServiceCollection services)
    {
        services.TryAddSingleton<IRouteCandidateTable, DefaultRouteCandidateTable>();
        services.TryAddSingleton<ITargetAvailability, InMemoryTargetAvailability>();
        services.TryAddSingleton<IChatRouter, LocalFirstChatRouter>();
        return services;
    }

    private static void BindSection(IConfiguration section, MeaiPipelineOptions options)
    {
        var ollamaBase = section["OllamaBaseUrl"];
        if (!string.IsNullOrWhiteSpace(ollamaBase))
        {
            options.OllamaBaseUrl = ollamaBase;
        }

        var ollamaModel = section["OllamaModel"];
        if (!string.IsNullOrWhiteSpace(ollamaModel))
        {
            options.OllamaModel = ollamaModel;
        }

        var localPath = section["LocalModelPath"];
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            options.LocalModelPath = localPath;
        }

        if (int.TryParse(section["LocalContextSize"], out var ctx) && ctx > 0)
        {
            options.LocalContextSize = ctx;
        }

        if (int.TryParse(section["LocalMaxTokens"], out var max) && max > 0)
        {
            options.LocalMaxTokens = max;
        }
    }
}
