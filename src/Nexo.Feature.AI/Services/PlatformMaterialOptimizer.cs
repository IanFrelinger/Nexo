using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Services
{
    /// <summary>
    /// Optimizes materials for specific platforms and performance requirements
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public partial class PlatformMaterialOptimizer : IPlatformMaterialOptimizer
    {
        private readonly ILogger<PlatformMaterialOptimizer> _logger;
        private readonly IPerformanceAnalyzer _performanceAnalyzer;
        private readonly IShaderOptimizer _shaderOptimizer;
        private readonly ITextureOptimizer _textureOptimizer;

        public PlatformMaterialOptimizer(
            ILogger<PlatformMaterialOptimizer> logger,
            IPerformanceAnalyzer performanceAnalyzer,
            IShaderOptimizer shaderOptimizer,
            ITextureOptimizer textureOptimizer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _performanceAnalyzer = performanceAnalyzer ?? throw new ArgumentNullException(nameof(performanceAnalyzer));
            _shaderOptimizer = shaderOptimizer ?? throw new ArgumentNullException(nameof(shaderOptimizer));
            _textureOptimizer = textureOptimizer ?? throw new ArgumentNullException(nameof(textureOptimizer));
        }
        // This class acts as an orchestrator for various material optimization functionalities,
        // with specific categories defined in partial classes.
    }

    /// <summary>
    /// Interface for platform material optimization
    /// </summary>
    public partial interface IPlatformMaterialOptimizer
    {
        Task<Material> OptimizeMaterialAsync(Material material, PlatformType targetPlatform);
        Task<Material> OptimizeMaterialAsync(Material material, PerformanceRequirements requirements);
        Task<Material> OptimizeMaterialAsync(Material material, PlatformType targetPlatform, PerformanceRequirements requirements);
        Task<OptimizationReport> GenerateOptimizationReportAsync(Material originalMaterial, Material optimizedMaterial, PlatformType targetPlatform);
    }
}
