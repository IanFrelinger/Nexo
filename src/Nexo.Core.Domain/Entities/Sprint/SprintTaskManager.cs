using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Core.Domain.Values;
using Nexo.Core.Domain.ValueObjects;

namespace Nexo.Core.Domain.Entities.Sprint
{
    /// <summary>
    /// Manages sprint tasks and their operations
    /// </summary>
    public sealed class SprintTaskManager
    {
        private readonly List<SprintTask> _tasks = [];

        /// <summary>
        /// Gets the list of tasks associated with the sprint.
        /// </summary>
        public IReadOnlyList<SprintTask> Tasks => _tasks.AsReadOnly();

        /// <summary>
        /// Adds a task to the sprint.
        /// </summary>
        /// <param name="task">The task to add</param>
        /// <exception cref="ArgumentNullException">Thrown when task is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when task is already in the sprint</exception>
        public void AddTask(SprintTask task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            if (_tasks.Contains(task))
                throw new InvalidOperationException("Task is already in the sprint");

            _tasks.Add(task);
        }

        /// <summary>
        /// Removes a task from the sprint.
        /// </summary>
        /// <param name="task">The task to remove</param>
        /// <returns>True if the task was removed, false if it was not found</returns>
        public bool RemoveTask(SprintTask task)
        {
            return _tasks.Remove(task);
        }

        /// <summary>
        /// Gets a task by its identifier.
        /// </summary>
        /// <param name="taskId">The task identifier</param>
        /// <returns>The task if found, null otherwise</returns>
        public SprintTask? GetTaskById(string taskId)
        {
            return _tasks.FirstOrDefault(t => t.Id == taskId);
        }

        /// <summary>
        /// Gets tasks by status.
        /// </summary>
        /// <param name="status">The task status</param>
        /// <returns>A collection of tasks with the specified status</returns>
        public IEnumerable<SprintTask> GetTasksByStatus(Values.TaskStatus status)
        {
            return _tasks.Where(t => t.Status == status);
        }

        /// <summary>
        /// Gets tasks by assignee.
        /// </summary>
        /// <param name="assigneeId">The assignee identifier</param>
        /// <returns>A collection of tasks assigned to the specified assignee</returns>
        public IEnumerable<SprintTask> GetTasksByAssignee(string assigneeId)
        {
            return _tasks.Where(t => t.AssigneeId == assigneeId);
        }

        /// <summary>
        /// Gets the total number of tasks.
        /// </summary>
        /// <returns>The total number of tasks</returns>
        public int GetTaskCount()
        {
            return _tasks.Count;
        }

        /// <summary>
        /// Gets the total story points for all tasks.
        /// </summary>
        /// <returns>The total story points</returns>
        public int GetTotalStoryPoints()
        {
            return _tasks.Sum(t => t.StoryPoints);
        }

        /// <summary>
        /// Gets the total story points for completed tasks.
        /// </summary>
        /// <returns>The total story points for completed tasks</returns>
        public int GetCompletedStoryPoints()
        {
            return _tasks.Where(t => t.Status == Values.TaskStatus.Done).Sum(t => t.StoryPoints);
        }

        /// <summary>
        /// Gets the completion percentage based on story points.
        /// </summary>
        /// <returns>The completion percentage (0-100)</returns>
        public double GetCompletionPercentage()
        {
            var totalStoryPoints = GetTotalStoryPoints();
            if (totalStoryPoints == 0) return 0;

            var completedStoryPoints = GetCompletedStoryPoints();
            return (double)completedStoryPoints / totalStoryPoints * 100;
        }

        /// <summary>
        /// Gets tasks by priority.
        /// </summary>
        /// <param name="priority">The task priority</param>
        /// <returns>A collection of tasks with the specified priority</returns>
        public IEnumerable<SprintTask> GetTasksByPriority(TaskPriority priority)
        {
            return _tasks.Where(t => t.Priority == priority);
        }

        /// <summary>
        /// Gets all task statuses and their counts.
        /// </summary>
        /// <returns>A dictionary of status to count</returns>
        public Dictionary<Values.TaskStatus, int> GetTaskStatusCounts()
        {
            return _tasks.GroupBy(t => t.Status)
                         .ToDictionary(g => g.Key, g => g.Count());
        }
    }
}
