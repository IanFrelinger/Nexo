// This file has been refactored into specialized integration components:
// - AgentCodingStandardsIntegration/Core/AgentCodingStandardsIntegrationService.cs - Main orchestrator
// - AgentCodingStandardsIntegration/Operations/* - Validation, recommendations, configuration, compliance

// Re-export for backward compatibility
using Nexo.Feature.Analysis.Services.AgentCodingStandardsIntegration.Core;
using Nexo.Feature.Analysis.Services.AgentCodingStandardsIntegration.Operations;

namespace Nexo.Feature.Analysis.Services
{
    // All agent coding standards integration classes are available via specialized namespaces above.
}


