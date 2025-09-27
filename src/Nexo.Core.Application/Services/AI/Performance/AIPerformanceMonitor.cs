using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Performance
{
    /// <summary>
    /// Real AI performance monitoring service for tracking and optimizing AI operations.
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public partial class AIPerformanceMonitor
    {
        private readonly ILogger<AIPerformanceMonitor> _logger;
        private readonly Dictionary<string, PerformanceMetrics> _activeOperations;
        private readonly List<PerformanceMetrics> _historicalMetrics;
        private readonly object _lockObject = new object();

        public AIPerformanceMonitor(ILogger<AIPerformanceMonitor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _activeOperations = new Dictionary<string, PerformanceMetrics>();
            _historicalMetrics = new List<PerformanceMetrics>();
        }

        /// <summary>
        /// Starts performance monitoring for an AI operation.
        /// </summary>
        public Task<PerformanceMetrics> StartOperationAsync(string operationId, AIOperationType operationType, AIProviderType providerType, AIEngineType engineType)
        {
            return StartOperationInternalAsync(operationId, operationType, providerType, engineType);
        }

        /// <summary>
        /// Ends performance monitoring for an AI operation.
        /// </summary>
        public Task<PerformanceMetrics> EndOperationAsync(string operationId, bool success, string? errorMessage = null)
        {
            return EndOperationInternalAsync(operationId, success, errorMessage);
        }

        /// <summary>
        /// Gets performance metrics for a specific operation.
        /// </summary>
        public Task<PerformanceMetrics> GetOperationMetricsAsync(string operationId)
        {
            return GetOperationMetricsInternalAsync(operationId);
        }

        /// <summary>
        /// Gets historical performance metrics with optional filtering.
        /// </summary>
        public Task<List<PerformanceMetrics>> GetHistoricalMetricsAsync(TimeSpan? timeRange = null, AIOperationType? operationType = null, AIProviderType? providerType = null)
        {
            return GetHistoricalMetricsInternalAsync(timeRange, operationType, providerType);
        }

        /// <summary>
        /// Calculates performance statistics for AI operations.
        /// </summary>
        public Task<PerformanceStatistics> GetPerformanceStatisticsAsync(TimeSpan? timeRange = null)
        {
            return GetPerformanceStatisticsInternalAsync(timeRange);
        }

        /// <summary>
        /// Generates performance recommendations based on current metrics.
        /// </summary>
        public Task<List<PerformanceRecommendation>> GetPerformanceRecommendationsAsync()
        {
            return GetPerformanceRecommendationsInternalAsync();
        }
    }
}