using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Core.Application.Services.Environment;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Interfaces.Infrastructure;

namespace Nexo.Core.Application.Services.Environment;

/// <summary>
/// Interface for environment-specific adaptations
/// </summary>
public interface IEnvironmentAdaptationService
{
    /// <summary>
    /// Apply environment-specific adaptations
    /// </summary>
    Task ApplyEnvironmentAdaptationsAsync(EnvironmentProfile environment);
    
    /// <summary>
    /// Get environment-specific strategies
    /// </summary>
    Task<IEnumerable<string>> GetEnvironmentStrategiesAsync(EnvironmentProfile environment);
    
    /// <summary>
    /// Check if environment supports feature
    /// </summary>
    Task<bool> SupportsFeatureAsync(EnvironmentProfile environment, string feature);
    
    /// <summary>
    /// Get environment constraints
    /// </summary>
    Task<Nexo.Core.Domain.Entities.Infrastructure.ResourceConstraints> GetEnvironmentConstraintsAsync(EnvironmentProfile environment);
    
    /// <summary>
    /// Optimize for environment
    /// </summary>
    Task<OptimizationResult> OptimizeForEnvironmentAsync(EnvironmentProfile environment, string code);
    
    /// <summary>
    /// Get environment recommendations
    /// </summary>
    Task<IEnumerable<string>> GetEnvironmentRecommendationsAsync(EnvironmentProfile environment);
}
