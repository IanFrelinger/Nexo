using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Nexo.Core.Domain.Enums;
using Nexo.Core.Domain.ValueObjects;
using TaskStatus = Nexo.Core.Domain.Enums.TaskStatus;

namespace Nexo.Core.Domain.Entities
{
    /// <summary>
    /// Represents a Sprint, a time-boxed iteration during which a team works on a defined set of tasks.
    /// </summary>
    public sealed class Sprint
    {
        /// <summary>
        /// Stores the collection of tasks planned within the sprint.
        /// </summary>
        private readonly List<SprintTask> _tasks = [];

        /// <summary>
        /// Represents the list of criteria that define when tasks or the sprint itself are considered complete.
        /// This ensures a shared understanding of the quality standards and expectations for the sprint.
        /// </summary>
        private readonly List<string> _definitionOfDone = [];

        /// <summary>
        /// Holds key-value pairs representing metrics associated with the sprint.
        /// </summary>
        private readonly Dictionary<string, object> _metrics = new();

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
        public IReadOnlyList<SprintTask> Tasks => _tasks.AsReadOnly();

        /// <summary>
        /// Gets the list of criteria that define the completion and quality standards
        /// for the sprint, known as the Definition of Done (DoD).
        /// </summary>
        public IReadOnlyList<string> DefinitionOfDone => _definitionOfDone.AsReadOnly();

        /// <summary>
        /// Gets or sets the total available capacity in days for the sprint.
        /// </summary>
        public int CapacityDays { get; private set; }

        /// <summary>
        /// Gets the start date and time of the sprint in Coordinated Universal Time (UTC).
        /// </summary>
        public DateTimeOffset StartDate { get; private set; }

        /// <summary>
        /// Gets or sets the end date and time of the sprint. This property is
        /// typically set when the sprint is closed, indicating its conclusion.
        /// </summary>
        public Nullable<DateTimeOffset> EndDate { get; private set; }

        /// <summary>
        /// Gets the current status of the sprint, which can be Planning, Active, or Closed.
        /// </summary>
        public SprintStatus Status { get; private set; }

        /// <summary>
        /// Gets the collection of key-value pairs representing various metrics
        /// associated with the sprint, such as performance indicators or custom statistics.
        /// </summary>
        public IReadOnlyDictionary<string, object> Metrics => new ReadOnlyDictionary<string, object>(_metrics);

        /// <summary>
        /// Gets the sequential number of the sprint within a project or workflow.
        /// </summary>
        public int SprintNumber { get; private set; }

        /// <summary>
        /// Represents a sprint in the system which encompasses its goal, tasks, capacity, dates, status, and associated metrics.
        /// </summary>
        public Sprint(string goal, int capacityDays, int sprintNumber)
        {
            if (string.IsNullOrWhiteSpace(goal))
                throw new ArgumentException("Goal cannot be null or whitespace", nameof(goal));

            if (capacityDays <= 0)
                throw new ArgumentException("Capacity days must be greater than zero", nameof(capacityDays));

            if (sprintNumber <= 0)
                throw new ArgumentException("Sprint number must be greater than zero", nameof(sprintNumber));

            Id = SprintId.New();
            Goal = goal;
            CapacityDays = capacityDays;
            SprintNumber = sprintNumber;
            StartDate = DateTimeOffset.UtcNow;
            Status = SprintStatus.Planning;
        }

        /// <summary>
        /// Starts the sprint.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the sprint is not in the 'Planning' status.
        /// Thrown when there are no tasks associated with the sprint.
        /// Thrown when the definition of done is not defined for the sprint.
        /// </exception>
        public void Start()
        {
            if (Status != SprintStatus.Planning)
                throw new InvalidOperationException(
                    $"Cannot start sprint in '{Status}' status. Sprint must be in 'Planning' status.");

            if (_tasks.Count == 0)
                throw new InvalidOperationException("Cannot start sprint without tasks.");

            if (_definitionOfDone.Count == 0)
                throw new InvalidOperationException("Cannot start sprint without definition of done.");

            Status = SprintStatus.Active;
            SetMetric("StartTime", DateTimeOffset.UtcNow);
        }

