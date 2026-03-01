namespace Nexo.Core.Application.ParallelTesting.Models;

/// <summary>
/// Key-value overrides for test runs (e.g. filter, category, assembly).
/// </summary>
public record ParameterSet
{
    public IReadOnlyDictionary<string, string> Overrides { get; init; } = new Dictionary<string, string>();
}
