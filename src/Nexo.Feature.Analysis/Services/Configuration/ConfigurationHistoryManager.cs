using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Analysis.Models;

namespace Nexo.Feature.Analysis.Services.Configuration;

/// <summary>
/// Manages configuration history and restoration.
/// </summary>
public partial class ConfigurationHistoryManager
{
    private readonly ILogger<ConfigurationHistoryManager> _logger;
    private readonly List<CodingStandardConfiguration> _configurationHistory;

    public ConfigurationHistoryManager(ILogger<ConfigurationHistoryManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configurationHistory = new List<CodingStandardConfiguration>();
    }

    public void AddToHistory(CodingStandardConfiguration configuration)
    {
        _configurationHistory.Add(configuration);
        if (_configurationHistory.Count > 10) // Keep only last 10 configurations
        {
            _configurationHistory.RemoveAt(0);
        }
    }

    public async Task<List<CodingStandardConfiguration>> GetConfigurationHistoryAsync()
    {
        return await Task.FromResult(_configurationHistory.ToList());
    }

    public async Task RestoreFromHistoryAsync(string configurationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Restoring coding standards configuration from history: {ConfigurationId}", configurationId);

        var configuration = _configurationHistory.FirstOrDefault(c => c.Id == configurationId);
        if (configuration == null)
        {
            throw new ArgumentException($"Configuration with ID '{configurationId}' not found in history");
        }

        await Task.CompletedTask;
    }
}
