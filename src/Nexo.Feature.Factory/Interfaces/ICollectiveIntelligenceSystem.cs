using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Factory.Models;

namespace Nexo.Feature.Factory.Interfaces;

/// <summary>
/// Collective intelligence system for feature knowledge sharing and cross-project learning
/// </summary>
public interface ICollectiveIntelligenceSystem
{
    /// <summary>
    /// Creates feature knowledge sharing system
    /// </summary>
    /// <param name="sharingRequest">Knowledge sharing request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Knowledge sharing result</returns>
    Task<FeatureKnowledgeSharingResult> ShareFeatureKnowledgeAsync(FeatureKnowledgeSharingRequest sharingRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Implements cross-project learning capabilities
    /// </summary>
    /// <param name="learningRequest">Cross-project learning request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cross-project learning result</returns>
    Task<CrossProjectLearningResult> LearnFromCrossProjectsAsync(CrossProjectLearningRequest learningRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds industry pattern recognition capabilities
    /// </summary>
    /// <param name="recognitionRequest">Industry pattern recognition request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Pattern recognition result</returns>
    Task<IndustryPatternRecognitionResult> RecognizeIndustryPatternsAsync(IndustryPatternRecognitionRequest recognitionRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates collective intelligence database
    /// </summary>
    /// <param name="databaseRequest">Collective intelligence database request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Database creation result</returns>
    Task<CollectiveIntelligenceDatabaseResult> CreateCollectiveIntelligenceDatabaseAsync(CollectiveIntelligenceDatabaseRequest databaseRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches collective intelligence for relevant knowledge
    /// </summary>
    /// <param name="searchRequest">Intelligence search request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search result</returns>
    Task<CollectiveIntelligenceSearchResult> SearchCollectiveIntelligenceAsync(CollectiveIntelligenceSearchRequest searchRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets collective intelligence metrics
    /// </summary>
    /// <param name="metricsRequest">Collective intelligence metrics request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collective intelligence metrics</returns>
    Task<CollectiveIntelligenceMetrics> GetCollectiveIntelligenceMetricsAsync(CollectiveIntelligenceMetricsRequest metricsRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronizes collective intelligence across nodes
    /// </summary>
    /// <param name="syncRequest">Intelligence synchronization request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Synchronization result</returns>
    Task<CollectiveIntelligenceSyncResult> SynchronizeCollectiveIntelligenceAsync(CollectiveIntelligenceSyncRequest syncRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports collective intelligence data
    /// </summary>
    /// <param name="exportRequest">Collective intelligence export request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export result</returns>
    Task<CollectiveIntelligenceExportResult> ExportCollectiveIntelligenceAsync(CollectiveIntelligenceExportRequest exportRequest, CancellationToken cancellationToken = default);
} 