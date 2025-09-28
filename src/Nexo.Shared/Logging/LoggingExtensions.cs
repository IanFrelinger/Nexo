using System;
using System.Collections.Generic;

namespace Nexo.Shared.Logging
{
    /// <summary>
    /// Extension methods for logging
    /// </summary>
    public static class LoggingExtensions
    {
        /// <summary>
        /// Logs an operation start
        /// </summary>
        public static void LogOperationStart(string operationName, Dictionary<string, object>? properties = null, string? correlationId = null)
        {
            var startProperties = properties ?? new Dictionary<string, object>();
            startProperties["Operation"] = operationName;
            startProperties["Status"] = "Started";
            
            LoggingManager.LogInformation($"Operation started: {operationName}", startProperties, correlationId);
        }

        /// <summary>
        /// Logs an operation completion
        /// </summary>
        public static void LogOperationComplete(string operationName, TimeSpan duration, Dictionary<string, object>? properties = null, string? correlationId = null)
        {
            var completeProperties = properties ?? new Dictionary<string, object>();
            completeProperties["Operation"] = operationName;
            completeProperties["Status"] = "Completed";
            completeProperties["Duration"] = duration.TotalMilliseconds;
            
            LoggingManager.LogInformation($"Operation completed: {operationName}", completeProperties, correlationId);
        }

        /// <summary>
        /// Logs an operation failure
        /// </summary>
        public static void LogOperationFailure(string operationName, Exception exception, TimeSpan duration, Dictionary<string, object>? properties = null, string? correlationId = null)
        {
            var failureProperties = properties ?? new Dictionary<string, object>();
            failureProperties["Operation"] = operationName;
            failureProperties["Status"] = "Failed";
            failureProperties["Duration"] = duration.TotalMilliseconds;
            
            LoggingManager.LogError($"Operation failed: {operationName}", exception, failureProperties, correlationId);
        }
    }
}
