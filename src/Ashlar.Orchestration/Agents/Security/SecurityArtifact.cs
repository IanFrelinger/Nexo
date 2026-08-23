using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;
using Ashlar.Orchestration.Agents;
using Ashlar.Abstractions.Agents;
using Ashlar.Orchestration.Architect.Models;
using System.Text.Json;
using ModelInput = Ashlar.Abstractions.ModelInput;

namespace Ashlar.Orchestration.Agents.Security;

/// <summary>
/// Artifact to be analyzed for security issues.
/// </summary>
public sealed record SecurityArtifact
{
    /// <summary>Artifact display name.</summary>
    public required string Name { get; init; }

    /// <summary>Artifact type label (source, config, etc.).</summary>
    public required string Type { get; init; }

    /// <summary>Artifact content to analyze.</summary>
    public required string Content { get; init; }

    /// <summary>Language or format of the content.</summary>
    public required string Language { get; init; }
}
