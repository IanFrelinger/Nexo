using Ashlar.Core.Application.ParallelTesting.Models;

namespace Ashlar.Core.Application.ParallelTesting.Ports;

/// <summary>
/// Spawns N test instances with different parameter sets.
/// </summary>
public interface IInstanceSpawner
{
    Task<IReadOnlyList<TestInstance>> SpawnAsync(int count, IReadOnlyList<ParameterSet> paramSets, string solutionOrProjectPath, CancellationToken cancellationToken = default);
}
