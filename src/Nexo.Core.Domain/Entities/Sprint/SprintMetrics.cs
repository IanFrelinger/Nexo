using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Core.Domain.Values;

namespace Nexo.Core.Domain.Entities.Sprint
{
    /// <summary>
    /// Handles sprint metrics calculation and management
    /// </summary>
    public sealed class SprintMetrics
    {
        private readonly Dictionary<string, object> _metrics = new();

        /// <summary>
        /// Calculates and returns the total story points for all tasks in the sprint.
        /// </summary>
        /// <param name="tasks">The collection of tasks in the sprint</param>
        /// <returns>The sum of story points for all tasks in the sprint</returns>
        public int GetTotalStoryPoints(IEnumerable<SprintTask> tasks)
        {
            return tasks.Sum(t => t.StoryPoints);
        }

        /// <summary>
        /// Calculates and returns the sum of story points for all tasks with a status of "Done".
        /// </summary>
        /// <param name="tasks">The collection of tasks in the sprint</param>
        /// <returns>The sum of the story points for all tasks with a status of "Done"</returns>
        public int GetCompletedStoryPoints(IEnumerable<SprintTask> tasks)
        {
            return tasks.Where(t => t.Status == Values.TaskStatus.Done).Sum(t => t.StoryPoints);
        }

        /// <summary>
        /// Calculates and returns the completion percentage of the sprint based on the story points of completed tasks.
        /// </summary>
        /// <param name="tasks">The collection of tasks in the sprint</param>
        /// <returns>The completion percentage as a double. Returns 0 if there are no story points in the sprint</returns>
        public double GetCompletionPercentage(IEnumerable<SprintTask> tasks)
        {
            var totalStoryPoints = GetTotalStoryPoints(tasks);
            if (totalStoryPoints == 0) return 0;

            var completedStoryPoints = GetCompletedStoryPoints(tasks);
            return (double)completedStoryPoints / totalStoryPoints * 100;
        }

        /// <summary>
        /// Calculates and returns the number of tasks with a specific status.
        /// </summary>
        /// <param name="tasks">The collection of tasks in the sprint</param>
        /// <param name="status">The status to count</param>
        /// <returns>The number of tasks with the specified status</returns>
        public int GetTaskCountByStatus(IEnumerable<SprintTask> tasks, Values.TaskStatus status)
        {
            return tasks.Count(t => t.Status == status);
        }

        /// <summary>
        /// Calculates and returns the number of completed tasks.
        /// </summary>
        /// <param name="tasks">The collection of tasks in the sprint</param>
        /// <returns>The number of completed tasks</returns>
        public int GetCompletedTaskCount(IEnumerable<SprintTask> tasks)
        {
            return GetTaskCountByStatus(tasks, Values.TaskStatus.Done);
        }

        /// <summary>
        /// Sets a metric value.
        /// </summary>
        /// <param name="key">The metric key</param>
        /// <param name="value">The metric value</param>
        public void SetMetric(string key, object value)
        {
            _metrics[key] = value;
        }

        /// <summary>
        /// Gets a metric value.
        /// </summary>
        /// <param name="key">The metric key</param>
        /// <returns>The metric value, or null if not found</returns>
        public object? GetMetric(string key)
        {
            return _metrics.TryGetValue(key, out var value) ? value : null;
        }

        /// <summary>
        /// Gets all metrics.
        /// </summary>
        /// <returns>A dictionary of all metrics</returns>
        public IReadOnlyDictionary<string, object> GetAllMetrics()
        {
            return _metrics.AsReadOnly();
        }

        /// <summary>
        /// Updates sprint metrics based on current task state.
        /// </summary>
        /// <param name="tasks">The collection of tasks in the sprint</param>
        /// <param name="startDate">The sprint start date</param>
        /// <param name="endDate">The sprint end date</param>
        public void UpdateMetrics(IEnumerable<SprintTask> tasks, DateTime startDate, DateTime? endDate)
        {
            SetMetric("TotalStoryPoints", GetTotalStoryPoints(tasks));
            SetMetric("CompletedStoryPoints", GetCompletedStoryPoints(tasks));
            SetMetric("CompletionPercentage", GetCompletionPercentage(tasks));
            SetMetric("TotalTasks", tasks.Count());
            SetMetric("CompletedTasks", GetCompletedTaskCount(tasks));
            
            if (startDate != default && endDate.HasValue)
            {
                SetMetric("DurationDays", (endDate.Value - startDate).TotalDays);
            }
        }
    }
}
