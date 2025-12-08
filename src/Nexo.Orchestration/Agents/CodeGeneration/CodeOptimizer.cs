using Microsoft.Extensions.Logging;

namespace Nexo.Orchestration.Agents.CodeGeneration;

/// <summary>
/// Optimizes generated code for performance, readability, and best practices.
/// </summary>
public sealed class CodeOptimizer
{
    private readonly ILogger<CodeOptimizer>? _logger;

    public CodeOptimizer(ILogger<CodeOptimizer>? logger = null)
    {
        _logger = logger;
    }

    public Task<string> OptimizeAsync(
        string code,
        string language,
        CodeAnalysis? analysis,
        CancellationToken cancellationToken = default)
    {
        var optimized = code;

        // Apply optimizations based on analysis
        if (analysis != null)
        {
            // Fix security issues
            optimized = FixSecurityIssues(optimized, analysis, language);

            // Fix performance issues
            optimized = FixPerformanceIssues(optimized, analysis, language);

            // Reduce complexity if too high
            if (analysis.Complexity > 10)
            {
                optimized = ReduceComplexity(optimized, language);
            }
        }

        // Apply general optimizations
        optimized = ApplyGeneralOptimizations(optimized, language);

        _logger?.LogDebug("Optimized code: {OriginalLength} -> {OptimizedLength} characters",
            code.Length, optimized.Length);

        return Task.FromResult(optimized);
    }

    private string FixSecurityIssues(string code, CodeAnalysis analysis, string language)
    {
        var result = code;

        foreach (var issue in analysis.Issues.Where(i => i.Category == "Security"))
        {
            if (issue.Message.Contains("Hardcoded password") || issue.Message.Contains("Hardcoded API key"))
            {
                // Replace with configuration reference
                var configRef = language == "csharp"
                    ? "configuration.GetValue<string>(\"Password\")"
                    : "os.getenv('PASSWORD')";
                result = System.Text.RegularExpressions.Regex.Replace(
                    result,
                    @"password\s*=\s*[""'][^""']+[""']",
                    $"password = {configRef}",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
        }

        return result;
    }

    private string FixPerformanceIssues(string code, CodeAnalysis analysis, string language)
    {
        var result = code;

        foreach (var issue in analysis.Issues.Where(i => i.Category == "Performance"))
        {
            if (issue.Message.Contains("sleep"))
            {
                // Replace blocking sleep with async delay
                if (language == "csharp")
                {
                    result = result.Replace("Thread.Sleep", "await Task.Delay");
                }
                else if (language == "python")
                {
                    result = result.Replace("time.sleep", "await asyncio.sleep");
                }
            }
        }

        return result;
    }

    private string ReduceComplexity(string code, string language)
    {
        // Simplified complexity reduction - extract complex logic into helper methods
        // In a real implementation, this would use AST analysis and refactoring
        return code; // Placeholder - would need sophisticated refactoring
    }

    private string ApplyGeneralOptimizations(string code, string language)
    {
        var optimized = code;

        // Remove excessive whitespace
        optimized = System.Text.RegularExpressions.Regex.Replace(optimized, @"\n\s*\n\s*\n", "\n\n");

        // Ensure consistent indentation
        // (Simplified - real implementation would preserve structure)

        return optimized;
    }
}

