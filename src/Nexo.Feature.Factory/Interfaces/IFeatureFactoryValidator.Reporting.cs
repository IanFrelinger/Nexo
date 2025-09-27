using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Factory.Interfaces;

/// <summary>
/// Validation reporting functionality
/// </summary>
public partial interface IFeatureFactoryValidator
{
    /// <summary>
    /// Creates validation reporting system
    /// </summary>
    /// <param name="reportRequest">Validation report request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation report</returns>
    Task<ValidationReportResult> GenerateValidationReportAsync(ValidationReportRequest reportRequest, CancellationToken cancellationToken = default);
}
