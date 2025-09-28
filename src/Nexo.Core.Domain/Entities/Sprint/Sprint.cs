using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Core.Domain.Values;
using Nexo.Core.Domain.ValueObjects;

namespace Nexo.Core.Domain.Entities.Sprint
{
    /// <summary>
    /// Represents a Sprint, a time-boxed iteration during which a team works on a defined set of tasks.
    /// </summary>
    public sealed class Sprint
    {
        private readonly SprintTaskManager _taskManager;
        private readonly SprintMetrics _metrics;
        private readonly List<string> _definitionOfDone = [];

        /// <summary>
        /// Gets the unique identifier of the sprint instance.
        /// </summary>
        public SprintId Id { get; private set; }

        /// <summary>
        /// Gets or sets the goal of the sprint, which defines the overarching objective or purpose
        /// that the sprint aims to achieve.
        /// </summary>
        public string Goal { get; private set; }

        /// <summary>
        /// Gets the list of tasks associated with the sprint.
        /// </summary>
        public IReadOnlyList<SprintTask> Tasks => _taskManager.Tasks;

        /// <summary>
        /// Gets the list of criteria that define the completion and quality standards
        /// for the sprint, known as the Definition of Done (DoD).
        /// </summary>
        public IReadOnlyList<string> DefinitionOfDone => _definitionOfDone.AsReadOnly();

        /// <summary>
        /// Gets or sets the start date of the sprint.
        /// </summary>
        public DateTime StartDate { get; private set; }

        /// <summary>
        /// Gets or sets the end date of the sprint.
        /// </summary>
        public DateTime? EndDate { get; private set; }

        /// <summary>
        /// Gets or sets the current status of the sprint.
        /// </summary>
        public SprintStatus Status { get; private set; }

        /// <summary>
        /// Gets or sets the team identifier associated with the sprint.
        /// </summary>
        public string? TeamId { get; private set; }

        /// <summary>
        /// Gets or sets the sprint number within the project.
        /// </summary>
        public int SprintNumber { get; private set; }

        /// <summary>
        /// Gets or sets the project identifier this sprint belongs to.
        /// </summary>
        public string? ProjectId { get; private set; }

        /// <summary>
        /// Gets the metrics associated with the sprint.
        /// </summary>
        public IReadOnlyDictionary<string, object> Metrics => _metrics.GetAllMetrics();

        /// <summary>
        /// Initializes a new instance of the Sprint class.
        /// </summary>
        /// <param name="id">The unique identifier for the sprint</param>
        /// <param name="goal">The goal of the sprint</param>
        /// <param name="startDate">The start date of the sprint</param>
        /// <param name="endDate">The end date of the sprint</param>
        /// <param name="teamId">The team identifier</param>
        /// <param name="sprintNumber">The sprint number</param>
        /// <param name="projectId">The project identifier</param>
        public Sprint(SprintId id, string goal, DateTime startDate, DateTime? endDate = null, 
                     string? teamId = null, int sprintNumber = 1, string? projectId = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Goal = goal ?? throw new ArgumentNullException(nameof(goal));
            StartDate = startDate;
            EndDate = endDate;
            TeamId = teamId;
            SprintNumber = sprintNumber;
            ProjectId = projectId;
            Status = SprintStatus.Planning;
            
            _taskManager = new SprintTaskManager();
            _metrics = new SprintMetrics();
        }

        /// <summary>
        /// Adds a task to the sprint.
        /// </summary>
        /// <param name="task">The task to add</param>
        public void AddTask(SprintTask task)
        {
            _taskManager.AddTask(task);
            UpdateMetrics();
        }

        /// <summary>
        /// Removes a task from the sprint.
        /// </summary>
        /// <param name="task">The task to remove</param>
        /// <returns>True if the task was removed, false if it was not found</returns>
        public bool RemoveTask(SprintTask task)
        {
            var removed = _taskManager.RemoveTask(task);
            if (removed)
            {
                UpdateMetrics();
            }
            return removed;
        }

        /// <summary>
        /// Adds a criterion to the Definition of Done.
        /// </summary>
        /// <param name="criterion">The criterion to add</param>
        public void AddDefinitionOfDoneCriterion(string criterion)
        {
            if (!string.IsNullOrWhiteSpace(criterion) && !_definitionOfDone.Contains(criterion))
            {
                _definitionOfDone.Add(criterion);
            }
        }

        /// <summary>
        /// Removes a criterion from the Definition of Done.
        /// </summary>
        /// <param name="criterion">The criterion to remove</param>
        /// <returns>True if the criterion was removed, false if it was not found</returns>
        public bool RemoveDefinitionOfDoneCriterion(string criterion)
        {
            return _definitionOfDone.Remove(criterion);
        }

        /// <summary>
        /// Updates the sprint status.
        /// </summary>
        /// <param name="newStatus">The new status</param>
        public void UpdateStatus(SprintStatus newStatus)
        {
            Status = newStatus;
            UpdateMetrics();
        }

        /// <summary>
        /// Gets the total story points for all tasks in the sprint.
        /// </summary>
        /// <returns>The sum of story points for all tasks in the sprint</returns>
        public int GetTotalStoryPoints()
        {
            return _taskManager.GetTotalStoryPoints();
        }

        /// <summary>
        /// Gets the total story points for completed tasks.
        /// </summary>
        /// <returns>The sum of story points for completed tasks</returns>
        public int GetCompletedStoryPoints()
        {
            return _taskManager.GetCompletedStoryPoints();
        }

        /// <summary>
        /// Gets the completion percentage of the sprint.
        /// </summary>
        /// <returns>The completion percentage (0-100)</returns>
        public double GetCompletionPercentage()
        {
            return _taskManager.GetCompletionPercentage();
        }

        /// <summary>
        /// Gets tasks by status.
        /// </summary>
        /// <param name="status">The task status</param>
        /// <returns>A collection of tasks with the specified status</returns>
        public IEnumerable<SprintTask> GetTasksByStatus(Values.TaskStatus status)
        {
            return _taskManager.GetTasksByStatus(status);
        }

        /// <summary>
        /// Gets tasks by assignee.
        /// </summary>
        /// <param name="assigneeId">The assignee identifier</param>
        /// <returns>A collection of tasks assigned to the specified assignee</returns>
        public IEnumerable<SprintTask> GetTasksByAssignee(string assigneeId)
        {
            return _taskManager.GetTasksByAssignee(assigneeId);
        }

        /// <summary>
        /// Updates the sprint metrics based on current state.
        /// </summary>
        private void UpdateMetrics()
        {
            _metrics.UpdateMetrics(_taskManager.Tasks, StartDate, EndDate);
        }
    }
}
