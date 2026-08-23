using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;
using Ashlar.Orchestration.Agents;
using Ashlar.Abstractions.Agents;
using Ashlar.Orchestration.Architect.Models;
using System.Text.Json;
using ModelInput = Ashlar.Abstractions.ModelInput;

namespace Ashlar.Orchestration.Agents.CodeGeneration;

/// <summary>
/// Context for code generation (dependencies, existing code, etc.).
/// </summary>
public sealed record CodeContext
{
    public IReadOnlyDictionary<string, object> Dependencies { get; init; } = new Dictionary<string, object>();
}
