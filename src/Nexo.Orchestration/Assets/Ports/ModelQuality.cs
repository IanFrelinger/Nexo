namespace Nexo.Orchestration.Assets.Ports;

/// <summary>
/// Quality levels for 3D model generation.
/// 
/// Defines quality tiers:
/// - Draft: Fast, low quality
/// - Low: Basic quality
/// - Medium: Balanced quality and speed
/// - High: High quality
/// - Production: Maximum quality
/// 
/// Used by IModel3DGenerator to specify generation quality.
/// </summary>
public enum ModelQuality
{
    Draft,
    Low,
    Medium,
    High,
    Production
}
