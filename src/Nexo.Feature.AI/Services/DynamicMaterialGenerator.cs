using System;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Services
{
    /// <summary>
    /// Dynamic material generation pipeline that adapts to user requirements
    /// </summary>
    public partial class DynamicMaterialGenerator : IDynamicMaterialGenerator
    {
        private readonly ILogger<DynamicMaterialGenerator> _logger;
        private readonly IMaterialContextAnalyzer _contextAnalyzer;
        private readonly IMaterialGenerationAgent _materialAgent;
        private readonly IPlatformOptimizer _platformOptimizer;
        private readonly IUserGuidedAgentExpansion _expansionService;

        public DynamicMaterialGenerator(
            ILogger<DynamicMaterialGenerator> logger,
            IMaterialContextAnalyzer contextAnalyzer,
            IMaterialGenerationAgent materialAgent,
            IPlatformOptimizer platformOptimizer,
            IUserGuidedAgentExpansion expansionService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _contextAnalyzer = contextAnalyzer ?? throw new ArgumentNullException(nameof(contextAnalyzer));
            _materialAgent = materialAgent ?? throw new ArgumentNullException(nameof(materialAgent));
            _platformOptimizer = platformOptimizer ?? throw new ArgumentNullException(nameof(platformOptimizer));
            _expansionService = expansionService ?? throw new ArgumentNullException(nameof(expansionService));
        }

}
