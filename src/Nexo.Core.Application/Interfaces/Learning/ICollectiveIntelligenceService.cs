using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Models.Learning;

namespace Nexo.Core.Application.Interfaces.Learning
{
    /// <summary>
    /// Interface for collective intelligence service in Phase 9.
    /// Enables cross-project learning and industry pattern recognition.
    /// </summary>
    public interface ICollectiveIntelligenceService
    {
        /// <summary>
        /// Creates feature knowledge sharing system.
        /// </summary>
        /// <param name="featureKnowledge">The feature knowledge to share</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Knowledge sharing result</returns>
        Task<KnowledgeSharingResult> ShareFeatureKnowledgeAsync(
            FeatureKnowledge featureKnowledge,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Implements cross-project learning.
        /// </summary>
        /// <param name="projectData">The project data to learn from</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Cross-project learning result</returns>
        Task<CrossProjectLearningResult> LearnFromProjectAsync(
            ProjectData projectData,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds industry pattern recognition.
        /// </summary>
        /// <param name="industryPattern">The industry pattern to recognize</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Pattern recognition result</returns>
        Task<PatternRecognitionResult> RecognizeIndustryPatternAsync(
            IndustryPattern industryPattern,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates collective intelligence database.
        /// </summary>
        /// <param name="intelligenceData">The intelligence data to store</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Database creation result</returns>
        Task<DatabaseCreationResult> CreateIntelligenceDatabaseAsync(
            IntelligenceData intelligenceData,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches collective intelligence for insights.
        /// </summary>
        /// <param name="searchQuery">The search query</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Search results</returns>
        Task<IntelligenceSearchResult> SearchIntelligenceAsync(
            IntelligenceSearchQuery searchQuery,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets collective intelligence statistics.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Intelligence statistics</returns>
        Task<IntelligenceStatistics> GetIntelligenceStatisticsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Exports collective intelligence data.
        /// </summary>
        /// <param name="exportOptions">The export options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Intelligence export</returns>
        Task<IntelligenceExport> ExportIntelligenceAsync(
            IntelligenceExportOptions exportOptions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Imports collective intelligence data.
        /// </summary>
        /// <param name="importData">The import data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Import result</returns>
        Task<IntelligenceImportResult> ImportIntelligenceAsync(
            IntelligenceImportData importData,
            CancellationToken cancellationToken = default);
    }
}
