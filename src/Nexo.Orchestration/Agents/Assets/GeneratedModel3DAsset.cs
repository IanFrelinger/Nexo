using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Assets.Models;
using Nexo.Orchestration.Assets.Ports;
using System.Text.RegularExpressions;

namespace Nexo.Orchestration.Agents.Assets;

/// <summary>
/// Generated 3D model asset.
/// </summary>
internal sealed record GeneratedModel3DAsset : GeneratedAssetBase
{
    /// <summary>Output format of the generated model.</summary>
    public required Model3DFormat Format { get; init; }

    /// <summary>Number of vertices in the mesh.</summary>
    public required int VertexCount { get; init; }

    /// <summary>Number of triangles in the mesh.</summary>
    public required int TriangleCount { get; init; }

    /// <summary>Paths to generated texture files.</summary>
    public required IReadOnlyList<string> TexturePaths { get; init; }
}
