using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;
using Ashlar.Orchestration.Architect.Models;
using Ashlar.Orchestration.Assets.Models;
using Ashlar.Orchestration.Assets.Ports;
using System.Text.RegularExpressions;

namespace Ashlar.Orchestration.Agents.Assets;

/// <summary>
/// Generated image asset.
/// </summary>
internal sealed record GeneratedImageAsset : GeneratedAssetBase
{
    public required ImageSize Size { get; init; }
    public required string MimeType { get; init; }
}
