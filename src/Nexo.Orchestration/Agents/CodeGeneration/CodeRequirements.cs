using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Orchestration.Agents;
using Nexo.Abstractions.Agents;
using Nexo.Orchestration.Architect.Models;
using System.Text.Json;
using ModelInput = Nexo.Abstractions.ModelInput;

namespace Nexo.Orchestration.Agents.CodeGeneration;

/// <summary>
/// Requirements for code generation.
/// </summary>
public sealed record CodeRequirements
{
    /// <summary>Desired functionality to implement.</summary>
    public required string Functionality { get; init; }

    /// <summary>Target programming language, if specified.</summary>
    public string? Language { get; init; }

    /// <summary>Design patterns to apply.</summary>
    public IReadOnlyList<string> Patterns { get; init; } = Array.Empty<string>();

    /// <summary>Constraints the generated code must satisfy.</summary>
    public IReadOnlyList<string> Constraints { get; init; } = Array.Empty<string>();
}
