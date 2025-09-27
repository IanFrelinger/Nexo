using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.Feature.Pipeline.Services
{
    /// <summary>
    /// Service for managing workflow configurations and providing default templates.
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public partial class WorkflowConfigurationService : IWorkflowConfigurationService
    {
        private readonly ILogger<WorkflowConfigurationService> _logger;
        private readonly Dictionary<string, WorkflowConfiguration> _templates;

        public WorkflowConfigurationService(ILogger<WorkflowConfigurationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _templates = new Dictionary<string, WorkflowConfiguration>();
            InitializeDefaultTemplates();
        }

    }
}