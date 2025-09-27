using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Domain.Models;
using Nexo.Core.Domain.Models.GuidedGeneration;
using Nexo.Infrastructure.Orchestration;

namespace Nexo.Infrastructure.GuidedGeneration
{
    /// <summary>
    /// Service for guided tool generation.
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public partial class GuidedGenerationService : IGuidedGenerationService
    {
        private readonly ToolGenerationOrchestrator _orchestrator;
        private readonly ILogger<GuidedGenerationService> _logger;
        private readonly Dictionary<string, GenerationSession> _sessions = new();

        public GuidedGenerationService(
            ToolGenerationOrchestrator orchestrator,
            ILogger<GuidedGenerationService> logger)
        {
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Starts a new guided generation session.
        /// </summary>
        public async Task<GenerationSession> StartSessionAsync(CancellationToken cancellationToken = default)
        {
            return await StartSessionInternalAsync(cancellationToken);
        }

        /// <summary>
        /// Gets the next step in the generation process.
        /// </summary>
        public async Task<GenerationStep?> GetNextStepAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            return await GetNextStepInternalAsync(sessionId, cancellationToken);
        }

        /// <summary>
        /// Submits input for the current step.
        /// </summary>
        public async Task<ValidationResult> SubmitStepInputAsync(string sessionId, string input, CancellationToken cancellationToken = default)
        {
            return await SubmitStepInputInternalAsync(sessionId, input, cancellationToken);
        }

        /// <summary>
        /// Moves to the next step.
        /// </summary>
        public async Task<bool> MoveToNextStepAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            return await MoveToNextStepInternalAsync(sessionId, cancellationToken);
        }

        /// <summary>
        /// Moves to the previous step.
        /// </summary>
        public async Task<bool> MoveToPreviousStepAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            return await MoveToPreviousStepInternalAsync(sessionId, cancellationToken);
        }

        /// <summary>
        /// Generates the tool based on session data.
        /// </summary>
        public async Task<GeneratedTool> GenerateToolAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            return await GenerateToolInternalAsync(sessionId, cancellationToken);
        }

        /// <summary>
        /// Gets a session by ID.
        /// </summary>
        public async Task<GenerationSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            return await GetSessionInternalAsync(sessionId, cancellationToken);
        }

        /// <summary>
        /// Cancels a session.
        /// </summary>
        public async Task<bool> CancelSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            return await CancelSessionInternalAsync(sessionId, cancellationToken);
        }
    }
}