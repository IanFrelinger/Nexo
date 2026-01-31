namespace Nexo.BrickContracts;

/// <summary>
/// Wire format version for brick catalog and execute APIs. Used for backward compatibility.
/// </summary>
public static class WireFormatVersion
{
    /// <summary>
    /// Current version (e.g. "2025-01"). Include in catalog and execute payloads.
    /// </summary>
    public const string Current = "2025-01";
}
