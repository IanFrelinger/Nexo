using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities;
using Nexo.Core.Domain.Values;
using Nexo.Core.Domain.ValueObjects;

namespace Nexo.Core.Application.Commands.Project
{
    /// <summary>
    /// Command to create a new project (replaces Project constructor and initialization)
    /// </summary>
    public class CreateProjectCommand : BaseCommand<CreateProjectInput, CreateProjectOutput>
    {
        public override string CommandId => "project.create";
        public override string CommandName => "Create Project";
        public override string Description => "Creates a new project with the specified properties and initializes it";

        public override Task<CommandResult<CreateProjectOutput>> ExecuteAsync(CreateProjectInput input, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(input.Name))
                {
                    return Task.FromResult(CreateFailureResult("Project name cannot be null or empty", startTime));
                }

                if (string.IsNullOrWhiteSpace(input.Path))
                {
                    return Task.FromResult(CreateFailureResult("Project path cannot be null or empty", startTime));
                }

                // Create project ID and name value objects
                var projectId = new ProjectId(Guid.NewGuid());
                var projectName = new ProjectName(input.Name);
                var projectPath = new ProjectPath(input.Path);

                // Create the project entity using constructor
                var project = new Nexo.Core.Domain.Entities.Project(projectName, projectPath, input.Runtime ?? ContainerRuntime.Docker);

                // Set initial metadata if provided
                if (input.Metadata != null)
                {
                    foreach (var kvp in input.Metadata)
                    {
                        project.SetMetadata(kvp.Key, kvp.Value);
                    }
                }

                var output = new CreateProjectOutput
                {
                    Project = project,
                    ProjectId = projectId.ToString(),
                    CreatedAt = DateTime.UtcNow
                };

                var warnings = new List<string>();
                if (input.Metadata == null || input.Metadata.Count == 0)
                {
                    warnings.Add("No initial metadata provided for the project");
                }

                return Task.FromResult(CreateSuccessResult(output, startTime, warnings.ToArray()));
            }
            catch (Exception ex)
            {
                return Task.FromResult(CreateFailureResult($"Failed to create project: {ex.Message}", startTime));
            }
        }

        protected override bool ValidateInput(CreateProjectInput input)
        {
            return input != null && 
                   !string.IsNullOrWhiteSpace(input.Name) && 
                   !string.IsNullOrWhiteSpace(input.Path);
        }
    }

}
