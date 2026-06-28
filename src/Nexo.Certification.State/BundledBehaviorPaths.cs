namespace Nexo.Certification.State;

/// <summary>
/// Path helpers for file-backed bundled behavior sidecars (path-safe base64 content hashes).
/// </summary>
public static class BundledBehaviorPaths
{
    public static string ToPathSafeContentHash(string contentHash) =>
        contentHash.Replace('/', '_');

    public static IEnumerable<string> CandidateDirectories(string behaviorRoot, string behaviorCertContentHash)
    {
        yield return Path.Combine(behaviorRoot, behaviorCertContentHash);
        yield return Path.Combine(behaviorRoot, ToPathSafeContentHash(behaviorCertContentHash));
    }
}
