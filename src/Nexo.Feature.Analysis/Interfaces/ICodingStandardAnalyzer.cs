using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Analysis.Models;

namespace Nexo.Feature.Analysis.Interfaces
{
    /// <summary>
    /// Defines the contract for a configurable coding standards analyzer that can enforce
    /// specific coding standards on code generation agents.
    /// </summary>
    public interface ICodingStandardAnalyzer
    {
        /// <summary>
        /// Validates code against the configured coding standards.
        /// </summary>
        /// <param name="code">The code to validate</param>
        /// <param name="filePath">The file path of the code (optional)</param>
        /// <param name="agentId">The ID of the agent that generated the code (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The validation result</returns>
        Task<CodingStandardValidationResult> ValidateCodeAsync(
            string code, 
            string? filePath = null, 
            string? agentId = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates multiple code files against the configured coding standards.
        /// </summary>
        /// <param name="codeFiles">Dictionary of file paths to code content</param>
        /// <param name="agentId">The ID of the agent that generated the code (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The validation results for all files</returns>
        Task<Dictionary<string, CodingStandardValidationResult>> ValidateCodeFilesAsync(
            Dictionary<string, string> codeFiles, 
            string? agentId = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current coding standards configuration.
        /// </summary>
        /// <returns>The current configuration</returns>
        CodingStandardConfiguration GetConfiguration();

        /// <summary>
        /// Updates the coding standards configuration.
        /// </summary>
        /// <param name="configuration">The new configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the update operation</returns>
        Task UpdateConfigurationAsync(CodingStandardConfiguration configuration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads coding standards configuration from a file or source.
        /// </summary>
        /// <param name="source">The source of the configuration (file path, URL, etc.)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the load operation</returns>
        Task LoadConfigurationAsync(string source, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the available coding standards.
        /// </summary>
        /// <returns>List of available coding standards</returns>
        Task<List<CodingStandard>> GetAvailableStandardsAsync();

        /// <summary>
        /// Gets the coding standards that apply to a specific agent.
        /// </summary>
        /// <param name="agentId">The agent ID</param>
        /// <returns>List of applicable coding standards</returns>
        Task<List<CodingStandard>> GetStandardsForAgentAsync(string agentId);

        /// <summary>
        /// Gets the coding standards that apply to a specific file type.
        /// </summary>
        /// <param name="fileExtension">The file extension (e.g., ".cs", ".js")</param>
        /// <returns>List of applicable coding standards</returns>
        Task<List<CodingStandard>> GetStandardsForFileTypeAsync(string fileExtension);

        /// <summary>
        /// Auto-fixes violations in the provided code when possible.
        /// </summary>
        /// <param name="code">The code to fix</param>
        /// <param name="filePath">The file path of the code (optional)</param>
        /// <param name="agentId">The ID of the agent that generated the code (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The fixed code and list of applied fixes</returns>
        Task<(string FixedCode, List<string> AppliedFixes)> AutoFixCodeAsync(
            string code, 
            string? filePath = null, 
            string? agentId = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if the analyzer is configured and ready to use.
        /// </summary>
        /// <returns>True if the analyzer is ready, false otherwise</returns>
        bool IsConfigured();

        /// <summary>
        /// Gets statistics about the current configuration and usage.
        /// </summary>
        /// <returns>Statistics about the analyzer</returns>
        Task<CodingStandardAnalyzerStatistics> GetStatisticsAsync();
    }

    /// <summary>
    /// Represents statistics about the coding standards analyzer.
    /// </summary>
    public class CodingStandardAnalyzerStatistics
    {
        /// <summary>
        /// Gets or sets the total number of configured standards.
        /// </summary>
        public int TotalStandards { get; set; }

        /// <summary>
        /// Gets or sets the total number of configured rules.
        /// </summary>
        public int TotalRules { get; set; }

        /// <summary>
        /// Gets or sets the number of enabled standards.
        /// </summary>
        public int EnabledStandards { get; set; }

        /// <summary>
        /// Gets or sets the number of enabled rules.
        /// </summary>
        public int EnabledRules { get; set; }

        /// <summary>
        /// Gets or sets the number of configured agents.
        /// </summary>
        public int ConfiguredAgents { get; set; }

        /// <summary>
        /// Gets or sets the number of configured file types.
        /// </summary>
        public int ConfiguredFileTypes { get; set; }

        /// <summary>
        /// Gets or sets the total number of validations performed.
        /// </summary>
        public long TotalValidations { get; set; }

        /// <summary>
        /// Gets or sets the total number of violations found.
        /// </summary>
        public long TotalViolations { get; set; }

        /// <summary>
        /// Gets or sets the total number of auto-fixes applied.
        /// </summary>
        public long TotalAutoFixes { get; set; }

        /// <summary>
        /// Gets or sets the average validation time in milliseconds.
        /// </summary>
        public double AverageValidationTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the last configuration update time.
        /// </summary>
        public DateTime LastConfigurationUpdate { get; set; }

        /// <summary>
        /// Gets or sets the last validation time.
        /// </summary>
        public DateTime LastValidationTime { get; set; }
    }
}
