using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities;
using Nexo.Core.Domain.Values;

namespace Nexo.Core.Application.Commands.Sprint
{
    /// <summary>
    /// Command to calculate sprint metrics (replaces SprintMetrics class functionality)
    /// </summary>
    public class CalculateSprintMetricsCommand : BaseCommand<SprintMetricsInput, SprintMetricsOutput>
    {
        public override string CommandId => "sprint.calculate-metrics";
        public override string CommandName => "Calculate Sprint Metrics";
        public override string Description => "Calculates various metrics for a sprint including completion percentage, story points, and task counts";

        public override Task<CommandResult<SprintMetricsOutput>> ExecuteAsync(SprintMetricsInput input, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                var metrics = new SprintMetricsOutput();

                // Calculate total story points
                metrics.TotalStoryPoints = input.Tasks.Sum(t => t.StoryPoints);

                // Calculate completed story points
                metrics.CompletedStoryPoints = input.Tasks
                    .Where(t => t.Status == Nexo.Core.Domain.Values.TaskStatus.Done)
                    .Sum(t => t.StoryPoints);

                // Calculate completion percentage
                metrics.CompletionPercentage = metrics.TotalStoryPoints > 0 
                    ? (double)metrics.CompletedStoryPoints / metrics.TotalStoryPoints * 100 
                    : 0;

                // Calculate task counts by status
                metrics.TaskCountsByStatus = input.Tasks
                    .GroupBy(t => t.Status)
                    .ToDictionary(g => g.Key.Name, g => g.Count());

                // Calculate total tasks
                metrics.TotalTasks = input.Tasks.Count();

                // Calculate completed tasks
                metrics.CompletedTasks = input.Tasks.Count(t => t.Status == Nexo.Core.Domain.Values.TaskStatus.Done);

                // Calculate duration if dates are provided
                if (input.StartDate != default && input.EndDate.HasValue)
                {
                    metrics.DurationDays = (input.EndDate.Value - input.StartDate).TotalDays;
                }

                // Add any warnings
                var warnings = new List<string>();
                if (metrics.CompletionPercentage < 50 && input.StartDate != default && input.EndDate.HasValue)
                {
                    var daysRemaining = (input.EndDate.Value - DateTime.UtcNow).TotalDays;
                    if (daysRemaining < 3)
                    {
                        warnings.Add("Sprint is behind schedule with less than 3 days remaining");
                    }
                }

                return Task.FromResult(CreateSuccessResult(metrics, startTime, warnings.ToArray()));
            }
            catch (Exception ex)
            {
                return Task.FromResult(CreateFailureResult($"Failed to calculate sprint metrics: {ex.Message}", startTime));
            }
        }

        protected override bool ValidateInput(SprintMetricsInput input)
        {
            return input != null && input.Tasks != null;
        }
    }

    /// <summary>
    /// Input for sprint metrics calculation
    /// </summary>
    public class SprintMetricsInput
    {
        public IEnumerable<SprintTask> Tasks { get; set; } = Enumerable.Empty<SprintTask>();
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SprintId { get; set; }
    }

    /// <summary>
    /// Output for sprint metrics calculation
    /// </summary>
    public class SprintMetricsOutput
    {
        public int TotalStoryPoints { get; set; }
        public int CompletedStoryPoints { get; set; }
        public double CompletionPercentage { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public double DurationDays { get; set; }
        public Dictionary<string, int> TaskCountsByStatus { get; set; } = new Dictionary<string, int>();
    }
}
