using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;
using Ashlar.Orchestration.Agents;
using Ashlar.Abstractions.Agents;
using Ashlar.Orchestration.Architect.Models;
using System.Text.Json;
using ModelInput = Ashlar.Abstractions.ModelInput;

namespace Ashlar.Orchestration.Agents.CodeGeneration;

/// <summary>
/// Result of code generation.
/// </summary>
public sealed record CodeGenerationResult
{
    /// <summary>Generated source code.</summary>
    public required string Code { get; init; }

    /// <summary>Programming language of the generated code.</summary>
    public required string Language { get; init; }

    /// <summary>Optional static analysis of the generated code.</summary>
    public CodeAnalysis? Analysis { get; init; }

    /// <summary>UTC timestamp when generation completed.</summary>
    public required DateTimeOffset GeneratedAt { get; init; }

    /// <summary>Additional generation metadata.</summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}
