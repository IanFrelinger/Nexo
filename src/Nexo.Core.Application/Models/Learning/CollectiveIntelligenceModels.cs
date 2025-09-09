using System;
using System.Collections.Generic;

namespace Nexo.Core.Application.Models.Learning
{
    /// <summary>
    /// Represents feature knowledge for sharing.
    /// </summary>
    public class FeatureKnowledge
    {
        public string Id { get; set; } = string.Empty;
        public string FeatureId { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string KnowledgeType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public List<string> Tags { get; set; } = new List<string>();
        public double Confidence { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public int ShareCount { get; set; }
        public double Rating { get; set; }
    }

    /// <summary>
    /// Represents project data for cross-project learning.
    /// </summary>
    public class ProjectData
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Technology { get; set; } = string.Empty;
        public List<string> Features { get; set; } = new List<string>();
        public Dictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();
        public List<string> Patterns { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents an industry pattern.
    /// </summary>
    public class IndustryPattern
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<string> Technologies { get; set; } = new List<string>();
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        public List<string> Examples { get; set; } = new List<string>();
        public double Frequency { get; set; }
        public double Confidence { get; set; }
        public DateTime DiscoveredAt { get; set; }
        public string Source { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents intelligence data for database storage.
    /// </summary>
    public class IntelligenceData
    {
        public string Id { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public List<string> Categories { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; }
        public string Source { get; set; } = string.Empty;
        public double Weight { get; set; }
    }

    /// <summary>
    /// Represents an intelligence search query.
    /// </summary>
    public class IntelligenceSearchQuery
    {
        public string Query { get; set; } = string.Empty;
        public List<string> Categories { get; set; } = new List<string>();
        public List<string> Tags { get; set; } = new List<string>();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxResults { get; set; } = 100;
        public string SortBy { get; set; } = "relevance";
        public bool IncludeMetadata { get; set; } = true;
    }

    /// <summary>
    /// Represents intelligence export options.
    /// </summary>
    public class IntelligenceExportOptions
    {
        public string Format { get; set; } = "JSON";
        public List<string> DataTypes { get; set; } = new List<string>();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IncludeMetadata { get; set; } = true;
        public bool Compress { get; set; } = false;
        public string Filter { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents intelligence import data.
    /// </summary>
    public class IntelligenceImportData
    {
        public string Id { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public DateTime ImportedAt { get; set; }
        public string Source { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents the result of knowledge sharing.
    /// </summary>
    public class KnowledgeSharingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string KnowledgeId { get; set; } = string.Empty;
        public int ShareCount { get; set; }
        public List<string> Recipients { get; set; } = new List<string>();
        public Dictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();
        public DateTime SharedAt { get; set; }
    }

    /// <summary>
    /// Represents the result of cross-project learning.
    /// </summary>
    public class CrossProjectLearningResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public List<string> LearnedPatterns { get; set; } = new List<string>();
        public List<string> Insights { get; set; } = new List<string>();
        public Dictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();
        public DateTime LearnedAt { get; set; }
    }

    /// <summary>
    /// Represents the result of pattern recognition.
    /// </summary>
    public class PatternRecognitionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string PatternId { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public List<string> Matches { get; set; } = new List<string>();
        public List<string> Recommendations { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public DateTime RecognizedAt { get; set; }
    }

    /// <summary>
    /// Represents the result of database creation.
    /// </summary>
    public class DatabaseCreationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string DatabaseId { get; set; } = string.Empty;
        public int RecordCount { get; set; }
        public Dictionary<string, object> Schema { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Represents intelligence search results.
    /// </summary>
    public class IntelligenceSearchResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<IntelligenceItem> Items { get; set; } = new List<IntelligenceItem>();
        public int TotalCount { get; set; }
        public int PageCount { get; set; }
        public int CurrentPage { get; set; }
        public Dictionary<string, object> Facets { get; set; } = new Dictionary<string, object>();
        public DateTime SearchedAt { get; set; }
    }

    /// <summary>
    /// Represents an intelligence item.
    /// </summary>
    public class IntelligenceItem
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public double Relevance { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; }
        public string Source { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents intelligence statistics.
    /// </summary>
    public class IntelligenceStatistics
    {
        public int TotalItems { get; set; }
        public int TotalProjects { get; set; }
        public int TotalPatterns { get; set; }
        public int TotalKnowledge { get; set; }
        public Dictionary<string, int> CategoryCounts { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, double> QualityMetrics { get; set; } = new Dictionary<string, double>();
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Represents intelligence export.
    /// </summary>
    public class IntelligenceExport
    {
        public string Id { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public long Size { get; set; }
        public int ItemCount { get; set; }
        public DateTime ExportedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents the result of intelligence import.
    /// </summary>
    public class IntelligenceImportResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ImportedCount { get; set; }
        public int SkippedCount { get; set; }
        public int ErrorCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public Dictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();
        public DateTime ImportedAt { get; set; }
    }
}
