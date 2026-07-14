using Nexo.AI.Pipeline.Governance;

namespace Nexo.AI.Pipeline.Routing;

/// <summary>
/// Builds the ordered escalation candidate list for a request (local-first).
/// </summary>
public interface IRouteCandidateTable
{
    /// <summary>
    /// Returns candidate target keys in preference order for the requested tier.
    /// Cloud keys are only included when the caller may leave the machine.
    /// </summary>
    IReadOnlyList<string> GetCandidates(RouteCapabilityTier tier, bool cloudPermitted);
}

/// <summary>
/// Default local-first escalation:
/// fast → local:ollama, local:onnx;
/// balanced → local:ollama, local:onnx, cloud:bedrock:balanced;
/// heavy → local:ollama, local:onnx, cloud:bedrock:balanced, cloud:bedrock:heavy.
/// </summary>
public sealed class DefaultRouteCandidateTable : IRouteCandidateTable
{
    /// <summary>Cloud target key placeholders (Phase 4 registers the clients).</summary>
    public const string CloudBedrockFast = "cloud:bedrock:fast";
    public const string CloudBedrockBalanced = "cloud:bedrock:balanced";
    public const string CloudBedrockHeavy = "cloud:bedrock:heavy";

    /// <inheritdoc />
    public IReadOnlyList<string> GetCandidates(RouteCapabilityTier tier, bool cloudPermitted)
    {
        var list = new List<string>
        {
            MeaiTargetKeys.LocalOllama,
            MeaiTargetKeys.LocalOnnx,
        };

        if (!cloudPermitted)
        {
            return list;
        }

        switch (tier)
        {
            case RouteCapabilityTier.Fast:
                list.Add(CloudBedrockFast);
                break;
            case RouteCapabilityTier.Balanced:
                list.Add(CloudBedrockBalanced);
                break;
            case RouteCapabilityTier.Heavy:
                list.Add(CloudBedrockBalanced);
                list.Add(CloudBedrockHeavy);
                break;
        }

        return list;
    }
}

/// <summary>
/// Selects a governed target pipeline using policy × availability × capability.
/// </summary>
public interface IChatRouter
{
    /// <summary>Chooses a target key and records candidates/reason for audit.</summary>
    RouteDecision Select(string? callerIdentity, string? modelId, RouteCapabilityTier tier, string? preferredTarget);
}