        /// <summary>
        /// Closes the sprint.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the sprint is not in the 'Active' status.
        /// </exception>
        public void Close()
        {
            if (Status != SprintStatus.Active)
                throw new InvalidOperationException($"Cannot close sprint in '{Status}' status. Sprint must be in 'Active' status.");

            Status = SprintStatus.Closed;
            EndDate = DateTimeOffset.UtcNow;
            
            // Calculate metrics
            CalculateMetrics();
        }

        /// <summary>
        /// Adds a task to the sprint.
        /// </summary>
        /// <param name="task">The task to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when the task is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the sprint is not in the planning phase or the task already exists in the sprint.</exception>
        public void AddTask(SprintTask task)
        {
            ArgumentNullException.ThrowIfNull(task);

            if (Status != SprintStatus.Planning)
                throw new InvalidOperationException("Cannot add tasks after planning phase.");

            if (_tasks.Any(t => t.Id == task.Id))
                throw new InvalidOperationException($"Task with ID '{task.Id}' already exists in the sprint.");

            _tasks.Add(task);
        }

        /// <summary>
        /// Removes a task from the sprint.
        /// </summary>
        /// <param name="taskId">The ID of the task to remove.</param>
        /// <returns>True if the task was removed; otherwise, false.</returns>
        /// <exception cref="InvalidOperationException">Thrown when removing a task is attempted outside of the planning phase.</exception>
        public bool RemoveTask(string taskId)
        {
            if (Status != SprintStatus.Planning)
                throw new InvalidOperationException("Cannot remove tasks after planning phase.");

            return _tasks.RemoveAll(t => t.Id == taskId) > 0;
        }

        /// <summary>
        /// Retrieves a task from the sprint based on the specified task ID.
        /// </summary>
        /// <param name="taskId">The unique identifier of the task to retrieve.</param>
        /// <returns>The matching <see cref="SprintTask"/> if found; otherwise, null.</returns>
        public SprintTask GetTask(string taskId) =>
            _tasks.FirstOrDefault(t => t.Id == taskId);

        /// <summary>
        /// Adds a definition of a done criterion.
        /// </summary>
        /// <param name="criterion">The criterion to add to the sprint's definition of done.</param>
        /// <exception cref="ArgumentException">Thrown when the provided criterion is null, empty, or consists only of white spaces.</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to modify the definition of done outside the planning phase.</exception>
        public void AddDefinitionOfDone(string criterion)
        {
            if (string.IsNullOrWhiteSpace(criterion))
                throw new ArgumentException("Criterion cannot be null, empty, or whitespace", nameof(criterion));

            if (Status != SprintStatus.Planning)
                throw new InvalidOperationException("Cannot modify definition of done after planning phase.");

            if (!_definitionOfDone.Contains(criterion, StringComparer.OrdinalIgnoreCase))
            {
                _definitionOfDone.Add(criterion);
            }
        }

        /// <summary>
        /// Updates the sprint goal.
        /// </summary>
        /// <param name="newGoal">The new goal to be set for the sprint.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="newGoal"/> is null or whitespace.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when attempting to update the goal while the sprint is no longer in the Planning phase.
        /// </exception>
        public void UpdateGoal(string newGoal)
        {
            if (string.IsNullOrWhiteSpace(newGoal))
                throw new ArgumentException("New goal cannot be null or whitespace", nameof(newGoal));

            if (Status != SprintStatus.Planning)
                throw new InvalidOperationException("Cannot update goal after planning phase.");

            Goal = newGoal;
        }

        /// <summary>
        /// Updates the capacity days for the sprint.
        /// </summary>
        /// <param name="newCapacityDays">The new capacity in days.</param>
        /// <exception cref="ArgumentException">Thrown if the provided capacity days are less than or equal to zero.</exception>
        /// <exception cref="InvalidOperationException">Thrown if attempting to update capacity when the sprint status is not in the planning phase.</exception>
        public void UpdateCapacity(int newCapacityDays)
        {
            if (newCapacityDays <= 0)
                throw new ArgumentException("Capacity days must be greater than zero", nameof(newCapacityDays));

            if (Status != SprintStatus.Planning)
                throw new InvalidOperationException("Cannot update capacity after planning phase.");

            CapacityDays = newCapacityDays;
        }

