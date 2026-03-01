using Nexo.Core.Application.ParallelTesting.Models;

namespace Nexo.Core.Application.ParallelTesting.Ports;

/// <summary>
/// Generates diverse parameter combinations for parallel testing.
/// </summary>
public interface IParameterMatrixGenerator
{
    Task<IReadOnlyList<ParameterSet>> GenerateAsync(Scenario scenario, CancellationToken cancellationToken = default);
}
