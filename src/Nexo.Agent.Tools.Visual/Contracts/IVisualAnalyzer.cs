using Nexo.Agent.Contracts;

namespace Nexo.Agent.Tools.Visual.Contracts;

/// <summary>
/// Interface for visual analysis tools that can process images and provide insights.
/// </summary>
public interface IVisualAnalyzer : ITool
{
    /// <summary>
    /// Analyzes an image and provides structured insights.
    /// </summary>
    Task<VisualAnalysisResult> AnalyzeImageAsync(VisualAnalysisRequest request, ToolContext context);
    
    /// <summary>
    /// Compares two images and identifies differences.
    /// </summary>
    Task<VisualComparisonResult> CompareImagesAsync(VisualComparisonRequest request, ToolContext context);
    
    /// <summary>
    /// Extracts text from images using OCR.
    /// </summary>
    Task<OcrResult> ExtractTextAsync(OcrRequest request, ToolContext context);
}

/// <summary>
/// Request for visual analysis.
/// </summary>
public record VisualAnalysisRequest
{
    public required string ImagePath { get; init; }
    public required string AnalysisType { get; init; } // "ui", "gameplay", "performance", "accessibility"
    public string? Prompt { get; init; }
    public Dictionary<string, object>? Parameters { get; init; }
}

/// <summary>
/// Result of visual analysis.
/// </summary>
public record VisualAnalysisResult
{
    public required bool Success { get; init; }
    public required string AnalysisType { get; init; }
    public required string Summary { get; init; }
    public required List<VisualInsight> Insights { get; init; }
    public required Dictionary<string, object> Metrics { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Individual insight from visual analysis.
/// </summary>
public record VisualInsight
{
    public required string Type { get; init; } // "issue", "recommendation", "observation"
    public required string Category { get; init; } // "ui", "performance", "accessibility", "gameplay"
    public required string Description { get; init; }
    public required string Severity { get; init; } // "low", "medium", "high", "critical"
    public Rectangle? BoundingBox { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Request for image comparison.
/// </summary>
public record VisualComparisonRequest
{
    public required string ImagePath1 { get; init; }
    public required string ImagePath2 { get; init; }
    public required string ComparisonType { get; init; } // "ui", "gameplay", "performance"
    public double Threshold { get; init; } = 0.1; // Sensitivity threshold
}

/// <summary>
/// Result of image comparison.
/// </summary>
public record VisualComparisonResult
{
    public required bool Success { get; init; }
    public required double SimilarityScore { get; init; }
    public required List<VisualDifference> Differences { get; init; }
    public required string Summary { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Individual difference found between images.
/// </summary>
public record VisualDifference
{
    public required string Type { get; init; } // "added", "removed", "changed", "moved"
    public required Rectangle BoundingBox { get; init; }
    public required string Description { get; init; }
    public required double Confidence { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Request for OCR text extraction.
/// </summary>
public record OcrRequest
{
    public required string ImagePath { get; init; }
    public string? Language { get; init; } = "en";
    public bool IncludeConfidence { get; init; } = true;
}

/// <summary>
/// Result of OCR text extraction.
/// </summary>
public record OcrResult
{
    public required bool Success { get; init; }
    public required string ExtractedText { get; init; }
    public required List<TextRegion> TextRegions { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Text region found in image.
/// </summary>
public record TextRegion
{
    public required string Text { get; init; }
    public required Rectangle BoundingBox { get; init; }
    public required double Confidence { get; init; }
    public string? Language { get; init; }
}

/// <summary>
/// Rectangle structure for bounding boxes.
/// </summary>
public record Rectangle
{
    public required int X { get; init; }
    public required int Y { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
}
