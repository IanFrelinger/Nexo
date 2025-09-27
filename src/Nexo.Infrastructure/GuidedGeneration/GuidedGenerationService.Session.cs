using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Models.GuidedGeneration;

namespace Nexo.Infrastructure.GuidedGeneration
{
    /// <summary>
    /// Session management functionality for GuidedGenerationService.
    /// </summary>
    public partial class GuidedGenerationService
    {
        /// <summary>
        /// Starts a new guided generation session.
        /// </summary>
        private async Task<GenerationSession> StartSessionInternalAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Starting new guided generation session");

                var session = new GenerationSession
                {
                    Id = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow,
                    Status = GenerationStatus.InProgress
                };

                // Initialize steps
                session.Steps = CreateGenerationSteps();
                session.CurrentStepIndex = 0;

                _sessions[session.Id] = session;

                _logger.LogInformation("Started guided generation session: {SessionId}", session.Id);
                return await Task.FromResult(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting guided generation session");
                throw;
            }
        }

        /// <summary>
        /// Gets a session by ID.
        /// </summary>
        private async Task<GenerationSession?> GetSessionInternalAsync(string sessionId, CancellationToken cancellationToken)
        {
            try
            {
                _sessions.TryGetValue(sessionId, out var session);
                return await Task.FromResult(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session: {SessionId}", sessionId);
                throw;
            }
        }

        /// <summary>
        /// Cancels a session.
        /// </summary>
        private async Task<bool> CancelSessionInternalAsync(string sessionId, CancellationToken cancellationToken)
        {
            try
            {
                var session = await GetSessionInternalAsync(sessionId, cancellationToken);
                if (session == null)
                {
                    return false;
                }

                session.Status = GenerationStatus.Cancelled;
                session.CompletedAt = DateTime.UtcNow;

                _logger.LogInformation("Session cancelled: {SessionId}", sessionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling session: {SessionId}", sessionId);
                throw;
            }
        }
    }
}
