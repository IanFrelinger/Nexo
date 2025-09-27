using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.Feature.Pipeline.Services
{
    /// <summary>
    /// Template management functionality for workflow configuration service.
    /// </summary>
    public partial class WorkflowConfigurationService
    {
        public Task<WorkflowConfiguration> GetDefaultConfigurationAsync(WorkflowType type, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting default configuration for workflow type: {Type}", type);

            var templateName = $"default-{type.ToString().ToLowerInvariant()}";
            if (_templates.TryGetValue(templateName, out var template))
            {
                // Clone the template to avoid modifying the original
                return Task.FromResult(CloneConfiguration(template));
            }

            // Create a basic default configuration
            return Task.FromResult(CreateDefaultConfiguration(type));
        }

        public Task<IEnumerable<string>> GetAvailableTemplatesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_templates.Keys.ToList().AsEnumerable());
        }

        public Task<string> GetTemplateDocumentationAsync(string templateName, CancellationToken cancellationToken = default)
        {
            if (_templates.TryGetValue(templateName, out var template))
            {
                return Task.FromResult(template.Description);
            }

            return Task.FromResult($"Template '{templateName}' not found.");
        }

        private WorkflowConfiguration CreateDefaultConfiguration(WorkflowType type)
        {
            return new WorkflowConfiguration
            {
                Name = $"Default {type} Workflow",
                Description = $"Default workflow for {type.ToString().ToLowerInvariant()} operations",
                Type = type,
                Steps = new List<WorkflowStep>
                {
                    new WorkflowStep
                    {
                        Name = $"Default {type} Step",
                        Description = $"Default step for {type.ToString().ToLowerInvariant()} workflow",
                        Type = StepType.Command,
                        Command = "echo",
                        Arguments = new List<string> { $"Running {type} workflow" },
                        IsRequired = true
                    }
                }
            };
        }

        private WorkflowConfiguration CloneConfiguration(WorkflowConfiguration source)
        {
            var json = JsonSerializer.Serialize(source);
            return JsonSerializer.Deserialize<WorkflowConfiguration>(json) ?? source;
        }
    }
}