        /// <summary>
        /// Sets a metric value.
        /// </summary>
        /// <param name="key">The key identifying the metric.</param>
        /// <param name="value">The value of the metric to be set.</param>
        /// <exception cref="ArgumentException">Thrown when the key is null, empty, or contains only whitespace.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the value is null.</exception>
        public void SetMetric(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null, empty, or whitespace", nameof(key));
            ArgumentNullException.ThrowIfNull(value);

            _metrics[key] = value;
        }

        /// <summary>
        /// Calculates the total number of story points from all tasks in the sprint.
        /// </summary>
        /// <returns>The sum of story points from all tasks.</returns>
        public int GetTotalStoryPoints() =>
            _tasks.Sum(t => t.StoryPoints);

        /// <summary>
        /// Calculates and returns the total number of story points for tasks marked as completed within the sprint.
        /// </summary>
        /// <returns>
        /// The sum of the story points for all tasks with a status of "Done".
        /// </returns>
        public int GetCompletedStoryPoints() =>
            _tasks.Where(t => (int)t.Status == (int)TaskStatus.Done).Sum(t => t.StoryPoints);

        /// <summary>
        /// Calculates and returns the completion percentage of the sprint based on the story points of completed tasks.
        /// </summary>
        /// <returns>The completion percentage as a double. Returns 0 if there are no story points in the sprint.</returns>
        public double GetCompletionPercentage()
        {
            var total = GetTotalStoryPoints();
            return total > 0 ? (double)GetCompletedStoryPoints() / total * 100 : 0;
        }

        /// <summary>
        /// Calculates and updates metrics for the sprint, including story points, task counts,
        /// completion percentages, and duration based on the sprint's data.
        /// </summary>
        private void CalculateMetrics()
        {
            SetMetric("TotalStoryPoints", GetTotalStoryPoints());
            SetMetric("CompletedStoryPoints", GetCompletedStoryPoints());
            SetMetric("CompletionPercentage", GetCompletionPercentage());
            SetMetric("TotalTasks", _tasks.Count);
            SetMetric("CompletedTasks", _tasks.Count(t => (int)t.Status == (int)TaskStatus.Done));
            
            if (StartDate != default && EndDate.HasValue)
            {
                SetMetric("DurationDays", (EndDate.Value - StartDate).TotalDays);
            }
        }

        // For reconstitution from persistence
        /// <summary>
        /// Reconstitutes a Sprint entity from persistent storage with all necessary properties and state.
        /// </summary>
        /// <param name="id">The unique identifier of the sprint.</param>
        /// <param name="goal">The goal of the sprint.</param>
        /// <param name="capacityDays">The capacity of the sprint in days.</param>
        /// <param name="sprintNumber">The sprint number indicating its sequence.</param>
        /// <param name="status">The current status of the sprint.</param>
        /// <param name="startDate">The start date of the sprint.</param>
        /// <param name="endDate">The optional end date of the sprint.</param>
        /// <param name="tasks">The collection of tasks associated with the sprint.</param>
        /// <param name="definitionOfDone">The list of criteria that define the completion of the sprint.</param>
        /// <param name="metrics">An optional dictionary of metrics related to the sprint.</param>
        /// <returns>A reconstituted instance of the <see cref="Sprint"/> class with the specified state.</returns>
        internal static Sprint Reconstitute(
            SprintId id,
            string goal,
            int capacityDays,
            int sprintNumber,
            SprintStatus status,
            DateTimeOffset startDate,
            Nullable<DateTimeOffset> endDate,
            IEnumerable<SprintTask> tasks,
            IEnumerable<string> definitionOfDone,
            IDictionary<string, object> metrics = null)
        {
            var sprint = new Sprint(goal, capacityDays, sprintNumber)
            {
                Id = id,
                Status = status,
                StartDate = startDate,
                EndDate = endDate
            };
            sprint._tasks.AddRange(tasks);
            sprint._definitionOfDone.AddRange(definitionOfDone);
            if (metrics == null) return sprint;
            foreach (var kv in metrics)
            {
                sprint._metrics[kv.Key] = kv.Value;
            }
            return sprint;
        }
    }
}