namespace Nexo.Certification.State;

/// <summary>
/// Portable bundled artifact verification for cross-project reuse (Project C).
/// </summary>
public static class AttestedStateLogArtifactVerifier
{
    public const string SchemaFileName = "state-schema.json";
    public const string LogFileName = "attested-state-log.json";
    public const string LiveStateFileName = "live-state.json";
    public const string BehaviorsDirectoryName = "behaviors";

    public static StateLogTrustResult VerifyBundledArtifacts(
        string artifactRoot,
        string? hmacKey = null,
        ITransitionReplayer? replayer = null,
        ILiveStateHashReader? liveStateReader = null,
        bool bindLiveStateSidecar = false)
    {
        if (string.IsNullOrWhiteSpace(artifactRoot))
            throw new ArgumentException("Artifact root is required.", nameof(artifactRoot));

        var schemaPath = Path.Combine(artifactRoot, SchemaFileName);
        var logPath = Path.Combine(artifactRoot, LogFileName);
        var behaviorRoot = Path.Combine(artifactRoot, BehaviorsDirectoryName);

        var schema = AttestedStateLogWireFormat.DeserializeSchema(File.ReadAllText(schemaPath));
        var log = AttestedStateLogWireFormat.DeserializeLog(File.ReadAllText(logPath));
        var resolver = new FileBundledBehaviorCatalogResolver(behaviorRoot);

        var liveReader = liveStateReader;
        if (bindLiveStateSidecar && liveReader is null)
        {
            var liveStatePath = Path.Combine(artifactRoot, LiveStateFileName);
            if (File.Exists(liveStatePath))
            {
                var liveState = AttestedStateLogWireFormat.DeserializeLiveState(File.ReadAllText(liveStatePath));
                liveReader = new FixedLiveStateHashReader(liveState.CurrentStateHash);
            }
        }

        return StateLogVerifier.Verify(log, schema, resolver, hmacKey, replayer, liveReader);
    }
}
