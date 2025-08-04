using System;
using Nexo.Shared.Interfaces;
using Nexo.Shared.Models;
using Nexo.Feature.Container.Models;
using Nexo.Feature.Container.Interfaces;
using Nexo.Shared.Enums;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Nexo.Feature.Container.UseCases
{
    /// <summary>
    /// Executes commands within a specified container and handles orchestration, validation, and execution lifecycle.
    /// </summary>
    public sealed class ExecuteInContainerUseCase : IExecuteInContainerUseCase
    {
        /// <summary>
        /// Provides an abstraction for interacting with container orchestration services.
        /// Used to handle operations such as starting, stopping, and executing commands
        /// within containers.
        /// </summary>
        private readonly IContainerOrchestrator _orchestrator;

        /// <summary>
        /// Responsible for validating commands before execution in the container-based workflow.
        /// Ensures commands comply with predefined validation rules.
        /// This is an instance of the <see cref="ICommandValidator"/> interface.
        /// </summary>
        private readonly ICommandValidator _validator;

        /// <summary>
        /// Implements the executing in a container use case.
        /// </summary>
        public ExecuteInContainerUseCase(
            IContainerOrchestrator orchestrator,
            ICommandValidator validator)
        {
            if (orchestrator == null)
                throw new ArgumentNullException(nameof(orchestrator));
            if (validator == null)
                throw new ArgumentNullException(nameof(validator));

            _orchestrator = orchestrator;
            _validator = validator;
        }

        /// <summary>
        /// Executes a command inside a specified container asynchronously.
        /// </summary>
        /// <param name="request">The request containing the container name, command, and other execution parameters.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. Optional.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the execution result of the command.</returns>
        public async Task<CommandResult> ExecuteAsync(
            ExecuteInContainerRequest request,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Validate command
            var validationResult = await _validator.ValidateAsync(request.Command, cancellationToken);
            if (!validationResult.IsValid)
            {
                var criticalErrors = validationResult.Errors
                    .Where(e => e.Severity >= ValidationSeverity.Error)
                    .ToList();

                if (criticalErrors.Count != 0)
                {
                    var errorMessage = string.Join("; ", criticalErrors.Select(e => e.Message));
                    throw new InvalidOperationException($"Command validation failed: {errorMessage}");
                }
            }

            // Check if the container is running
            var isRunning = await _orchestrator.IsContainerRunningAsync(
                request.ContainerName, 
                cancellationToken);

            if (!isRunning)
            {
                // Attempt to start the container
                try
                {
                    await _orchestrator.StartContainerAsync(
                        request.ContainerName, 
                        cancellationToken);

                    // Wait a moment for the container to be ready
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

                    // Verify it's running now
                    isRunning = await _orchestrator.IsContainerRunningAsync(
                        request.ContainerName, 
                        cancellationToken);

                    if (!isRunning)
                    {
                        return new CommandResult
                        {
                            IsSuccess = false,
                            Error = $"Failed to start container '{request.ContainerName}'"
                        };
                    }
                }
                catch (Exception ex)
                {
                    return new CommandResult
                    {
                        IsSuccess = false,
                        Error = $"Error starting container '{request.ContainerName}': {ex.Message}"
                    };
                }
            }

            // Execute command with timeout
            CommandResult result;
            long executionTimeMs = 0;
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                cts.CancelAfter(request.TimeoutMs);
                try
                {
                    var startTime = DateTimeOffset.UtcNow;
                    
                    result = await _orchestrator.ExecuteInContainerAsync(
                        request.ContainerName,
                        request.Command,
                        cts.Token);

                    var executionTime = DateTimeOffset.UtcNow - startTime;
                    executionTimeMs = (long)executionTime.TotalMilliseconds;
                }
                catch (OperationCanceledException) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    return new CommandResult
                    {
                        IsSuccess = false,
                        Error = $"Command execution timed out after {request.TimeoutMs}ms",
                        ExitCode = -1
                    };
                }
                catch (Exception ex)
                {
                    return new CommandResult
                    {
                        IsSuccess = false,
                        Error = $"Command execution failed: {ex.Message}",
                        ExitCode = -1
                    };
                }
            }
            result.ExecutionTimeMs = executionTimeMs;
            return result;
        }
    }
}