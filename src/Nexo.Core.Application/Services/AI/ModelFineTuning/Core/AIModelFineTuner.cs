using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Core.Application.Services.AI.ModelFineTuning.Validation;
using Nexo.Core.Application.Services.AI.ModelFineTuning.Execution;
using Nexo.Core.Application.Services.AI.ModelFineTuning.Session;

namespace Nexo.Core.Application.Services.AI.ModelFineTuning.Core
{
    /// <summary>
    /// AI model fine-tuning service for customizing and optimizing AI models
    /// </summary>
    public partial class AIModelFineTuner
    {
        private readonly ILogger<AIModelFineTuner> _logger;
        private readonly FineTuningSessionManager _sessionManager;
        private readonly FineTuningValidator _validator;
        private readonly FineTuningExecutor _executor;

        public AIModelFineTuner(ILogger<AIModelFineTuner> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sessionManager = new FineTuningSessionManager(_logger);
            _validator = new FineTuningValidator(_logger);
            _executor = new FineTuningExecutor(_logger);
        }

        /// <summary>
        /// Starts a fine-tuning session for an AI model
        /// </summary>
        public async Task<FineTuningSession> StartFineTuningAsync(FineTuningRequest request)
        {
            try
            {
                _logger.LogInformation("Starting fine-tuning session for model {ModelId}", request.BaseModelId);

                var session = _sessionManager.CreateSession(request);

                // Initialize fine-tuning process
                await _executor.InitializeFineTuningAsync(session);

                // Start fine-tuning process
                _ = Task.Run(() => _executor.ExecuteFineTuningAsync(session));

                _logger.LogInformation("Fine-tuning session {SessionId} started successfully", session.SessionId);
                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start fine-tuning session for model {ModelId}", request.BaseModelId);
                throw;
            }
        }

        /// <summary>
        /// Gets the status of a fine-tuning session
        /// </summary>
        public Task<FineTuningSession?> GetSessionStatusAsync(string sessionId)
        {
            return _sessionManager.GetSessionStatusAsync(sessionId);
        }

        /// <summary>
        /// Cancels a fine-tuning session
        /// </summary>
        public Task<bool> CancelFineTuningAsync(string sessionId)
        {
            return _sessionManager.CancelFineTuningAsync(sessionId);
        }

        /// <summary>
        /// Gets all active fine-tuning sessions
        /// </summary>
        public Task<List<FineTuningSession>> GetActiveSessionsAsync()
        {
            return _sessionManager.GetActiveSessionsAsync();
        }

        /// <summary>
        /// Validates fine-tuning data for quality and compatibility
        /// </summary>
        public async Task<FineTuningValidationResult> ValidateFineTuningDataAsync(FineTuningData data)
        {
            return await _validator.ValidateFineTuningDataAsync(data);
        }
    }
}
