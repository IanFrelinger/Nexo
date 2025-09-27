using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Services
{
    /// <summary>
    /// Batch material generation functionality
    /// </summary>
    public partial class DynamicMaterialGenerator : IDynamicMaterialGenerator
    {
        public async Task<MaterialGenerationResult> GenerateMaterialBatchAsync(List<MaterialGenerationRequest> requests)
        {
            _logger.LogInformation("Generating material batch: {RequestCount} materials", requests.Count);

            var results = new List<MaterialGenerationResult>();
            var batchContext = new BatchGenerationContext
            {
                BatchId = Guid.NewGuid().ToString(),
                RequestCount = requests.Count,
                StartTime = DateTime.UtcNow
            };

            try
            {
                // Analyze all requests to identify common patterns
                var commonPatterns = await AnalyzeCommonPatternsAsync(requests);
                
                // Generate materials in parallel where possible
                var tasks = requests.Select(async request =>
                {
                    try
                    {
                        return await GenerateMaterialAsync(request);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error generating material in batch: {RequestId}", request.RequestId);
                        return new MaterialGenerationResult
                        {
                            RequestId = request.RequestId,
                            Success = false,
                            ErrorMessage = ex.Message
                        };
                    }
                });

                results = (await Task.WhenAll(tasks)).ToList();
                
                batchContext.EndTime = DateTime.UtcNow;
                batchContext.Duration = batchContext.EndTime - batchContext.StartTime;
                batchContext.SuccessCount = results.Count(r => r.Success);
                batchContext.FailureCount = results.Count(r => !r.Success);

                _logger.LogInformation("Batch generation complete: {SuccessCount}/{TotalCount} successful", 
                    batchContext.SuccessCount, batchContext.RequestCount);

                return new MaterialGenerationResult
                {
                    RequestId = batchContext.BatchId,
                    Success = true,
                    BatchResults = results,
                    GenerationMetadata = new MaterialGenerationMetadata
                    {
                        BatchContext = batchContext,
                        CommonPatterns = commonPatterns
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch material generation");
                throw;
            }
        }

        private async Task<List<CommonPattern>> AnalyzeCommonPatternsAsync(List<MaterialGenerationRequest> requests)
        {
            var patterns = new List<CommonPattern>();

            // Analyze material type patterns
            var materialTypeGroups = requests.GroupBy(r => r.MaterialType);
            foreach (var group in materialTypeGroups)
            {
                patterns.Add(new CommonPattern
                {
                    Type = PatternType.MaterialType,
                    Value = group.Key.ToString(),
                    Frequency = group.Count(),
                    Confidence = (double)group.Count() / requests.Count
                });
            }

            // Analyze visual style patterns
            var styleGroups = requests.GroupBy(r => r.VisualStyle);
            foreach (var group in styleGroups)
            {
                patterns.Add(new CommonPattern
                {
                    Type = PatternType.VisualStyle,
                    Value = group.Key,
                    Frequency = group.Count(),
                    Confidence = (double)group.Count() / requests.Count
                });
            }

            return patterns;
        }
    }
}
