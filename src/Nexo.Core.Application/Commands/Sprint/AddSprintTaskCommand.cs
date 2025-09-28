using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities;
using Nexo.Core.Domain.Values;
using Nexo.Core.Domain.ValueObjects;

namespace Nexo.Core.Application.Commands.Sprint
{
    /// <summary>
    /// Command to add a task to a sprint (replaces SprintTaskManager.AddTask functionality)
    /// </summary>
    public class AddSprintTaskCommand : BaseCommand<AddSprintTaskInput, AddSprintTaskOutput>
    {
        public override string CommandId => "sprint.add-task";
        public override string CommandName => "Add Sprint Task";
        public override string Description => "Adds a new task to a sprint with validation and status updates";

        public override Task<CommandResult<AddSprintTaskOutput>> ExecuteAsync(AddSprintTaskInput input, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // Validate the task
                var validationResult = ValidateTask(input.Task);
                if (!validationResult.IsValid)
                {
                    return Task.FromResult(CreateFailureResult($"Task validation failed: {string.Join(", ", validationResult.Errors)}", startTime));
                }

                // Create the sprint task
                var sprintTask = new SprintTask(
                    input.Task.Id,
                    input.Task.Description,
                    input.Task.StoryPoints,
                    input.Task.Priority
                );

                // Set assignee if provided
                if (!string.IsNullOrEmpty(input.Task.AssigneeId))
                {
                    sprintTask.AssigneeId = input.Task.AssigneeId;
                }

                // Add to sprint (this would be done through the sprint entity)
                var output = new AddSprintTaskOutput
                {
                    Task = sprintTask,
                    SprintId = input.SprintId,
                    AddedAt = DateTime.UtcNow
                };

                var warnings = new List<string>();
                if (input.Task.StoryPoints > 8)
                {
                    warnings.Add("Task has high story points - consider breaking it down");
                }

                if (string.IsNullOrEmpty(input.Task.AssigneeId))
                {
                    warnings.Add("Task has no assignee - consider assigning to a team member");
                }

                return Task.FromResult(CreateSuccessResult(output, startTime, warnings.ToArray()));
            }
            catch (Exception ex)
            {
                return Task.FromResult(CreateFailureResult($"Failed to add sprint task: {ex.Message}", startTime));
            }
        }

        protected override bool ValidateInput(AddSprintTaskInput input)
        {
            return input != null && 
                   input.Task != null && 
                   !string.IsNullOrEmpty(input.SprintId);
        }

        private TaskValidationResult ValidateTask(SprintTaskInput task)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(task.Description))
            {
                errors.Add("Task description is required");
            }

            if (task.StoryPoints < 0)
            {
                errors.Add("Story points must be non-negative");
            }

            if (task.StoryPoints > 20)
            {
                errors.Add("Story points should not exceed 20 (consider breaking down large tasks)");
            }

            return new TaskValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }
    }

    /// <summary>
    /// Input for adding a sprint task
    /// </summary>
    public class AddSprintTaskInput
    {
        public string SprintId { get; set; } = string.Empty;
        public SprintTaskInput Task { get; set; } = new SprintTaskInput();
    }

    /// <summary>
    /// Output for adding a sprint task
    /// </summary>
    public class AddSprintTaskOutput
    {
        public SprintTask Task { get; set; } = new SprintTask("", "", 0, TaskPriority.Medium);
        public string SprintId { get; set; } = string.Empty;
        public DateTime AddedAt { get; set; }
    }

    /// <summary>
    /// Input data for a sprint task
    /// </summary>
    public class SprintTaskInput
    {
        public string Id { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int StoryPoints { get; set; }
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public string? AssigneeId { get; set; }
    }

    /// <summary>
    /// Result of task validation
    /// </summary>
    public class TaskValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}
