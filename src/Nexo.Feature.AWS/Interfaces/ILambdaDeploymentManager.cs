using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.AWS.Interfaces
{
    /// <summary>
    /// Lambda function deployment and management interface.
    /// This interface acts as an orchestrator, delegating specific functionalities to partial interface implementations.
    /// </summary>
    public partial interface ILambdaDeploymentManager
    {
    }

    /// <summary>
    /// Lambda deployment result
    /// </summary>
    public partial class LambdaDeploymentResult
    {
        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Operation message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Function name
        /// </summary>
        public string FunctionName { get; set; } = string.Empty;

        /// <summary>
        /// Function ARN
        /// </summary>
        public string? FunctionArn { get; set; }

        /// <summary>
        /// Function version
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Operation timestamp
        /// </summary>
        public DateTime OperatedAt { get; set; }

        /// <summary>
        /// Error details if operation failed
        /// </summary>
        public string? ErrorDetails { get; set; }
    }

    /// <summary>
    /// Lambda invocation result
    /// </summary>
    public partial class LambdaInvocationResult
    {
        /// <summary>
        /// Whether the invocation was successful
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Invocation message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Function response payload
        /// </summary>
        public string? ResponsePayload { get; set; }

        /// <summary>
        /// Function error message
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Function error type
        /// </summary>
        public string? ErrorType { get; set; }

        /// <summary>
        /// Function stack trace
        /// </summary>
        public string? StackTrace { get; set; }

        /// <summary>
        /// Invocation duration in milliseconds
        /// </summary>
        public long DurationMs { get; set; }

        /// <summary>
        /// Memory used in MB
        /// </summary>
        public int MemoryUsedMB { get; set; }

        /// <summary>
        /// Billed duration in milliseconds
        /// </summary>
        public long BilledDurationMs { get; set; }

        /// <summary>
        /// Invocation timestamp
        /// </summary>
        public DateTime InvokedAt { get; set; }
    }

    /// <summary>
    /// Lambda function information
    /// </summary>
    public partial class LambdaFunctionInfo
    {
        /// <summary>
        /// Function name
        /// </summary>
        public string FunctionName { get; set; } = string.Empty;

        /// <summary>
        /// Function ARN
        /// </summary>
        public string FunctionArn { get; set; } = string.Empty;

        /// <summary>
        /// Function runtime
        /// </summary>
        public string Runtime { get; set; } = string.Empty;

        /// <summary>
        /// Function handler
        /// </summary>
        public string Handler { get; set; } = string.Empty;

        /// <summary>
        /// Function description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Function timeout in seconds
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Function memory size in MB
        /// </summary>
        public int MemorySize { get; set; }

        /// <summary>
        /// Function code size in bytes
        /// </summary>
        public long CodeSize { get; set; }

        /// <summary>
        /// Function version
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Function role ARN
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Function last modified date
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Function state
        /// </summary>
        public string State { get; set; } = string.Empty;

        /// <summary>
        /// Function state reason
        /// </summary>
        public string? StateReason { get; set; }

        /// <summary>
        /// Function state reason code
        /// </summary>
        public string? StateReasonCode { get; set; }

        /// <summary>
        /// Environment variables
        /// </summary>
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Lambda list result
    /// </summary>
    public partial class LambdaListResult
    {
        /// <summary>
        /// Whether the list operation was successful
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// List message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// List of functions
        /// </summary>
        public List<LambdaFunctionInfo> Functions { get; set; } = new List<LambdaFunctionInfo>();

        /// <summary>
        /// List timestamp
        /// </summary>
        public DateTime ListedAt { get; set; }

        /// <summary>
        /// Error details if list failed
        /// </summary>
        public string? ErrorDetails { get; set; }
    }

    /// <summary>
    /// Lambda logs result
    /// </summary>
    public partial class LambdaLogsResult
    {
        /// <summary>
        /// Whether the logs retrieval was successful
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Logs message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Function name
        /// </summary>
        public string FunctionName { get; set; } = string.Empty;

        /// <summary>
        /// Log entries
        /// </summary>
        public List<LambdaLogEntry> LogEntries { get; set; } = new List<LambdaLogEntry>();

        /// <summary>
        /// Log retrieval timestamp
        /// </summary>
        public DateTime RetrievedAt { get; set; }

        /// <summary>
        /// Error details if retrieval failed
        /// </summary>
        public string? ErrorDetails { get; set; }
    }

    /// <summary>
    /// Lambda log entry
    /// </summary>
    public partial class LambdaLogEntry
    {
        /// <summary>
        /// Log timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Log message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Log level
        /// </summary>
        public string Level { get; set; } = string.Empty;

        /// <summary>
        /// Request ID
        /// </summary>
        public string? RequestId { get; set; }

        /// <summary>
        /// Duration in milliseconds
        /// </summary>
        public long? DurationMs { get; set; }

        /// <summary>
        /// Memory used in MB
        /// </summary>
        public int? MemoryUsedMB { get; set; }

        /// <summary>
        /// Billed duration in milliseconds
        /// </summary>
        public long? BilledDurationMs { get; set; }
    }

    /// <summary>
    /// Lambda package result
    /// </summary>
    public partial class LambdaPackageResult
    {
        /// <summary>
        /// Whether the package creation was successful
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Package creation message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Output package path
        /// </summary>
        public string PackagePath { get; set; } = string.Empty;

        /// <summary>
        /// Package size in bytes
        /// </summary>
        public long PackageSizeBytes { get; set; }

        /// <summary>
        /// Package creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Error details if creation failed
        /// </summary>
        public string? ErrorDetails { get; set; }
    }
} 