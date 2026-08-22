namespace Ashlar.AI.Pipeline;

/// <summary>
/// Configurable AWS Bedrock model ids for tiered cloud targets.
/// Model families change; keep ids in config, not code.
/// </summary>
public sealed class BedrockMeaiOptions
{
    /// <summary>When true, register <c>cloud:bedrock:*</c> governed clients.</summary>
    public bool Enabled { get; set; }

    /// <summary>Optional AWS region (falls back to default credential/region chain).</summary>
    public string? Region { get; set; }

    /// <summary>Model id for <c>cloud:bedrock:fast</c>.</summary>
    public string FastModelId { get; set; } = "amazon.nova-micro-v1:0";

    /// <summary>Model id for <c>cloud:bedrock:balanced</c>.</summary>
    public string BalancedModelId { get; set; } = "amazon.nova-lite-v1:0";

    /// <summary>Model id for <c>cloud:bedrock:heavy</c>.</summary>
    public string HeavyModelId { get; set; } = "amazon.nova-pro-v1:0";
}
