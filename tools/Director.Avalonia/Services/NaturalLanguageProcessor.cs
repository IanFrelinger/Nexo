using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Director.Avalonia.Services;

/// <summary>
/// Service for processing natural language prompts and converting them to code modifications
/// </summary>
public class NaturalLanguageProcessor
{
    private readonly ILogger<NaturalLanguageProcessor> _logger;
    private readonly CodeModificationService _codeModificationService;

    public NaturalLanguageProcessor(ILogger<NaturalLanguageProcessor> logger, CodeModificationService codeModificationService)
    {
        _logger = logger;
        _codeModificationService = codeModificationService;
    }

    /// <summary>
    /// Processes a natural language prompt and generates code modifications
    /// </summary>
    public async Task<CodeModificationResult> ProcessPromptAsync(string prompt, string targetClass, string targetMethod)
    {
        try
        {
            _logger.LogInformation($"Processing natural language prompt: {prompt}");

            // Parse the prompt to extract intent and parameters
            var parsedPrompt = ParsePrompt(prompt);
            
            // Generate code based on the parsed prompt
            var generatedCode = await GenerateCodeFromPromptAsync(parsedPrompt, targetClass, targetMethod);
            
            // Validate the generated code
            var validationResult = await _codeModificationService.ValidateCodeAsync(generatedCode, targetClass, targetMethod);
            
            var result = new CodeModificationResult
            {
                Success = validationResult.IsValid,
                GeneratedCode = generatedCode,
                ValidationResult = validationResult,
                ParsedPrompt = parsedPrompt,
                TargetClass = targetClass,
                TargetMethod = targetMethod
            };

            _logger.LogInformation($"Processed prompt successfully: {prompt}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to process prompt: {prompt}");
            return new CodeModificationResult
            {
                Success = false,
                Error = ex.Message,
                TargetClass = targetClass,
                TargetMethod = targetMethod
            };
        }
    }

    /// <summary>
    /// Processes a complex prompt with multiple modifications
    /// </summary>
    public async Task<List<CodeModificationResult>> ProcessComplexPromptAsync(string prompt, List<string> targetClasses)
    {
        try
        {
            _logger.LogInformation($"Processing complex prompt: {prompt}");

            var results = new List<CodeModificationResult>();
            var parsedPrompt = ParsePrompt(prompt);

            // Split complex prompts into individual modifications
            var modifications = SplitComplexPrompt(parsedPrompt);

            foreach (var modification in modifications)
            {
                var targetClass = modification.TargetClass ?? targetClasses.FirstOrDefault() ?? "Unknown";
                var targetMethod = modification.TargetMethod ?? "Process";

                var result = await ProcessPromptAsync(modification.OriginalPrompt, targetClass, targetMethod);
                results.Add(result);
            }

            _logger.LogInformation($"Processed complex prompt with {results.Count} modifications");
            await Task.CompletedTask; // Ensure async method
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to process complex prompt: {prompt}");
            return new List<CodeModificationResult>();
        }
    }

    /// <summary>
    /// Suggests code modifications based on a natural language description
    /// </summary>
    public async Task<List<CodeSuggestion>> SuggestModificationsAsync(string description, string currentCode)
    {
        try
        {
            _logger.LogInformation($"Generating suggestions for: {description}");

            var suggestions = new List<CodeSuggestion>();

            // Analyze the current code
            var codeAnalysis = AnalyzeCode(currentCode);
            
            // Generate suggestions based on the description
            var parsedDescription = ParsePrompt(description);
            
            suggestions.AddRange(GenerateSuggestionsFromDescription(parsedDescription, codeAnalysis));

            _logger.LogInformation($"Generated {suggestions.Count} suggestions");
            await Task.CompletedTask; // Ensure async method
            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to generate suggestions: {description}");
            return new List<CodeSuggestion>();
        }
    }

    private ParsedPrompt ParsePrompt(string prompt)
    {
        var parsed = new ParsedPrompt
        {
            OriginalPrompt = prompt,
            Intent = ExtractIntent(prompt),
            Parameters = ExtractParameters(prompt),
            TargetClass = ExtractTargetClass(prompt),
            TargetMethod = ExtractTargetMethod(prompt),
            Modifications = ExtractModifications(prompt)
        };

        return parsed;
    }

    private IntentType ExtractIntent(string prompt)
    {
        var lowerPrompt = prompt.ToLowerInvariant();

        if (lowerPrompt.Contains("add") || lowerPrompt.Contains("create") || lowerPrompt.Contains("new"))
        {
            return IntentType.Add;
        }
        else if (lowerPrompt.Contains("modify") || lowerPrompt.Contains("change") || lowerPrompt.Contains("update"))
        {
            return IntentType.Modify;
        }
        else if (lowerPrompt.Contains("remove") || lowerPrompt.Contains("delete") || lowerPrompt.Contains("eliminate"))
        {
            return IntentType.Remove;
        }
        else if (lowerPrompt.Contains("fix") || lowerPrompt.Contains("repair") || lowerPrompt.Contains("correct"))
        {
            return IntentType.Fix;
        }
        else if (lowerPrompt.Contains("optimize") || lowerPrompt.Contains("improve") || lowerPrompt.Contains("enhance"))
        {
            return IntentType.Optimize;
        }
        else
        {
            return IntentType.Unknown;
        }
    }

