using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Models.GuidedGeneration;

namespace Nexo.Infrastructure.GuidedGeneration
{
    /// <summary>
    /// Step management functionality for GuidedGenerationService.
    /// </summary>
    public partial class GuidedGenerationService
    {
        /// <summary>
        /// Gets the next step in the generation process.
        /// </summary>
        private async Task<GenerationStep?> GetNextStepInternalAsync(string sessionId, CancellationToken cancellationToken)
        {
            try
            {
                var session = await GetSessionInternalAsync(sessionId, cancellationToken);
                if (session == null || !session.CanProceed)
                {
                    return null;
                }

                return await Task.FromResult(session.CurrentStep);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next step for session: {SessionId}", sessionId);
                throw;
            }
        }

        /// <summary>
        /// Submits input for the current step.
        /// </summary>
        private async Task<ValidationResult> SubmitStepInputInternalAsync(string sessionId, string input, CancellationToken cancellationToken)
        {
            try
            {
                var session = await GetSessionInternalAsync(sessionId, cancellationToken);
                if (session == null || !session.CanProceed)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Session not found or not in progress"
                    };
                }

                var currentStep = session.CurrentStep;
                if (currentStep == null)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "No current step available"
                    };
                }

                // Validate input
                var validationResult = currentStep.ValidationRule?.Validate(input) ?? new ValidationResult { IsValid = true };
                
                if (validationResult.IsValid)
                {
                    currentStep.UserInput = input;
                    currentStep.IsCompleted = true;
                    
                    // Store input based on step type
                    StoreStepInput(session, currentStep, input);
                    
                    _logger.LogDebug("Step input submitted for session: {SessionId}, step: {StepId}", sessionId, currentStep.Id);
                }

                return validationResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting step input for session: {SessionId}", sessionId);
                throw;
            }
        }

        /// <summary>
        /// Moves to the next step.
        /// </summary>
        private async Task<bool> MoveToNextStepInternalAsync(string sessionId, CancellationToken cancellationToken)
        {
            try
            {
                var session = await GetSessionInternalAsync(sessionId, cancellationToken);
                if (session == null)
                {
                    return false;
                }

                var moved = session.MoveToNextStep();
                if (moved)
                {
                    _logger.LogDebug("Moved to next step for session: {SessionId}, step: {StepIndex}", sessionId, session.CurrentStepIndex);
                }

                return moved;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving to next step for session: {SessionId}", sessionId);
                throw;
            }
        }

        /// <summary>
        /// Moves to the previous step.
        /// </summary>
        private async Task<bool> MoveToPreviousStepInternalAsync(string sessionId, CancellationToken cancellationToken)
        {
            try
            {
                var session = await GetSessionInternalAsync(sessionId, cancellationToken);
                if (session == null)
                {
                    return false;
                }

                var moved = session.MoveToPreviousStep();
                if (moved)
                {
                    _logger.LogDebug("Moved to previous step for session: {SessionId}, step: {StepIndex}", sessionId, session.CurrentStepIndex);
                }

                return moved;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving to previous step for session: {SessionId}", sessionId);
                throw;
            }
        }
    }
}
