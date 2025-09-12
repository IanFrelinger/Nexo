using System;
using System.Collections.Generic;

namespace Nexo.Feature.AI.Models.RAG
{
    /// <summary>
    /// Represents a chunk of documentation for RAG processing
    /// </summary>
    public class DocumentationChunk
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string DocumentationType { get; set; } = string.Empty; // API, Language, Framework, etc.
        public string Version { get; set; } = string.Empty; // C# version or .NET version
        public string Runtime { get; set; } = string.Empty; // .NET Framework, .NET Core, .NET 5+, etc.
        public List<string> Tags { get; set; } = new List<string>();
        public List<string> Categories { get; set; } = new List<string>();
        public string SourceUrl { get; set; } = string.Empty;
        public DateTime IndexedAt { get; set; } = DateTime.UtcNow;
        public double RelevanceScore { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents a RAG query
    /// </summary>
    public class RAGQuery
    {
        public string Query { get; set; } = string.Empty;
        public int MaxResults { get; set; } = 10;
        public double SimilarityThreshold { get; set; } = 0.7;
        public DocumentationContextType ContextType { get; set; } = DocumentationContextType.General;
        public List<DocumentationFilter> Filters { get; set; } = new List<DocumentationFilter>();
    }

    /// <summary>
    /// Represents a RAG response
    /// </summary>
    public class RAGResponse
    {
        public string Query { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public List<DocumentationChunk> RetrievedChunks { get; set; } = new List<DocumentationChunk>();
        public string AIResponse { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; }
        public long ProcessingTimeMs { get; set; }
    }

    /// <summary>
    /// Documentation context types for different use cases
    /// </summary>
    public enum DocumentationContextType
    {
        General,
        CodeGeneration,
        ProblemSolving,
        APIReference,
        LanguageFeatures,
        FrameworkSpecific,
        PerformanceOptimization,
        Security,
        Testing,
        Debugging
    }

    /// <summary>
    /// Filter for documentation search
    /// </summary>
    public class DocumentationFilter
    {
        public string Field { get; set; } = string.Empty; // Version, Runtime, Type, etc.
        public string Value { get; set; } = string.Empty;
        public FilterOperator Operator { get; set; } = FilterOperator.Equals;
    }

    /// <summary>
    /// Filter operators
    /// </summary>
    public enum FilterOperator
    {
        Equals,
        Contains,
        StartsWith,
        EndsWith,
        GreaterThan,
        LessThan,
        In
    }
}
