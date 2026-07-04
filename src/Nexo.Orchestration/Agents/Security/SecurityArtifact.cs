using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Orchestration.Agents;
using Nexo.Abstractions.Agents;
using Nexo.Orchestration.Architect.Models;
using System.Text.Json;
using ModelInput = Nexo.Abstractions.ModelInput;

namespace Nexo.Orchestration.Agents.Security;

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
