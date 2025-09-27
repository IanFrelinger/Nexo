using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Factory.Interfaces;

/// <summary>
/// Performance validation functionality
/// </summary>
public partial interface IFeatureFactoryValidator
{
    /// <summary>
    /// Adds performance benchmarking capabilities
    /// </summary>
    /// <param name="benchmarkRequest">Performance benchmark request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Benchmark results</returns>
    Task<PerformanceBenchmarkResult> RunPerformanceBenchmarksAsync(PerformanceBenchmarkRequest benchmarkRequest, CancellationToken cancellationToken = default);
}
