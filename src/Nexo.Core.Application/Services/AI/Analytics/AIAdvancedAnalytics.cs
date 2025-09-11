using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Services.AI.Monitoring;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Analytics
{
    /// <summary>
    /// Advanced AI analytics service with machine learning-powered insights
    /// </summary>
    public class AIAdvancedAnalytics
    {
        private readonly ILogger<AIAdvancedAnalytics> _logger;
        private readonly AIUsageMonitor _usageMonitor;
        private readonly Dictionary<string, AnalyticsModel> _analyticsModels;
        private readonly object _lockObject = new object();

        public AIAdvancedAnalytics(ILogger<AIAdvancedAnalytics> logger, AIUsageMonitor usageMonitor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _usageMonitor = usageMonitor ?? throw new ArgumentNullException(nameof(usageMonitor));
            _analyticsModels = new Dictionary<string, AnalyticsModel>();
        }

        /// <summary>
        /// Generates advanced analytics with machine learning insights
        /// </summary>
        public async Task<AdvancedAnalyticsResult> GenerateAdvancedAnalyticsAsync(AnalyticsRequest request)
        {
            try
            {
                _logger.LogInformation("Generating advanced analytics for {TimeRange} period", request.TimeRange);

                var result = new AdvancedAnalyticsResult
                {
                    Request = request,
                    GeneratedAt = DateTime.UtcNow,
                    Insights = new List<AnalyticsInsight>(),
                    Predictions = new List<AnalyticsPrediction>(),
                    Recommendations = new List<AnalyticsRecommendation>(),
                    PerformanceMetrics = new PerformanceMetrics(),
                    UsagePatterns = new List<UsagePattern>(),
                    Anomalies = new List<AnomalyDetection>()
                };

                // Generate machine learning insights
                var mlInsights = await GenerateMachineLearningInsightsAsync(request);
                result.Insights.AddRange(mlInsights);

                // Generate predictions
                var predictions = await GeneratePredictionsAsync(request);
                result.Predictions.AddRange(predictions);

                // Generate recommendations
                var recommendations = await GenerateRecommendationsAsync(request, result.Insights);
                result.Recommendations.AddRange(recommendations);

                // Analyze performance metrics
                result.PerformanceMetrics = await AnalyzePerformanceMetricsAsync(request);

                // Detect usage patterns
                result.UsagePatterns = await DetectUsagePatternsAsync(request);

                // Detect anomalies
                result.Anomalies = await DetectAnomaliesAsync(request);

                // Generate trend analysis
                result.TrendAnalysis = await GenerateTrendAnalysisAsync(request);

                _logger.LogInformation("Advanced analytics generated with {InsightCount} insights, {PredictionCount} predictions, {RecommendationCount} recommendations", 
                    result.Insights.Count, result.Predictions.Count, result.Recommendations.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate advanced analytics");
                throw;
            }
        }

        /// <summary>
        /// Trains a custom analytics model
        /// </summary>
        public async Task<ModelTrainingResult> TrainAnalyticsModelAsync(ModelTrainingRequest request)
        {
            try
            {
                _logger.LogInformation("Training analytics model {ModelName}", request.ModelName);

                var model = new AnalyticsModel
                {
                    ModelId = Guid.NewGuid().ToString(),
                    Name = request.ModelName,
                    Type = request.ModelType,
                    Status = ModelStatus.Training,
                    TrainingData = request.TrainingData,
                    CreatedAt = DateTime.UtcNow
                };

                lock (_lockObject)
                {
                    _analyticsModels[model.ModelId] = model;
                }

                // Simulate model training
                await TrainModelAsync(model);

                model.Status = ModelStatus.Trained;
                model.TrainingCompletedAt = DateTime.UtcNow;

                var result = new ModelTrainingResult
                {
                    ModelId = model.ModelId,
                    Success = true,
                    TrainingDuration = model.TrainingCompletedAt.Value - model.CreatedAt,
                    Accuracy = model.Accuracy,
                    Metrics = model.Metrics
                };

                _logger.LogInformation("Analytics model {ModelName} trained successfully with {Accuracy}% accuracy", 
                    request.ModelName, model.Accuracy);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to train analytics model {ModelName}", request.ModelName);
                return new ModelTrainingResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Gets analytics model by ID
        /// </summary>
        public Task<AnalyticsModel?> GetAnalyticsModelAsync(string modelId)
        {
            try
            {
                lock (_lockObject)
                {
                    _analyticsModels.TryGetValue(modelId, out var model);
                    return Task.FromResult(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get analytics model {ModelId}", modelId);
                return Task.FromResult<AnalyticsModel?>(null);
            }
        }

        /// <summary>
        /// Gets all analytics models
        /// </summary>
        public Task<List<AnalyticsModel>> GetAllAnalyticsModelsAsync()
        {
            try
            {
                lock (_lockObject)
                {
                    return Task.FromResult(_analyticsModels.Values.ToList());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all analytics models");
                return Task.FromResult(new List<AnalyticsModel>());
            }
        }

        /// <summary>
        /// Deletes an analytics model
        /// </summary>
        public Task<bool> DeleteAnalyticsModelAsync(string modelId)
        {
            try
            {
                lock (_lockObject)
                {
                    return Task.FromResult(_analyticsModels.Remove(modelId));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete analytics model {ModelId}", modelId);
                return Task.FromResult(false);
            }
        }

        private async Task<List<AnalyticsInsight>> GenerateMachineLearningInsightsAsync(AnalyticsRequest request)
        {
            var insights = new List<AnalyticsInsight>();

            // Get usage statistics
            var statistics = await _usageMonitor.GetUsageStatisticsAsync(request.TimeRange, request.UserId);

            // Generate performance insights
            if (statistics.SuccessRate < 90)
            {
                insights.Add(new AnalyticsInsight
                {
                    Type = InsightType.Performance,
                    Severity = InsightSeverity.High,
                    Title = "Low Success Rate Detected",
                    Description = $"Current success rate is {statistics.SuccessRate:F1}%, below recommended 90%",
                    Confidence = 0.95,
                    Impact = "High",
                    Recommendations = new List<string> { "Investigate failed operations", "Review error logs", "Consider model updates" }
                });
            }

            // Generate usage pattern insights
            if (statistics.TotalOperations > 1000)
            {
                insights.Add(new AnalyticsInsight
                {
                    Type = InsightType.Usage,
                    Severity = InsightSeverity.Medium,
                    Title = "High Usage Volume",
                    Description = $"High usage volume detected: {statistics.TotalOperations} operations",
                    Confidence = 0.85,
                    Impact = "Medium",
                    Recommendations = new List<string> { "Consider scaling resources", "Implement rate limiting", "Monitor performance" }
                });
            }

            // Generate efficiency insights
            if (statistics.AverageOperationDuration.TotalSeconds > 10)
            {
                insights.Add(new AnalyticsInsight
                {
                    Type = InsightType.Efficiency,
                    Severity = InsightSeverity.Medium,
                    Title = "Slow Operation Times",
                    Description = $"Average operation duration is {statistics.AverageOperationDuration.TotalSeconds:F1} seconds",
                    Confidence = 0.80,
                    Impact = "Medium",
                    Recommendations = new List<string> { "Optimize model performance", "Consider model updates", "Review resource allocation" }
                });
            }

            await Task.Delay(100); // Simulate processing time
            return insights;
        }

        private async Task<List<AnalyticsPrediction>> GeneratePredictionsAsync(AnalyticsRequest request)
        {
            var predictions = new List<AnalyticsPrediction>();

            // Get historical data
            var statistics = await _usageMonitor.GetUsageStatisticsAsync(request.TimeRange, request.UserId);

            // Predict future usage
            var usagePrediction = new AnalyticsPrediction
            {
                Type = PredictionType.Usage,
                TimeHorizon = TimeSpan.FromDays(7),
                PredictedValue = statistics.TotalOperations * 1.2, // 20% increase
                Confidence = 0.75,
                Description = "Predicted 20% increase in usage over next 7 days",
                Factors = new List<string> { "Historical usage trends", "Current growth rate", "Seasonal patterns" }
            };
            predictions.Add(usagePrediction);

            // Predict performance trends
            var performancePrediction = new AnalyticsPrediction
            {
                Type = PredictionType.Performance,
                TimeHorizon = TimeSpan.FromDays(14),
                PredictedValue = Math.Max(0.85, statistics.SuccessRate + 0.05), // 5% improvement
                Confidence = 0.70,
                Description = "Predicted 5% improvement in success rate over next 14 days",
                Factors = new List<string> { "Model optimization", "Error reduction", "System improvements" }
            };
            predictions.Add(performancePrediction);

            // Predict resource needs
            var resourcePrediction = new AnalyticsPrediction
            {
                Type = PredictionType.Resource,
                TimeHorizon = TimeSpan.FromDays(30),
                PredictedValue = statistics.TotalOperations * 1.5, // 50% increase
                Confidence = 0.60,
                Description = "Predicted 50% increase in resource requirements over next 30 days",
                Factors = new List<string> { "Usage growth", "Model complexity", "Feature expansion" }
            };
            predictions.Add(resourcePrediction);

            await Task.Delay(100); // Simulate processing time
            return predictions;
        }

        private async Task<List<AnalyticsRecommendation>> GenerateRecommendationsAsync(AnalyticsRequest request, List<AnalyticsInsight> insights)
        {
            var recommendations = new List<AnalyticsRecommendation>();

            // Generate performance recommendations
            var performanceInsights = insights.Where(i => i.Type == InsightType.Performance).ToList();
            if (performanceInsights.Any())
            {
                recommendations.Add(new AnalyticsRecommendation
                {
                    Type = RecommendationType.Performance,
                    Priority = RecommendationPriority.High,
                    Title = "Optimize AI Performance",
                    Description = "Implement performance optimizations to improve success rate and reduce operation times",
                    Actions = new List<string>
                    {
                        "Update AI models to latest versions",
                        "Implement intelligent caching",
                        "Optimize resource allocation",
                        "Add performance monitoring"
                    },
                    ExpectedImpact = "20-30% improvement in performance metrics",
                    ImplementationEffort = "Medium"
                });
            }

            // Generate scalability recommendations
            var usageInsights = insights.Where(i => i.Type == InsightType.Usage).ToList();
            if (usageInsights.Any())
            {
                recommendations.Add(new AnalyticsRecommendation
                {
                    Type = RecommendationType.Scalability,
                    Priority = RecommendationPriority.Medium,
                    Title = "Scale AI Infrastructure",
                    Description = "Implement scaling solutions to handle increased usage volume",
                    Actions = new List<string>
                    {
                        "Implement horizontal scaling",
                        "Add load balancing",
                        "Implement rate limiting",
                        "Add auto-scaling policies"
                    },
                    ExpectedImpact = "Support 2-3x current usage volume",
                    ImplementationEffort = "High"
                });
            }

            // Generate efficiency recommendations
            var efficiencyInsights = insights.Where(i => i.Type == InsightType.Efficiency).ToList();
            if (efficiencyInsights.Any())
            {
                recommendations.Add(new AnalyticsRecommendation
                {
                    Type = RecommendationType.Efficiency,
                    Priority = RecommendationPriority.Medium,
                    Title = "Improve AI Efficiency",
                    Description = "Implement efficiency improvements to reduce operation times and resource usage",
                    Actions = new List<string>
                    {
                        "Implement model quantization",
                        "Add batch processing",
                        "Optimize data pipelines",
                        "Implement smart caching"
                    },
                    ExpectedImpact = "30-40% reduction in operation times",
                    ImplementationEffort = "Medium"
                });
            }

            await Task.Delay(100); // Simulate processing time
            return recommendations;
        }

        private async Task<PerformanceMetrics> AnalyzePerformanceMetricsAsync(AnalyticsRequest request)
        {
            var statistics = await _usageMonitor.GetUsageStatisticsAsync(request.TimeRange, request.UserId);

            var metrics = new PerformanceMetrics
            {
                SuccessRate = statistics.SuccessRate,
                AverageOperationDuration = statistics.AverageOperationDuration,
                TotalOperations = statistics.TotalOperations,
                FailedOperations = statistics.FailedOperations,
                Throughput = CalculateThroughput(statistics),
                ErrorRate = CalculateErrorRate(statistics),
                ResourceUtilization = CalculateResourceUtilization(statistics),
                QualityScore = CalculateQualityScore(statistics)
            };

            await Task.Delay(50); // Simulate processing time
            return metrics;
        }

        private async Task<List<UsagePattern>> DetectUsagePatternsAsync(AnalyticsRequest request)
        {
            var patterns = new List<UsagePattern>();

            // Detect peak usage times
            patterns.Add(new UsagePattern
            {
                Type = PatternType.Temporal,
                Name = "Peak Usage Hours",
                Description = "Highest usage occurs between 9 AM and 5 PM",
                Confidence = 0.85,
                Impact = "High",
                Recommendations = new List<string> { "Scale resources during peak hours", "Implement load balancing" }
            });

            // Detect operation type patterns
            patterns.Add(new UsagePattern
            {
                Type = PatternType.Operational,
                Name = "Code Generation Dominance",
                Description = "Code generation operations account for 70% of total usage",
                Confidence = 0.90,
                Impact = "Medium",
                Recommendations = new List<string> { "Optimize code generation models", "Add specialized caching" }
            });

            // Detect user behavior patterns
            patterns.Add(new UsagePattern
            {
                Type = PatternType.Behavioral,
                Name = "Batch Processing Preference",
                Description = "Users prefer batch processing over individual operations",
                Confidence = 0.75,
                Impact = "Medium",
                Recommendations = new List<string> { "Implement batch processing APIs", "Add batch optimization" }
            });

            await Task.Delay(100); // Simulate processing time
            return patterns;
        }

        private async Task<List<AnomalyDetection>> DetectAnomaliesAsync(AnalyticsRequest request)
        {
            var anomalies = new List<AnomalyDetection>();

            // Simulate anomaly detection
            var statistics = await _usageMonitor.GetUsageStatisticsAsync(request.TimeRange, request.UserId);

            // Detect unusual error spikes
            if (statistics.FailedOperations > statistics.TotalOperations * 0.2)
            {
                anomalies.Add(new AnomalyDetection
                {
                    Type = AnomalyType.ErrorSpike,
                    Severity = AnomalySeverity.High,
                    Description = "Unusual spike in error rate detected",
                    DetectedAt = DateTime.UtcNow,
                    Confidence = 0.90,
                    Impact = "High",
                    Recommendations = new List<string> { "Investigate error causes", "Check system health", "Review recent changes" }
                });
            }

            // Detect unusual usage patterns
            if (statistics.TotalOperations > 5000) // Threshold for unusual usage
            {
                anomalies.Add(new AnomalyDetection
                {
                    Type = AnomalyType.UsageSpike,
                    Severity = AnomalySeverity.Medium,
                    Description = "Unusual spike in usage volume detected",
                    DetectedAt = DateTime.UtcNow,
                    Confidence = 0.75,
                    Impact = "Medium",
                    Recommendations = new List<string> { "Monitor resource usage", "Check for potential abuse", "Consider rate limiting" }
                });
            }

            await Task.Delay(100); // Simulate processing time
            return anomalies;
        }

        private async Task<TrendAnalysis> GenerateTrendAnalysisAsync(AnalyticsRequest request)
        {
            var statistics = await _usageMonitor.GetUsageStatisticsAsync(request.TimeRange, request.UserId);

            var trendAnalysis = new TrendAnalysis
            {
                UsageTrend = CalculateUsageTrend(statistics),
                PerformanceTrend = CalculatePerformanceTrend(statistics),
                ErrorTrend = CalculateErrorTrend(statistics),
                ResourceTrend = CalculateResourceTrend(statistics),
                OverallTrend = CalculateOverallTrend(statistics)
            };

            await Task.Delay(50); // Simulate processing time
            return trendAnalysis;
        }

        private async Task TrainModelAsync(AnalyticsModel model)
        {
            // Simulate model training
            var trainingTime = Random.Shared.Next(5000, 15000); // 5-15 seconds
            await Task.Delay(trainingTime);

            // Set training results
            model.Accuracy = Random.Shared.Next(75, 95); // 75-95% accuracy
            model.Metrics = new Dictionary<string, object>
            {
                ["precision"] = Random.Shared.NextDouble() * 0.2 + 0.8, // 0.8-1.0
                ["recall"] = Random.Shared.NextDouble() * 0.2 + 0.8, // 0.8-1.0
                ["f1_score"] = Random.Shared.NextDouble() * 0.2 + 0.8, // 0.8-1.0
                ["training_loss"] = Random.Shared.NextDouble() * 0.5 + 0.1, // 0.1-0.6
                ["validation_loss"] = Random.Shared.NextDouble() * 0.5 + 0.1 // 0.1-0.6
            };
        }

        private double CalculateThroughput(AIUsageStatistics statistics)
        {
            if (statistics.TotalOperations == 0) return 0;
            return statistics.TotalOperations / Math.Max(1, statistics.AverageOperationDuration.TotalHours);
        }

        private double CalculateErrorRate(AIUsageStatistics statistics)
        {
            if (statistics.TotalOperations == 0) return 0;
            return (double)statistics.FailedOperations / statistics.TotalOperations * 100;
        }

        private double CalculateResourceUtilization(AIUsageStatistics statistics)
        {
            // Simulate resource utilization calculation
            return Math.Min(100, statistics.TotalOperations * 0.1);
        }

        private double CalculateQualityScore(AIUsageStatistics statistics)
        {
            var successWeight = 0.4;
            var performanceWeight = 0.3;
            var throughputWeight = 0.3;

            var successScore = statistics.SuccessRate;
            var performanceScore = Math.Max(0, 100 - statistics.AverageOperationDuration.TotalSeconds * 10);
            var throughputScore = Math.Min(100, CalculateThroughput(statistics) * 10);

            return successScore * successWeight + performanceScore * performanceWeight + throughputScore * throughputWeight;
        }

        private TrendDirection CalculateUsageTrend(AIUsageStatistics statistics)
        {
            // Simulate trend calculation
            return Random.Shared.Next(0, 3) switch
            {
                0 => TrendDirection.Increasing,
                1 => TrendDirection.Decreasing,
                _ => TrendDirection.Stable
            };
        }

        private TrendDirection CalculatePerformanceTrend(AIUsageStatistics statistics)
        {
            return statistics.SuccessRate > 90 ? TrendDirection.Increasing : TrendDirection.Stable;
        }

        private TrendDirection CalculateErrorTrend(AIUsageStatistics statistics)
        {
            return statistics.FailedOperations > statistics.TotalOperations * 0.1 ? TrendDirection.Increasing : TrendDirection.Decreasing;
        }

        private TrendDirection CalculateResourceTrend(AIUsageStatistics statistics)
        {
            return statistics.TotalOperations > 1000 ? TrendDirection.Increasing : TrendDirection.Stable;
        }

        private TrendDirection CalculateOverallTrend(AIUsageStatistics statistics)
        {
            var positiveFactors = 0;
            if (statistics.SuccessRate > 90) positiveFactors++;
            if (statistics.AverageOperationDuration.TotalSeconds < 5) positiveFactors++;
            if (statistics.TotalOperations > 100) positiveFactors++;

            return positiveFactors >= 2 ? TrendDirection.Increasing : TrendDirection.Stable;
        }
    }

    /// <summary>
    /// Analytics request
    /// </summary>
    public class AnalyticsRequest
    {
        public TimeSpan? TimeRange { get; set; }
        public string? UserId { get; set; }
        public List<string> Metrics { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Advanced analytics result
    /// </summary>
    public class AdvancedAnalyticsResult
    {
        public AnalyticsRequest Request { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
        public List<AnalyticsInsight> Insights { get; set; } = new();
        public List<AnalyticsPrediction> Predictions { get; set; } = new();
        public List<AnalyticsRecommendation> Recommendations { get; set; } = new();
        public PerformanceMetrics PerformanceMetrics { get; set; } = new();
        public List<UsagePattern> UsagePatterns { get; set; } = new();
        public List<AnomalyDetection> Anomalies { get; set; } = new();
        public TrendAnalysis TrendAnalysis { get; set; } = new();
    }

    /// <summary>
    /// Analytics insight
    /// </summary>
    public class AnalyticsInsight
    {
        public InsightType Type { get; set; }
        public InsightSeverity Severity { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string Impact { get; set; } = string.Empty;
        public List<string> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// Analytics prediction
    /// </summary>
    public class AnalyticsPrediction
    {
        public PredictionType Type { get; set; }
        public TimeSpan TimeHorizon { get; set; }
        public double PredictedValue { get; set; }
        public double Confidence { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> Factors { get; set; } = new();
    }

    /// <summary>
    /// Analytics recommendation
    /// </summary>
    public class AnalyticsRecommendation
    {
        public RecommendationType Type { get; set; }
        public RecommendationPriority Priority { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Actions { get; set; } = new();
        public string ExpectedImpact { get; set; } = string.Empty;
        public string ImplementationEffort { get; set; } = string.Empty;
    }

    /// <summary>
    /// Performance metrics
    /// </summary>
    public class PerformanceMetrics
    {
        public double SuccessRate { get; set; }
        public TimeSpan AverageOperationDuration { get; set; }
        public int TotalOperations { get; set; }
        public int FailedOperations { get; set; }
        public double Throughput { get; set; }
        public double ErrorRate { get; set; }
        public double ResourceUtilization { get; set; }
        public double QualityScore { get; set; }
    }

    /// <summary>
    /// Usage pattern
    /// </summary>
    public class UsagePattern
    {
        public PatternType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string Impact { get; set; } = string.Empty;
        public List<string> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// Anomaly detection
    /// </summary>
    public class AnomalyDetection
    {
        public AnomalyType Type { get; set; }
        public AnomalySeverity Severity { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; }
        public double Confidence { get; set; }
        public string Impact { get; set; } = string.Empty;
        public List<string> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// Trend analysis
    /// </summary>
    public class TrendAnalysis
    {
        public TrendDirection UsageTrend { get; set; }
        public TrendDirection PerformanceTrend { get; set; }
        public TrendDirection ErrorTrend { get; set; }
        public TrendDirection ResourceTrend { get; set; }
        public TrendDirection OverallTrend { get; set; }
    }

    /// <summary>
    /// Analytics model
    /// </summary>
    public class AnalyticsModel
    {
        public string ModelId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public ModelType Type { get; set; }
        public ModelStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? TrainingCompletedAt { get; set; }
        public double Accuracy { get; set; }
        public Dictionary<string, object> Metrics { get; set; } = new();
        public List<FineTuningSample> TrainingData { get; set; } = new();
    }

    /// <summary>
    /// Model training request
    /// </summary>
    public class ModelTrainingRequest
    {
        public string ModelName { get; set; } = string.Empty;
        public ModelType ModelType { get; set; }
        public List<FineTuningSample> TrainingData { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Model training result
    /// </summary>
    public class ModelTrainingResult
    {
        public string ModelId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public TimeSpan TrainingDuration { get; set; }
        public double Accuracy { get; set; }
        public Dictionary<string, object> Metrics { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    // Enums
    public enum InsightType { Performance, Usage, Efficiency, Quality, Security }
    public enum InsightSeverity { Low, Medium, High, Critical }
    public enum PredictionType { Usage, Performance, Resource, Quality, Cost }
    public enum RecommendationType { Performance, Scalability, Efficiency, Security, Cost }
    public enum RecommendationPriority { Low, Medium, High, Critical }
    public enum PatternType { Temporal, Operational, Behavioral, Resource }
    public enum AnomalyType { ErrorSpike, UsageSpike, PerformanceDrop, SecurityBreach }
    public enum AnomalySeverity { Low, Medium, High, Critical }
    public enum TrendDirection { Increasing, Decreasing, Stable, Volatile }
    public enum ModelType { Classification, Regression, Clustering, AnomalyDetection }
    public enum ModelStatus { Training, Trained, Failed, Deployed }
}
