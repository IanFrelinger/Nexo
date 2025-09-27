using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Services;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Results;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Domain.Enums.Code;
using Nexo.Core.Domain.Entities.Pipeline;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Pipeline
{
    /// <summary>
    /// AI-powered code review pipeline step for comprehensive code analysis.
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public partial class AICodeReviewStep : IPipelineStep<Nexo.Core.Domain.Entities.AI.CodeReviewRequest>
    {
        private readonly IAIRuntimeSelector _runtimeSelector;
        private readonly ILogger<AICodeReviewStep> _logger;

        public AICodeReviewStep(IAIRuntimeSelector runtimeSelector, ILogger<AICodeReviewStep> logger)
        {
            _runtimeSelector = runtimeSelector ?? throw new ArgumentNullException(nameof(runtimeSelector));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string Name => "AI Code Review";
        public int Order => 2;
        // This class acts as an orchestrator for various code review functionalities,
        // with specific categories defined in partial classes.
    }
}