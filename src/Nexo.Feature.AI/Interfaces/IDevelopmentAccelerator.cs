using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.AI.Interfaces
{
    /// <summary>
    /// Defines the contract for intelligent code acceleration features such as code suggestions, refactoring, and test generation.
    /// </summary>
    public interface IDevelopmentAccelerator
    {
        /// <summary>
        /// Provides intelligent code suggestions based on the given source code and context.
        /// </summary>
        /// <param name="sourceCode">The source code to analyze.</param>
        /// <param name="context">Optional context or metadata for the suggestion engine.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A list of suggested code improvements or completions.</returns>
        Task<IList<string>> SuggestCodeAsync(string sourceCode, IDictionary<string, object> context = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Provides intelligent refactoring suggestions for the given source code.
        /// </summary>
        /// <param name="sourceCode">The source code to refactor.</param>
        /// <param name="context">Optional context or metadata for the refactoring engine.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A list of refactoring suggestions or actions.</returns>
        Task<IList<string>> SuggestRefactoringsAsync(string sourceCode, IDictionary<string, object> context = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates unit tests or test cases for the given source code.
        /// </summary>
        /// <param name="sourceCode">The source code to generate tests for.</param>
        /// <param name="context">Optional context or metadata for the test generation engine.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A list of generated test code snippets or files.</returns>
        Task<IList<string>> GenerateTestsAsync(string sourceCode, IDictionary<string, object> context = null, CancellationToken cancellationToken = default);
    }
} 