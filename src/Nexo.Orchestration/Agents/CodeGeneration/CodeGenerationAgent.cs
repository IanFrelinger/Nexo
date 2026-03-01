using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Agents.Models;
using Nexo.Orchestration.Architect.Models;
using System.Text.Json;
using ModelInput = Nexo.Abstractions.ModelInput;

namespace Nexo.Orchestration.Agents.CodeGeneration;

/// <summary>
/// Agent specialized for code generation in multiple programming languages.
/// 
/// Responsibilities:
/// - Generates code from requirements using LLM (IModel)
/// - Supports multiple languages (C#, Python, JavaScript, etc.)
/// - Analyzes generated code for quality, security, and performance
/// - Optimizes code based on analysis results
/// - Validates code against constraints
/// - Extracts requirements from agent specifications
/// - Gathers context from dependency outputs
/// 
/// Uses CodeAnalyzer and CodeOptimizer for code quality improvements.
/// Inherits from BaseAgent for lifecycle management.
/// </summary>
public sealed class CodeGenerationAgent : BaseAgent
{
    private readonly IModel? _model;
    private readonly CodeAnalyzer? _analyzer;
    private readonly CodeOptimizer? _optimizer;

    public CodeGenerationAgent(
        AgentSpawnSpec spec,
        ILogger<BaseAgent> logger,
        IModel? model = null,
        CodeAnalyzer? analyzer = null,
        CodeOptimizer? optimizer = null)
        : base(spec, logger)
    {
        _model = model;
        _analyzer = analyzer;
        _optimizer = optimizer;
    }

