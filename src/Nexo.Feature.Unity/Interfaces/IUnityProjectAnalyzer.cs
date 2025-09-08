using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Feature.Unity.Interfaces
{
    /// <summary>
    /// Interface for analyzing Unity projects for optimization opportunities and best practices
    /// </summary>
    public interface IUnityProjectAnalyzer
    {
        /// <summary>
        /// Analyzes a Unity project for optimization opportunities
        /// </summary>
        Task<UnityProjectAnalysis> AnalyzeProjectAsync(string projectPath);
        
        /// <summary>
        /// Analyzes Unity scripts for performance issues
        /// </summary>
        Task<UnityScriptAnalysis> AnalyzeScriptsAsync(string projectPath);
        
        /// <summary>
        /// Analyzes Unity assets for optimization opportunities
        /// </summary>
        Task<UnityAssetAnalysis> AnalyzeAssetsAsync(string projectPath);
        
        /// <summary>
        /// Analyzes Unity scenes for optimization opportunities
        /// </summary>
        Task<UnitySceneAnalysis> AnalyzeScenesAsync(string projectPath);
        
        /// <summary>
        /// Identifies iteration optimization opportunities in Unity scripts
        /// </summary>
        Task<IEnumerable<IterationOptimizationOpportunity>> IdentifyIterationOptimizationsAsync(UnityScriptAnalysis scriptAnalysis);
    }
    
    /// <summary>
    /// Interface for Unity performance profiling and monitoring
    /// </summary>
    public interface IUnityPerformanceProfiler
    {
        /// <summary>
        /// Profiles gameplay performance for a specified duration
        /// </summary>
        Task<UnityPerformanceProfile> ProfileGameplayAsync(UnityProfilingSession session, TimeSpan duration);
        
        /// <summary>
        /// Generates Unity-specific optimization recommendations
        /// </summary>
        Task<IEnumerable<UnityOptimizationRecommendation>> GenerateUnityOptimizationsAsync(UnityPerformanceAnalysis analysis);
        
        /// <summary>
        /// Handles real-time performance spikes during gameplay
        /// </summary>
        Task HandlePerformanceSpikeAsync(UnityFrameData frameData, UnityProfilingSession session);
    }
    
    /// <summary>
    /// Interface for Unity build optimization
    /// </summary>
    public interface IUnityBuildOptimizer
    {
        /// <summary>
        /// Optimizes Unity build for multiple platforms
        /// </summary>
        Task<UnityBuildOptimization> OptimizeBuildAsync(UnityBuildRequest request);
        
        /// <summary>
        /// Optimizes build for a specific platform
        /// </summary>
        Task<PlatformBuildOptimization> OptimizeForPlatformAsync(UnityBuildRequest request, UnityBuildTarget platform);
        
        /// <summary>
        /// Applies build optimizations to the project
        /// </summary>
        Task ApplyBuildOptimizationsAsync(string projectPath, UnityBuildOptimization optimization);
    }
    
    /// <summary>
    /// Interface for Unity asset analysis
    /// </summary>
    public interface IUnityAssetAnalyzer
    {
        /// <summary>
        /// Analyzes Unity assets for optimization opportunities
        /// </summary>
        Task<UnityAssetAnalysis> AnalyzeAssetsAsync(string projectPath);
        
        /// <summary>
        /// Analyzes texture assets for optimization
        /// </summary>
        Task<IEnumerable<TextureOptimizationOpportunity>> AnalyzeTexturesAsync(string projectPath);
        
        /// <summary>
        /// Analyzes audio assets for optimization
        /// </summary>
        Task<IEnumerable<AudioOptimizationOpportunity>> AnalyzeAudioAsync(string projectPath);
    }
    
    /// <summary>
    /// Interface for Unity script analysis
    /// </summary>
    public interface IUnityScriptAnalyzer
    {
        /// <summary>
        /// Analyzes Unity scripts for performance issues
        /// </summary>
        Task<UnityScriptAnalysis> AnalyzeScriptsAsync(string projectPath);
        
        /// <summary>
        /// Analyzes a specific script for performance issues
        /// </summary>
        Task<UnityScriptAnalysis> AnalyzeScriptAsync(string scriptPath);
        
        /// <summary>
        /// Detects iteration patterns in Unity scripts
        /// </summary>
        Task<IEnumerable<IterationPattern>> DetectIterationPatternsAsync(UnityScriptMethod method);
    }
    
    /// <summary>
    /// Interface for Unity scene analysis
    /// </summary>
    public interface IUnitySceneAnalyzer
    {
        /// <summary>
        /// Analyzes Unity scenes for optimization opportunities
        /// </summary>
        Task<UnitySceneAnalysis> AnalyzeScenesAsync(string projectPath);
        
        /// <summary>
        /// Analyzes a specific scene for optimization opportunities
        /// </summary>
        Task<UnitySceneAnalysis> AnalyzeSceneAsync(string scenePath);
        
        /// <summary>
        /// Identifies rendering optimization opportunities
        /// </summary>
        Task<IEnumerable<RenderingOptimizationOpportunity>> IdentifyRenderingOptimizationsAsync(UnitySceneAnalysis sceneAnalysis);
    }
}
