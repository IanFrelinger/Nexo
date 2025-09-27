using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Factory.Interfaces;

/// <summary>
/// Quality validation functionality
/// </summary>
public partial interface IFeatureFactoryValidator
{
    /// <summary>
    /// Validates code quality and standards
    /// </summary>
    /// <param name="qualityRequest">Code quality validation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Code quality validation result</returns>
    Task<CodeQualityResult> ValidateCodeQualityAsync(CodeQualityRequest qualityRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates security compliance
    /// </summary>
    /// <param name="securityRequest">Security validation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Security validation result</returns>
    Task<SecurityValidationResult> ValidateSecurityComplianceAsync(SecurityValidationRequest securityRequest, CancellationToken cancellationToken = default);
}