    protected override Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        Logger.LogDebug("CodeGenerationAgent {AgentId} initializing", Spec.AgentId);
        return Task.CompletedTask;
    }

    protected override Task OnDependenciesResolvedAsync(
        IReadOnlyDictionary<string, object> dependencyOutputs,
        CancellationToken cancellationToken)
    {
        Logger.LogDebug("CodeGenerationAgent {AgentId} dependencies resolved", Spec.AgentId);
        return Task.CompletedTask;
    }

    protected override async Task<object> OnExecuteAsync(
        IReadOnlyDictionary<string, object>? dependencies,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation("CodeGenerationAgent {AgentId} executing", Spec.AgentId);

        // Extract requirements from goal/description
        var requirements = ExtractRequirements(Spec.Goal, Spec.Description);
        var language = DetermineLanguage(requirements);
        var codeContext = GatherCodeContext(dependencies ?? new Dictionary<string, object>());

        // Generate code using LLM
        var generatedCode = await GenerateCodeAsync(requirements, language, codeContext, cancellationToken);

        // Analyze generated code
        var analysis = _analyzer != null
            ? await _analyzer.AnalyzeAsync(generatedCode, language, cancellationToken)
            : null;

        // Optimize if requested
        var optimizedCode = _optimizer != null && ShouldOptimize(analysis)
            ? await _optimizer.OptimizeAsync(generatedCode, language, analysis, cancellationToken)
            : generatedCode;

        // Validate constraints
        ValidateConstraints(optimizedCode, analysis);

        return new CodeGenerationResult
        {
            Code = optimizedCode,
            Language = language,
            Analysis = analysis,
            GeneratedAt = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["agentId"] = Spec.AgentId,
                ["requirements"] = requirements,
                ["originalCodeLength"] = generatedCode.Length,
                ["optimizedCodeLength"] = optimizedCode.Length
            }
        };
    }

    private CodeRequirements ExtractRequirements(string goal, string? description)
    {
        // Extract requirements from goal and description using simple parsing
        // In a real implementation, this would use LLM or NLP
        var fullText = $"{goal} {description ?? ""}";
        
        return new CodeRequirements
        {
            Functionality = goal,
            Language = ExtractLanguage(fullText),
            Patterns = ExtractPatterns(fullText),
            Constraints = Spec.Constraints.Select(c => c.Description).ToList()
        };
    }

    private string DetermineLanguage(CodeRequirements requirements)
    {
        if (!string.IsNullOrWhiteSpace(requirements.Language))
        {
            return requirements.Language;
        }

        // Default based on domain or constraints
        return Spec.Domain.ToLowerInvariant() switch
        {
            "infrastructure" => "yaml",
            "security" => "csharp",
            "gameplay" => "csharp",
            _ => "csharp"
        };
    }

    private CodeContext GatherCodeContext(IReadOnlyDictionary<string, object> dependencies)
    {
        var contextDeps = new Dictionary<string, object>();

        foreach (var (key, value) in dependencies)
        {
            if (value is JsonElement jsonElement)
            {
                // Try to extract code-related information
                if (jsonElement.TryGetProperty("code", out var codeProp))
                {
                    contextDeps[key] = codeProp.GetString() ?? "";
                }
                else if (jsonElement.TryGetProperty("schema", out var schemaProp))
                {
                    contextDeps[key] = schemaProp;
                }
                else
                {
                    contextDeps[key] = value;
                }
            }
            else
            {
                contextDeps[key] = value;
            }
        }

        return new CodeContext
        {
            Dependencies = contextDeps
        };
    }

    private async Task<string> GenerateCodeAsync(
        CodeRequirements requirements,
        string language,
        CodeContext context,
        CancellationToken cancellationToken)
    {
        if (_model == null)
        {
            // Fallback: return placeholder code
            return GeneratePlaceholderCode(requirements, language);
        }

        var prompt = BuildCodeGenerationPrompt(requirements, language, context);
        var modelInput = new ModelInput(new List<(string role, string content)>
        {
            ("user", prompt)
        });
        var modelOutput = await _model.CompleteAsync(modelInput, cancellationToken);
        
        // Extract code from response (handle markdown code blocks)
        return ExtractCodeFromResponse(modelOutput.Text, language);
    }

    private string BuildCodeGenerationPrompt(
        CodeRequirements requirements,
        string language,
        CodeContext context)
    {
        var prompt = $@"Generate {language} code that implements the following requirements:

Goal: {requirements.Functionality}

Language: {language}
Patterns: {string.Join(", ", requirements.Patterns)}

Constraints:
{string.Join("\n", requirements.Constraints.Select(c => $"- {c}"))}

Dependencies:
{string.Join("\n", context.Dependencies.Select(kvp => $"- {kvp.Key}: {kvp.Value}"))}

Generate clean, well-documented, production-ready code. Include comments explaining key decisions.";

        return prompt;
    }

    private string ExtractCodeFromResponse(string response, string language)
    {
        // Extract code from markdown code blocks
        var codeBlockPattern = $@"```{language}\s*(.*?)```";
        var match = System.Text.RegularExpressions.Regex.Match(
            response,
            codeBlockPattern,
            System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (match.Success && match.Groups.Count > 1)
        {
            return match.Groups[1].Value.Trim();
        }

        // Fallback: try generic code block
        var genericPattern = @"```\s*(.*?)```";
        match = System.Text.RegularExpressions.Regex.Match(
            response,
            genericPattern,
            System.Text.RegularExpressions.RegexOptions.Singleline);

        if (match.Success && match.Groups.Count > 1)
        {
            return match.Groups[1].Value.Trim();
        }

        // Last resort: return response as-is
        return response.Trim();
    }

    private string GeneratePlaceholderCode(CodeRequirements requirements, string language)
    {
        var msg = requirements.Functionality;
        return language.ToLowerInvariant() switch
        {
            "csharp" => $@"// Generated placeholder for: {msg}
public class GeneratedCode
{{
    public void Execute()
    {{
        throw new NotImplementedException(""Implement: {msg}"");
    }}
}}",
            "python" => $@"# Generated placeholder for: {msg}
def generated_code():
    raise NotImplementedError(""Implement: {msg}"")",
            "javascript" => $@"// Generated placeholder for: {msg}
function generatedCode() {{
    throw new Error(""Implement: {msg}"");
}}",
            "typescript" => $@"// Generated placeholder for: {msg}
function generatedCode(): void {{
    throw new Error(""Implement: {msg}"");
}}",
            _ => $"// Generated placeholder for: {msg}\n// Language: {language}"
        };
    }

    private bool ShouldOptimize(CodeAnalysis? analysis)
    {
        if (analysis == null) return false;

        // Optimize if there are issues or if optimization is requested in constraints
        return analysis.Issues.Count > 0 ||
               Spec.Constraints.Any(c => c.Description.ToLowerInvariant().Contains("optimize"));
    }

    private void ValidateConstraints(string code, CodeAnalysis? analysis)
    {
        foreach (var constraint in Spec.Constraints)
        {
            if (constraint.Type == "performance" && analysis != null)
            {
                // Check performance metrics
                if (analysis.Complexity > 10)
                {
                    Logger.LogWarning("Code complexity ({Complexity}) exceeds recommended threshold", analysis.Complexity);
                }
            }

            if (constraint.Type == "security" && analysis != null)
            {
                // Check security issues
                var securityIssues = analysis.Issues.Where(i => i.Severity == IssueSeverity.High && i.Category == "Security").ToList();
                if (securityIssues.Count > 0)
                {
                    throw new InvalidOperationException($"Security constraint violated: {string.Join(", ", securityIssues.Select(i => i.Message))}");
                }
            }
        }
    }

    private string ExtractLanguage(string text)
    {
        var languages = new[] { "csharp", "python", "javascript", "typescript", "java", "go", "rust", "yaml", "json" };
        var lowerText = text.ToLowerInvariant();
        
        foreach (var lang in languages)
        {
            if (lowerText.Contains(lang))
            {
                return lang;
            }
        }

        return "";
    }

    private List<string> ExtractPatterns(string text)
    {
        var patterns = new List<string>();
        var knownPatterns = new[] { "singleton", "factory", "observer", "strategy", "repository", "mvc", "mvp", "mvvm" };
        var lowerText = text.ToLowerInvariant();

        foreach (var pattern in knownPatterns)
        {
            if (lowerText.Contains(pattern))
            {
                patterns.Add(pattern);
            }
        }

        return patterns;
    }

    protected override Task OnShutdownAsync(CancellationToken cancellationToken)
    {
        Logger.LogDebug("CodeGenerationAgent {AgentId} shutting down", Spec.AgentId);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Requirements for code generation.
/// </summary>
public sealed record CodeRequirements
{
    public required string Functionality { get; init; }
    public string? Language { get; init; }
    public IReadOnlyList<string> Patterns { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Constraints { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Context for code generation (dependencies, existing code, etc.).
/// </summary>
public sealed record CodeContext
{
    public IReadOnlyDictionary<string, object> Dependencies { get; init; } = new Dictionary<string, object>();
}

/// <summary>
/// Result of code generation.
/// </summary>
public sealed record CodeGenerationResult
{
    public required string Code { get; init; }
    public required string Language { get; init; }
    public CodeAnalysis? Analysis { get; init; }
    public required DateTimeOffset GeneratedAt { get; init; }
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}

