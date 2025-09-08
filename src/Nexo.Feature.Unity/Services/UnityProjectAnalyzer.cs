using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Unity.Interfaces;
using Nexo.Feature.Unity.Models;

namespace Nexo.Feature.Unity.Services
{
    /// <summary>
    /// Analyzes Unity projects for optimization opportunities and best practices
    /// </summary>
    public class UnityProjectAnalyzer : IUnityProjectAnalyzer
    {
        private readonly IFileSystemService _fileSystem;
        private readonly IUnityAssetAnalyzer _assetAnalyzer;
        private readonly IUnityScriptAnalyzer _scriptAnalyzer;
        private readonly IUnitySceneAnalyzer _sceneAnalyzer;
        private readonly ILogger<UnityProjectAnalyzer> _logger;
        
        public UnityProjectAnalyzer(
            IFileSystemService fileSystem,
            IUnityAssetAnalyzer assetAnalyzer,
            IUnityScriptAnalyzer scriptAnalyzer,
            IUnitySceneAnalyzer sceneAnalyzer,
            ILogger<UnityProjectAnalyzer> logger)
        {
            _fileSystem = fileSystem;
            _assetAnalyzer = assetAnalyzer;
            _scriptAnalyzer = scriptAnalyzer;
            _sceneAnalyzer = sceneAnalyzer;
            _logger = logger;
        }
        
        public async Task<UnityProjectAnalysis> AnalyzeProjectAsync(string projectPath)
        {
            _logger.LogInformation("Starting Unity project analysis for: {ProjectPath}", projectPath);
            
            var analysis = new UnityProjectAnalysis
            {
                ProjectPath = projectPath,
                AnalysisTimestamp = DateTime.UtcNow
            };
            
            try
            {
                // Analyze Unity project structure
                analysis.ProjectStructure = await AnalyzeProjectStructure(projectPath);
                
                // Analyze assets for optimization
                analysis.AssetAnalysis = await _assetAnalyzer.AnalyzeAssetsAsync(projectPath);
                
                // Analyze scripts for performance issues
                analysis.ScriptAnalysis = await _scriptAnalyzer.AnalyzeScriptsAsync(projectPath);
                
                // Analyze scenes for optimization opportunities
                analysis.SceneAnalysis = await _sceneAnalyzer.AnalyzeScenesAsync(projectPath);
                
                // Generate performance recommendations
                analysis.PerformanceRecommendations = await GeneratePerformanceRecommendations(analysis);
                
                // Identify iteration optimization opportunities
                analysis.IterationOptimizations = await IdentifyIterationOptimizationsAsync(analysis.ScriptAnalysis);
                
                _logger.LogInformation("Unity project analysis completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze Unity project: {ProjectPath}", projectPath);
                throw;
            }
            
            return analysis;
        }
        
        public async Task<UnityScriptAnalysis> AnalyzeScriptsAsync(string projectPath)
        {
            return await _scriptAnalyzer.AnalyzeScriptsAsync(projectPath);
        }
        
        public async Task<UnityAssetAnalysis> AnalyzeAssetsAsync(string projectPath)
        {
            return await _assetAnalyzer.AnalyzeAssetsAsync(projectPath);
        }
        
        public async Task<UnitySceneAnalysis> AnalyzeScenesAsync(string projectPath)
        {
            return await _sceneAnalyzer.AnalyzeScenesAsync(projectPath);
        }
        
        public async Task<IEnumerable<IterationOptimizationOpportunity>> IdentifyIterationOptimizationsAsync(UnityScriptAnalysis scriptAnalysis)
        {
            var opportunities = new List<IterationOptimizationOpportunity>();
            
            foreach (var script in scriptAnalysis.Scripts)
            {
                // Find Update() methods with iteration patterns
                var updateMethods = script.Methods.Where(m => m.Name == "Update" || m.Name == "FixedUpdate");
                
                foreach (var method in updateMethods)
                {
                    var iterationPatterns = await _scriptAnalyzer.DetectIterationPatternsAsync(method);
                    
                    foreach (var pattern in iterationPatterns)
                    {
                        // Check if pattern can be optimized for Unity
                        var optimization = await EvaluateUnityOptimization(pattern);
                        
                        if (optimization.HasOptimization)
                        {
                            opportunities.Add(new IterationOptimizationOpportunity
                            {
                                ScriptPath = script.Path,
                                MethodName = method.Name,
                                LineNumber = pattern.LineNumber,
                                CurrentPattern = pattern.Code,
                                OptimizedPattern = optimization.OptimizedCode,
                                EstimatedPerformanceGain = optimization.EstimatedGain,
                                UnitySpecificOptimization = optimization.UnityOptimizations,
                                Priority = DetermineOptimizationPriority(optimization.EstimatedGain, pattern.EstimatedPerformanceImpact)
                            });
                        }
                    }
                }
            }
            
            return opportunities.OrderByDescending(o => o.EstimatedPerformanceGain);
        }
        
