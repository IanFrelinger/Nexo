using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;

namespace Nexo.Core.Application.Services.AI.Runtime
{
    /// <summary>
    /// Interface for AI engines that perform actual AI operations
    /// </summary>
    public interface IAIEngine
    {
        /// <summary>
        /// Gets the engine information
        /// </summary>
        AIEngineInfo EngineInfo { get; }

        /// <summary>
        /// Gets the current engine status
        /// </summary>
        AIOperationStatus Status { get; }

        /// <summary>
        /// Initializes the engine with the specified model
        /// </summary>
        Task InitializeAsync(ModelInfo model, AIOperationContext context);

        /// <summary>
        /// Generates code based on the provided prompt
        /// </summary>
        Task<CodeGenerationResult> GenerateCodeAsync(CodeGenerationRequest request);

        /// <summary>
        /// Reviews code and provides feedback
        /// </summary>
        Task<CodeReviewResult> ReviewCodeAsync(string code, AIOperationContext context);

        /// <summary>
        /// Optimizes code for better performance
        /// </summary>
        Task<CodeGenerationResult> OptimizeCodeAsync(string code, AIOperationContext context);

        /// <summary>
        /// Generates documentation for code
        /// </summary>
        Task<string> GenerateDocumentationAsync(string code, AIOperationContext context);

        /// <summary>
        /// Generates tests for code
        /// </summary>
        Task<CodeGenerationResult> GenerateTestsAsync(string code, AIOperationContext context);

        /// <summary>
        /// Refactors code according to best practices
        /// </summary>
        Task<CodeGenerationResult> RefactorCodeAsync(string code, AIOperationContext context);

        /// <summary>
        /// Analyzes code and provides insights
        /// </summary>
        Task<AIResponse> AnalyzeCodeAsync(string code, AIOperationContext context);

        /// <summary>
        /// Translates code between languages
        /// </summary>
        Task<CodeGenerationResult> TranslateCodeAsync(string code, string targetLanguage, AIOperationContext context);

        /// <summary>
        /// Generates a general AI response
        /// </summary>
        Task<AIResponse> GenerateResponseAsync(string prompt, AIOperationContext context);

        /// <summary>
        /// Streams a response as it's generated
        /// </summary>
        IAsyncEnumerable<string> StreamResponseAsync(string prompt, AIOperationContext context);

        /// <summary>
        /// Cancels the current operation
        /// </summary>
        Task CancelAsync();

        /// <summary>
        /// Disposes the engine and releases resources
        /// </summary>
        Task DisposeAsync();

        /// <summary>
        /// Gets the current memory usage
        /// </summary>
        long GetMemoryUsage();

        /// <summary>
        /// Gets the current CPU usage
        /// </summary>
        double GetCpuUsage();

        /// <summary>
        /// Checks if the engine is healthy
        /// </summary>
        bool IsHealthy();
    }
}
