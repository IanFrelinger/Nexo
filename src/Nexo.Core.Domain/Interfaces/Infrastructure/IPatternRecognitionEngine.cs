using System;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Domain.Interfaces.Infrastructure;

/// <summary>
/// Interface for pattern recognition engine
/// </summary>
public interface IPatternRecognitionEngine
{
    /// <summary>
    /// Identify patterns in user feedback
    /// </summary>
    Task<IEnumerable<Pattern>> IdentifyPatternsAsync(IEnumerable<UserFeedback> feedback, IEnumerable<PerformanceData> performanceData);
    
    /// <summary>
    /// Analyze correlations between performance data and adaptation records
    /// </summary>
    Task<IEnumerable<Nexo.Core.Domain.Entities.Infrastructure.Correlation>> AnalyzeCorrelationsAsync(IEnumerable<PerformanceData> performanceData, IEnumerable<AdaptationRecord> adaptationRecords);
    
    /// <summary>
    /// Detect anomalies in data
    /// </summary>
    Task<IEnumerable<Anomaly>> DetectAnomaliesAsync(IEnumerable<object> data);
    
    /// <summary>
    /// Predict future trends based on historical data
    /// </summary>
    Task<IEnumerable<TrendPrediction>> PredictTrendsAsync(IEnumerable<object> historicalData, TimeSpan predictionWindow);
    
    /// <summary>
    /// Classify data into categories
    /// </summary>
    Task<IEnumerable<Classification>> ClassifyDataAsync(IEnumerable<object> data, string categoryType);
    
    /// <summary>
    /// Get pattern recognition statistics
    /// </summary>
    Task<PatternRecognitionStatistics> GetStatisticsAsync();
}

/// <summary>
/// Pattern information
/// </summary>
public record Pattern
{
    /// <summary>
    /// Pattern identifier
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Pattern type
    /// </summary>
    public PatternType Type { get; init; }
    
    /// <summary>
    /// Pattern description
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Pattern confidence (0-1)
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// Pattern frequency
    /// </summary>
    public double Frequency { get; init; }
    
    /// <summary>
    /// Pattern strength
    /// </summary>
    public double Strength { get; init; }
    
    /// <summary>
    /// Supporting data points
    /// </summary>
    public List<object> SupportingData { get; init; } = new();
    
    /// <summary>
    /// Pattern timestamp
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Pattern types
/// </summary>
public enum PatternType
{
    /// <summary>
    /// Performance pattern
    /// </summary>
    Performance,
    
    /// <summary>
    /// User behavior pattern
    /// </summary>
    UserBehavior,
    
    /// <summary>
    /// Error pattern
    /// </summary>
    Error,
    
    /// <summary>
    /// Resource usage pattern
    /// </summary>
    ResourceUsage,
    
    /// <summary>
    /// Temporal pattern
    /// </summary>
    Temporal,
    
    /// <summary>
    /// Correlation pattern
    /// </summary>
    Correlation
}

/// <summary>
/// Correlation information
/// </summary>
public record Correlation
{
    /// <summary>
    /// Correlation identifier
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// First variable name
    /// </summary>
    public string Variable1 { get; init; } = string.Empty;
    
    /// <summary>
    /// Second variable name
    /// </summary>
    public string Variable2 { get; init; } = string.Empty;
    
    /// <summary>
    /// Correlation coefficient (-1 to 1)
    /// </summary>
    public double Coefficient { get; init; }
    
    /// <summary>
    /// Correlation strength
    /// </summary>
    public CorrelationStrength Strength { get; init; }
    
    /// <summary>
    /// Correlation significance (p-value)
    /// </summary>
    public double Significance { get; init; }
    
    /// <summary>
    /// Correlation direction
    /// </summary>
    public CorrelationDirection Direction { get; init; }
    
    /// <summary>
    /// Correlation timestamp
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Correlation strength levels
/// </summary>
public enum CorrelationStrength
{
    /// <summary>
    /// Very weak correlation
    /// </summary>
    VeryWeak,
    
