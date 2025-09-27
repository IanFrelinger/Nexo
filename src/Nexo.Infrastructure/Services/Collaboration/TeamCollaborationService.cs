using System;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.AI;

namespace Nexo.Infrastructure.Services.Collaboration
{
    /// <summary>
    /// Team collaboration service for Phase 9.
    /// Provides team-based feature development and collaboration workflows.
    /// </summary>
    public partial class TeamCollaborationService : ITeamCollaborationService
    {
        private readonly ILogger<TeamCollaborationService> _logger;
        private readonly IModelOrchestrator _modelOrchestrator;

        public TeamCollaborationService(
            ILogger<TeamCollaborationService> logger,
            IModelOrchestrator modelOrchestrator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
        }

    }
}
