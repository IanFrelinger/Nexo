using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Agent.Contracts;
using Nexo.Agent.Tools.Visual.Contracts;

namespace Nexo.Agent.Tools.Visual.Implementations;

/// <summary>
/// Unity-specific visual analyzer for game screenshots and UI analysis.
/// </summary>
public sealed class UnityVisualAnalyzer : IVisualAnalyzer
{
    private readonly ILogger<UnityVisualAnalyzer> _logger;
    private readonly IVisualAnalyzer _baseAnalyzer;

    public UnityVisualAnalyzer(ILogger<UnityVisualAnalyzer> logger, IVisualAnalyzer baseAnalyzer)
    {
        _logger = logger;
        _baseAnalyzer = baseAnalyzer;
    }

    public string Id => "tool.unity.visual.analyze";
    public string Name => "Unity Visual Analyzer";
    public string Description => "Analyzes Unity game screenshots for gameplay, UI, and performance insights";
    public string Version => "1.0.0";
    public ToolPermissions Permissions => ToolPermissions.FileRead;

    public async Task<VisualAnalysisResult> AnalyzeImageAsync(VisualAnalysisRequest request, ToolContext context)
    {
        try
        {
            _logger.LogInformation("Starting Unity visual analysis of {ImagePath}", request.ImagePath);

            // Enhance the request with Unity-specific prompts
            var enhancedRequest = EnhanceRequestForUnity(request);
            
            // Call the base analyzer
            var result = await _baseAnalyzer.AnalyzeImageAsync(enhancedRequest, context);
            
            // Post-process with Unity-specific insights
            var enhancedResult = PostProcessForUnity(result, request.AnalysisType);
            
            _logger.LogInformation("Unity visual analysis completed successfully");
            return enhancedResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unity visual analysis failed for {ImagePath}", request.ImagePath);
            return new VisualAnalysisResult
            {
                Success = false,
                AnalysisType = request.AnalysisType,
                Summary = "Unity analysis failed",
                Insights = new List<VisualInsight>(),
                Metrics = new Dictionary<string, object>(),
                Error = ex.Message
            };
        }
    }

