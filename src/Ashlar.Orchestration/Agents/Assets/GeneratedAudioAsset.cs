using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;
using Ashlar.Orchestration.Architect.Models;
using Ashlar.Orchestration.Assets.Models;
using Ashlar.Orchestration.Assets.Ports;
using System.Text.RegularExpressions;

namespace Ashlar.Orchestration.Agents.Assets;

/// <summary>
/// Generated audio asset.
/// </summary>
internal sealed record GeneratedAudioAsset : GeneratedAssetBase
{
    /// <summary>MIME type of the generated audio file.</summary>
    public required string MimeType { get; init; }

    /// <summary>Duration of the audio in milliseconds.</summary>
    public required int DurationMs { get; init; }

    /// <summary>Sample rate in hertz, if known.</summary>
    public int? SampleRate { get; init; }
}
