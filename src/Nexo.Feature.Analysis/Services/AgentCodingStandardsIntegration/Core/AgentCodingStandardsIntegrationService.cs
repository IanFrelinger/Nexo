using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Analysis.Interfaces;
using Nexo.Feature.Analysis.Models;
using Nexo.Feature.Analysis.Services.AgentCodingStandardsIntegration.Operations;

namespace Nexo.Feature.Analysis.Services.AgentCodingStandardsIntegration.Core
{
    /// <summary>
    /// Orchestrator that integrates coding standards enforcement with code generation agents.
    /// Delegates to specialized operation services.
    /// </summary>
    public partial class AgentCodingStandardsIntegrationService
    {
        private readonly ILogger<AgentCodingStandardsIntegrationService> _logger;
        private readonly CodeValidationOperations _validation;
        private readonly RecommendationOperations _recommendations;
        private readonly ConfigurationOperations _configuration;
        private readonly ComplianceSummaryOperations _compliance;

        public AgentCodingStandardsIntegrationService(
            ILogger<AgentCodingStandardsIntegrationService> logger,
            ICodingStandardAnalyzer codingStandardAnalyzer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (codingStandardAnalyzer == null) throw new ArgumentNullException(nameof(codingStandardAnalyzer));

            _validation = new CodeValidationOperations(codingStandardAnalyzer, logger);
            _recommendations = new RecommendationOperations(codingStandardAnalyzer, logger);
            _configuration = new ConfigurationOperations(codingStandardAnalyzer, logger);
            _compliance = new ComplianceSummaryOperations(codingStandardAnalyzer, logger);
        }

        public Task<AgentCodeValidationResult> ValidateAgentGeneratedCodeAsync(
            string agentId,
            string generatedCode,
            string? filePath = null,
            CancellationToken cancellationToken = default)
            => _validation.ValidateAgentGeneratedCodeAsync(agentId, generatedCode, filePath, cancellationToken);

        public Task<Dictionary<string, AgentCodeValidationResult>> ValidateAgentGeneratedCodeFilesAsync(
            string agentId,
            Dictionary<string, string> generatedCodeFiles,
            CancellationToken cancellationToken = default)
            => _validation.ValidateAgentGeneratedCodeFilesAsync(agentId, generatedCodeFiles, cancellationToken);

        public Task<Dictionary<string, AgentCodeValidationResult>> ValidateCodeArtifactsAsync(
            Dictionary<string, (string Content, string? GeneratedBy)> artifacts,
            CancellationToken cancellationToken = default)
            => _validation.ValidateCodeArtifactsAsync(artifacts, cancellationToken);

        public Task<List<CodingStandardRecommendation>> GetAgentRecommendationsAsync(
            string agentId,
            CancellationToken cancellationToken = default)
            => _recommendations.GetAgentRecommendationsAsync(agentId, cancellationToken);

        public Task ConfigureAgentStandardsAsync(
            string agentId,
            List<string> standards,
            CancellationToken cancellationToken = default)
            => _configuration.ConfigureAgentStandardsAsync(agentId, standards, cancellationToken);

        public Task<CodingStandardsComplianceSummary> GetComplianceSummaryAsync(
            CancellationToken cancellationToken = default)
            => _compliance.GetComplianceSummaryAsync(cancellationToken);
    }
}


