namespace Ashlar.CLI.Unity.Pipeline;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Visual and performance budgets for generated Unity assets.</summary>
public sealed record AestheticConstraints
{
    /// <summary>Default art pack or style preset to apply.</summary>
    public string? DefaultPack { get; init; }

    /// <summary>Maximum triangle count allowed per generated object.</summary>
    public int MaxTriBudgetPerObject { get; init; }

    /// <summary>Maximum draw calls allowed for a generated scene slice.</summary>
    public int MaxDrawCalls { get; init; }
}
