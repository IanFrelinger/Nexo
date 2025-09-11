using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.Monitoring
{
    /// <summary>
    /// Interface for alerting services
    /// </summary>
    public interface IAlertingService
    {
        Task<bool> SendAlertAsync(string message, AlertLevel level, Dictionary<string, string> metadata = null);
        Task<List<Alert>> GetActiveAlertsAsync();
        Task<bool> AcknowledgeAlertAsync(string alertId);
        Task<bool> ResolveAlertAsync(string alertId);
    }

    public class Alert
    {
        public string Id { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public AlertLevel Level { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsAcknowledged { get; set; }
        public bool IsResolved { get; set; }
    }
}