        private async Task<UnityProjectStructure> AnalyzeProjectStructure(string projectPath)
        {
            var structure = new UnityProjectStructure();
            
            // Read Unity version from ProjectVersion.txt
            var versionFile = Path.Combine(projectPath, "ProjectSettings", "ProjectVersion.txt");
            if (File.Exists(versionFile))
            {
                var versionContent = await File.ReadAllTextAsync(versionFile);
                var versionLine = versionContent.Split('\n').FirstOrDefault(l => l.StartsWith("m_EditorVersion:"));
                if (versionLine != null)
                {
                    structure.UnityVersion = versionLine.Split(':')[1].Trim();
                }
            }
            
            // Read project name from ProjectSettings.asset
            var projectSettingsFile = Path.Combine(projectPath, "ProjectSettings", "ProjectSettings.asset");
            if (File.Exists(projectSettingsFile))
            {
                var settingsContent = await File.ReadAllTextAsync(projectSettingsFile);
                structure.ProjectName = ExtractProjectName(settingsContent);
            }
            
            // Find all scripts
            structure.Scripts = await FindFilesAsync(projectPath, "*.cs");
            
            // Find all scenes
            structure.Scenes = await FindFilesAsync(projectPath, "*.unity");
            
            // Find all assets
            structure.Assets = await FindFilesAsync(Path.Combine(projectPath, "Assets"), "*.*");
            
            // Find packages
            structure.Packages = await FindFilesAsync(Path.Combine(projectPath, "Packages"), "*.json");
            
            return structure;
        }
        
        private async Task<IEnumerable<PerformanceRecommendation>> GeneratePerformanceRecommendations(UnityProjectAnalysis analysis)
        {
            var recommendations = new List<PerformanceRecommendation>();
            
            // Script performance recommendations
            if (analysis.ScriptAnalysis.PerformanceIssues.Any())
            {
                recommendations.Add(new PerformanceRecommendation
                {
                    Category = "Script Performance",
                    Description = "Optimize script performance issues",
                    EstimatedImprovement = 0.2,
                    Priority = OptimizationPriority.High,
                    Actions = analysis.ScriptAnalysis.PerformanceIssues.Select(i => i.Description).ToList()
                });
            }
            
            // Asset optimization recommendations
            if (analysis.AssetAnalysis.OptimizableAssetSize > 0)
            {
                var sizeReduction = (double)analysis.AssetAnalysis.OptimizableAssetSize / analysis.AssetAnalysis.TotalAssetSize;
                recommendations.Add(new PerformanceRecommendation
                {
                    Category = "Asset Optimization",
                    Description = "Optimize asset sizes and formats",
                    EstimatedImprovement = sizeReduction,
                    Priority = OptimizationPriority.Medium,
                    Actions = new[] { "Compress textures", "Optimize audio formats", "Remove unused assets" }
                });
            }
            
            // Rendering optimization recommendations
            if (analysis.SceneAnalysis.OptimizableDrawCalls > 0)
            {
                var drawCallReduction = (double)analysis.SceneAnalysis.OptimizableDrawCalls / analysis.SceneAnalysis.TotalDrawCalls;
                recommendations.Add(new PerformanceRecommendation
                {
                    Category = "Rendering Performance",
                    Description = "Optimize rendering performance",
                    EstimatedImprovement = drawCallReduction * 0.3, // 30% of draw call reduction
                    Priority = OptimizationPriority.High,
                    Actions = new[] { "Batch draw calls", "Use LOD groups", "Optimize materials" }
                });
            }
            
            return recommendations;
        }
        
