using System;
using System.Collections.Generic;
using Nexo.Core.Domain.Enums;
using TaskStatus = Nexo.Core.Domain.Enums.TaskStatus;

namespace Nexo.Core.Domain.Entities
{
    /// <summary>
    /// Represents a task that is part of a sprint. Each task in a sprint has specific attributes such as description,
    /// priority, status, assignee, and story points needed to aid in sprint planning and execution.
    /// </summary>
    public sealed class SprintTask
    {
        /// <summary>
        /// Gets the unique identifier of the task.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets or sets the detailed description of the sprint task.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets the list of criteria that need to be met for the task to be considered complete.
        /// </summary>
        public List<string> AcceptanceCriteria { get; } = new List<string>();

        /// <summary>
        /// Gets or sets the estimated effort required to complete the task, measured in story points.
        /// </summary>
        public int StoryPoints { get; set; }

        /// <summary>
        /// Gets or sets the priority level of the task.
        /// </summary>
        public TaskPriority Priority { get; set; }

        /// <summary>
        /// Gets the current status of the sprint.
        /// </summary>
        public System.Threading.Tasks.TaskStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the assignee for the task.
        /// </summary>
        public string? AssigneeId { get; set; }

        /// <summary>
        /// Represents a task within a sprint.
        /// </summary>
        /// <param name="id">The unique identifier of the task.</param>
        /// <param name="description">The description of the task.</param>
        /// <param name="storyPoints">The estimated effort in story points for the task.</param>
        /// <param name="priority">The priority level of the task.</param>
        /// <exception cref="ArgumentException">Thrown when id or description is null or empty, or when storyPoints is less than or equal to zero.</exception>
        public SprintTask(string id, string description, int storyPoints, TaskPriority priority)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id cannot be null or empty", nameof(id));
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Description cannot be null or empty", nameof(description));
            
            if (storyPoints <= 0)
                throw new ArgumentException("Story points must be greater than zero", nameof(storyPoints));

            Id = id;
            Description = description;
            StoryPoints = storyPoints;
            Priority = priority;
            Status = (System.Threading.Tasks.TaskStatus)TaskStatus.Todo;
        }
    }
}