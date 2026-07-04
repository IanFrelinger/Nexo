namespace Nexo.Core.Application.NodeCapabilityRuntime.Models;

/// <summary>
/// Load state of a model in the local cache.
/// </summary>
public enum ModelState
{
    /// <summary>Model is loaded in memory and ready for immediate inference.</summary>
    Hot,

    /// <summary>Model is partially prepared or recently used but not fully hot.</summary>
    Warm,

    /// <summary>Model is on disk or in registry but not loaded in memory.</summary>
    Cold
}
