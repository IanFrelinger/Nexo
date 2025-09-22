using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Agent.Contracts;
using Nexo.Agent.Tools.Visual.Contracts;

namespace Nexo.Agent.Tools.Visual.Implementations;

/// <summary>
/// Visual analyzer using OLLama's vision models for image analysis.
/// </summary>
public sealed class OllamaVisualAnalyzer : IVisualAnalyzer
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaVisualAnalyzer> _logger;
    private readonly string _ollamaBaseUrl;
    private readonly string _visionModel;

    public OllamaVisualAnalyzer(HttpClient httpClient, ILogger<OllamaVisualAnalyzer> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _ollamaBaseUrl = Environment.GetEnvironmentVariable("OLLAMA_BASE_URL") ?? "http://localhost:11434";
        _visionModel = Environment.GetEnvironmentVariable("OLLAMA_VISION_MODEL") ?? "llava:7b";
    }

    public string Id => "tool.visual.analyze";
    public string Name => "Visual Analyzer";
    public string Description => "Analyzes images using OLLama vision models for UI, gameplay, and performance insights";
    public string Version => "1.0.0";
    public ToolPermissions Permissions => ToolPermissions.FileRead;

    public async Task<VisualAnalysisResult> AnalyzeImageAsync(VisualAnalysisRequest request, ToolContext context)
    {
        try
        {
            _logger.LogInformation("Starting visual analysis of {ImagePath} with type {AnalysisType}", 
                request.ImagePath, request.AnalysisType);

            // Convert image to base64
            var imageBase64 = await ConvertImageToBase64Async(request.ImagePath);
            
            // Create analysis prompt based on type
            var prompt = CreateAnalysisPrompt(request.AnalysisType, request.Prompt);
            
            // Call OLLama vision model
            var response = await CallOllamaVisionAsync(prompt, imageBase64, context.CancellationToken);
            
            // Parse response into structured result
            var result = ParseAnalysisResponse(response, request.AnalysisType);
            
            _logger.LogInformation("Visual analysis completed successfully");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Visual analysis failed for {ImagePath}", request.ImagePath);
            return new VisualAnalysisResult
            {
                Success = false,
                AnalysisType = request.AnalysisType,
                Summary = "Analysis failed",
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
            _logger.LogInformation("Starting image comparison between {Image1} and {Image2}", 
                request.ImagePath1, request.ImagePath2);

            // Convert both images to base64
            var image1Base64 = await ConvertImageToBase64Async(request.ImagePath1);
            var image2Base64 = await ConvertImageToBase64Async(request.ImagePath2);
            
            // Create comparison prompt
            var prompt = CreateComparisonPrompt(request.ComparisonType, request.Threshold);
            
            // For comparison, we'll analyze each image separately and then compare results
            var result1 = await CallOllamaVisionAsync(prompt, image1Base64, context.CancellationToken);
            var result2 = await CallOllamaVisionAsync(prompt, image2Base64, context.CancellationToken);
            
            // Parse and compare results
            var comparison = ParseComparisonResponse(result1, result2, request.ComparisonType);
            
            _logger.LogInformation("Image comparison completed successfully");
            return comparison;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Image comparison failed between {Image1} and {Image2}", 
                request.ImagePath1, request.ImagePath2);
            return new VisualComparisonResult
            {
                Success = false,
                SimilarityScore = 0.0,
                Differences = new List<VisualDifference>(),
                Summary = "Comparison failed",
                Error = ex.Message
            };
        }
    }

    public async Task<OcrResult> ExtractTextAsync(OcrRequest request, ToolContext context)
    {
        try
        {
            _logger.LogInformation("Starting OCR text extraction from {ImagePath}", request.ImagePath);

            // Convert image to base64
            var imageBase64 = await ConvertImageToBase64Async(request.ImagePath);
            
            // Create OCR prompt
            var prompt = CreateOcrPrompt(request.Language, request.IncludeConfidence);
            
            // Call OLLama vision model
            var response = await CallOllamaVisionAsync(prompt, imageBase64, context.CancellationToken);
            
            // Parse OCR response
            var result = ParseOcrResponse(response, request.IncludeConfidence);
            
            _logger.LogInformation("OCR text extraction completed successfully");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR text extraction failed for {ImagePath}", request.ImagePath);
            return new OcrResult
            {
                Success = false,
                ExtractedText = string.Empty,
                TextRegions = new List<TextRegion>(),
                Error = ex.Message
            };
        }
    }

    private async Task<string> ConvertImageToBase64Async(string imagePath)
    {
        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException($"Image file not found: {imagePath}");
        }

        using var image = Image.FromFile(imagePath);
        using var ms = new MemoryStream();
        
        // Convert to JPEG for better compression
        image.Save(ms, ImageFormat.Jpeg);
        var imageBytes = ms.ToArray();
        
        return Convert.ToBase64String(imageBytes);
    }

    private string CreateAnalysisPrompt(string analysisType, string? customPrompt)
    {
        if (!string.IsNullOrEmpty(customPrompt))
        {
            return customPrompt;
        }

        return analysisType switch
        {
            "ui" => @"Analyze this UI screenshot and provide insights about:
1. UI element visibility and contrast
2. Layout and spacing issues
3. Accessibility concerns
4. Visual hierarchy
5. Color scheme and branding

Respond in JSON format with:
- summary: brief overview
- insights: array of {type, category, description, severity, boundingBox}
- metrics: object with numerical measurements",
            
            "gameplay" => @"Analyze this gameplay screenshot and provide insights about:
1. Game state and progression
2. UI elements (health, ammo, minimap)
3. Visual effects and particles
4. Lighting and atmosphere
5. Performance indicators

Respond in JSON format with:
- summary: brief overview
- insights: array of {type, category, description, severity, boundingBox}
- metrics: object with numerical measurements",
            
            "performance" => @"Analyze this screenshot for performance indicators:
1. Frame rate indicators
2. Visual quality settings
3. Rendering artifacts
4. UI responsiveness
5. Memory usage indicators

Respond in JSON format with:
- summary: brief overview
- insights: array of {type, category, description, severity, boundingBox}
- metrics: object with numerical measurements",
            
            "accessibility" => @"Analyze this screenshot for accessibility:
1. Color contrast ratios
2. Text readability
3. UI element sizes
4. Navigation clarity
5. Visual indicators

Respond in JSON format with:
- summary: brief overview
- insights: array of {type, category, description, severity, boundingBox}
- metrics: object with numerical measurements",
            
            _ => @"Analyze this image and provide general insights about:
1. Visual composition
2. Key elements and objects
3. Color and lighting
4. Potential issues or improvements

Respond in JSON format with:
- summary: brief overview
- insights: array of {type, category, description, severity, boundingBox}
- metrics: object with numerical measurements"
        };
    }

    private string CreateComparisonPrompt(string comparisonType, double threshold)
    {
        return comparisonType switch
        {
            "ui" => $@"Compare these two UI screenshots and identify differences:
1. UI element changes (added, removed, modified)
2. Layout changes
3. Color or styling changes
4. Content changes
5. Navigation changes

Threshold for significance: {threshold}

Respond in JSON format with:
- similarityScore: 0.0 to 1.0
- differences: array of {{type, boundingBox, description, confidence}}
- summary: brief overview",
            
            "gameplay" => $@"Compare these two gameplay screenshots and identify differences:
1. Game state changes
2. UI element changes
3. Visual effect changes
4. Lighting changes
5. Object position changes

Threshold for significance: {threshold}

Respond in JSON format with:
- similarityScore: 0.0 to 1.0
- differences: array of {{type, boundingBox, description, confidence}}
- summary: brief overview",
            
            _ => $@"Compare these two images and identify differences:
1. Object changes
2. Color changes
3. Layout changes
4. Content changes

Threshold for significance: {threshold}

Respond in JSON format with:
- similarityScore: 0.0 to 1.0
- differences: array of {{type, boundingBox, description, confidence}}
- summary: brief overview"
        };
    }

    private string CreateOcrPrompt(string? language, bool includeConfidence)
    {
        var confidenceText = includeConfidence ? " Include confidence scores for each text region." : "";
        return $@"Extract all text from this image in {language ?? "English"}.{confidenceText}

Respond in JSON format with:
- extractedText: all text as a single string
- textRegions: array of {{text, boundingBox, confidence, language}}";
    }

    private async Task<string> CallOllamaVisionAsync(string prompt, string imageBase64, CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            model = _visionModel,
            prompt = prompt,
            images = new[] { imageBase64 },
            stream = false
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_ollamaBaseUrl}/api/generate", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);
        
        return responseObj.GetProperty("response").GetString() ?? string.Empty;
    }

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
            Summary = $"Images compared with {comparity:P1} similarity"
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