        private async Task<UnityOptimizationEvaluation> EvaluateUnityOptimization(IterationPattern pattern)
        {
            var evaluation = new UnityOptimizationEvaluation();
            
            // Unity-specific optimizations
            if (pattern.UsesGetComponent)
            {
                evaluation.UnityOptimizations.Add("Cache GetComponent calls");
                evaluation.EstimatedGain += 0.3; // 30% improvement
            }
            
            if (pattern.UsesForeach && pattern.CollectionType == "List<T>")
            {
                evaluation.OptimizedCode = pattern.Code.Replace("foreach", "for");
                evaluation.UnityOptimizations.Add("Use for-loop instead of foreach for better Unity performance");
                evaluation.EstimatedGain += 0.2; // 20% improvement
            }
            
            if (pattern.UsesLINQ)
            {
                evaluation.OptimizedCode = ConvertLinqToForLoop(pattern.Code);
                evaluation.UnityOptimizations.Add("Replace LINQ with for-loop for better mobile performance");
                evaluation.EstimatedGain += 0.4; // 40% improvement on mobile
            }
            
            evaluation.HasOptimization = evaluation.UnityOptimizations.Any();
            
            return evaluation;
        }
        
        private string ConvertLinqToForLoop(string code)
        {
            // Simple LINQ to for-loop conversion examples
            if (code.Contains(".Where("))
            {
                return code.Replace(".Where(", "// Convert to for-loop with if condition");
            }
            
            if (code.Contains(".Select("))
            {
                return code.Replace(".Select(", "// Convert to for-loop with assignment");
            }
            
            if (code.Contains(".FirstOrDefault("))
            {
                return code.Replace(".FirstOrDefault(", "// Convert to for-loop with early return");
            }
            
            return code;
        }
        
        private OptimizationPriority DetermineOptimizationPriority(double estimatedGain, double performanceImpact)
        {
            var combinedImpact = estimatedGain * performanceImpact;
            
            return combinedImpact switch
            {
                >= 0.5 => OptimizationPriority.Critical,
                >= 0.3 => OptimizationPriority.High,
                >= 0.1 => OptimizationPriority.Medium,
                _ => OptimizationPriority.Low
            };
        }
        
        private async Task<IEnumerable<string>> FindFilesAsync(string directory, string pattern)
        {
            if (!Directory.Exists(directory))
                return new List<string>();
            
            var files = new List<string>();
            
            try
            {
                var foundFiles = Directory.GetFiles(directory, pattern, SearchOption.AllDirectories);
                files.AddRange(foundFiles.Select(f => Path.GetRelativePath(directory, f)));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to find files in directory: {Directory}", directory);
            }
            
            return files;
        }
        
        private string ExtractProjectName(string projectSettingsContent)
        {
            // Extract project name from ProjectSettings.asset
            var lines = projectSettingsContent.Split('\n');
            var productNameLine = lines.FirstOrDefault(l => l.Trim().StartsWith("productName:"));
            
            if (productNameLine != null)
            {
                var parts = productNameLine.Split(':');
                if (parts.Length > 1)
                {
                    return parts[1].Trim();
                }
            }
            
            return "Unknown Project";
        }
    }
    
    /// <summary>
    /// File system service interface
    /// </summary>
    public interface IFileSystemService
    {
        Task<bool> FileExistsAsync(string path);
        Task<bool> DirectoryExistsAsync(string path);
        Task<string> ReadAllTextAsync(string path);
        Task<IEnumerable<string>> GetFilesAsync(string directory, string pattern, SearchOption searchOption);
    }
    
    /// <summary>
    /// File system service implementation
    /// </summary>
    public class FileSystemService : IFileSystemService
    {
        public async Task<bool> FileExistsAsync(string path)
        {
            return await Task.FromResult(File.Exists(path));
        }
        
        public async Task<bool> DirectoryExistsAsync(string path)
        {
            return await Task.FromResult(Directory.Exists(path));
        }
        
        public async Task<string> ReadAllTextAsync(string path)
        {
            return await File.ReadAllTextAsync(path);
        }
        
        public async Task<IEnumerable<string>> GetFilesAsync(string directory, string pattern, SearchOption searchOption)
        {
            if (!Directory.Exists(directory))
                return new List<string>();
            
            try
            {
                var files = Directory.GetFiles(directory, pattern, searchOption);
                return await Task.FromResult(files);
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}
