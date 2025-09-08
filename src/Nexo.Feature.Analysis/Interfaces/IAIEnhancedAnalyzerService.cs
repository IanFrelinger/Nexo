using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Analysis.Interfaces
{
    /// <summary>
    /// Defines the contract for an AI-enhanced analyzer service that provides intelligent code analysis and suggestions.
    /// </summary>
    public interface IAIEnhancedAnalyzerService : IAnalyzerService
    {
        /// <summary>
        /// Provides AI-powered suggestions for the given code.
        /// </summary>
        /// <param name="code">The code to analyze.</param>
        /// <param name="context">Optional context or metadata for the analysis engine.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A list of AI-generated suggestions.</returns>
        Task<IList<string>> GetAISuggestionsAsync(string code, IDictionary<string, object>? context = null, CancellationToken cancellationToken = default);
    }
} 