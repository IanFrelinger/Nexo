using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Models.Collaboration;

namespace Nexo.Core.Application.Interfaces.Collaboration
{
    /// <summary>
    /// Interface for team collaboration service in Phase 9.
    /// Provides team-based feature development and collaboration workflows.
    /// </summary>
    public interface ITeamCollaborationService
    {
        /// <summary>
        /// Implements team-based feature development.
        /// </summary>
        /// <param name="teamConfig">The team configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Team development result</returns>
        Task<TeamDevelopmentResult> ImplementTeamBasedDevelopmentAsync(
            TeamConfiguration teamConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates collaboration workflows.
        /// </summary>
        /// <param name="workflowConfig">The workflow configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Workflow creation result</returns>
        Task<WorkflowCreationResult> CreateCollaborationWorkflowsAsync(
            WorkflowConfiguration workflowConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds team analytics and reporting.
        /// </summary>
        /// <param name="analyticsConfig">The analytics configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Analytics implementation result</returns>
        Task<AnalyticsImplementationResult> AddTeamAnalyticsAsync(
            AnalyticsConfiguration analyticsConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates team optimization features.
        /// </summary>
        /// <param name="optimizationConfig">The optimization configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Optimization implementation result</returns>
        Task<OptimizationImplementationResult> CreateTeamOptimizationFeaturesAsync(
            OptimizationConfiguration optimizationConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets team collaboration metrics.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collaboration metrics</returns>
        Task<CollaborationMetrics> GetCollaborationMetricsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates team performance dashboard.
        /// </summary>
        /// <param name="dashboardConfig">The dashboard configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dashboard creation result</returns>
        Task<DashboardCreationResult> CreateTeamPerformanceDashboardAsync(
            DashboardConfiguration dashboardConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Implements team communication features.
        /// </summary>
        /// <param name="communicationConfig">The communication configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Communication implementation result</returns>
        Task<CommunicationImplementationResult> ImplementTeamCommunicationAsync(
            CommunicationConfiguration communicationConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates team knowledge sharing system.
        /// </summary>
        /// <param name="knowledgeConfig">The knowledge configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Knowledge sharing result</returns>
        Task<KnowledgeSharingResult> CreateTeamKnowledgeSharingAsync(
            KnowledgeConfiguration knowledgeConfig,
            CancellationToken cancellationToken = default);
    }
}
