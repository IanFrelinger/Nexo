using System;
using System.Collections.Generic;

namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Performance metrics for an aggregator.
    /// </summary>
    public class AggregatorPerformanceMetrics
    {
        /// <summary>
        /// Total execution time in milliseconds.
        /// </summary>
        public long TotalExecutionTimeMs { get; set; }
        
        /// <summary>
        /// Number of behaviors executed.
        /// </summary>
        public int BehaviorsExecuted { get; set; }
        
        /// <summary>
        /// Number of behaviors that succeeded.
        /// </summary>
        public int BehaviorsSucceeded { get; set; }
        
        /// <summary>
        /// Number of behaviors that failed.
        /// </summary>
        public int BehaviorsFailed { get; set; }
        
        /// <summary>
        /// Number of direct commands executed.
        /// </summary>
        public int DirectCommandsExecuted { get; set; }
        
        /// <summary>
        /// Number of direct commands that succeeded.
        /// </summary>
        public int DirectCommandsSucceeded { get; set; }
        
        /// <summary>
        /// Number of direct commands that failed.
        /// </summary>
        public int DirectCommandsFailed { get; set; }
        
        /// <summary>
        /// Total number of commands executed across all behaviors and direct commands.
        /// </summary>
        public int TotalCommandsExecuted { get; set; }
        
        /// <summary>
        /// Total number of commands that succeeded.
        /// </summary>
        public int TotalCommandsSucceeded { get; set; }
        
        /// <summary>
        /// Total number of commands that failed.
        /// </summary>
        public int TotalCommandsFailed { get; set; }
        
        /// <summary>
        /// Memory usage in bytes.
        /// </summary>
        public long MemoryUsageBytes { get; set; }
        
        /// <summary>
        /// CPU usage percentage.
        /// </summary>
        public double CpuUsagePercentage { get; set; }
        
        /// <summary>
        /// Number of parallel executions.
        /// </summary>
        public int ParallelExecutions { get; set; }
        
        /// <summary>
        /// Number of retries performed.
        /// </summary>
        public int RetriesPerformed { get; set; }
        
        /// <summary>
        /// Number of fallbacks executed.
        /// </summary>
        public int FallbacksExecuted { get; set; }
        
        /// <summary>
        /// Timestamp when execution started.
        /// </summary>
        public DateTime StartTime { get; set; }
        
        /// <summary>
        /// Timestamp when execution completed.
        /// </summary>
        public DateTime EndTime { get; set; }
        
        /// <summary>
        /// Success rate as a percentage.
        /// </summary>
        public double SuccessRate => TotalCommandsExecuted > 0 ? (double)TotalCommandsSucceeded / TotalCommandsExecuted * 100 : 0;
        
        /// <summary>
        /// Failure rate as a percentage.
        /// </summary>
        public double FailureRate => TotalCommandsExecuted > 0 ? (double)TotalCommandsFailed / TotalCommandsExecuted * 100 : 0;
        
        /// <summary>
        /// Average execution time per command in milliseconds.
        /// </summary>
        public double AverageCommandExecutionTimeMs => TotalCommandsExecuted > 0 ? (double)TotalExecutionTimeMs / TotalCommandsExecuted : 0;
        
        /// <summary>
        /// Average execution time per behavior in milliseconds.
        /// </summary>
        public double AverageBehaviorExecutionTimeMs => BehaviorsExecuted > 0 ? (double)TotalExecutionTimeMs / BehaviorsExecuted : 0;
    }
} 