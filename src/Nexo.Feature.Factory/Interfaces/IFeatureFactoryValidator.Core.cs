using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Factory.Interfaces;

/// <summary>
/// Core feature factory validation functionality
/// </summary>
public partial interface IFeatureFactoryValidator
{
    /// <summary>
    /// Creates comprehensive test scenarios for Feature Factory validation
    /// </summary>
    /// <param name="scenarioRequest">Test scenario request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test scenarios</returns>
    Task<TestScenarioResult> CreateTestScenariosAsync(TestScenarioRequest scenarioRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Implements real-world feature generation tests
    /// </summary>
    /// <param name="testRequest">Feature generation test request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test results</returns>
    Task<FeatureGenerationTestResult> RunFeatureGenerationTestsAsync(FeatureGenerationTestRequest testRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that Feature Factory generates production-ready features in 2 days
    /// </summary>
    /// <param name="validationRequest">Production readiness validation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Production readiness validation result</returns>
    Task<ProductionReadinessResult> ValidateProductionReadinessAsync(ProductionReadinessRequest validationRequest, CancellationToken cancellationToken = default);
}
