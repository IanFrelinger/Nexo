using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Analysis.Interfaces;
using Nexo.Feature.Analysis.Models;

namespace Nexo.Feature.Analysis.Services.AgentCodingStandardsIntegration.Operations
{
    public partial class ConfigurationOperations
    {
        private readonly ICodingStandardAnalyzer _analyzer;
        private readonly ILogger _logger;

        public ConfigurationOperations(ICodingStandardAnalyzer analyzer, ILogger logger)
        {
            _analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ConfigureAgentStandardsAsync(string agentId, List<string> standards, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Configuring coding standards for agent {AgentId}", agentId);
            try
            {
                var configuration = _analyzer.GetConfiguration();
                if (!configuration.AgentSettings.ContainsKey(agentId))
                {
                    configuration.AgentSettings[agentId] = new CodingStandardAgentSettings { AgentId = agentId, IsEnabled = true };
                }
                configuration.AgentSettings[agentId].AppliedStandards = standards;
                await _analyzer.UpdateConfigurationAsync(configuration, cancellationToken);
                _logger.LogInformation("Coding standards configured for agent {AgentId}", agentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring standards for agent {AgentId}", agentId);
                throw;
            }
        }
    }
}


