namespace Ashlar.Brick.Contracts.Capabilities;
/// <summary>Compute tier of a Ashlar node used for routing and federation decisions.</summary>
public enum NodeTierDto
{
    /// <summary>Minimal edge device or constrained runtime.</summary>
    Nano,

    /// <summary>Small workstation or single-tenant host.</summary>
    Micro,

    /// <summary>Typical server or developer machine.</summary>
    Standard,

    /// <summary>High-capacity cluster or dedicated inference node.</summary>
    Core
}
