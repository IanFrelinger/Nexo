using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Domain.Interfaces.Infrastructure;

/// <summary>
/// Adaptation strategy interface
/// </summary>
public interface IAdaptationStrategy
{
    /// <summary>
    /// Strategy identifier
    /// </summary>
    string StrategyId { get; }
    
    /// <summary>
    /// Strategy name
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Check if this strategy can handle the adaptation
    /// </summary>
    bool CanHandle(AdaptationContext context);
    
    /// <summary>
    /// Apply the adaptation strategy
    /// </summary>
    Task<AdaptationRecord> ApplyAsync(AdaptationContext context);
    
    /// <summary>
    /// Get priority for this strategy
    /// </summary>
    int GetPriority(AdaptationContext context);
}
