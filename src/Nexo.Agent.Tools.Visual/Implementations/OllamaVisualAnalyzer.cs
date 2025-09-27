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
/// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
/// </summary>
public sealed partial class OllamaVisualAnalyzer : IVisualAnalyzer
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
    // This class acts as an orchestrator for various visual analysis functionalities,
    // with specific categories defined in partial classes.
}