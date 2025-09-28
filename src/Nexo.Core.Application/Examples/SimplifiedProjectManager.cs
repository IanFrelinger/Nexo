using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Commands;
using Nexo.Core.Application.Validation;
using Nexo.Core.Domain.ValueObjects;
using Nexo.Core.Domain.Values;

namespace Nexo.Core.Application.Examples
{
    /// <summary>
    /// Example of applying test structure principles to project management:
    /// - Simple, focused operations
    /// - Clear success/failure patterns
    /// - No complex orchestration
    /// - Direct error handling
    /// </summary>
    public class SimplifiedProjectManager
    {
        private readonly SimplifiedCommandExecutor _executor;
        private readonly SimplifiedValidator<CreateProjectInput> _validator;

        public SimplifiedProjectManager()
        {
            _executor = new SimplifiedCommandExecutor();
            _validator = new SimplifiedValidator<CreateProjectInput>();
            
            // Register simple commands (like registering test methods)
            RegisterCommands();
            SetupValidation();
        }

        /// <summary>
        /// Create a project (like running a test)
        /// </summary>
        public async Task<ProjectResult> CreateProjectAsync(CreateProjectInput input, CancellationToken cancellationToken = default)
        {
            // Simple validation (like test input validation)
            var validationResult = _validator.Validate(input);
            if (!validationResult.IsValid)
            {
                return new ProjectResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.Message))}"
                };
            }

            // Simple execution (like test execution)
            var result = await _executor.ExecuteAsync<CreateProjectInput, CreateProjectOutput>(
                "CreateProject", 
                input, 
                cancellationToken);

            return new ProjectResult
            {
                IsSuccess = result.IsSuccess,
                ErrorMessage = result.ErrorMessage,
                Project = result.Data
            };
        }

        /// <summary>
        /// Get project metrics (like running a metrics test)
        /// </summary>
        public async Task<MetricsResult> GetProjectMetricsAsync(string projectId, CancellationToken cancellationToken = default)
        {
            var input = new CalculateProjectMetricsInput { ProjectId = projectId };
            
            var result = await _executor.ExecuteAsync<CalculateProjectMetricsInput, CalculateProjectMetricsOutput>(
                "CalculateMetrics", 
                input, 
                cancellationToken);

            return new MetricsResult
            {
                IsSuccess = result.IsSuccess,
                ErrorMessage = result.ErrorMessage,
                Metrics = result.Data
            };
        }

        private void RegisterCommands()
        {
            // Register simple commands (like registering test methods)
            _executor.RegisterCommand(new CreateProjectCommand());
            _executor.RegisterCommand(new CalculateProjectMetricsCommand());
        }

        private void SetupValidation()
        {
            // Simple validation rules (like test assertions)
            _validator.AddRule(
                "ProjectNameRequired", 
                input => !string.IsNullOrWhiteSpace(input.ProjectName), 
                "Project name is required");

            _validator.AddRule(
                "ProjectPathRequired", 
                input => !string.IsNullOrWhiteSpace(input.ProjectPath), 
                "Project path is required");

            _validator.AddRule(
                "ValidProjectName", 
                input => input.ProjectName!.Length >= 3, 
                "Project name must be at least 3 characters");
        }
    }

    /// <summary>
    /// Simple project creation command (like a test method)
    /// </summary>
    public class CreateProjectCommand : SimplifiedCommand<CreateProjectInput, CreateProjectOutput>
    {
        public override string Name => "CreateProject";

        public override async Task<CommandResult<CreateProjectOutput>> ExecuteAsync(CreateProjectInput input, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // Simple project creation logic (like test logic)
                await Task.Delay(10, cancellationToken); // Simulate work

                var output = new CreateProjectOutput
                {
                    ProjectId = Guid.NewGuid().ToString(),
                    ProjectName = input.ProjectName,
                    ProjectPath = input.ProjectPath,
                    CreatedAt = DateTime.UtcNow
                };

                return Success(output, DateTime.UtcNow - startTime);
            }
            catch (Exception ex)
            {
                return Failure($"Project creation failed: {ex.Message}", DateTime.UtcNow - startTime);
            }
        }
    }

    /// <summary>
    /// Simple metrics calculation command (like a test method)
    /// </summary>
    public class CalculateProjectMetricsCommand : SimplifiedCommand<CalculateProjectMetricsInput, CalculateProjectMetricsOutput>
    {
        public override string Name => "CalculateMetrics";

        public override async Task<CommandResult<CalculateProjectMetricsOutput>> ExecuteAsync(CalculateProjectMetricsInput input, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // Simple metrics calculation (like test logic)
                await Task.Delay(5, cancellationToken); // Simulate work

                var output = new CalculateProjectMetricsOutput
                {
                    ProjectId = input.ProjectId,
                    TotalTasks = 10,
                    CompletedTasks = 7,
                    ProgressPercentage = 70.0
                };

                return Success(output, DateTime.UtcNow - startTime);
            }
            catch (Exception ex)
            {
                return Failure($"Metrics calculation failed: {ex.Message}", DateTime.UtcNow - startTime);
            }
        }
    }

    // Simple result classes (like test result classes)
    public class ProjectResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public CreateProjectOutput? Project { get; set; }
    }

    public class MetricsResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public CalculateProjectMetricsOutput? Metrics { get; set; }
    }
}
