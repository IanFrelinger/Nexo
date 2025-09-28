using System;
using System.Collections.Generic;

namespace Nexo.Core.Application.Commands.Project
{
    /// <summary>
    /// Output for calculating project metrics
    /// </summary>
    public class CalculateProjectMetricsOutput
    {
        public string ProjectId { get; set; } = string.Empty;
        public DateTime CalculatedAt { get; set; }
        public int TotalAgents { get; set; }
        public int ActiveAgents { get; set; }
        public int InactiveAgents { get; set; }
        public double HealthScore { get; set; }
        public double ProgressPercentage { get; set; }
        public double DurationDays { get; set; }
        public Dictionary<string, int> AgentDistribution { get; set; } = new Dictionary<string, int>();
        public int MetadataCount { get; set; }
    }
}
