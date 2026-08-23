using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;
using Ashlar.Orchestration.Architect.Models;
using Ashlar.Orchestration.Assets.Models;
using Ashlar.Orchestration.Assets.Ports;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Ashlar.Orchestration.Agents.Assets;

/// <summary>
/// Base record for generated assets.
/// </summary>
public abstract record GeneratedAssetBase
{
    public required string FilePath { get; init; }
}
