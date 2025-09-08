using System;
using System.Collections.Generic;

namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Represents the result of a command execution.
    /// </summary>
    public class CommandResult
{
    /// <summary>
    /// Whether the command execution was successful.
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// Error message if the command failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Exception that occurred during execution, if any.
    /// </summary>
    public Exception? Exception { get; set; }
    
    /// <summary>
    /// Data returned by the command execution.
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
    /// Creates a successful command result.
    /// </summary>
    /// <param name="data">The data returned by the command.</param>
    /// <param name="executionTimeMs">Execution time in milliseconds.</param>
    /// <param name="startTime">Start time.</param>
    /// <param name="endTime">End time.</param>
    /// <returns>A successful command result.</returns>
    public static CommandResult Success(object? data = null, long executionTimeMs = 0, DateTime startTime = default(DateTime), DateTime endTime = default(DateTime))
    {
        return new CommandResult
        {
            IsSuccess = true,
            Data = data,
            ExecutionTimeMs = executionTimeMs,
            StartTime = startTime == default(DateTime) ? DateTime.UtcNow : startTime,
            EndTime = endTime == default(DateTime) ? DateTime.UtcNow : endTime
        };
    }
    
    /// <summary>
    /// Creates a failed command result.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="executionTimeMs">Execution time in milliseconds.</param>
    /// <param name="startTime">Start time.</param>
    /// <param name="endTime">End time.</param>
    /// <returns>A failed command result.</returns>
    public static CommandResult Failure(string errorMessage, Exception? exception = null, long executionTimeMs = 0, DateTime startTime = default(DateTime), DateTime endTime = default(DateTime))
    {
        return new CommandResult
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
    /// Adds a warning to the result.
    /// </summary>
    /// <param name="warning">The warning message.</param>
    /// <returns>This command result for chaining.</returns>
    public CommandResult AddWarning(string warning)
    {
        Warnings.Add(warning);
        return this;
    }
    
    /// <summary>
    /// Adds an information message to the result.
    /// </summary>
    /// <param name="information">The information message.</param>
    /// <returns>This command result for chaining.</returns>
    public CommandResult AddInformation(string information)
    {
        Information.Add(information);
        return this;
    }
    
    /// <summary>
    /// Adds metadata to the result.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>This command result for chaining.</returns>
    public CommandResult AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        return this;
    }
}
}