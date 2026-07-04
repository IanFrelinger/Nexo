namespace Nexo.Core.Application.Adaptation.Models;

/// <summary>
/// Type of fix applied during self-adaptation.
/// </summary>
public enum AdaptationFixType
{
    /// <summary>Source-level fix applied to a .cs file.</summary>
    Source,

    /// <summary>Brick manifest recompiled and redeployed.</summary>
    Brick,
}