    /// <summary>
    /// Weak correlation
    /// </summary>
    Weak,
    
    /// <summary>
    /// Moderate correlation
    /// </summary>
    Moderate,
    
    /// <summary>
    /// Strong correlation
    /// </summary>
    Strong,
    
    /// <summary>
    /// Very strong correlation
    /// </summary>
    VeryStrong
}

/// <summary>
/// Correlation direction
/// </summary>
public enum CorrelationDirection
{
    /// <summary>
    /// Positive correlation
    /// </summary>
    Positive,
    
    /// <summary>
    /// Negative correlation
    /// </summary>
    Negative,
    
    /// <summary>
    /// No correlation
    /// </summary>
    None
}

/// <summary>
/// Anomaly information
/// </summary>
public record Anomaly
{
    /// <summary>
    /// Anomaly identifier
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Anomaly type
    /// </summary>
    public AnomalyType Type { get; init; }
    
    /// <summary>
    /// Anomaly description
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Anomaly severity
    /// </summary>
    public AnomalySeverity Severity { get; init; }
    
    /// <summary>
    /// Anomaly score (0-1)
    /// </summary>
    public double Score { get; init; }
    
    /// <summary>
    /// Anomaly confidence (0-1)
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// Anomaly timestamp
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Anomaly data
    /// </summary>
    public object? Data { get; init; }
}

/// <summary>
/// Anomaly types
/// </summary>
public enum AnomalyType
{
    /// <summary>
    /// Performance anomaly
    /// </summary>
    Performance,
    
    /// <summary>
    /// User behavior anomaly
    /// </summary>
    UserBehavior,
    
    /// <summary>
    /// System anomaly
    /// </summary>
    System,
    
    /// <summary>
    /// Data anomaly
    /// </summary>
    Data,
    
    /// <summary>
    /// Network anomaly
    /// </summary>
    Network
}

/// <summary>
/// Trend prediction
/// </summary>
public record TrendPrediction
{
    /// <summary>
    /// Prediction identifier
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Predicted variable
    /// </summary>
    public string Variable { get; init; } = string.Empty;
    
    /// <summary>
    /// Predicted value
    /// </summary>
    public double PredictedValue { get; init; }
    
    /// <summary>
    /// Prediction confidence (0-1)
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// Prediction timestamp
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Prediction window
    /// </summary>
    public TimeSpan PredictionWindow { get; init; }
    
    /// <summary>
    /// Prediction method used
    /// </summary>
    public string Method { get; init; } = string.Empty;
}

/// <summary>
/// Classification result
/// </summary>
public record Classification
{
    /// <summary>
    /// Classification identifier
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Classified data
    /// </summary>
    public object Data { get; init; } = new();
    
    /// <summary>
    /// Assigned category
    /// </summary>
    public string Category { get; init; } = string.Empty;
    
    /// <summary>
    /// Classification confidence (0-1)
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// Classification timestamp
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Classification method used
    /// </summary>
    public string Method { get; init; } = string.Empty;
}

/// <summary>
/// Pattern recognition statistics
/// </summary>
public record PatternRecognitionStatistics
{
    /// <summary>
    /// Total patterns identified
    /// </summary>
    public int TotalPatterns { get; init; }
    
    /// <summary>
    /// Total correlations found
    /// </summary>
    public int TotalCorrelations { get; init; }
    
    /// <summary>
    /// Total anomalies detected
    /// </summary>
    public int TotalAnomalies { get; init; }
    
    /// <summary>
    /// Average pattern confidence
    /// </summary>
    public double AveragePatternConfidence { get; init; }
    
    /// <summary>
    /// Average correlation strength
    /// </summary>
    public double AverageCorrelationStrength { get; init; }
    
    /// <summary>
    /// Last analysis timestamp
    /// </summary>
    public DateTime LastAnalysis { get; init; }
    
    /// <summary>
    /// Analysis success rate
    /// </summary>
    public double SuccessRate { get; init; }
}