using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using System;

namespace Nexo.Core.Application.Services.AI.Performance
{
    /// <summary>
    /// Performance metrics for AI operations.
    /// </summary>
    public class PerformanceMetrics
    {
        public string OperationId { get; set; } = string.Empty;
        public AIOperationType OperationType { get; set; }
        public AIProviderType ProviderType { get; set; }
        public AIEngineType EngineType { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public AIOperationStatus Status { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public long InitialMemoryUsage { get; set; }
        public long FinalMemoryUsage { get; set; }
        public long MemoryDelta { get; set; }
        public double InitialCpuUsage { get; set; }
        public double FinalCpuUsage { get; set; }
        public double CpuDelta { get; set; }
        public double PerformanceScore { get; set; }
    }

    /// <summary>
    /// Performance statistics for AI operations.
    /// </summary>
    public class PerformanceStatistics
    {
        public int TotalOperations { get; set; }
        public int SuccessfulOperations { get; set; }
        public int FailedOperations { get; set; }
        public double SuccessRate { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public TimeSpan MinDuration { get; set; }
        public TimeSpan MaxDuration { get; set; }
        public double AveragePerformanceScore { get; set; }
        public double AverageMemoryUsage { get; set; }
        public double AverageCpuUsage { get; set; }
        public string PerformanceTrend { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Performance recommendation for AI operations.
    /// </summary>
    public class PerformanceRecommendation
    {
        public string Type { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
    }
}
