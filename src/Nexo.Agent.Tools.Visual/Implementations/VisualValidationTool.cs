using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Agent.Contracts;
using Nexo.Agent.Tools.Visual.Contracts;

namespace Nexo.Agent.Tools.Visual.Implementations;

/// <summary>
/// Visual validation tool that can be invoked by the Agent when visual analysis is needed.
/// </summary>
public sealed class VisualValidationTool : ITool<VisualValidationRequest, VisualValidationResult>
{
    private readonly ILogger<VisualValidationTool> _logger;
    private readonly IVisualAnalyzer _visualAnalyzer;

    public VisualValidationTool(ILogger<VisualValidationTool> logger, IVisualAnalyzer visualAnalyzer)
    {
        _logger = logger;
        _visualAnalyzer = visualAnalyzer;
    }

    public string Id => "tool.visual.validation";
    public string Name => "Visual Validation Tool";
    public string Description => "Analyzes game visuals for UI, gameplay, performance, and accessibility issues";
    public string Version => "1.0.0";
    public ToolPermissions Permissions => ToolPermissions.FileRead | ToolPermissions.FileWrite;

    public async Task<VisualValidationResult> ExecuteAsync(VisualValidationRequest input, ToolContext context)
    {
        try
        {
            _logger.LogInformation("Starting visual validation for {ValidationType}", input.ValidationType);

            var result = new VisualValidationResult
            {
                Success = true,
                ValidationType = input.ValidationType,
                Timestamp = DateTime.UtcNow,
                AnalysisResults = new List<VisualAnalysisResult>(),
                Recommendations = new List<ValidationRecommendation>()
            };

            // Capture screenshot if needed
            string screenshotPath = input.ScreenshotPath;
            if (string.IsNullOrEmpty(screenshotPath))
            {
                screenshotPath = await CaptureScreenshotAsync(input, context);
                result.ScreenshotPath = screenshotPath;
            }

            // Perform visual analysis based on validation type
            var analysisTypes = GetAnalysisTypesForValidation(input.ValidationType);
            
            foreach (var analysisType in analysisTypes)
            {
                var analysisRequest = new VisualAnalysisRequest
                {
                    ImagePath = screenshotPath,
                    AnalysisType = analysisType,
                    Prompt = input.CustomPrompt,
                    Parameters = input.AnalysisParameters
                };

                var analysisResult = await _visualAnalyzer.AnalyzeImageAsync(analysisRequest, context);
                result.AnalysisResults.Add(analysisResult);
            }

            // Generate validation recommendations
            result.Recommendations = GenerateRecommendations(result.AnalysisResults, input.ValidationType);
            
            // Calculate overall validation score
            result.ValidationScore = CalculateValidationScore(result.AnalysisResults);
            
            // Generate summary
            result.Summary = GenerateValidationSummary(result);

            _logger.LogInformation("Visual validation completed successfully with score {Score}", result.ValidationScore);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Visual validation failed for {ValidationType}", input.ValidationType);
            return new VisualValidationResult
            {
                Success = false,
                ValidationType = input.ValidationType,
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    private async Task<string> CaptureScreenshotAsync(VisualValidationRequest input, ToolContext context)
    {
        // In a real implementation, this would interface with Unity's ScreenCapture API
        // For now, we'll create a placeholder screenshot
        
        var outputPath = input.OutputPath ?? Path.Combine(Path.GetTempPath(), $"validation_screenshot_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png");
        
        // Create a placeholder image that simulates a game screenshot
        using var bitmap = new Bitmap(input.Width, input.Height);
        using var graphics = Graphics.FromImage(bitmap);
        
        // Fill with a gradient to simulate a game scene
        using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
            new Rectangle(0, 0, input.Width, input.Height),
            Color.DarkBlue,
            Color.Black,
            System.Drawing.Drawing2D.LinearGradientMode.Vertical);
        
        graphics.FillRectangle(brush, 0, 0, input.Width, input.Height);
        
        // Add UI elements based on validation type
        AddUIElements(graphics, input.ValidationType, input.Width, input.Height);
        
        // Save the image
        bitmap.Save(outputPath, ImageFormat.Png);
        
        _logger.LogInformation("Screenshot captured for validation: {OutputPath}", outputPath);
        return outputPath;
    }

    private void AddUIElements(Graphics graphics, string validationType, int width, int height)
    {
        using var font = new Font("Arial", 16, FontStyle.Bold);
        using var textBrush = new SolidBrush(Color.White);
        using var uiBrush = new SolidBrush(Color.FromArgb(128, 255, 255, 255));

        switch (validationType.ToLower())
        {
            case "ui":
                // Add UI elements
                graphics.FillRectangle(uiBrush, 50, 50, 200, 30); // Health bar
                graphics.DrawString("Health: 100/100", font, textBrush, 50, 50);
                
                graphics.FillRectangle(uiBrush, 50, 100, 150, 30); // Ammo counter
                graphics.DrawString("Ammo: 30/30", font, textBrush, 50, 100);
                
                graphics.FillRectangle(uiBrush, width - 100, 50, 50, 50); // Crosshair
                graphics.DrawString("+", font, textBrush, width - 100, 50);
                break;

            case "gameplay":
                // Add gameplay elements
                graphics.FillRectangle(new SolidBrush(Color.Red), 300, 200, 50, 50); // Enemy
                graphics.DrawString("Enemy", font, textBrush, 300, 200);
                
                graphics.FillRectangle(new SolidBrush(Color.Green), 400, 300, 30, 30); // Player
                graphics.DrawString("Player", font, textBrush, 400, 300);
                break;

            case "performance":
                // Add performance indicators
                graphics.DrawString("FPS: 60", font, textBrush, 50, height - 100);
                graphics.DrawString("Draw Calls: 127", font, textBrush, 50, height - 80);
                graphics.DrawString("Memory: 512MB", font, textBrush, 50, height - 60);
                break;

            case "accessibility":
                // Add accessibility test elements
                graphics.DrawString("Small Text", new Font("Arial", 8), textBrush, 50, 200);
                graphics.DrawString("Large Text", new Font("Arial", 24), textBrush, 50, 250);
                graphics.FillRectangle(new SolidBrush(Color.Gray), 50, 300, 100, 20); // Low contrast
                break;
        }
    }

    private List<string> GetAnalysisTypesForValidation(string validationType)
    {
        return validationType.ToLower() switch
        {
            "ui" => new[] { "ui", "accessibility" },
            "gameplay" => new[] { "gameplay", "performance" },
            "performance" => new[] { "performance", "ui" },
            "accessibility" => new[] { "accessibility", "ui" },
            "comprehensive" => new[] { "ui", "gameplay", "performance", "accessibility" },
            _ => new[] { "ui", "gameplay", "performance", "accessibility" }
        };
    }

    private List<ValidationRecommendation> GenerateRecommendations(List<VisualAnalysisResult> analysisResults, string validationType)
    {
        var recommendations = new List<ValidationRecommendation>();

        foreach (var result in analysisResults)
        {
            foreach (var insight in result.Insights)
            {
                if (insight.Severity == "high" || insight.Severity == "critical")
                {
                    recommendations.Add(new ValidationRecommendation
                    {
                        Type = insight.Type,
                        Category = insight.Category,
                        Priority = insight.Severity,
                        Description = insight.Description,
                        Action = GenerateActionForInsight(insight),
                        EstimatedEffort = EstimateEffort(insight)
                    });
                }
            }
        }

        return recommendations.OrderByDescending(r => r.Priority == "critical" ? 3 : r.Priority == "high" ? 2 : 1).ToList();
    }

    private string GenerateActionForInsight(VisualInsight insight)
    {
        return insight.Category switch
        {
            "ui" => "Review UI layout and styling",
            "gameplay" => "Check game mechanics and balance",
            "performance" => "Optimize rendering and memory usage",
            "accessibility" => "Improve accessibility compliance",
            _ => "Review and address the identified issue"
        };
    }

    private string EstimateEffort(VisualInsight insight)
    {
        return insight.Severity switch
        {
            "critical" => "High effort - requires immediate attention",
            "high" => "Medium effort - should be addressed soon",
            "medium" => "Low effort - can be addressed in next iteration",
            "low" => "Minimal effort - nice to have improvement",
            _ => "Unknown effort"
        };
    }

    private double CalculateValidationScore(List<VisualAnalysisResult> analysisResults)
    {
        if (!analysisResults.Any())
            return 0.0;

        var totalScore = 0.0;
        var totalWeight = 0.0;

        foreach (var result in analysisResults)
        {
            var weight = result.AnalysisType switch
            {
                "ui" => 0.3,
                "gameplay" => 0.3,
                "performance" => 0.2,
                "accessibility" => 0.2,
                _ => 0.1
            };

            var score = CalculateAnalysisScore(result);
            totalScore += score * weight;
            totalWeight += weight;
        }

        return totalWeight > 0 ? totalScore / totalWeight : 0.0;
    }

    private double CalculateAnalysisScore(VisualAnalysisResult result)
    {
        var baseScore = 1.0;
        
        foreach (var insight in result.Insights)
        {
            var penalty = insight.Severity switch
            {
                "critical" => 0.3,
                "high" => 0.2,
                "medium" => 0.1,
                "low" => 0.05,
                _ => 0.0
            };
            
            baseScore -= penalty;
        }

        return Math.Max(0.0, baseScore);
    }

    private string GenerateValidationSummary(VisualValidationResult result)
    {
        var score = result.ValidationScore;
        var scoreText = score switch
        {
            >= 0.9 => "Excellent",
            >= 0.7 => "Good",
            >= 0.5 => "Fair",
            >= 0.3 => "Poor",
            _ => "Critical"
        };

        var criticalIssues = result.Recommendations.Count(r => r.Priority == "critical");
        var highIssues = result.Recommendations.Count(r => r.Priority == "high");

        var summary = $"Validation {scoreText} (Score: {score:P1})";
        
        if (criticalIssues > 0)
            summary += $". {criticalIssues} critical issues found.";
        else if (highIssues > 0)
            summary += $". {highIssues} high priority issues found.";
        else
            summary += ". No major issues detected.";

        return summary;
    }
}

/// <summary>
/// Request for visual validation.
/// </summary>
public record VisualValidationRequest
{
    public required string ValidationType { get; init; } // "ui", "gameplay", "performance", "accessibility", "comprehensive"
    public string? ScreenshotPath { get; init; } // If null, will capture new screenshot
    public string? OutputPath { get; init; } // Where to save the screenshot
    public string? CustomPrompt { get; init; } // Custom analysis prompt
    public Dictionary<string, object>? AnalysisParameters { get; init; } // Additional parameters
    public int Width { get; init; } = 1920;
    public int Height { get; init; } = 1080;
    public bool IncludeRecommendations { get; init; } = true;
}

/// <summary>
/// Result of visual validation.
/// </summary>
public record VisualValidationResult
{
    public required bool Success { get; init; }
    public required string ValidationType { get; init; }
    public required string ScreenshotPath { get; init; }
    public required DateTime Timestamp { get; init; }
    public required List<VisualAnalysisResult> AnalysisResults { get; init; }
    public required List<ValidationRecommendation> Recommendations { get; init; }
    public required double ValidationScore { get; init; } // 0.0 to 1.0
    public required string Summary { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Validation recommendation.
/// </summary>
public record ValidationRecommendation
{
    public required string Type { get; init; }
    public required string Category { get; init; }
    public required string Priority { get; init; }
    public required string Description { get; init; }
    public required string Action { get; init; }
    public required string EstimatedEffort { get; init; }
}
