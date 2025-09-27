using System;
using System.Collections.Generic;
using Nexo.Core.Domain.Enums.Safety;

namespace Nexo.Core.Application.Services.AI.Pipeline
{
    /// <summary>
    /// AI requirements for code review operations
    /// </summary>
    public partial class AIRequirements
    {
        public int QualityThreshold { get; set; } = 80;
        public SafetyLevel SafetyLevel { get; set; } = SafetyLevel.Medium;
        public PerformanceTarget PerformanceTarget { get; set; } = PerformanceTarget.Balanced;
        public bool RequireOffline { get; set; } = false;
        public Dictionary<string, object> CustomRequirements { get; set; } = new();
    }

    /// <summary>
    /// Safety levels for AI operations
    /// </summary>
    public enum SafetyLevel
    {
        Low,
        Medium,
        High,
        Maximum
    }

    /// <summary>
    /// Performance targets for AI operations
    /// </summary>
    public enum PerformanceTarget
    {
        Speed,
        Quality,
        Balanced,
        Maximum
    }
}
