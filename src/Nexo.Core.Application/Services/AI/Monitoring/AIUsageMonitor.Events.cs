using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Monitoring
{
    /// <summary>
    /// AI usage event handling
    /// </summary>
    public partial class AIUsageMonitor
    {
        /// <summary>
        /// Logs a custom usage event
        /// </summary>
        public async Task LogUsageEventAsync(AIUsageEvent usageEvent)
        {
            try
            {
                _logger.LogDebug("Logging AI usage event {EventType} for operation {OperationId}", 
                    usageEvent.EventType, usageEvent.OperationId);

                lock (_lockObject)
                {
                    _usageHistory.Add(usageEvent);
                    
                    // Keep only last 10000 events to prevent memory issues
                    if (_usageHistory.Count > 10000)
                    {
                        _usageHistory.RemoveAt(0);
                    }
                }

                // Add to session if it exists
                AIUsageSession? session;
                lock (_lockObject)
                {
                    _activeSessions.TryGetValue(usageEvent.OperationId, out session);
                }

                if (session != null)
                {
                    session.Events.Add(usageEvent);
                }

                await Task.Delay(10); // Simulate async operation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log AI usage event");
            }
        }

        /// <summary>
        /// Gets usage events for a specific operation
        /// </summary>
        public async Task<List<AIUsageEvent>> GetOperationEventsAsync(string operationId)
        {
            try
            {
                List<AIUsageEvent> events;
                lock (_lockObject)
                {
                    events = _usageHistory
                        .Where(e => e.OperationId == operationId)
                        .OrderBy(e => e.Timestamp)
                        .ToList();
                }

                await Task.Delay(10);
                return events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get events for operation {OperationId}", operationId);
                throw;
            }
        }
    }
}
