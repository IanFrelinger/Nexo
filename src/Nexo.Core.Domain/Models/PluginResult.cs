using System;

namespace Nexo.Core.Domain.Models
{
    /// <summary>
    /// Result of plugin execution
    /// </summary>
    public partial class PluginResult
    {
        /// <summary>
        /// Whether the execution was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Output message from the plugin
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Error message if execution failed
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Exit code for the execution
        /// </summary>
        public int ExitCode { get; set; }
        
        /// <summary>
        /// Additional data returned by the plugin
        /// </summary>
        public object? Data { get; set; }
        
        /// <summary>
        /// When the execution completed
        /// </summary>
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Duration of the execution
        /// </summary>
        public TimeSpan Duration { get; set; }
    }
}
