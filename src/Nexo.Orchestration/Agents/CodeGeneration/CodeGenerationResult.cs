using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Orchestration.Agents;
using Nexo.Abstractions.Agents;
using Nexo.Orchestration.Architect.Models;
using System.Text.Json;
using ModelInput = Nexo.Abstractions.ModelInput;

namespace Nexo.Orchestration.Agents.CodeGeneration;

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
