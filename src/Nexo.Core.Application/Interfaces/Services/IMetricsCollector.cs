using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.Monitoring
{
    /// <summary>
    /// Interface for metrics collection services
    /// </summary>
    public interface IMetricsCollector
    {
        Task RecordMetricAsync(string name, double value, Dictionary<string, string> tags = null);
        Task<Dictionary<string, double>> GetMetricsAsync(string name);
        Task<List<string>> GetAvailableMetricsAsync();
        Task<bool> ClearMetricsAsync();
    }
}
