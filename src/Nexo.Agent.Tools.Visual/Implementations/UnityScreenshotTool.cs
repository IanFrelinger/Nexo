using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Agent.Contracts;
using Nexo.Agent.Tools.Visual.Contracts;

namespace Nexo.Agent.Tools.Visual.Implementations;

/// <summary>
/// Tool for capturing and analyzing Unity game screenshots.
/// </summary>
public sealed class UnityScreenshotTool : ITool<UnityScreenshotRequest, UnityScreenshotResult>
{
    private readonly ILogger<UnityScreenshotTool> _logger;
    private readonly IVisualAnalyzer _visualAnalyzer;

    public UnityScreenshotTool(ILogger<UnityScreenshotTool> logger, IVisualAnalyzer visualAnalyzer)
    {
        _logger = logger;
        _visualAnalyzer = visualAnalyzer;
    }

    public string Id => "tool.unity.screenshot";
    public string Name => "Unity Screenshot Tool";
    public string Description => "Captures and analyzes Unity game screenshots for visual insights";
    public string Version => "1.0.0";
    public ToolPermissions Permissions => ToolPermissions.FileRead | ToolPermissions.FileWrite;

    public async Task<UnityScreenshotResult> ExecuteAsync(UnityScreenshotRequest input, ToolContext context)
    {
        try
        {
            _logger.LogInformation("Starting Unity screenshot capture and analysis");

            var result = new UnityScreenshotResult
            {
                Success = true,
                ScreenshotPath = input.OutputPath,
                Timestamp = DateTime.UtcNow,
                AnalysisResults = new List<VisualAnalysisResult>()
            };

            // Capture screenshot if needed
            if (string.IsNullOrEmpty(input.ScreenshotPath))
            {
                result.ScreenshotPath = await CaptureScreenshotAsync(input, context);
            }
            else
            {
                result.ScreenshotPath = input.ScreenshotPath;
            }

            // Perform visual analysis
            if (input.AnalysisTypes?.Any() == true)
            {
                foreach (var analysisType in input.AnalysisTypes)
                {
                    var analysisRequest = new VisualAnalysisRequest
                    {
                        ImagePath = result.ScreenshotPath,
                        AnalysisType = analysisType,
                        Prompt = input.CustomPrompt,
                        Parameters = input.AnalysisParameters
                    };

                    var analysisResult = await _visualAnalyzer.AnalyzeImageAsync(analysisRequest, context);
                    result.AnalysisResults.Add(analysisResult);
                }
            }

            // Generate summary
            result.Summary = GenerateSummary(result.AnalysisResults);

            _logger.LogInformation("Unity screenshot capture and analysis completed successfully");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unity screenshot capture and analysis failed");
            return new UnityScreenshotResult
            {
                Success = false,
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    private async Task<string> CaptureScreenshotAsync(UnityScreenshotRequest input, ToolContext context)
    {
        // In a real implementation, this would interface with Unity's ScreenCapture API
        // For now, we'll simulate screenshot capture
        
        var outputPath = input.OutputPath ?? Path.Combine(Path.GetTempPath(), $"unity_screenshot_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png");
        
        // Create a placeholder image (in real implementation, this would be the actual screenshot)
        using var bitmap = new Bitmap(1920, 1080);
        using var graphics = Graphics.FromImage(bitmap);
        
        // Fill with a gradient to simulate a game scene
        using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
            new Rectangle(0, 0, 1920, 1080),
            Color.DarkBlue,
            Color.Black,
            System.Drawing.Drawing2D.LinearGradientMode.Vertical);
        
        graphics.FillRectangle(brush, 0, 0, 1920, 1080);
        
        // Add some text to simulate UI elements
        using var font = new Font("Arial", 24, FontStyle.Bold);
        using var textBrush = new SolidBrush(Color.White);
        graphics.DrawString("Unity Game Screenshot", font, textBrush, 50, 50);
        graphics.DrawString("Health: 100/100", font, textBrush, 50, 100);
        graphics.DrawString("Ammo: 30/30", font, textBrush, 50, 150);
        
        // Save the image
        bitmap.Save(outputPath, ImageFormat.Png);
        
        _logger.LogInformation("Screenshot captured and saved to {OutputPath}", outputPath);
        return outputPath;
    }

    private string GenerateSummary(List<VisualAnalysisResult> analysisResults)
    {
        if (!analysisResults.Any())
            return "No analysis performed";

        var summaries = analysisResults.Select(r => $"{r.AnalysisType}: {r.Summary}");
        return string.Join("; ", summaries);
    }
}

/// <summary>
/// Request for Unity screenshot capture and analysis.
/// </summary>
public record UnityScreenshotRequest
{
    public string? ScreenshotPath { get; init; } // If null, will capture new screenshot
    public string? OutputPath { get; init; } // Where to save the screenshot
    public List<string>? AnalysisTypes { get; init; } // Types of analysis to perform
    public string? CustomPrompt { get; init; } // Custom analysis prompt
    public Dictionary<string, object>? AnalysisParameters { get; init; } // Additional parameters
    public int Width { get; init; } = 1920;
    public int Height { get; init; } = 1080;
    public bool IncludeUI { get; init; } = true;
    public bool IncludeGameplay { get; init; } = true;
}

/// <summary>
/// Result of Unity screenshot capture and analysis.
/// </summary>
public record UnityScreenshotResult
{
    public required bool Success { get; init; }
    public required string ScreenshotPath { get; init; }
    public required DateTime Timestamp { get; init; }
    public required List<VisualAnalysisResult> AnalysisResults { get; init; }
    public required string Summary { get; init; }
    public string? Error { get; init; }
}
