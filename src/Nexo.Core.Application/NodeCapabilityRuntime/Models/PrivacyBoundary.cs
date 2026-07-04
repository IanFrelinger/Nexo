namespace Nexo.Core.Application.NodeCapabilityRuntime.Models;

/// <summary>
/// Privacy constraints governing where task data may be processed.
/// </summary>
public enum PrivacyBoundary
{
    /// <summary>Standard privacy; remote inference is permitted when policy allows.</summary>
    Standard,

    /// <summary>Data must remain on-device; remote inference is not permitted.</summary>
    LocalOnly
}
