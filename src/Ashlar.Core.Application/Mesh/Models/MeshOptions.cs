namespace Ashlar.Core.Application.Mesh.Models;

/// <summary>
/// Options for mesh instance identity and paths.
/// </summary>
public sealed class MeshOptions
{
    /// <summary>
    /// This instance's peer ID. Used when sending requests so the fulfiller can respond.
    /// </summary>
    public string PeerId { get; set; } = "";

    /// <summary>
    /// Optional CSV of trusted peer IDs for routing and mesh capability requests.
    /// </summary>
    public string? TrustedPeerIdsCsv { get; set; }

    /// <summary>
    /// Optional CSV of explicitly untrusted peer IDs.
    /// </summary>
    public string? UntrustedPeerIdsCsv { get; set; }
}
