using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

/// <summary>
/// Feature knowledge sharing request
/// </summary>
public record FeatureKnowledgeSharingRequest
{
    public List<FeatureKnowledge> KnowledgeItems { get; init; } = new();
    public string ProjectId { get; init; } = string.Empty;
    public string OrganizationId { get; init; } = string.Empty;
    public bool EnableAnonymization { get; init; }
    public bool EnableEncryption { get; init; }
    public List<string> SharingScopes { get; init; } = new();
    public Dictionary<string, object> SharingParameters { get; init; } = new();
}

/// <summary>
/// Feature knowledge
/// </summary>
public record FeatureKnowledge
{
    public string KnowledgeId { get; init; } = string.Empty;
    public string FeatureId { get; init; } = string.Empty;
    public string ProjectId { get; init; } = string.Empty;
    public string KnowledgeType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public List<string> Tags { get; init; } = new();
    public List<string> Categories { get; init; } = new();
    public double QualityScore { get; init; }
    public int UsageCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastUpdated { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Feature knowledge sharing result
/// </summary>
public record FeatureKnowledgeSharingResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<SharedKnowledge> SharedKnowledge { get; init; } = new();
    public int KnowledgeItemsShared { get; init; }
    public int RecipientsNotified { get; init; }
    public double SharingSuccessRate { get; init; }
    public TimeSpan SharingDuration { get; init; }
    public Dictionary<string, double> SharingMetrics { get; init; } = new();
    public DateTime SharedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Shared knowledge
/// </summary>
public record SharedKnowledge
{
    public string KnowledgeId { get; init; } = string.Empty;
    public string SharingId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public List<string> Recipients { get; init; } = new();
    public DateTime SharedAt { get; init; }
    public Dictionary<string, object> SharingData { get; init; } = new();
}

/// <summary>
/// Cross-project learning request
/// </summary>
public record CrossProjectLearningRequest
{
    public List<string> SourceProjectIds { get; init; } = new();
    public string TargetProjectId { get; init; } = string.Empty;
    public List<string> LearningTypes { get; init; } = new();
    public bool EnablePatternTransfer { get; init; }
    public bool EnableKnowledgeTransfer { get; init; }
    public Dictionary<string, object> LearningParameters { get; init; } = new();
}

/// <summary>
/// Cross-project learning result
/// </summary>
public record CrossProjectLearningResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<TransferredKnowledge> TransferredKnowledge { get; init; } = new();
    public List<TransferredPattern> TransferredPatterns { get; init; } = new();
    public int KnowledgeItemsTransferred { get; init; }
    public int PatternsTransferred { get; init; }
    public double TransferSuccessRate { get; init; }
    public TimeSpan TransferDuration { get; init; }
    public Dictionary<string, double> TransferMetrics { get; init; } = new();
    public DateTime TransferredAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Transferred knowledge
/// </summary>
public record TransferredKnowledge
{
    public string KnowledgeId { get; init; } = string.Empty;
    public string SourceProjectId { get; init; } = string.Empty;
    public string TargetProjectId { get; init; } = string.Empty;
    public string KnowledgeType { get; init; } = string.Empty;
    public double RelevanceScore { get; init; }
    public DateTime TransferredAt { get; init; }
    public Dictionary<string, object> TransferData { get; init; } = new();
}

/// <summary>
/// Transferred pattern
/// </summary>
public record TransferredPattern
{
    public string PatternId { get; init; } = string.Empty;
    public string SourceProjectId { get; init; } = string.Empty;
    public string TargetProjectId { get; init; } = string.Empty;
    public string PatternType { get; init; } = string.Empty;
    public double AdaptationScore { get; init; }
    public DateTime TransferredAt { get; init; }
    public Dictionary<string, object> AdaptationData { get; init; } = new();
}

/// <summary>
/// Industry pattern recognition request
/// </summary>
public record IndustryPatternRecognitionRequest
{
    public string Industry { get; init; } = string.Empty;
    public List<string> PatternTypes { get; init; } = new();
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public bool EnableRealTimeRecognition { get; init; }
    public Dictionary<string, object> RecognitionParameters { get; init; } = new();
}

/// <summary>
/// Industry pattern recognition result
/// </summary>
public record IndustryPatternRecognitionResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<IndustryPattern> RecognizedPatterns { get; init; } = new();
    public List<IndustryTrend> IndustryTrends { get; init; } = new();
    public int PatternsRecognized { get; init; }
    public int NewPatternsDiscovered { get; init; }
    public double RecognitionAccuracy { get; init; }
    public TimeSpan RecognitionDuration { get; init; }
    public Dictionary<string, double> RecognitionMetrics { get; init; } = new();
    public DateTime RecognizedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Industry pattern
/// </summary>
public record IndustryPattern
{
    public string PatternId { get; init; } = string.Empty;
    public string Industry { get; init; } = string.Empty;
    public string PatternType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double Prevalence { get; init; }
    public double Effectiveness { get; init; }
    public List<string> Companies { get; init; } = new();
    public List<string> UseCases { get; init; } = new();
    public DateTime FirstObserved { get; init; }
    public DateTime LastObserved { get; init; }
    public Dictionary<string, object> PatternData { get; init; } = new();
}

/// <summary>
/// Industry trend
/// </summary>
public record IndustryTrend
{
    public string TrendId { get; init; } = string.Empty;
    public string Industry { get; init; } = string.Empty;
    public string TrendType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double TrendStrength { get; init; }
    public string Direction { get; init; } = string.Empty;
    public List<TrendDataPoint> DataPoints { get; init; } = new();
    public DateTime TrendStart { get; init; }
    public DateTime TrendEnd { get; init; }
    public Dictionary<string, object> TrendData { get; init; } = new();
}

/// <summary>
/// Collective intelligence database request
/// </summary>
public record CollectiveIntelligenceDatabaseRequest
{
    public string DatabaseName { get; init; } = string.Empty;
    public List<string> DatabaseTypes { get; init; } = new();
    public bool EnableDistributedStorage { get; init; }
    public bool EnableRealTimeIndexing { get; init; }
    public Dictionary<string, object> DatabaseParameters { get; init; } = new();
}

/// <summary>
/// Collective intelligence database result
/// </summary>
public record CollectiveIntelligenceDatabaseResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public string DatabaseId { get; init; } = string.Empty;
    public string DatabaseName { get; init; } = string.Empty;
    public List<string> DatabaseTypes { get; init; } = new();
    public long StorageCapacity { get; init; }
    public long UsedStorage { get; init; }
    public int KnowledgeItems { get; init; }
    public int ConnectedNodes { get; init; }
    public DateTime CreatedAt { get; init; }
    public Dictionary<string, object> DatabaseInfo { get; init; } = new();
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Collective intelligence search request
/// </summary>
public record CollectiveIntelligenceSearchRequest
{
    public string Query { get; init; } = string.Empty;
    public List<string> SearchTypes { get; init; } = new();
    public List<string> Filters { get; init; } = new();
    public int MaxResults { get; init; }
    public bool EnableSemanticSearch { get; init; }
    public Dictionary<string, object> SearchParameters { get; init; } = new();
}

/// <summary>
/// Collective intelligence search result
/// </summary>
public record CollectiveIntelligenceSearchResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<SearchResult> SearchResults { get; init; } = new();
    public int TotalResults { get; init; }
    public int ResultsReturned { get; init; }
    public TimeSpan SearchDuration { get; init; }
    public double SearchRelevance { get; init; }
    public Dictionary<string, double> SearchMetrics { get; init; } = new();
    public DateTime SearchedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Search result
/// </summary>
public record SearchResult
{
    public string ResultId { get; init; } = string.Empty;
    public string ResultType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public double RelevanceScore { get; init; }
    public string Source { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public Dictionary<string, object> ResultData { get; init; } = new();
}

/// <summary>
/// Collective intelligence metrics request
/// </summary>
public record CollectiveIntelligenceMetricsRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public List<string> MetricTypes { get; init; } = new();
    public bool IncludeTrends { get; init; }
    public Dictionary<string, object> MetricsParameters { get; init; } = new();
}

/// <summary>
/// Collective intelligence metrics
/// </summary>
public record CollectiveIntelligenceMetrics
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public int TotalKnowledgeItems { get; init; }
    public int ActiveProjects { get; init; }
    public int ConnectedOrganizations { get; init; }
    public double KnowledgeSharingRate { get; init; }
    public double CrossProjectLearningRate { get; init; }
    public List<IntelligenceTrend> IntelligenceTrends { get; init; } = new();
    public Dictionary<string, double> DetailedMetrics { get; init; } = new();
    public DateTime GeneratedAt { get; init; }
}

/// <summary>
/// Intelligence trend
/// </summary>
public record IntelligenceTrend
{
    public string TrendId { get; init; } = string.Empty;
    public string MetricName { get; init; } = string.Empty;
    public string Direction { get; init; } = string.Empty;
    public double TrendStrength { get; init; }
    public List<TrendDataPoint> DataPoints { get; init; } = new();
    public DateTime TrendStart { get; init; }
    public DateTime TrendEnd { get; init; }
    public Dictionary<string, object> TrendData { get; init; } = new();
}

/// <summary>
/// Collective intelligence synchronization request
/// </summary>
public record CollectiveIntelligenceSyncRequest
{
    public List<string> NodeIds { get; init; } = new();
    public string SyncType { get; init; } = string.Empty;
    public bool EnableBidirectionalSync { get; init; }
    public bool EnableConflictResolution { get; init; }
    public Dictionary<string, object> SyncParameters { get; init; } = new();
}

/// <summary>
/// Collective intelligence synchronization result
/// </summary>
public record CollectiveIntelligenceSyncResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<SyncResult> SyncResults { get; init; } = new();
    public int NodesSynchronized { get; init; }
    public int ConflictsResolved { get; init; }
    public double SyncSuccessRate { get; init; }
    public TimeSpan SyncDuration { get; init; }
    public Dictionary<string, double> SyncMetrics { get; init; } = new();
    public DateTime SyncedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Sync result
/// </summary>
public record SyncResult
{
    public string NodeId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int ItemsSynced { get; init; }
    public int ConflictsResolved { get; init; }
    public DateTime SyncedAt { get; init; }
    public Dictionary<string, object> SyncData { get; init; } = new();
}

/// <summary>
/// Collective intelligence export request
/// </summary>
public record CollectiveIntelligenceExportRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string Format { get; init; } = string.Empty;
    public List<string> DataTypes { get; init; } = new();
    public bool IncludeMetadata { get; init; }
    public string? FilePath { get; init; }
}

/// <summary>
/// Collective intelligence export result
/// </summary>
public record CollectiveIntelligenceExportResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public int DataRecordsExported { get; init; }
    public DateTime ExportedAt { get; init; }
    public string? ErrorMessage { get; init; }
} 