using Nexo.Agent.Tools.Visual.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Agent.Tools.Visual.Implementations;

/// <summary>
/// Insight processing functionality for Unity visual analyzer.
/// </summary>
public sealed partial class UnityVisualAnalyzer
{
    private List<VisualInsight> ProcessUIInsights(List<VisualInsight> insights)
    {
        var unityInsights = new List<VisualInsight>();
        
        // Check for Unity-specific UI patterns
        var hasHealthBar = insights.Any(i => i.Description.Contains("health", StringComparison.OrdinalIgnoreCase));
        var hasAmmoCounter = insights.Any(i => i.Description.Contains("ammo", StringComparison.OrdinalIgnoreCase));
        var hasCrosshair = insights.Any(i => i.Description.Contains("crosshair", StringComparison.OrdinalIgnoreCase));
        
        if (!hasHealthBar)
        {
            unityInsights.Add(new VisualInsight
            {
                Type = "recommendation",
                Category = "ui",
                Description = "Consider adding a health bar for better player feedback",
                Severity = "medium"
            });
        }
        
        if (!hasAmmoCounter)
        {
            unityInsights.Add(new VisualInsight
            {
                Type = "recommendation",
                Category = "ui",
                Description = "Consider adding an ammo counter for weapon management",
                Severity = "medium"
            });
        }
        
        if (!hasCrosshair)
        {
            unityInsights.Add(new VisualInsight
            {
                Type = "recommendation",
                Category = "ui",
                Description = "Consider adding a crosshair for better aiming",
                Severity = "low"
            });
        }
        
        return unityInsights;
    }

    private List<VisualInsight> ProcessGameplayInsights(List<VisualInsight> insights)
    {
        var unityInsights = new List<VisualInsight>();
        
        // Check for gameplay-specific elements
        var hasEnemies = insights.Any(i => i.Description.Contains("enemy", StringComparison.OrdinalIgnoreCase));
        var hasWeapons = insights.Any(i => i.Description.Contains("weapon", StringComparison.OrdinalIgnoreCase));
        var hasObjectives = insights.Any(i => i.Description.Contains("objective", StringComparison.OrdinalIgnoreCase));
        
        if (!hasEnemies)
        {
            unityInsights.Add(new VisualInsight
            {
                Type = "observation",
                Category = "gameplay",
                Description = "No enemies visible in current frame",
                Severity = "low"
            });
        }
        
        if (!hasWeapons)
        {
            unityInsights.Add(new VisualInsight
            {
                Type = "recommendation",
                Category = "gameplay",
                Description = "Consider adding weapon indicators for combat feedback",
                Severity = "medium"
            });
        }
        
        return unityInsights;
    }

    private List<VisualInsight> ProcessPerformanceInsights(List<VisualInsight> insights)
    {
        var unityInsights = new List<VisualInsight>();
        
        // Check for performance-related issues
        var hasArtifacts = insights.Any(i => i.Description.Contains("artifact", StringComparison.OrdinalIgnoreCase));
        var hasLowQuality = insights.Any(i => i.Description.Contains("low quality", StringComparison.OrdinalIgnoreCase));
        
        if (hasArtifacts)
        {
            unityInsights.Add(new VisualInsight
            {
                Type = "issue",
                Category = "performance",
                Description = "Rendering artifacts detected - consider optimizing shaders or reducing quality settings",
                Severity = "high"
            });
        }
        
        if (hasLowQuality)
        {
            unityInsights.Add(new VisualInsight
            {
                Type = "recommendation",
                Category = "performance",
                Description = "Consider adjusting quality settings for better visual fidelity",
                Severity = "medium"
            });
        }
        
        return unityInsights;
    }

    private List<VisualInsight> ProcessAccessibilityInsights(List<VisualInsight> insights)
    {
        var unityInsights = new List<VisualInsight>();
        
        // Check for accessibility issues
        var hasContrastIssues = insights.Any(i => i.Description.Contains("contrast", StringComparison.OrdinalIgnoreCase));
        var hasTextIssues = insights.Any(i => i.Description.Contains("text", StringComparison.OrdinalIgnoreCase));
        
        if (hasContrastIssues)
        {
            unityInsights.Add(new VisualInsight
            {
                Type = "issue",
                Category = "accessibility",
                Description = "Color contrast issues detected - ensure WCAG AA compliance",
                Severity = "high"
            });
        }
        
        if (hasTextIssues)
        {
            unityInsights.Add(new VisualInsight
            {
                Type = "recommendation",
                Category = "accessibility",
                Description = "Consider increasing text size or improving readability",
                Severity = "medium"
            });
        }
        
        return unityInsights;
    }
}
