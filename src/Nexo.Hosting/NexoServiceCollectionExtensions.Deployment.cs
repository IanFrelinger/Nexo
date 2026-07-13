namespace Nexo.Hosting;

public static partial class NexoServiceCollectionExtensions
{
    /// <summary>
    /// Resolves the deployment profile from (in priority order):
    /// 1. Explicit <see cref="NexoHostingOptions.DeploymentProfile"/> set by the caller.
    /// 2. <c>NEXO_DEPLOYMENT_PROFILE</c> environment variable (case-insensitive;
    ///    accepts "full", "server", "edge", "airgapped"/"air-gapped", "system"/"core").
    /// 3. Falls back to <see cref="NexoDeploymentProfile.Full"/>.
    /// </summary>
    private static NexoDeploymentProfile ResolveDeploymentProfile(NexoHostingOptions options)
    {
        if (options.DeploymentProfile.HasValue)
        {
            return options.DeploymentProfile.Value;
        }

        var raw = Environment.GetEnvironmentVariable("NEXO_DEPLOYMENT_PROFILE");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return NexoDeploymentProfile.Full;
        }

        if (TryParseDeploymentProfile(raw, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException(
            $"NEXO_DEPLOYMENT_PROFILE='{raw}' is not recognized. " +
            "Valid values: full, server, edge, air-gapped, system.");
    }

    private static bool TryParseDeploymentProfile(string? raw, out NexoDeploymentProfile profile)
    {
        profile = NexoDeploymentProfile.Full;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var normalized = raw.Trim().ToLowerInvariant();
        profile = normalized switch
        {
            "full" => NexoDeploymentProfile.Full,
            "server" => NexoDeploymentProfile.Server,
            "edge" => NexoDeploymentProfile.Edge,
            "airgapped" => NexoDeploymentProfile.AirGapped,
            "air-gapped" => NexoDeploymentProfile.AirGapped,
            "system" => NexoDeploymentProfile.System,
            "core" => NexoDeploymentProfile.System,
            _ => profile
        };

        return normalized is "full" or "server" or "edge" or "airgapped" or "air-gapped" or "system" or "core";
    }

    /// <summary>
    /// Maps a deployment profile to the set of subsystem modules that should
    /// be registered.  The peeling order (Full → Server → Edge → AirGapped
    /// → System) progressively strips capabilities:
    /// <list type="bullet">
    ///   <item><c>Full</c>     — everything; used in development &amp; CI.</item>
    ///   <item><c>Server</c>   — same as Full (reserved for future server-specific gating).</item>
    ///   <item><c>Edge</c>     — persistence + pipelines only; no NCR, no agents.</item>
    ///   <item><c>AirGapped</c>— NCR + adaptation + persistence; no network transport.</item>
    ///   <item><c>System</c>   — bare minimum for CLI tooling; nothing optional.</item>
    /// </list>
    /// </summary>
    private static ModuleSelection GetModuleSelection(NexoDeploymentProfile profile)
    {
        return profile switch
        {
            NexoDeploymentProfile.Full => new ModuleSelection(
                IncludeNodeCapabilityRuntime: true,
                IncludeRuntimeTransport: true,
                IncludePersistence: true,
                IncludeAdaptation: true,
                IncludePipelineComposition: true,
                IncludeBackgroundAgents: true,
                IncludeBackgroundAgentRag: true,
                IncludeObservationPipeline: true,
                IncludeTrustServices: true,
                IncludeSkills: true,
                IncludeWorkflowIntegrations: true,
                IncludeTestingAdapters: true),
            NexoDeploymentProfile.Server => new ModuleSelection(
                IncludeNodeCapabilityRuntime: true,
                IncludeRuntimeTransport: true,
                IncludePersistence: true,
                IncludeAdaptation: true,
                IncludePipelineComposition: true,
                IncludeBackgroundAgents: true,
                IncludeBackgroundAgentRag: true,
                IncludeObservationPipeline: true,
                IncludeTrustServices: true,
                IncludeSkills: true,
                IncludeWorkflowIntegrations: true,
                IncludeTestingAdapters: true),
            NexoDeploymentProfile.Edge => new ModuleSelection(
                IncludeNodeCapabilityRuntime: false,
                IncludeRuntimeTransport: false,
                IncludePersistence: true,
                IncludeAdaptation: false,
                IncludePipelineComposition: true,
                IncludeBackgroundAgents: false,
                IncludeBackgroundAgentRag: false,
                IncludeObservationPipeline: false,
                IncludeTrustServices: false,
                IncludeSkills: false,
                IncludeWorkflowIntegrations: false,
                IncludeTestingAdapters: false),
            NexoDeploymentProfile.AirGapped => new ModuleSelection(
                IncludeNodeCapabilityRuntime: true,
                IncludeRuntimeTransport: false,
                IncludePersistence: true,
                IncludeAdaptation: true,
                IncludePipelineComposition: true,
                IncludeBackgroundAgents: false,
                IncludeBackgroundAgentRag: false,
                IncludeObservationPipeline: false,
                IncludeTrustServices: false,
                IncludeSkills: false,
                IncludeWorkflowIntegrations: false,
                IncludeTestingAdapters: false),
            NexoDeploymentProfile.System => new ModuleSelection(
                IncludeNodeCapabilityRuntime: false,
                IncludeRuntimeTransport: false,
                IncludePersistence: false,
                IncludeAdaptation: false,
                IncludePipelineComposition: false,
                IncludeBackgroundAgents: false,
                IncludeBackgroundAgentRag: false,
                IncludeObservationPipeline: false,
                IncludeTrustServices: false,
                IncludeSkills: false,
                IncludeWorkflowIntegrations: false,
                IncludeTestingAdapters: false),
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, "Unknown Nexo deployment profile.")
        };
    }

    /// <summary>
    /// Applies the <c>NEXO_STRICT_MODE</c> ("1" / "true") environment variable
    /// when the caller has not already enabled strict mode programmatically.
    /// Strict mode turns configuration warnings into hard failures — intended
    /// for CI gates where misconfiguration should break the build.
    /// </summary>
    private static void ResolveStrictMode(NexoHostingOptions options)
    {
        if (!options.StrictMode.Enabled)
        {
            options.StrictMode.Enabled = ParseBooleanEnvironmentVariable("NEXO_STRICT_MODE");
        }
    }

    internal static bool ParseBooleanEnvironmentVariable(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }
}
