using Microsoft.Extensions.Logging;
using Nexo.Agent.Contracts;
using Nexo.Agent.Tools.Visual.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Agent.Tools.Visual.Implementations;

/// <summary>
/// Core analysis functionality for Unity visual analyzer.
/// </summary>
public sealed partial class UnityVisualAnalyzer
{
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
}
