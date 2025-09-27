using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Factory.Interfaces;

/// <summary>
/// Testing functionality
/// </summary>
public partial interface IFeatureFactoryValidator
{
    /// <summary>
    /// Runs end-to-end validation tests
    /// </summary>
    /// <param name="e2eRequest">End-to-end test request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>End-to-end test results</returns>
    Task<EndToEndTestResult> RunEndToEndTestsAsync(EndToEndTestRequest e2eRequest, CancellationToken cancellationToken = default);
}
