using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Analysis.Ports;
using Nexo.Infrastructure.Analysis;
using Nexo.Infrastructure.Adaptation;

namespace Nexo.Infrastructure.Adaptation.Sdk.Extensions;
/// <summary>
/// DI extensions for P2.3 shared adaptation cache.
/// </summary>
public static class SharedAdaptationServiceCollectionExtensions
{
    /// <summary>
    /// Registers the shared adaptation broadcaster and sync.
    /// Requires AddAdaptationInfrastructure and AddCodeAnalyzers.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="sharedPath">Base path for shared adaptations. Default: ~/.nexo/shared-adaptations.</param>
    /// <param name="regressionOverride">Optional override for tests. When provided, used instead of resolving from DI.</param>
    /// <param name="options">Optional options (SourcePeerId, TrustedPeerIds) for P2.2 mesh attribution.</param>
    public static IServiceCollection AddSharedAdaptationCache(
        this IServiceCollection services,
        string? sharedPath = null,
        IRegressionTestRunner? regressionOverride = null,
        SharedAdaptationOptions? options = null)
    {
        var peerId = options?.SourcePeerId
            ?? Environment.GetEnvironmentVariable("NEXO_MESH_PEER_ID")
            ?? Guid.NewGuid().ToString("N");
        var trustedPeerIds = options?.TrustedPeerIds;

        services.AddSingleton<IPeerIdProvider>(_ => new StaticPeerIdProvider(peerId));
        services.AddSingleton<ISharedAdaptationBroadcaster>(sp =>
        {
            var log = sp.GetRequiredService<IAdaptationLog>();
            var regression = regressionOverride ?? sp.GetRequiredService<IRegressionTestRunner>();
            var immutable = sp.GetRequiredService<IImmutableCoreRegistry>();
            var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<FileBasedSharedAdaptationStore>>();
            return new FileBasedSharedAdaptationStore(sharedPath, log, regression, immutable, logger, peerId, trustedPeerIds);
        });
        services.AddSingleton<ISharedAdaptationSync>(sp =>
        {
            var store = (FileBasedSharedAdaptationStore)sp.GetRequiredService<ISharedAdaptationBroadcaster>();
            return store;
        });
        services.AddSingleton<Nexo.Core.Application.Adaptation.Ports.ISneakernetTransport>(sp =>
        {
            var sync = sp.GetRequiredService<ISharedAdaptationSync>();
            var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<Nexo.Infrastructure.Adaptation.SneakernetTransport>>();
            return new SneakernetTransport(sync, logger);
        });
        return services;
    }

    private sealed class StaticPeerIdProvider : IPeerIdProvider
    {
        private readonly string _peerId;
        /// <summary>Initializes a static peer identifier provider.</summary>
        public StaticPeerIdProvider(string peerId) => _peerId = peerId;
        /// <summary>Gets peer id.</summary>
        public string GetPeerId() => _peerId;
    }
}
