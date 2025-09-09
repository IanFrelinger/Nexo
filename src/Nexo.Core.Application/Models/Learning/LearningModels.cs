using System;
using System.Collections.Generic;

namespace Nexo.Core.Application.Models.Learning
{
    /// <summary>
    /// Represents a feature pattern for learning.
    /// </summary>
    public class FeaturePattern
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Complexity { get; set; } = string.Empty;
        public List<string> Technologies { get; set; } = new List<string>();
        public List<string> Patterns { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; }
        public DateTime LastUsed { get; set; }
        public int UsageCount { get; set; }
        public double SuccessRate { get; set; }
        public TimeSpan AverageGenerationTime { get; set; }
    }

    /// <summary>
    /// Represents domain knowledge for accumulation.
    /// </summary>
    public class DomainKnowledge
    {
        public string Id { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string KnowledgeType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        public List<string> Tags { get; set; } = new List<string>();
        public double Confidence { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdated { get; set; }
        public int ReferenceCount { get; set; }
        public string Source { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents usage data for pattern analysis.
    /// </summary>
    public class UsageData
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string FeatureId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents learning feedback for continuous improvement.
    /// </summary>
    public class LearningFeedback
    {
        public string Id { get; set; } = string.Empty;
        public string FeatureId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string FeedbackType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int Rating { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; }
        public string SessionId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents learning context for insights.
    /// </summary>
    public class LearningContext
    {
        public string UserId { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string FeatureType { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public DateTime RequestTime { get; set; }
        public string SessionId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents learning data for model updates.
    /// </summary>
    public class LearningData
    {
        public string Id { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public List<string> Labels { get; set; } = new List<string>();
        public double Weight { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Source { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents validation data for learning effectiveness.
    /// </summary>
    public class ValidationData
    {
        public string Id { get; set; } = string.Empty;
        public string ValidationType { get; set; } = string.Empty;
        public Dictionary<string, object> TestData { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> ExpectedResults { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; }
        public string Source { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents learning data export options.
    /// </summary>
    public class LearningDataExportOptions
    {
        public string Format { get; set; } = "JSON";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<string> DataTypes { get; set; } = new List<string>();
        public bool IncludeMetadata { get; set; } = true;
        public bool Compress { get; set; } = false;
    }

    /// <summary>
    /// Represents the result of learning from feature patterns.
    /// </summary>
    public class LearningResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string PatternId { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public List<string> Insights { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public DateTime ProcessedAt { get; set; }
    }

    /// <summary>
    /// Represents the result of domain knowledge accumulation.
    /// </summary>
    public class KnowledgeAccumulationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string KnowledgeId { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public List<string> RelatedKnowledge { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public DateTime ProcessedAt { get; set; }
    }

    /// <summary>
    /// Represents the result of usage pattern analysis.
    /// </summary>
    public class UsagePatternAnalysisResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<UsagePattern> Patterns { get; set; } = new List<UsagePattern>();
        public List<Recommendation> Recommendations { get; set; } = new List<Recommendation>();
        public Dictionary<string, object> Statistics { get; set; } = new Dictionary<string, object>();
        public DateTime AnalyzedAt { get; set; }
    }

    /// <summary>
    /// Represents a usage pattern.
    /// </summary>
    public class UsagePattern
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Frequency { get; set; }
        public double Confidence { get; set; }
        public List<string> Triggers { get; set; } = new List<string>();
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents a recommendation.
    /// </summary>
    public class Recommendation
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Priority { get; set; }
        public double Confidence { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents the result of feedback processing.
    /// </summary>
    public class FeedbackProcessingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string FeedbackId { get; set; } = string.Empty;
        public List<string> Actions { get; set; } = new List<string>();
        public Dictionary<string, object> Impact { get; set; } = new Dictionary<string, object>();
        public DateTime ProcessedAt { get; set; }
    }

    /// <summary>
    /// Represents learning insights.
    /// </summary>
    public class LearningInsights
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string InsightType { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public List<Recommendation> Recommendations { get; set; } = new List<Recommendation>();
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Represents the result of model updates.
    /// </summary>
    public class ModelUpdateResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ModelId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public Dictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Represents the result of learning validation.
    /// </summary>
    public class LearningValidationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public double Accuracy { get; set; }
        public double Precision { get; set; }
        public double Recall { get; set; }
        public double F1Score { get; set; }
        public Dictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();
        public DateTime ValidatedAt { get; set; }
    }

    /// <summary>
    /// Represents learning data export.
    /// </summary>
    public class LearningDataExport
    {
        public string Id { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public long Size { get; set; }
        public DateTime ExportedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
