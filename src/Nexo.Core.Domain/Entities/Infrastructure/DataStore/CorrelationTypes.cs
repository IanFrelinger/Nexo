using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Entities.Infrastructure.DataStore
{
    /// <summary>
    /// Correlation information
    /// </summary>
    public record Correlation
    {
        /// <summary>
        /// Correlation strength
        /// </summary>
        public double Strength { get; init; } = 0.0;
        
        /// <summary>
        /// Direction of correlation
        /// </summary>
        public string Direction { get; init; } = "Unknown";
        
        /// <summary>
        /// Description of the correlation
        /// </summary>
        public string Description { get; init; } = string.Empty;
        
        /// <summary>
        /// Confidence level
        /// </summary>
        public double Confidence { get; init; } = 0.0;
        
        /// <summary>
        /// Statistical significance
        /// </summary>
        public bool IsSignificant { get; init; } = false;
    }

    /// <summary>
    /// Correlation analysis result
    /// </summary>
    public record CorrelationAnalysis
    {
        /// <summary>
        /// Analysis identifier
        /// </summary>
        public string Id { get; init; } = string.Empty;
        
        /// <summary>
        /// Analysis name
        /// </summary>
        public string Name { get; init; } = string.Empty;
        
        /// <summary>
        /// Correlations found
        /// </summary>
        public List<Correlation> Correlations { get; init; } = new();
        
        /// <summary>
        /// Analysis parameters
        /// </summary>
        public Dictionary<string, object> Parameters { get; init; } = new();
        
        /// <summary>
        /// Analysis metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();
        
        /// <summary>
        /// Analysis timestamp
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Correlation matrix
    /// </summary>
    public record CorrelationMatrix
    {
        /// <summary>
        /// Matrix identifier
        /// </summary>
        public string Id { get; init; } = string.Empty;
        
        /// <summary>
        /// Variable names
        /// </summary>
        public List<string> Variables { get; init; } = new();
        
        /// <summary>
        /// Correlation coefficients
        /// </summary>
        public Dictionary<string, Dictionary<string, double>> Coefficients { get; init; } = new();
        
        /// <summary>
        /// Matrix metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

    /// <summary>
    /// Correlation query
    /// </summary>
    public record CorrelationQuery
    {
        /// <summary>
        /// Variables to analyze
        /// </summary>
        public List<string> Variables { get; init; } = new();
        
        /// <summary>
        /// Analysis method
        /// </summary>
        public string Method { get; init; } = "pearson";
        
        /// <summary>
        /// Significance threshold
        /// </summary>
        public double SignificanceThreshold { get; init; } = 0.05;
        
        /// <summary>
        /// Minimum correlation strength
        /// </summary>
        public double MinimumStrength { get; init; } = 0.0;
        
        /// <summary>
        /// Query metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

    /// <summary>
    /// Correlation result
    /// </summary>
    public record CorrelationResult
    {
        /// <summary>
        /// Query that generated this result
        /// </summary>
        public CorrelationQuery Query { get; init; } = new();
        
        /// <summary>
        /// Correlation matrix
        /// </summary>
        public CorrelationMatrix Matrix { get; init; } = new();
        
        /// <summary>
        /// Significant correlations
        /// </summary>
        public List<Correlation> SignificantCorrelations { get; init; } = new();
        
        /// <summary>
        /// Result metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();
        
        /// <summary>
        /// Analysis timestamp
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }
}
