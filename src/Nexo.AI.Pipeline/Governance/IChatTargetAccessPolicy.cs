namespace Nexo.AI.Pipeline.Governance;

/// <summary>
/// Evaluates whether a caller may invoke a given target/model.
/// </summary>
public interface IChatTargetAccessPolicy
{
    /// <summary>
    /// Returns true when the invocation is permitted.
    /// </summary>
    bool IsAllowed(string? callerIdentity, string targetKey, string? modelId, out string? denyReason);
}
