using Ashlar.Core.Domain.Values;

namespace Ashlar.BackgroundAgents.DataSensitivity;

/// <summary>
/// Interface for data sensitivity levels - allows extensible, configurable sensitivity classification.
/// 
/// Framework provides primitive levels (Public, Internal, Confidential, Secret, TopSecret),
/// but users can define custom sensitivity levels with their own ordering and restrictions.
/// 
/// Extends ITypeValue to follow framework patterns for value objects.
/// </summary>
public interface IDataSensitivityLevel : ITypeValue
{
    /// <summary>
    /// Numeric value for ordering (lower = less sensitive, higher = more sensitive).
    /// </summary>
    int SensitivityValue { get; }
    
    /// <summary>
    /// Whether this level allows external LLM calls.
    /// </summary>
    bool AllowsExternalLLM { get; }
    
    /// <summary>
    /// Whether this level allows web search.
    /// </summary>
    bool AllowsWebSearch { get; }
    
    /// <summary>
    /// Whether this level requires local-only processing.
    /// </summary>
    bool RequiresLocalOnly { get; }
    
    /// <summary>
    /// Whether this level allows network exports.
    /// </summary>
    bool AllowsNetworkExports { get; }
    
    /// <summary>
    /// Description of this sensitivity level.
    /// </summary>
    string Description { get; }
}
