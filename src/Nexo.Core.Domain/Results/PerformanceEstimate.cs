using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Results
{
    /// <summary>
    /// Performance estimate for operations
    /// </summary>
    public class PerformanceEstimate
    {
        /// <summary>
        /// Estimated execution time
        /// </summary>
        public TimeSpan EstimatedDuration { get; set; }
        
        /// <summary>
        /// Estimated memory usage in bytes
        /// </summary>
        public long EstimatedMemoryUsage { get; set; }
        
        /// <summary>
        /// Estimated CPU usage percentage
        /// </summary>
        public double EstimatedCpuUsage { get; set; }
        
        /// <summary>
        /// Estimated disk I/O operations
        /// </summary>
        public long EstimatedDiskIO { get; set; }
        
        /// <summary>
        /// Estimated network operations
        /// </summary>
        public long EstimatedNetworkIO { get; set; }
        
        /// <summary>
        /// Confidence level (0-1)
        /// </summary>
        public double ConfidenceLevel { get; set; }
        
        /// <summary>
        /// Performance factors considered
        /// </summary>
        public List<string> Factors { get; set; } = new();
        
        /// <summary>
        /// Performance warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new();
        
        /// <summary>
        /// Additional data
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new();
        
        /// <summary>
        /// Timestamp when estimate was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Adds factor
        /// </summary>
        public void AddFactor(string factor)
        {
            Factors.Add(factor);
        }
        
        /// <summary>
        /// Adds warning
        /// </summary>
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }
        
        /// <summary>
        /// Adds data
        /// </summary>
        public void AddData(string key, object value)
        {
            Data[key] = value;
        }
    }
}
