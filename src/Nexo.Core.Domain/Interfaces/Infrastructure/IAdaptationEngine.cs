using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Domain.Interfaces.Infrastructure;

/// <summary>
/// Adaptation engine interface
/// </summary>
public interface IAdaptationEngine
{
    /// <summary>
    /// Trigger adaptation based on context
    /// </summary>
    Task TriggerAdaptationAsync(AdaptationContext context);
    
    /// <summary>
    /// Get current adaptation status
    /// </summary>
    Task<string> GetAdaptationStatusAsync();
    
    /// <summary>
    /// Check if adaptation is needed
    /// </summary>
    Task<bool> IsAdaptationNeededAsync(AdaptationContext context);
}