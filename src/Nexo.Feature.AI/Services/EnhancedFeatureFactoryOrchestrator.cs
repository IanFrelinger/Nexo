// This file has been refactored into specialized enhanced feature factory classes:
// - Core/EnhancedFeatureFactoryOrchestrator.cs - Main orchestrator
// - Analysis/RequestAnalyzer.cs - Request complexity analysis
// - Execution/CoordinationExecutor.cs - Specialized agent coordination
// - Generation/StandardGenerator.cs - Standard generation path
// - Processing/ResponseExtractor.cs - Response assembly

// Re-export for backward compatibility
using Nexo.Feature.AI.Services.EnhancedFeatureFactory.Core;
using Nexo.Feature.AI.Services.EnhancedFeatureFactory.Analysis;
using Nexo.Feature.AI.Services.EnhancedFeatureFactory.Execution;
using Nexo.Feature.AI.Services.EnhancedFeatureFactory.Generation;
using Nexo.Feature.AI.Services.EnhancedFeatureFactory.Processing;

namespace Nexo.Feature.AI.Services
{
    // All enhanced feature factory classes are available through specialized namespaces above.
}


