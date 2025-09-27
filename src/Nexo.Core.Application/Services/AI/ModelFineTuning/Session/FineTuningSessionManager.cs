using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.ModelFineTuning.Session
{
    /// <summary>
    /// Manages fine-tuning sessions
    /// </summary>
    public partial class FineTuningSessionManager
    {
        private readonly ILogger _logger;
        private readonly Dictionary<string, FineTuningSession> _activeSessions;
        private readonly object _lockObject = new object();

        public FineTuningSessionManager(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _activeSessions = new Dictionary<string, FineTuningSession>();
        }

        public FineTuningSession CreateSession(FineTuningRequest request)
        {
            var session = new FineTuningSession
            {
                SessionId = Guid.NewGuid().ToString(),
                Request = request,
                Status = FineTuningStatus.Initializing,
                StartTime = DateTime.UtcNow,
                Progress = 0,
                Metrics = new FineTuningMetrics()
            };

            lock (_lockObject)
            {
                _activeSessions[session.SessionId] = session;
            }

            return session;
        }

        public Task<FineTuningSession?> GetSessionStatusAsync(string sessionId)
        {
            try
            {
                lock (_lockObject)
                {
                    _activeSessions.TryGetValue(sessionId, out var session);
                    return Task.FromResult(session);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get session status for {SessionId}", sessionId);
                return Task.FromResult<FineTuningSession?>(null);
            }
        }

        public Task<bool> CancelFineTuningAsync(string sessionId)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_activeSessions.TryGetValue(sessionId, out var session))
                    {
                        session.Status = FineTuningStatus.Cancelled;
                        session.EndTime = DateTime.UtcNow;
                        _logger.LogInformation("Fine-tuning session {SessionId} cancelled", sessionId);
                        return Task.FromResult(true);
                    }
                }
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel fine-tuning session {SessionId}", sessionId);
                return Task.FromResult(false);
            }
        }

        public Task<List<FineTuningSession>> GetActiveSessionsAsync()
        {
            try
            {
                lock (_lockObject)
                {
                    return Task.FromResult(_activeSessions.Values
                        .Where(s => s.Status == FineTuningStatus.Running || s.Status == FineTuningStatus.Initializing)
                        .ToList());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get active fine-tuning sessions");
                return Task.FromResult(new List<FineTuningSession>());
            }
        }
    }
}
