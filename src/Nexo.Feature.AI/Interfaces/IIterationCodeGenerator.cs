using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Iteration;

namespace Nexo.Feature.AI.Interfaces;

/// <summary>
/// Interface for generating optimized iteration code using AI
/// </summary>
public interface IIterationCodeGenerator
{
    /// <summary>
    /// Generates optimal iteration code for the given context
    /// </summary>
    /// <param name="context">The iteration context</param>
    /// <param name="codeGenerationContext">Code generation parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated code string</returns>
    Task<string> GenerateOptimalIterationCodeAsync(
        IterationContext context, 
        CodeGenerationContext codeGenerationContext,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates iteration code for multiple platforms
    /// </summary>
    /// <param name="context">The iteration context</param>
    /// <param name="platforms">Target platforms</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of platform-specific code</returns>
    Task<Dictionary<PlatformTarget, string>> GenerateMultiPlatformCodeAsync(
        IterationContext context,
        IEnumerable<PlatformTarget> platforms,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Enhances existing iteration code with AI suggestions
    /// </summary>
    /// <param name="existingCode">The existing code to enhance</param>
    /// <param name="context">The iteration context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enhanced code with improvements</returns>
    Task<string> EnhanceIterationCodeAsync(
        string existingCode,
        IterationContext context,
        CancellationToken cancellationToken = default);
}
