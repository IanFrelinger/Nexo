using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Orchestration.Agents;
using Nexo.Abstractions.Agents;
using Nexo.Orchestration.Architect.Models;
using System.Text.Json;
using ModelInput = Nexo.Abstractions.ModelInput;

namespace Nexo.Orchestration.Agents.CodeGeneration;

/// <summary>
/// Context for code generation (dependencies, existing code, etc.).
/// </summary>
public sealed record CodeContext
{
    public IReadOnlyDictionary<string, object> Dependencies { get; init; } = new Dictionary<string, object>();
}
