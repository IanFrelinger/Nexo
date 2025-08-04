using System;
using System.Collections.Generic;

namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Metrics for pipeline execution.
    /// </summary>
    public class PipelineExecutionMetrics
    {
        /// <summary>
        /// Total execution time in milliseconds.
        /// </summary>
        public long TotalExecutionTimeMs { get; set; }
        
        /// <summary>
        /// Number of commands executed.
        /// </summary>
        public int CommandsExecuted { get; set; }
        
        /// <summary>
        /// Number of commands that succeeded.
        /// </summary>
        public int CommandsSucceeded { get; set; }
        
        /// <summary>
        /// Number of commands that failed.
        /// </summary>
        public int CommandsFailed { get; set; }
        
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
        /// Number of aggregators executed.
        /// </summary>
        public int AggregatorsExecuted { get; set; }
        
        /// <summary>
        /// Number of aggregators that succeeded.
        /// </summary>
        public int AggregatorsSucceeded { get; set; }
        
        /// <summary>
        /// Number of aggregators that failed.
        /// </summary>
        public int AggregatorsFailed { get; set; }
        
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
        public double SuccessRate => CommandsExecuted > 0 ? (double)CommandsSucceeded / CommandsExecuted * 100 : 0;
        
        /// <summary>
        /// Failure rate as a percentage.
        /// </summary>
        public double FailureRate => CommandsExecuted > 0 ? (double)CommandsFailed / CommandsExecuted * 100 : 0;
        
        /// <summary>
        /// Average execution time per command in milliseconds.
        /// </summary>
        public double AverageCommandExecutionTimeMs => CommandsExecuted > 0 ? (double)TotalExecutionTimeMs / CommandsExecuted : 0;
    }
} 