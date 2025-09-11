using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.Monitoring
{
    /// <summary>
    /// Interface for production monitoring services
    /// </summary>
    public interface IProductionMonitoringService
    {
        Task<bool> StartMonitoringAsync();
        Task<bool> StopMonitoringAsync();
        Task<Dictionary<string, object>> GetHealthStatusAsync();
        Task<List<string>> GetAlertsAsync();
        Task<bool> SendAlertAsync(string message, AlertLevel level);
    }

    public enum AlertLevel
    {
        Info,
        Warning,
        Error,
        Critical
    }
}
