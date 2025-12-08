using Microsoft.Extensions.Logging;

namespace Nexo.Orchestration.Agents.CodeGeneration;

/// <summary>
/// Analyzes generated code for quality, security, and performance issues.
/// </summary>
public sealed class CodeAnalyzer
{
    private readonly ILogger<CodeAnalyzer>? _logger;

    public CodeAnalyzer(ILogger<CodeAnalyzer>? logger = null)
    {
        _logger = logger;
    }

    public Task<CodeAnalysis> AnalyzeAsync(string code, string language, CancellationToken cancellationToken = default)
    {
        var analysis = new CodeAnalysis
        {
            Language = language,
            LineCount = code.Split('\n').Length,
            Complexity = CalculateComplexity(code, language),
            Issues = DetectIssues(code, language)
        };

        _logger?.LogDebug("Analyzed code: {LineCount} lines, complexity {Complexity}, {IssueCount} issues",
            analysis.LineCount, analysis.Complexity, analysis.Issues.Count);

        return Task.FromResult(analysis);
    }

    private int CalculateComplexity(string code, string language)
    {
        // Simplified cyclomatic complexity calculation
        var complexity = 1; // Base complexity

        // Count control flow statements
        var controlFlowKeywords = language.ToLowerInvariant() switch
        {
            "csharp" => new[] { "if", "else", "for", "foreach", "while", "switch", "case", "catch", "&&", "||", "?" },
            "python" => new[] { "if", "elif", "else", "for", "while", "except", "and", "or", "?" },
            "javascript" => new[] { "if", "else", "for", "while", "switch", "case", "catch", "&&", "||", "?" },
            _ => new[] { "if", "for", "while", "switch" }
        };

        var lowerCode = code.ToLowerInvariant();
        foreach (var keyword in controlFlowKeywords)
        {
            complexity += (lowerCode.Split(new[] { keyword }, StringSplitOptions.None).Length - 1);
        }

        return Math.Max(1, complexity);
    }

    private List<CodeIssue> DetectIssues(string code, string language)
    {
        var issues = new List<CodeIssue>();

        // Check for common security issues
        var securityPatterns = new[]
        {
            ("password", "Hardcoded password detected", IssueSeverity.High),
            ("api_key", "Hardcoded API key detected", IssueSeverity.High),
            ("secret", "Potential secret in code", IssueSeverity.Medium),
            ("eval(", "Use of eval() is dangerous", IssueSeverity.High),
            ("exec(", "Use of exec() is dangerous", IssueSeverity.High)
        };

        var lowerCode = code.ToLowerInvariant();
        foreach (var (pattern, message, severity) in securityPatterns)
        {
            if (lowerCode.Contains(pattern))
            {
                issues.Add(new CodeIssue
                {
                    Category = "Security",
                    Severity = severity,
                    Message = message,
                    LineNumber = FindLineNumber(code, pattern)
                });
            }
        }

        // Check for performance issues
        if (code.Contains("Thread.Sleep") || code.Contains("time.sleep"))
        {
            issues.Add(new CodeIssue
            {
                Category = "Performance",
                Severity = IssueSeverity.Medium,
                Message = "Blocking sleep detected - consider async alternatives",
                LineNumber = FindLineNumber(code, "sleep")
            });
        }

        // Check for code quality issues
        if (code.Split('\n').Length > 500)
        {
            issues.Add(new CodeIssue
            {
                Category = "Quality",
                Severity = IssueSeverity.Low,
                Message = "Code is very long - consider breaking into smaller functions",
                LineNumber = null
            });
        }

        return issues;
    }

    private int? FindLineNumber(string code, string pattern)
    {
        var lines = code.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].ToLowerInvariant().Contains(pattern.ToLowerInvariant()))
            {
                return i + 1;
            }
        }
        return null;
    }
}

/// <summary>
/// Result of code analysis.
/// </summary>
public sealed record CodeAnalysis
{
    public required string Language { get; init; }
    public required int LineCount { get; init; }
    public required int Complexity { get; init; }
    public IReadOnlyList<CodeIssue> Issues { get; init; } = new List<CodeIssue>();
}

/// <summary>
/// An issue found in code analysis.
/// </summary>
public sealed record CodeIssue
{
    public required string Category { get; init; }
    public required IssueSeverity Severity { get; init; }
    public required string Message { get; init; }
    public int? LineNumber { get; init; }
}

/// <summary>
/// Severity of a code issue.
/// </summary>
public enum IssueSeverity
{
    Low,
    Medium,
    High,
    Critical
}

