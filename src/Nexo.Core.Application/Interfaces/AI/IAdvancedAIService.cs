using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Models.AI;

namespace Nexo.Core.Application.Interfaces.AI
{
    /// <summary>
    /// Interface for advanced AI service in Phase 9.
    /// Provides enhanced NLP and intelligent code generation capabilities.
    /// </summary>
    public interface IAdvancedAIService
    {
        /// <summary>
        /// Implements advanced natural language processing capabilities.
        /// </summary>
        /// <param name="nlpConfig">The NLP configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>NLP implementation result</returns>
        Task<NLPImplementationResult> ImplementAdvancedNLPAsync(
            NLPConfiguration nlpConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates context-aware processing.
        /// </summary>
        /// <param name="contextConfig">The context configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Context processing result</returns>
        Task<ContextProcessingResult> CreateContextAwareProcessingAsync(
            ContextConfiguration contextConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds multi-language support.
        /// </summary>
        /// <param name="languageConfig">The language configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Language support result</returns>
        Task<LanguageSupportResult> AddMultiLanguageSupportAsync(
            LanguageConfiguration languageConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates advanced requirement analysis.
        /// </summary>
        /// <param name="analysisConfig">The analysis configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Analysis implementation result</returns>
        Task<AnalysisImplementationResult> CreateAdvancedRequirementAnalysisAsync(
            AnalysisConfiguration analysisConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Implements intelligent code generation algorithms.
        /// </summary>
        /// <param name="generationConfig">The generation configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Code generation result</returns>
        Task<CodeGenerationResult> ImplementIntelligentCodeGenerationAsync(
            CodeGenerationConfiguration generationConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates intelligent code optimization.
        /// </summary>
        /// <param name="optimizationConfig">The optimization configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Code optimization result</returns>
        Task<CodeOptimizationResult> CreateIntelligentCodeOptimizationAsync(
            CodeOptimizationConfiguration optimizationConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds code quality enhancement.
        /// </summary>
        /// <param name="qualityConfig">The quality configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Quality enhancement result</returns>
        Task<QualityEnhancementResult> AddCodeQualityEnhancementAsync(
            QualityConfiguration qualityConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates advanced testing strategies.
        /// </summary>
        /// <param name="testingConfig">The testing configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Testing strategy result</returns>
        Task<TestingStrategyResult> CreateAdvancedTestingStrategiesAsync(
            TestingConfiguration testingConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets advanced AI metrics.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Advanced AI metrics</returns>
        Task<AdvancedAIMetrics> GetAdvancedAIMetricsAsync(
            CancellationToken cancellationToken = default);
    }
}
