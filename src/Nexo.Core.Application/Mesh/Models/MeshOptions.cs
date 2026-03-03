namespace Nexo.Core.Application.Mesh.Models;

/// <summary>
/// Options for mesh instance identity and paths.
/// </summary>
public sealed class MeshOptions
{
    /// <summary>
    /// This instance's peer ID. Used when sending requests so the fulfiller can respond.
    /// </summary>
    public string PeerId { get; set; } = "";
}
