using Microsoft.Extensions.Logging;
using System;

namespace Nexo.Feature.AI.Services.EnhancedFeatureFactory.Processing
{
    public class ResponseExtractor
    {
        private readonly ILogger _logger;

        public ResponseExtractor(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public FeatureGenerationResponse BuildFeatureGenerationResponseFrom(CoordinatedResponse coordinatedResponse)
        {
            return new FeatureGenerationResponse
            {
                Success = true,
                GeneratedCode = coordinatedResponse.Result,
                QualityMetrics = ExtractQualityMetrics(coordinatedResponse),
                PerformanceOptimizations = ExtractPerformanceOptimizations(coordinatedResponse),
                SecurityValidation = ExtractSecurityValidation(coordinatedResponse),
                CrossPlatformCompatibility = ExtractPlatformCompatibility(coordinatedResponse),
                CoordinationDetails = new CoordinationDetails
                {
                    UsedSpecializedCoordination = true,
                    AgentCount = GetAgentCountFromMetadata(coordinatedResponse),
                    CoordinationStrategy = GetCoordinationStrategyFromMetadata(coordinatedResponse),
                    ExecutionTime = GetExecutionTimeFromMetadata(coordinatedResponse)
                }
            };
        }

        private QualityMetrics ExtractQualityMetrics(CoordinatedResponse response) => response.QualityMetrics ?? new QualityMetrics();
        private PerformanceOptimizations ExtractPerformanceOptimizations(CoordinatedResponse response) => response.PerformanceOptimizations ?? new PerformanceOptimizations();
        private SecurityValidation ExtractSecurityValidation(CoordinatedResponse response) => response.SecurityValidation ?? new SecurityValidation();
        private CrossPlatformCompatibility ExtractPlatformCompatibility(CoordinatedResponse response) => response.CrossPlatformCompatibility ?? new CrossPlatformCompatibility();
        private int GetAgentCountFromMetadata(CoordinatedResponse response) => response.Metadata != null && response.Metadata.TryGetValue("AgentCount", out var v) && v is int i ? i : 0;
        private string GetCoordinationStrategyFromMetadata(CoordinatedResponse response) => response.Metadata != null && response.Metadata.TryGetValue("CoordinationStrategy", out var v) && v is string s ? s : string.Empty;
        private TimeSpan GetExecutionTimeFromMetadata(CoordinatedResponse response) => response.Metadata != null && response.Metadata.TryGetValue("ExecutionTime", out var v) && v is TimeSpan t ? t : TimeSpan.Zero;
    }
}


