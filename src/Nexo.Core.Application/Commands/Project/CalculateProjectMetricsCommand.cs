using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities;
using Nexo.Core.Domain.Values;

namespace Nexo.Core.Application.Commands.Project
{
    /// <summary>
    /// Command to calculate project metrics (replaces Project metrics calculation methods)
    /// </summary>
    public class CalculateProjectMetricsCommand : BaseCommand<CalculateProjectMetricsInput, CalculateProjectMetricsOutput>
    {
        public override string CommandId => "project.calculate-metrics";
        public override string CommandName => "Calculate Project Metrics";
        public override string Description => "Calculates various metrics for a project including progress, agent statistics, and health";

        public override Task<CommandResult<CalculateProjectMetricsOutput>> ExecuteAsync(CalculateProjectMetricsInput input, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                var metrics = new CalculateProjectMetricsOutput
                {
                    ProjectId = input.ProjectId,
                    CalculatedAt = DateTime.UtcNow
                };

                // Calculate basic project metrics
                metrics.TotalAgents = input.Project.Agents.Count;
                metrics.ActiveAgents = input.Project.Agents.Count(a => a.Status == AgentStatus.Active);
                metrics.InactiveAgents = input.Project.Agents.Count(a => a.Status == AgentStatus.Inactive);

                // Calculate project health based on status
                metrics.HealthScore = CalculateHealthScore(input.Project.Status);

                // Calculate progress based on status
                metrics.ProgressPercentage = CalculateProgressPercentage(input.Project.Status);

                // Calculate project duration
                if (input.Project.CreatedAt != default)
                {
                    metrics.DurationDays = (DateTime.UtcNow - input.Project.CreatedAt).TotalDays;
                }

                // Calculate agent distribution by role
                metrics.AgentDistribution = input.Project.Agents
                    .GroupBy(a => a.Role?.ToString() ?? "Unknown")
                    .ToDictionary(g => g.Key, g => g.Count());

                // Calculate metadata statistics
                metrics.MetadataCount = input.Project.Metadata?.Count ?? 0;

                // Add warnings if needed
                var warnings = new List<string>();
                if (metrics.ActiveAgents == 0 && input.Project.Status == ProjectStatus.Active)
                {
                    warnings.Add("Project is active but has no active agents");
                }

                if (metrics.HealthScore < 0.5)
                {
                    warnings.Add("Project health score is below 50%");
                }

                if (metrics.DurationDays > 365)
                {
                    warnings.Add("Project has been running for over a year");
                }

                return Task.FromResult(CreateSuccessResult(metrics, startTime, warnings.ToArray()));
            }
            catch (Exception ex)
            {
                return Task.FromResult(CreateFailureResult($"Failed to calculate project metrics: {ex.Message}", startTime));
            }
        }

        protected override bool ValidateInput(CalculateProjectMetricsInput input)
        {
            return input != null && 
                   !string.IsNullOrWhiteSpace(input.ProjectId) && 
                   input.Project != null;
        }

        private double CalculateHealthScore(ProjectStatus status)
        {
            if (status == ProjectStatus.Active) return 1.0;
            if (status == ProjectStatus.Running) return 0.9;
            if (status == ProjectStatus.Initialized) return 0.7;
            if (status == ProjectStatus.Planning) return 0.6;
            if (status == ProjectStatus.OnHold) return 0.3;
            if (status == ProjectStatus.Stopped) return 0.2;
            if (status == ProjectStatus.Failed) return 0.1;
            if (status == ProjectStatus.Completed) return 0.8;
            if (status == ProjectStatus.Cancelled) return 0.0;
            return 0.5;
        }

        private double CalculateProgressPercentage(ProjectStatus status)
        {
            if (status == ProjectStatus.NotInitialized) return 0.0;
            if (status == ProjectStatus.Planning) return 0.1;
            if (status == ProjectStatus.Initialized) return 0.2;
            if (status == ProjectStatus.Active) return 0.5;
            if (status == ProjectStatus.Running) return 0.8;
            if (status == ProjectStatus.Completed) return 1.0;
            if (status == ProjectStatus.Cancelled) return 0.0;
            if (status == ProjectStatus.Failed) return 0.0;
            return 0.0;
        }
    }

}
