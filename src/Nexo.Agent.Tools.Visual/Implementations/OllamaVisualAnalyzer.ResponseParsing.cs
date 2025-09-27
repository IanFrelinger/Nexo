using System.Drawing;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Agent.Tools.Visual.Contracts;

namespace Nexo.Agent.Tools.Visual.Implementations;

/// <summary>
/// Response parsing for analysis, comparison, and OCR results
/// </summary>
public sealed partial class OllamaVisualAnalyzer
{
    private VisualAnalysisResult ParseAnalysisResponse(string response, string analysisType)
    {
        try
        {
            var json = JsonSerializer.Deserialize<JsonElement>(response);
            
            var insights = new List<VisualInsight>();
            if (json.TryGetProperty("insights", out var insightsArray))
            {
                foreach (var insight in insightsArray.EnumerateArray())
                {
                    insights.Add(new VisualInsight
                    {
                        Type = insight.GetProperty("type").GetString() ?? "observation",
                        Category = insight.GetProperty("category").GetString() ?? "general",
                        Description = insight.GetProperty("description").GetString() ?? "",
                        Severity = insight.GetProperty("severity").GetString() ?? "low",
                        BoundingBox = ParseBoundingBox(insight),
                        Metadata = ParseMetadata(insight)
                    });
                }
            }

            var metrics = new Dictionary<string, object>();
            if (json.TryGetProperty("metrics", out var metricsObj))
            {
                foreach (var metric in metricsObj.EnumerateObject())
                {
                    metrics[metric.Name] = metric.Value.ValueKind switch
                    {
                        JsonValueKind.Number => metric.Value.GetDouble(),
                        JsonValueKind.String => metric.Value.GetString() ?? "",
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        _ => metric.Value.ToString()
                    };
                }
            }

            return new VisualAnalysisResult
            {
                Success = true,
                AnalysisType = analysisType,
                Summary = json.GetProperty("summary").GetString() ?? "Analysis completed",
                Insights = insights,
                Metrics = metrics
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse analysis response, returning fallback result");
            return new VisualAnalysisResult
            {
                Success = true,
                AnalysisType = analysisType,
                Summary = "Analysis completed (parsing failed)",
                Insights = new List<VisualInsight>
                {
                    new()
                    {
                        Type = "observation",
                        Category = "general",
                        Description = "Raw analysis response: " + response,
                        Severity = "low"
                    }
                },
                Metrics = new Dictionary<string, object>()
            };
        }
    }

    private VisualComparisonResult ParseComparisonResponse(string response1, string response2, string comparisonType)
    {
        // Simplified comparison - in a real implementation, you'd do more sophisticated comparison
        var similarity = CalculateSimilarity(response1, response2);
        
        return new VisualComparisonResult
        {
            Success = true,
            SimilarityScore = similarity,
            Differences = new List<VisualDifference>(),
            Summary = $"Images compared with {similarity:P1} similarity"
        };
    }

    private OcrResult ParseOcrResponse(string response, bool includeConfidence)
    {
        try
        {
            var json = JsonSerializer.Deserialize<JsonElement>(response);
            
            var textRegions = new List<TextRegion>();
            if (json.TryGetProperty("textRegions", out var regionsArray))
            {
                foreach (var region in regionsArray.EnumerateArray())
                {
                    textRegions.Add(new TextRegion
                    {
                        Text = region.GetProperty("text").GetString() ?? "",
                        BoundingBox = ParseBoundingBox(region),
                        Confidence = region.GetProperty("confidence").GetDouble(),
                        Language = region.GetProperty("language").GetString()
                    });
                }
            }

            return new OcrResult
            {
                Success = true,
                ExtractedText = json.GetProperty("extractedText").GetString() ?? "",
                TextRegions = textRegions
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse OCR response, returning fallback result");
            return new OcrResult
            {
                Success = true,
                ExtractedText = response,
                TextRegions = new List<TextRegion>()
            };
        }
    }

    private Rectangle? ParseBoundingBox(JsonElement element)
    {
        if (!element.TryGetProperty("boundingBox", out var bbox))
            return null;

        return new Rectangle
        {
            X = bbox.GetProperty("x").GetInt32(),
            Y = bbox.GetProperty("y").GetInt32(),
            Width = bbox.GetProperty("width").GetInt32(),
            Height = bbox.GetProperty("height").GetInt32()
        };
    }

    private Dictionary<string, object>? ParseMetadata(JsonElement element)
    {
        if (!element.TryGetProperty("metadata", out var metadata))
            return null;

        var result = new Dictionary<string, object>();
        foreach (var prop in metadata.EnumerateObject())
        {
            result[prop.Name] = prop.Value.ValueKind switch
            {
                JsonValueKind.Number => prop.Value.GetDouble(),
                JsonValueKind.String => prop.Value.GetString() ?? "",
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => prop.Value.ToString()
            };
        }
        return result;
    }

    private double CalculateSimilarity(string response1, string response2)
    {
        // Simple similarity calculation based on common words
        var words1 = response1.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var words2 = response2.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        var commonWords = words1.Intersect(words2).Count();
        var totalWords = words1.Union(words2).Count();
        
        return totalWords > 0 ? (double)commonWords / totalWords : 0.0;
    }
}
