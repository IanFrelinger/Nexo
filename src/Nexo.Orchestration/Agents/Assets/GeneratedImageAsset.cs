using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Assets.Models;
using Nexo.Orchestration.Assets.Ports;
using System.Text.RegularExpressions;

namespace Nexo.Orchestration.Agents.Assets;

/// <summary>
/// Generated image asset.
/// </summary>
internal sealed record GeneratedImageAsset : GeneratedAssetBase
{
    public required ImageSize Size { get; init; }
    public required string MimeType { get; init; }
}
