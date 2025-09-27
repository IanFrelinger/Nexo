using Nexo.Core.Application.Services.Adaptation;
using Nexo.Feature.Monitoring.Services;

namespace Nexo.CLI.Commands.Adaptation.Utilities
{
    /// <summary>
    /// Handles icon display for various adaptation types and statuses
    /// </summary>
    public partial class IconHelper
    {
        public string GetEventIcon(AdaptationEventType eventType)
        {
            return eventType switch
            {
                AdaptationEventType.AdaptationApplied => "Processing",
                AdaptationEventType.PerformanceDegradation => "📉",
                AdaptationEventType.ResourceConstraint => "WARNING:",
                AdaptationEventType.UserFeedback => "💬",
                AdaptationEventType.EnvironmentChange => "🌍",
                AdaptationEventType.LearningInsight => "🧠",
                AdaptationEventType.ErrorOccurred => "ERROR:",
                _ => "Stats"
            };
        }
        
        public string GetConfidenceIcon(double confidence)
        {
            return confidence switch
            {
                >= 0.8 => "🟢",
                >= 0.6 => "🟡",
                >= 0.4 => "🟠",
                _ => "🔴"
            };
        }
        
        public string GetEffectivenessIcon(double effectiveness)
        {
            return effectiveness switch
            {
                >= 1.0 => "🟢",
                >= 0.8 => "🟡",
                >= 0.6 => "🟠",
                _ => "🔴"
            };
        }
        
        public string GetStatusIcon(AdaptationEngineStatus status)
        {
            return status switch
            {
                AdaptationEngineStatus.Running => "🟢",
                AdaptationEngineStatus.Starting => "🟡",
                AdaptationEngineStatus.Stopping => "🟠",
                AdaptationEngineStatus.Stopped => "⚪",
                AdaptationEngineStatus.Error => "🔴",
                _ => "UNKNOWN"
            };
        }
        
        public string GetPerformanceIcon(PerformanceSeverity severity)
        {
            return severity switch
            {
                PerformanceSeverity.Critical => "🔴",
                PerformanceSeverity.High => "🟠",
                PerformanceSeverity.Medium => "🟡",
                PerformanceSeverity.Low => "🟢",
                PerformanceSeverity.None => "⚪",
                _ => "UNKNOWN"
            };
        }
        
        public string GetAdaptationIcon(string adaptationType)
        {
            return adaptationType switch
            {
                "PerformanceOptimization" => "⚡",
                "ResourceOptimization" => "💾",
                "UserExperienceOptimization" => "👤",
                "EnvironmentOptimization" => "🌍",
                "SecurityOptimization" => "Security",
                "ReliabilityOptimization" => "Safety",
                _ => "Processing"
            };
        }
        
        public string GetTypeIcon(string type)
        {
            return type switch
            {
                "PerformanceOptimization" => "⚡",
                "ResourceOptimization" => "💾",
                "UserExperienceOptimization" => "👤",
                "EnvironmentOptimization" => "🌍",
                "SecurityOptimization" => "Security",
                "ReliabilityOptimization" => "Safety",
                _ => "Stats"
            };
        }
    }
}