    private Dictionary<string, string> ExtractParameters(string prompt)
    {
        var parameters = new Dictionary<string, string>();

        // Extract common parameters
        var patterns = new Dictionary<string, string>
        {
            { @"color\s+(\w+)", "color" },
            { @"size\s+(\d+)", "size" },
            { @"width\s+(\d+)", "width" },
            { @"height\s+(\d+)", "height" },
            { @"text\s+['""]([^'""]+)['""]", "text" },
            { @"value\s+(\d+)", "value" },
            { @"timeout\s+(\d+)", "timeout" },
            { @"delay\s+(\d+)", "delay" }
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(prompt, pattern.Key, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                parameters[pattern.Value] = match.Groups[1].Value;
            }
        }

        return parameters;
    }

    private string? ExtractTargetClass(string prompt)
    {
        var patterns = new[]
        {
            @"class\s+(\w+)",
            @"in\s+(\w+)\s+class",
            @"for\s+(\w+)\s+class"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(prompt, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        return null;
    }

    private string? ExtractTargetMethod(string prompt)
    {
        var patterns = new[]
        {
            @"method\s+(\w+)",
            @"function\s+(\w+)",
            @"in\s+(\w+)\s+method",
            @"for\s+(\w+)\s+method"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(prompt, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        return null;
    }

    private List<string> ExtractModifications(string prompt)
    {
        var modifications = new List<string>();

        // Split by common separators
        var sentences = prompt.Split(new[] { '.', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var sentence in sentences)
        {
            var trimmed = sentence.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                modifications.Add(trimmed);
            }
        }

        return modifications;
    }

    private List<CodeModification> SplitComplexPrompt(ParsedPrompt parsedPrompt)
    {
        var modifications = new List<CodeModification>();

        foreach (var modification in parsedPrompt.Modifications)
        {
            modifications.Add(new CodeModification
            {
                OriginalPrompt = modification,
                Intent = ExtractIntent(modification),
                Parameters = ExtractParameters(modification),
                TargetClass = ExtractTargetClass(modification) ?? parsedPrompt.TargetClass,
                TargetMethod = ExtractTargetMethod(modification) ?? parsedPrompt.TargetMethod
            });
        }

        return modifications;
    }

    private async Task<string> GenerateCodeFromPromptAsync(ParsedPrompt parsedPrompt, string targetClass, string targetMethod)
    {
        var codeGenerator = new CodeGenerator(_logger as ILogger<CodeGenerator> ?? new Logger<CodeGenerator>(new LoggerFactory()));
        return await codeGenerator.GenerateCodeAsync(parsedPrompt, targetClass, targetMethod);
    }

    private CodeAnalysis AnalyzeCode(string code)
    {
        return new CodeAnalysis
        {
            HasAsyncMethods = code.Contains("async") || code.Contains("await"),
            HasErrorHandling = code.Contains("try") || code.Contains("catch") || code.Contains("throw"),
            HasLogging = code.Contains("_logger") || code.Contains("Log"),
            HasValidation = code.Contains("Validate") || code.Contains("Check"),
            Complexity = CalculateComplexity(code)
        };
    }

    private int CalculateComplexity(string code)
    {
        var complexity = 0;
        
        // Count control structures
        complexity += Regex.Matches(code, @"\b(if|while|for|foreach|switch|case)\b").Count;
        complexity += Regex.Matches(code, @"\b(&&|\|\|)\b").Count;
        complexity += Regex.Matches(code, @"\b(try|catch|finally)\b").Count;
        
        return complexity;
    }

    private List<CodeSuggestion> GenerateSuggestionsFromDescription(ParsedPrompt description, CodeAnalysis analysis)
    {
        var suggestions = new List<CodeSuggestion>();

        // Generate suggestions based on the description and current code analysis
        if (description.Intent == IntentType.Optimize)
        {
            if (!analysis.HasErrorHandling)
            {
                suggestions.Add(new CodeSuggestion
                {
                    Type = SuggestionType.AddErrorHandling,
                    Description = "Add error handling for better robustness",
                    Code = "try { /* existing code */ } catch (Exception ex) { _logger.LogError(ex, \"Error occurred\"); }"
                });
            }

            if (!analysis.HasLogging)
            {
                suggestions.Add(new CodeSuggestion
                {
                    Type = SuggestionType.AddLogging,
                    Description = "Add logging for better debugging",
                    Code = "_logger.LogInformation(\"Method executed successfully\");"
                });
            }
        }

        return suggestions;
    }
}

public class ParsedPrompt
{
    public string OriginalPrompt { get; set; } = string.Empty;
    public IntentType Intent { get; set; }
    public Dictionary<string, string> Parameters { get; set; } = new();
    public string? TargetClass { get; set; }
    public string? TargetMethod { get; set; }
    public List<string> Modifications { get; set; } = new();
}

public class CodeModification
{
    public string OriginalPrompt { get; set; } = string.Empty;
    public IntentType Intent { get; set; }
    public Dictionary<string, string> Parameters { get; set; } = new();
    public string? TargetClass { get; set; }
    public string? TargetMethod { get; set; }
}

public class CodeModificationResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string GeneratedCode { get; set; } = string.Empty;
    public ValidationResult? ValidationResult { get; set; }
    public ParsedPrompt? ParsedPrompt { get; set; }
    public string TargetClass { get; set; } = string.Empty;
    public string TargetMethod { get; set; } = string.Empty;
}

public class CodeSuggestion
{
    public SuggestionType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int Priority { get; set; } = 1;
}

public class CodeAnalysis
{
    public bool HasAsyncMethods { get; set; }
    public bool HasErrorHandling { get; set; }
    public bool HasLogging { get; set; }
    public bool HasValidation { get; set; }
    public int Complexity { get; set; }
}


public enum SuggestionType
{
    AddErrorHandling,
    AddLogging,
    AddValidation,
    OptimizePerformance,
    ImproveReadability
}
