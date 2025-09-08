using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FeatureFactoryDemo.Data;
using FeatureFactoryDemo.Models;
using Nexo.Feature.Analysis.Interfaces;
using Nexo.Feature.Analysis.Models;

namespace FeatureFactoryDemo.Services
{
    /// <summary>
    /// Service for analyzing the codebase and providing context for code generation
    /// </summary>
    public class CodebaseAnalysisService
    {
        private readonly FeatureFactoryDbContext _context;
        private readonly ICodingStandardAnalyzer _codeAnalyzer;
        private readonly ILogger<CodebaseAnalysisService> _logger;
        
        public CodebaseAnalysisService(FeatureFactoryDbContext context, ICodingStandardAnalyzer codeAnalyzer, ILogger<CodebaseAnalysisService> logger)
        {
            _context = context;
            _codeAnalyzer = codeAnalyzer;
            _logger = logger;
        }
        
        /// <summary>
        /// Analyzes the codebase and stores context information
        /// </summary>
        public async Task AnalyzeCodebaseAsync(string codebasePath = "../src")
        {
            try
            {
                Console.WriteLine($"\n🔍 Analyzing codebase at: {codebasePath}");
                
                var csharpFiles = Directory.GetFiles(codebasePath, "*.cs", SearchOption.AllDirectories)
                    .Where(f => !f.Contains("bin") && !f.Contains("obj") && !f.Contains(".g.cs"))
                    .Take(20) // Limit to first 20 files for demo
                    .ToList();
                
                Console.WriteLine($"   Found {csharpFiles.Count} C# files to analyze");
                
                foreach (var filePath in csharpFiles)
                {
                    try
                    {
                        var content = await File.ReadAllTextAsync(filePath);
                        var relativePath = Path.GetRelativePath(codebasePath, filePath);
                        
                        // Analyze code quality
                        var analysisResult = await _codeAnalyzer.ValidateCodeAsync(content, relativePath, "codebase-analyzer");
                        
                        // Extract patterns
                        var patterns = ExtractCodePatterns(content);
                        var summary = GenerateCodeSummary(content, relativePath);
                        
                        // Store or update context
                        var existingContext = await _context.CodebaseContexts
                            .FirstOrDefaultAsync(c => c.FilePath == relativePath);
                        
                        if (existingContext != null)
                        {
                            existingContext.Content = content;
                            existingContext.Summary = summary;
                            existingContext.Patterns = patterns;
                            existingContext.QualityScore = analysisResult.Score;
                            existingContext.Violations = string.Join("; ", analysisResult.Violations.Select(v => v.Message));
                            existingContext.LastAnalyzed = DateTime.UtcNow;
                        }
                        else
                        {
                            var newContext = new CodebaseContext
                            {
                                FilePath = relativePath,
                                FileType = "C#",
                                Content = content,
                                Summary = summary,
                                Patterns = patterns,
                                QualityScore = analysisResult.Score,
                                Violations = string.Join("; ", analysisResult.Violations.Select(v => v.Message)),
                                LastAnalyzed = DateTime.UtcNow
                            };
                            
                            _context.CodebaseContexts.Add(newContext);
                        }
                        
                        Console.WriteLine($"   ✅ Analyzed: {relativePath} (Score: {analysisResult.Score}/100)");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to analyze file: {FilePath}", filePath);
                    }
                }
                
                await _context.SaveChangesAsync();
                Console.WriteLine($"   📊 Codebase analysis complete! Stored {csharpFiles.Count} file contexts.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze codebase");
            }
        }
        
        /// <summary>
        /// Gets relevant codebase context for code generation
        /// </summary>
        public async Task<CodebaseContext> GetRelevantContextAsync(string description, string platform)
        {
            try
            {
                // Look for files with similar patterns or keywords
                var keywords = description.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                
                var relevantContexts = await _context.CodebaseContexts
                    .Where(c => c.QualityScore >= 70 && 
                               (keywords.Any(k => c.Summary!.ToLower().Contains(k)) ||
                                keywords.Any(k => c.Patterns!.ToLower().Contains(k))))
                    .OrderByDescending(c => c.QualityScore)
                    .FirstOrDefaultAsync();
                
                if (relevantContexts != null)
                {
                    _logger.LogInformation("Found relevant codebase context: {FilePath}", relevantContexts.FilePath);
                    return relevantContexts;
                }
                
                // Fallback to highest quality file
                var fallbackContext = await _context.CodebaseContexts
                    .OrderByDescending(c => c.QualityScore)
                    .FirstOrDefaultAsync();
                
                return fallbackContext ?? new CodebaseContext();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get relevant codebase context");
                return new CodebaseContext();
            }
        }
        
        /// <summary>
        /// Gets codebase statistics
        /// </summary>
        public async Task<CodebaseStats> GetCodebaseStatsAsync()
        {
            try
            {
                var totalFiles = await _context.CodebaseContexts.CountAsync();
                var averageQuality = await _context.CodebaseContexts.AverageAsync(c => c.QualityScore);
                var highQualityFiles = await _context.CodebaseContexts.CountAsync(c => c.QualityScore >= 80);
                
                return new CodebaseStats
                {
                    TotalFiles = totalFiles,
                    AverageQualityScore = (int)Math.Round(averageQuality),
                    HighQualityFiles = highQualityFiles,
                    LastAnalyzed = await _context.CodebaseContexts.MaxAsync(c => c.LastAnalyzed)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get codebase statistics");
                return new CodebaseStats();
            }
        }
        
        private string ExtractCodePatterns(string content)
        {
            var patterns = new List<string>();
            
            // Extract common patterns
            if (content.Contains("public class")) patterns.Add("class-definition");
            if (content.Contains("public interface")) patterns.Add("interface-definition");
            if (content.Contains("async Task")) patterns.Add("async-methods");
            if (content.Contains("private readonly")) patterns.Add("dependency-injection");
            if (content.Contains("throw new")) patterns.Add("error-handling");
            if (content.Contains("LogInformation")) patterns.Add("logging");
            if (content.Contains("[Required]")) patterns.Add("data-annotations");
            if (content.Contains("DbContext")) patterns.Add("entity-framework");
            
            return string.Join(", ", patterns);
        }
        
        private string GenerateCodeSummary(string content, string filePath)
        {
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var summary = new List<string>();
            
            // Extract class names
            var classMatches = System.Text.RegularExpressions.Regex.Matches(content, @"public\s+class\s+(\w+)");
            foreach (System.Text.RegularExpressions.Match match in classMatches)
            {
                summary.Add($"class-{match.Groups[1].Value.ToLower()}");
            }
            
            // Extract interface names
            var interfaceMatches = System.Text.RegularExpressions.Regex.Matches(content, @"public\s+interface\s+(\w+)");
            foreach (System.Text.RegularExpressions.Match match in interfaceMatches)
            {
                summary.Add($"interface-{match.Groups[1].Value.ToLower()}");
            }
            
            // Add file type context
            if (filePath.Contains("Service")) summary.Add("service-layer");
            if (filePath.Contains("Controller")) summary.Add("controller");
            if (filePath.Contains("Model")) summary.Add("model");
            if (filePath.Contains("Repository")) summary.Add("repository");
            if (filePath.Contains("Context")) summary.Add("database-context");
            
            return string.Join(", ", summary);
        }
    }
    
    /// <summary>
    /// Statistics about the codebase
    /// </summary>
    public class CodebaseStats
    {
        public int TotalFiles { get; set; }
        public int AverageQualityScore { get; set; }
        public int HighQualityFiles { get; set; }
        public DateTime LastAnalyzed { get; set; }
    }
}
