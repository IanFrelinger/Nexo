using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Agent.Models;

namespace Nexo.Feature.Agent.Services
{
    /// <summary>
    /// Collaboration pattern analysis functionality
    /// </summary>
    public partial class MultiAgentCoordinator
    {
        /// <summary>
        /// Analyzes collaboration patterns and provides insights, including agent collaboration patterns,
        /// session performance metrics, and relevant recommendations.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous operation, containing the result of the collaboration analysis.</returns>
        public Task<CollaborationAnalysisResult> AnalyzeCollaborationPatternsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Analyzing collaboration patterns");

            try
            {
                var analysis = new CollaborationAnalysisResult
                {
                    Success = true,
                    AnalysisTimestamp = DateTime.UtcNow,
                    ActiveSessionsCount = _activeSessions.Count(s => s.Status == CollaborationSessionStatus.Active),
                    CompletedSessionsCount = _activeSessions.Count(s => s.Status == CollaborationSessionStatus.Completed),
                    RegisteredAgentsCount = _registeredAgents.Count,
                    // Analyze agent collaboration patterns
                    AgentCollaborationPatterns = AnalyzeAgentCollaborationPatterns(),
                    // Analyze session performance
                    SessionPerformanceMetrics = AnalyzeSessionPerformance()
                };

                // Generate collaboration recommendations
                analysis.Recommendations = GenerateCollaborationRecommendations(analysis);

                return Task.FromResult(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing collaboration patterns");
                return Task.FromResult(new CollaborationAnalysisResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        /// <summary>
        /// Analyzes collaboration patterns among agents by evaluating their participation in completed collaboration sessions.
        /// </summary>
        /// <returns>A list of collaboration patterns, detailing the collaboration frequency and count for each agent.</returns>
        private List<AgentCollaborationPattern> AnalyzeAgentCollaborationPatterns()
        {
            // Analyze which agents work well together
            var agentCollaborations = _activeSessions
                .Where(s => s.Status == CollaborationSessionStatus.Completed)
                .SelectMany(s => s.ParticipatingAgents)
                .GroupBy(a => a.Id.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            return agentCollaborations.Select(kvp => new AgentCollaborationPattern { AgentId = kvp.Key, CollaborationCount = kvp.Value, CollaborationFrequency = (double)kvp.Value / _activeSessions.Count(s => s.Status == CollaborationSessionStatus.Completed) }).ToList();
        }

        /// <summary>
        /// Analyzes the performance metrics of completed collaboration sessions.
        /// </summary>
        /// <returns>
        /// A <see cref="SessionPerformanceMetrics"/> object containing average session duration, average number of agents per session,
        /// and the success rate of completed sessions.
        /// </returns>
        private SessionPerformanceMetrics AnalyzeSessionPerformance()
        {
            var completedSessions = _activeSessions.Where(s => s.Status == CollaborationSessionStatus.Completed).ToList();
            
            if (!completedSessions.Any())
            {
                return new SessionPerformanceMetrics();
            }

            return new SessionPerformanceMetrics
            {
                AverageSessionDuration = completedSessions
                    .Where(s => s.CompletedAt.HasValue)
                    .Average(s => (s.CompletedAt!.Value - s.CreatedAt).TotalMilliseconds),
                AverageAgentsPerSession = completedSessions.Average(s => s.ParticipatingAgents.Count),
                SuccessRate = (double)completedSessions.Count(s => s.Status == CollaborationSessionStatus.Completed) / completedSessions.Count
            };
        }

        /// <summary>
        /// Generates a list of collaboration recommendations based on the analysis of agent collaboration patterns
        /// and session performance metrics.
        /// </summary>
        /// <param name="analysis">The results of the collaboration analysis containing agent patterns and performance metrics.</param>
        /// <returns>A list of recommendations to optimize agent collaboration and session performance.</returns>
        private List<CollaborationRecommendation> GenerateCollaborationRecommendations(CollaborationAnalysisResult analysis)
        {
            var recommendations = new List<CollaborationRecommendation>();

            // Recommend optimal agent combinations
            if (analysis.AgentCollaborationPatterns.Any())
            {
                var topCollaborators = analysis.AgentCollaborationPatterns
                    .OrderByDescending(p => p.CollaborationFrequency)
                    .Take(3);

                recommendations.Add(new CollaborationRecommendation
                {
                    Type = RecommendationType.AgentCombination,
                    Description = $"Consider pairing agents: {string.Join(", ", topCollaborators.Select(p => p.AgentId))}",
                    Priority = RecommendationPriority.Medium,
                    EstimatedImpact = "High"
                });
            }

            // Recommend session optimization
            if (analysis.SessionPerformanceMetrics.AverageSessionDuration > 300000) // 5 minutes
            {
                recommendations.Add(new CollaborationRecommendation
                {
                    Type = RecommendationType.Performance,
                    Description = "Consider optimizing session duration by reducing agent count or simplifying tasks",
                    Priority = RecommendationPriority.High,
                    EstimatedImpact = "Medium"
                });
            }

            return recommendations;
        }
    }
}
