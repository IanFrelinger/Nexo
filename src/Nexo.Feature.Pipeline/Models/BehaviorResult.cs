using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Represents the result of a behavior execution.
    /// </summary>
    public class BehaviorResult
{
    /// <summary>
    /// Whether the behavior execution was successful.
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// Error message if the behavior failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Exception that occurred during execution, if any.
    /// </summary>
    public Exception? Exception { get; set; }
    
    /// <summary>
    /// Results of individual command executions within the behavior.
    /// </summary>
    public List<CommandResult> CommandResults { get; set; } = new List<CommandResult>();
    
    /// <summary>
    /// Data returned by the behavior execution.
    /// </summary>
    public object? Data { get; set; }
    
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
    /// Number of commands that executed successfully.
    /// </summary>
    public int SuccessfulCommands => CommandResults.Count(r => r.IsSuccess);
    
    /// <summary>
    /// Number of commands that failed.
    /// </summary>
    public int FailedCommands => CommandResults.Count(r => !r.IsSuccess);
    
    /// <summary>
    /// Total number of commands executed.
    /// </summary>
    public int TotalCommands => CommandResults.Count;
    
    /// <summary>
    /// Creates a successful behavior result.
    /// </summary>
    /// <param name="data">The data returned by the behavior.</param>
    /// <param name="executionTimeMs">Execution time in milliseconds.</param>
    /// <param name="startTime">Start time.</param>
    /// <param name="endTime">End time.</param>
    /// <returns>A successful behavior result.</returns>
    public static BehaviorResult Success(object? data = null, long executionTimeMs = 0, DateTime startTime = default(DateTime), DateTime endTime = default(DateTime))
    {
        return new BehaviorResult
        {
            IsSuccess = true,
            Data = data,
            ExecutionTimeMs = executionTimeMs,
            StartTime = startTime == default(DateTime) ? DateTime.UtcNow : startTime,
            EndTime = endTime == default(DateTime) ? DateTime.UtcNow : endTime
        };
    }
    
    /// <summary>
    /// Creates a failed behavior result.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="executionTimeMs">Execution time in milliseconds.</param>
    /// <param name="startTime">Start time.</param>
    /// <param name="endTime">End time.</param>
    /// <returns>A failed behavior result.</returns>
    public static BehaviorResult Failure(string errorMessage, Exception? exception = null, long executionTimeMs = 0, DateTime startTime = default(DateTime), DateTime endTime = default(DateTime))
    {
        return new BehaviorResult
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
    /// Adds a command result to the behavior result.
    /// </summary>
    /// <param name="commandResult">The command result to add.</param>
    /// <returns>This behavior result for chaining.</returns>
    public BehaviorResult AddCommandResult(CommandResult commandResult)
    {
        CommandResults.Add(commandResult);
        return this;
    }
    
    /// <summary>
    /// Adds multiple command results to the behavior result.
    /// </summary>
    /// <param name="commandResults">The command results to add.</param>
    /// <returns>This behavior result for chaining.</returns>
    public BehaviorResult AddCommandResults(IEnumerable<CommandResult> commandResults)
    {
        CommandResults.AddRange(commandResults);
        return this;
    }
    
    /// <summary>
    /// Adds a warning to the result.
    /// </summary>
    /// <param name="warning">The warning message.</param>
    /// <returns>This behavior result for chaining.</returns>
    public BehaviorResult AddWarning(string warning)
    {
        Warnings.Add(warning);
        return this;
    }
    
    /// <summary>
    /// Adds an information message to the result.
    /// </summary>
    /// <param name="information">The information message.</param>
    /// <returns>This behavior result for chaining.</returns>
    public BehaviorResult AddInformation(string information)
    {
        Information.Add(information);
        return this;
    }
    
    /// <summary>
    /// Adds metadata to the result.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>This behavior result for chaining.</returns>
    public BehaviorResult AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        return this;
    }
}
}