namespace Nexo.Commercial.GameDomain.Assets;

/// <summary>Network variable record.</summary>
public sealed record NetworkVariable
{
    /// <summary>Name value.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Type value.</summary>
    public string Type { get; init; } = "float"; // float, int, bool, Vector3, Quaternion, string, custom
    /// <summary>Permission value.</summary>
    public string Permission { get; init; } = "server"; // server, owner, everyone
    /// <summary>Delivery mode value.</summary>
    public string DeliveryMode { get; init; } = "reliable"; // reliable, unreliable
    /// <summary>Send rate value.</summary>
    public double SendRate { get; init; } = 0.0; // 0 = every change, >0 = max sends per second
}