    public async Task<VisualComparisonResult> CompareImagesAsync(VisualComparisonRequest request, ToolContext context)
    {
        try
        {
            _logger.LogInformation("Starting Unity image comparison between {Image1} and {Image2}", 
                request.ImagePath1, request.ImagePath2);

            // Enhance the request with Unity-specific comparison logic
            var enhancedRequest = EnhanceComparisonRequestForUnity(request);
            
            // Call the base analyzer
            var result = await _baseAnalyzer.CompareImagesAsync(enhancedRequest, context);
            
            // Post-process with Unity-specific insights
            var enhancedResult = PostProcessComparisonForUnity(result, request.ComparisonType);
            
            _logger.LogInformation("Unity image comparison completed successfully");
            return enhancedResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unity image comparison failed between {Image1} and {Image2}", 
                request.ImagePath1, request.ImagePath2);
            return new VisualComparisonResult
            {
                Success = false,
                SimilarityScore = 0.0,
                Differences = new List<VisualDifference>(),
                Summary = "Unity comparison failed",
                Error = ex.Message
            };
        }
    }

    public async Task<OcrResult> ExtractTextAsync(OcrRequest request, ToolContext context)
    {
        // For Unity, we might want to focus on UI text extraction
        return await _baseAnalyzer.ExtractTextAsync(request, context);
    }

    private VisualAnalysisRequest EnhanceRequestForUnity(VisualAnalysisRequest request)
    {
        var enhancedPrompt = request.AnalysisType switch
        {
            "ui" => @"Analyze this Unity game UI screenshot and provide insights about:
1. HUD elements (health bar, ammo counter, crosshair, minimap)
2. UI element visibility and contrast ratios
3. Layout and spacing issues
4. Accessibility concerns (color contrast, text size)
5. Visual hierarchy and information architecture
6. Unity UI Toolkit specific elements
7. Mobile vs desktop UI considerations

Respond in JSON format with:
- summary: brief overview
- insights: array of {type, category, description, severity, boundingBox}
- metrics: object with numerical measurements",
            
            "gameplay" => @"Analyze this Unity gameplay screenshot and provide insights about:
1. Game state and progression indicators
2. Player health, ammo, and inventory status
3. Enemy positions and behaviors
4. Visual effects and particle systems
5. Lighting and atmosphere
6. Performance indicators (frame rate, quality settings)
7. Gameplay mechanics visibility
8. Camera positioning and field of view

Respond in JSON format with:
- summary: brief overview
- insights: array of {type, category, description, severity, boundingBox}
- metrics: object with numerical measurements",
            
            "performance" => @"Analyze this Unity screenshot for performance indicators:
1. Frame rate and performance metrics
2. Visual quality settings (textures, shadows, lighting)
3. Rendering artifacts or glitches
4. UI responsiveness and smoothness
5. Memory usage indicators
6. GPU/CPU intensive elements
7. Optimization opportunities
8. Platform-specific performance considerations

Respond in JSON format with:
- summary: brief overview
- insights: array of {type, category, description, severity, boundingBox}
- metrics: object with numerical measurements",
            
            "accessibility" => @"Analyze this Unity game screenshot for accessibility:
1. Color contrast ratios (WCAG compliance)
2. Text readability and font sizes
3. UI element sizes and touch targets
4. Navigation clarity and flow
5. Visual indicators and feedback
6. Colorblind-friendly design
7. Motion and animation considerations
8. Audio-visual synchronization

Respond in JSON format with:
- summary: brief overview
- insights: array of {type, category, description, severity, boundingBox}
- metrics: object with numerical measurements",
            
            _ => request.Prompt ?? @"Analyze this Unity game screenshot and provide general insights about:
1. Visual composition and art direction
2. Key game elements and objects
3. Color palette and lighting
4. UI/UX design quality
5. Performance and optimization opportunities
6. Accessibility considerations

Respond in JSON format with:
- summary: brief overview
- insights: array of {type, category, description, severity, boundingBox}
- metrics: object with numerical measurements"
        };

        return request with { Prompt = enhancedPrompt };
    }

    private VisualComparisonRequest EnhanceComparisonRequestForUnity(VisualComparisonRequest request)
    {
        var enhancedType = request.ComparisonType switch
        {
            "ui" => "Unity UI comparison",
            "gameplay" => "Unity gameplay comparison",
            "performance" => "Unity performance comparison",
            _ => request.ComparisonType
        };

        return request with { ComparisonType = enhancedType };
    }

    private VisualAnalysisResult PostProcessForUnity(VisualAnalysisResult result, string analysisType)
    {
        if (!result.Success)
            return result;

        // Add Unity-specific insights
        var unityInsights = new List<VisualInsight>();
        
        // Add Unity-specific metrics
        var unityMetrics = new Dictionary<string, object>(result.Metrics);
        
        // Add Unity-specific processing based on analysis type
        switch (analysisType)
        {
            case "ui":
                unityInsights.AddRange(ProcessUIInsights(result.Insights));
                unityMetrics["unity_ui_elements"] = CountUIElements(result.Insights);
                break;
                
            case "gameplay":
                unityInsights.AddRange(ProcessGameplayInsights(result.Insights));
                unityMetrics["unity_gameplay_score"] = CalculateGameplayScore(result.Insights);
                break;
                
            case "performance":
                unityInsights.AddRange(ProcessPerformanceInsights(result.Insights));
                unityMetrics["unity_performance_score"] = CalculatePerformanceScore(result.Insights);
                break;
                
            case "accessibility":
                unityInsights.AddRange(ProcessAccessibilityInsights(result.Insights));
                unityMetrics["unity_accessibility_score"] = CalculateAccessibilityScore(result.Insights);
                break;
        }

        return result with
        {
            Insights = result.Insights.Concat(unityInsights).ToList(),
            Metrics = unityMetrics,
            Summary = $"[Unity] {result.Summary}"
        };
    }

    private VisualComparisonResult PostProcessComparisonForUnity(VisualComparisonResult result, string comparisonType)
    {
        if (!result.Success)
            return result;

        // Add Unity-specific comparison insights
        var unityDifferences = new List<VisualDifference>();
        
        // Process differences for Unity-specific elements
        foreach (var difference in result.Differences)
        {
            var unityDifference = difference with
            {
                Description = $"[Unity] {difference.Description}",
                Metadata = new Dictionary<string, object>(difference.Metadata ?? new Dictionary<string, object>())
                {
                    ["unity_context"] = comparisonType,
                    ["analysis_timestamp"] = DateTime.UtcNow
                }
            };
            unityDifferences.Add(unityDifference);
        }

        return result with
        {
            Differences = unityDifferences,
            Summary = $"[Unity] {result.Summary}"
        };
    }

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

    private int CountUIElements(List<VisualInsight> insights)
    {
        return insights.Count(i => i.Category == "ui");
    }

    private double CalculateGameplayScore(List<VisualInsight> insights)
    {
        var positiveInsights = insights.Count(i => i.Type == "observation" && i.Severity == "low");
        var negativeInsights = insights.Count(i => i.Type == "issue" && i.Severity == "high");
        
        return Math.Max(0, Math.Min(1, (positiveInsights - negativeInsights) / 10.0));
    }

    private double CalculatePerformanceScore(List<VisualInsight> insights)
    {
        var performanceIssues = insights.Count(i => i.Category == "performance" && i.Severity == "high");
        return Math.Max(0, 1 - (performanceIssues * 0.2));
    }

    private double CalculateAccessibilityScore(List<VisualInsight> insights)
    {
        var accessibilityIssues = insights.Count(i => i.Category == "accessibility" && i.Severity == "high");
        return Math.Max(0, 1 - (accessibilityIssues * 0.3));
    }
}
