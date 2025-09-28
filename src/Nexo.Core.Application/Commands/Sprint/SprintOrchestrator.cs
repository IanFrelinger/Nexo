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
    /// Orchestrator that composes sprint-related commands into workflows
    /// This replaces the large Sprint class by composing small, focused commands
    /// </summary>
    public class SprintOrchestrator
    {
        private readonly ICommandOrchestrator<SprintMetricsInput, SprintMetricsOutput> _metricsOrchestrator;
        private readonly ICommandOrchestrator<AddSprintTaskInput, AddSprintTaskOutput> _taskOrchestrator;

        public SprintOrchestrator(
            ICommandOrchestrator<SprintMetricsInput, SprintMetricsOutput> metricsOrchestrator,
            ICommandOrchestrator<AddSprintTaskInput, AddSprintTaskOutput> taskOrchestrator)
        {
            _metricsOrchestrator = metricsOrchestrator;
            _taskOrchestrator = taskOrchestrator;
        }

        /// <summary>
        /// Creates a new sprint with initial setup
        /// </summary>
        public async Task<SprintCreationResult> CreateSprintAsync(CreateSprintInput input, CancellationToken cancellationToken = default)
        {
            var commands = new List<ICommand<SprintMetricsInput, SprintMetricsOutput>>
            {
                new CalculateSprintMetricsCommand()
            };

            var metricsInput = new SprintMetricsInput
            {
                Tasks = input.InitialTasks ?? Enumerable.Empty<SprintTask>(),
                StartDate = input.StartDate,
                EndDate = input.EndDate,
                SprintId = input.SprintId
            };

            var result = await _metricsOrchestrator.ExecuteAsync(commands, metricsInput, cancellationToken: cancellationToken);

            return new SprintCreationResult
            {
                IsSuccess = result.IsSuccess,
                SprintId = input.SprintId,
                InitialMetrics = result.FinalResult,
                Errors = result.Errors
            };
        }

        /// <summary>
        /// Adds multiple tasks to a sprint
        /// </summary>
        public async Task<SprintTaskAdditionResult> AddTasksToSprintAsync(AddTasksToSprintInput input, CancellationToken cancellationToken = default)
        {
            var commands = input.Tasks.Select(task => new AddSprintTaskCommand()).ToList();
            var results = new List<AddSprintTaskOutput>();

            foreach (var task in input.Tasks)
            {
                var taskInput = new AddSprintTaskInput
                {
                    SprintId = input.SprintId,
                    Task = task
                };

                var result = await _taskOrchestrator.ExecuteAsync(
                    new[] { new AddSprintTaskCommand() }, 
                    taskInput, 
                    cancellationToken: cancellationToken);

                if (result.IsSuccess && result.FinalResult != null)
                {
                    results.Add(result.FinalResult);
                }
            }

            return new SprintTaskAdditionResult
            {
                IsSuccess = results.Count == input.Tasks.Count(),
                AddedTasks = results,
                SprintId = input.SprintId
            };
        }

        /// <summary>
        /// Updates sprint metrics after changes
        /// </summary>
        public async Task<SprintMetricsOutput> UpdateSprintMetricsAsync(UpdateSprintMetricsInput input, CancellationToken cancellationToken = default)
        {
            var commands = new List<ICommand<SprintMetricsInput, SprintMetricsOutput>>
            {
                new CalculateSprintMetricsCommand()
            };

            var metricsInput = new SprintMetricsInput
            {
                Tasks = input.Tasks,
                StartDate = input.StartDate,
                EndDate = input.EndDate,
                SprintId = input.SprintId
            };

            var result = await _metricsOrchestrator.ExecuteAsync(commands, metricsInput, cancellationToken: cancellationToken);
            return result.FinalResult ?? new SprintMetricsOutput();
        }

        /// <summary>
        /// Executes a custom sprint workflow
        /// </summary>
        public Task<SprintWorkflowResult> ExecuteSprintWorkflowAsync(SprintWorkflowInput input, CancellationToken cancellationToken = default)
        {
            // This is a simplified example - in practice, you'd use proper typing
            // and the orchestrator would handle the command execution
            var results = new List<object>();
            var errors = new List<string>();

            return Task.FromResult(new SprintWorkflowResult
            {
                IsSuccess = errors.Count == 0,
                Results = results,
                Errors = errors
            });
        }
    }

    // Input/Output classes for the orchestrator
    public class CreateSprintInput
    {
        public string SprintId { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public IEnumerable<SprintTask>? InitialTasks { get; set; }
    }

    public class SprintCreationResult
    {
        public bool IsSuccess { get; set; }
        public string SprintId { get; set; } = string.Empty;
        public SprintMetricsOutput? InitialMetrics { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class AddTasksToSprintInput
    {
        public string SprintId { get; set; } = string.Empty;
        public IEnumerable<SprintTaskInput> Tasks { get; set; } = Enumerable.Empty<SprintTaskInput>();
    }

    public class SprintTaskAdditionResult
    {
        public bool IsSuccess { get; set; }
        public List<AddSprintTaskOutput> AddedTasks { get; set; } = new List<AddSprintTaskOutput>();
        public string SprintId { get; set; } = string.Empty;
    }

    public class UpdateSprintMetricsInput
    {
        public string SprintId { get; set; } = string.Empty;
        public IEnumerable<SprintTask> Tasks { get; set; } = Enumerable.Empty<SprintTask>();
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class SprintWorkflowInput
    {
        public string SprintId { get; set; } = string.Empty;
        public IEnumerable<SprintTaskInput> TasksToAdd { get; set; } = Enumerable.Empty<SprintTaskInput>();
        public bool CalculateMetrics { get; set; } = true;
    }

    public class SprintWorkflowResult
    {
        public bool IsSuccess { get; set; }
        public List<object> Results { get; set; } = new List<object>();
        public List<string> Errors { get; set; } = new List<string>();
    }
}
