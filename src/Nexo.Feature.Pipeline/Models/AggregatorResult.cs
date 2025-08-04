using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Represents the result of an aggregator execution.
    /// </summary>
    public class AggregatorResult
{
    /// <summary>
    /// Whether the aggregator execution was successful.
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// Error message if the aggregator failed.
    /// </summary>
    public string ErrorMessage { get; set; }
    
    /// <summary>
    /// Exception that occurred during execution, if any.
    /// </summary>
    public Exception Exception { get; set; }
    
    /// <summary>
    /// Results of individual behavior executions within the aggregator.
    /// </summary>
    public List<BehaviorResult> BehaviorResults { get; set; } = new List<BehaviorResult>();
    
    /// <summary>
    /// Results of direct command executions within the aggregator.
    /// </summary>
    public List<CommandResult> DirectCommandResults { get; set; } = new List<CommandResult>();
    
    /// <summary>
    /// Data returned by the aggregator execution.
    /// </summary>
    public object Data { get; set; }
    
    /// <summary>
    /// Execution time in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; set; }
    
    /// <summary>
    /// Timestamp when execution started.
    /// </summary>
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// Timestamp when execution completed.
    /// </summary>
    public DateTime EndTime { get; set; }
    
    /// <summary>
    /// Additional metadata about the execution.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    
    /// <summary>
    /// Warnings generated during execution.
    /// </summary>
    public List<string> Warnings { get; set; } = new List<string>();
    
    /// <summary>
    /// Information messages generated during execution.
    /// </summary>
    public List<string> Information { get; set; } = new List<string>();
    
    /// <summary>
    /// Number of behaviors that executed successfully.
    /// </summary>
    public int SuccessfulBehaviors => BehaviorResults.Count(r => r.IsSuccess);
    
    /// <summary>
    /// Number of behaviors that failed.
    /// </summary>
    public int FailedBehaviors => BehaviorResults.Count(r => !r.IsSuccess);
    
    /// <summary>
    /// Total number of behaviors executed.
    /// </summary>
    public int TotalBehaviors => BehaviorResults.Count;
    
    /// <summary>
    /// Number of direct commands that executed successfully.
    /// </summary>
    public int SuccessfulDirectCommands => DirectCommandResults.Count(r => r.IsSuccess);
    
    /// <summary>
    /// Number of direct commands that failed.
    /// </summary>
    public int FailedDirectCommands => DirectCommandResults.Count(r => !r.IsSuccess);
    
    /// <summary>
    /// Total number of direct commands executed.
    /// </summary>
    public int TotalDirectCommands => DirectCommandResults.Count;
    
    /// <summary>
    /// Total number of commands executed across all behaviors and direct commands.
    /// </summary>
    public int TotalCommands => BehaviorResults.Sum(r => r.TotalCommands) + TotalDirectCommands;
    
    /// <summary>
    /// Total number of successful commands across all behaviors and direct commands.
    /// </summary>
    public int TotalSuccessfulCommands => BehaviorResults.Sum(r => r.SuccessfulCommands) + SuccessfulDirectCommands;
    
    /// <summary>
    /// Creates a successful aggregator result.
    /// </summary>
    /// <param name="data">The data returned by the aggregator.</param>
    /// <param name="executionTimeMs">Execution time in milliseconds.</param>
    /// <param name="startTime">Start time.</param>
    /// <param name="endTime">End time.</param>
    /// <returns>A successful aggregator result.</returns>
    public static AggregatorResult Success(object data = null, long executionTimeMs = 0, DateTime startTime = default(DateTime), DateTime endTime = default(DateTime))
    {
        return new AggregatorResult
        {
            IsSuccess = true,
            Data = data,
            ExecutionTimeMs = executionTimeMs,
            StartTime = startTime == default(DateTime) ? DateTime.UtcNow : startTime,
            EndTime = endTime == default(DateTime) ? DateTime.UtcNow : endTime
        };
    }
    
    /// <summary>
    /// Creates a failed aggregator result.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="executionTimeMs">Execution time in milliseconds.</param>
    /// <param name="startTime">Start time.</param>
    /// <param name="endTime">End time.</param>
    /// <returns>A failed aggregator result.</returns>
    public static AggregatorResult Failure(string errorMessage, Exception exception = null, long executionTimeMs = 0, DateTime startTime = default(DateTime), DateTime endTime = default(DateTime))
    {
        return new AggregatorResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Exception = exception,
            ExecutionTimeMs = executionTimeMs,
            StartTime = startTime == default(DateTime) ? DateTime.UtcNow : startTime,
            EndTime = endTime == default(DateTime) ? DateTime.UtcNow : endTime
        };
    }
    
    /// <summary>
    /// Adds a behavior result to the aggregator result.
    /// </summary>
    /// <param name="behaviorResult">The behavior result to add.</param>
    /// <returns>This aggregator result for chaining.</returns>
    public AggregatorResult AddBehaviorResult(BehaviorResult behaviorResult)
    {
        BehaviorResults.Add(behaviorResult);
        return this;
    }
    
    /// <summary>
    /// Adds multiple behavior results to the aggregator result.
    /// </summary>
    /// <param name="behaviorResults">The behavior results to add.</param>
    /// <returns>This aggregator result for chaining.</returns>
    public AggregatorResult AddBehaviorResults(IEnumerable<BehaviorResult> behaviorResults)
    {
        BehaviorResults.AddRange(behaviorResults);
        return this;
    }
    
    /// <summary>
    /// Adds a direct command result to the aggregator result.
    /// </summary>
    /// <param name="commandResult">The command result to add.</param>
    /// <returns>This aggregator result for chaining.</returns>
    public AggregatorResult AddDirectCommandResult(CommandResult commandResult)
    {
        DirectCommandResults.Add(commandResult);
        return this;
    }
    
    /// <summary>
    /// Adds multiple direct command results to the aggregator result.
    /// </summary>
    /// <param name="commandResults">The command results to add.</param>
    /// <returns>This aggregator result for chaining.</returns>
    public AggregatorResult AddDirectCommandResults(IEnumerable<CommandResult> commandResults)
    {
        DirectCommandResults.AddRange(commandResults);
        return this;
    }
    
    /// <summary>
    /// Adds a warning to the result.
    /// </summary>
    /// <param name="warning">The warning message.</param>
    /// <returns>This aggregator result for chaining.</returns>
    public AggregatorResult AddWarning(string warning)
    {
        Warnings.Add(warning);
        return this;
    }
    
    /// <summary>
    /// Adds an information message to the result.
    /// </summary>
    /// <param name="information">The information message.</param>
    /// <returns>This aggregator result for chaining.</returns>
    public AggregatorResult AddInformation(string information)
    {
        Information.Add(information);
        return this;
    }
    
    /// <summary>
    /// Adds metadata to the result.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>This aggregator result for chaining.</returns>
    public AggregatorResult AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        return this;
    }
}
}