using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Domain.Models;
using Nexo.Core.Domain.Models.GuidedGeneration;

namespace Nexo.Core.Domain.Interfaces
{
    /// <summary>
    /// Interface for guided tool generation service
    /// </summary>
    public interface IGuidedGenerationService
    {
        /// <summary>
        /// Starts a new guided generation session
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>New generation session</returns>
        Task<GenerationSession> StartSessionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the next step in the generation process
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Next step or null if session is complete</returns>
        Task<GenerationStep?> GetNextStepAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Submits input for the current step
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <param name="input">User input</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        Task<ValidationResult> SubmitStepInputAsync(string sessionId, string input, CancellationToken cancellationToken = default);

        /// <summary>
        /// Moves to the next step
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if moved to next step</returns>
        Task<bool> MoveToNextStepAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Moves to the previous step
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if moved to previous step</returns>
        Task<bool> MoveToPreviousStepAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates the tool based on the session data
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated tool</returns>
        Task<GeneratedTool> GenerateToolAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a session by ID
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Session or null if not found</returns>
        Task<GenerationSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels a session
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if cancelled</returns>
        Task<bool> CancelSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    }
}
