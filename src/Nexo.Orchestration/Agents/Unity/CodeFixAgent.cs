using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Infrastructure.IO;
using Nexo.Orchestration.Architect.Models;
using System.Text;
using System.Text.RegularExpressions;
using ModelInput = Nexo.Abstractions.ModelInput;

namespace Nexo.Orchestration.Agents.Unity;

/// <summary>
/// Agent that automatically fixes C# syntax errors in Unity scripts.
/// 
/// Responsibilities:
/// - Receives syntax errors from UnityErrorAnalysisAgent
/// - Applies automatic fixes for common C# syntax errors
/// - Uses LLM for complex error fixes when available
/// - Updates Unity script files with fixes
/// - Reports fixed files and failed fixes
/// 
/// Inherits from BaseDomainAgent for LLM integration.
/// Used in Unity development workflows to automatically fix compilation errors.
/// </summary>
public class CodeFixAgent : BaseDomainAgent
{
    private readonly string? _projectPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeFixAgent"/> class.
    /// </summary>
    /// <param name="spec">The agent's spawn specification.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="model">Optional LLM for complex error fixes.</param>
    public CodeFixAgent(
        AgentSpawnSpec spec,
        ILogger<BaseAgent> logger,
        IModel? model = null)
        : base(spec, logger, model)
    {
        // Extract parameters from description or goal
        _projectPath = ExtractParameter(spec.Description ?? spec.Goal, "projectPath");
    }

    /// <summary>
    /// Extracts a parameter value from a text string using regex pattern matching.
    /// </summary>
    /// <param name="text">The text to search in.</param>
    /// <param name="key">The parameter key to extract.</param>
    /// <returns>The extracted parameter value, or null if not found.</returns>
    private string? ExtractParameter(string text, string key)
    {
        if (string.IsNullOrEmpty(text)) return null;
        var pattern = $@"{key}["":\s]+([^\s""\n]+)";
        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Initializes the Code Fix Agent.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    protected override Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Initializing Code Fix Agent: {AgentId}", Spec.AgentId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles dependencies being resolved for the Code Fix Agent.
    /// </summary>
    /// <param name="dependencyOutputs">Outputs from upstream agents, expected to contain a <see cref="UnityErrorAnalysis"/>.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    protected override Task OnDependenciesResolvedAsync(
        IReadOnlyDictionary<string, object> dependencyOutputs,
        CancellationToken cancellationToken)
    {
        Logger.LogDebug("Dependencies resolved for {AgentId}", Spec.AgentId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executes the code fixing process, applying fixes to Unity script files.
    /// </summary>
    /// <param name="dependencyOutputs">Optional outputs from upstream agents, expected to contain a <see cref="UnityErrorAnalysis"/> with syntax errors.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="CodeFixResult"/> object containing fixed files and failure information.</returns>
    protected override async Task<object> OnExecuteAsync(
        IReadOnlyDictionary<string, object>? dependencyOutputs,
        CancellationToken cancellationToken)
    {
        // Get syntax errors from dependency (UnityErrorAnalysisAgent)
        var errorAnalysis = dependencyOutputs?.Values
            .OfType<UnityErrorAnalysis>()
            .FirstOrDefault();

        if (errorAnalysis == null || errorAnalysis.SyntaxErrors.Count == 0)
        {
            Logger.LogInformation("No syntax errors to fix");
            return new CodeFixResult
            {
                FixedFiles = new List<string>(),
                FailedFixes = new List<string>(),
                TotalFixed = 0
            };
        }

        Logger.LogInformation("Fixing {Count} syntax errors", errorAnalysis.SyntaxErrors.Count);

        var result = new CodeFixResult
        {
            FixedFiles = new List<string>(),
            FailedFixes = new List<string>(),
            TotalFixed = 0
        };

        // Group errors by file
        var errorsByFile = errorAnalysis.SyntaxErrors.GroupBy(e => e.FilePath);

        foreach (var fileGroup in errorsByFile)
        {
            var filePath = ResolveFilePath(fileGroup.Key);
            if (string.IsNullOrEmpty(filePath) || !new FileInfo(filePath).Exists)
            {
                Logger.LogWarning("File not found: {FilePath}", fileGroup.Key);
                result.FailedFixes.Add($"{fileGroup.Key}: File not found");
                continue;
            }

            try
            {
                var fileContent = await TextFile.ReadAllTextAsync(filePath, cancellationToken);
                var originalContent = fileContent;
                var errorsInFile = fileGroup.OrderByDescending(e => e.LineNumber).ToList();

                // Fix errors from bottom to top to preserve line numbers
                foreach (var error in errorsInFile)
                {
                    fileContent = await FixSyntaxError(fileContent, error, cancellationToken);
                }

                if (fileContent != originalContent)
                {
                    await TextFile.WriteAllTextAsync(filePath, fileContent, cancellationToken);
                    result.FixedFiles.Add(filePath);
                    result.TotalFixed += errorsInFile.Count;
                    Logger.LogInformation("Fixed {Count} errors in {FilePath}", errorsInFile.Count, filePath);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to fix errors in {FilePath}", filePath);
                result.FailedFixes.Add($"{filePath}: {ex.Message}");
            }
        }

        return result;
    }

    /// <summary>
    /// Shuts down the Code Fix Agent.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    protected override Task OnShutdownAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Shutting down Code Fix Agent: {AgentId}", Spec.AgentId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the system prompt for this domain.
    /// </summary>
    /// <returns>The system prompt string for C# code fixing.</returns>
    protected override string GetSystemPrompt()
    {
        return @"You are a C# code fixing expert specializing in Unity development. Your role is to automatically fix syntax errors in C# code, ensuring the fixes are correct, maintain code style, and preserve functionality.";
    }

    /// <summary>
    /// Gets mock output when no model is available.
    /// </summary>
    /// <returns>An empty <see cref="CodeFixResult"/> object.</returns>
    protected override object GetMockOutput()
    {
        return new CodeFixResult
        {
            FixedFiles = new List<string>(),
            FailedFixes = new List<string>(),
            TotalFixed = 0
        };
    }

    /// <summary>
    /// Resolves a relative file path to an absolute path within the Unity project.
    /// </summary>
    /// <param name="relativePath">The relative file path from the Unity log.</param>
    /// <returns>The resolved absolute file path, or null if not found.</returns>
    private string? ResolveFilePath(string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
        {
            return relativePath;
        }

        if (!string.IsNullOrEmpty(_projectPath))
        {
            var fullPath = Path.Combine(_projectPath, "Assets", relativePath);
            if (new FileInfo(fullPath).Exists)
            {
                return fullPath;
            }
        }

        // Try to find the file in current directory or common Unity paths
        if (new FileInfo(relativePath).Exists)
        {
            return relativePath;
        }

        return null;
    }

    /// <summary>
    /// Fixes a syntax error in a file by applying rule-based or LLM-based fixes.
    /// </summary>
    /// <param name="fileContent">The current file content.</param>
    /// <param name="error">The syntax error to fix.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>The fixed file content.</returns>
    private async Task<string> FixSyntaxError(string fileContent, SyntaxError error, CancellationToken cancellationToken)
    {
        var lines = fileContent.Split('\n');
        if (error.LineNumber <= 0 || error.LineNumber > lines.Length)
        {
            return fileContent;
        }

        var lineIndex = error.LineNumber - 1;
        var originalLine = lines[lineIndex];

        // Apply fix based on error code
        string fixedLine = error.ErrorCode switch
        {
            "CS1002" => FixMissingSemicolon(originalLine),
            "CS1003" => FixMissingClosingParen(originalLine),
            "CS1011" => FixMissingClosingBrace(originalLine, lines, lineIndex),
            "CS1513" => FixMissingClosingBrace(originalLine, lines, lineIndex),
            "CS1001" => FixMissingIdentifier(originalLine),
            "CS1006" => FixMissingMethodName(originalLine),
            "CS1008" => FixMissingTypeName(originalLine),
            "CS1009" => FixInvalidCharacter(originalLine),
            "CS1010" => FixUnexpectedNewline(originalLine, lines, lineIndex),
            "CS1012" => FixExtraCharacter(originalLine),
            "CS1014" => FixMissingAccessor(originalLine),
            "CS1017" => FixMissingClosingParen(originalLine),
            "CS1018" => FixMissingIdentifier(originalLine),
            "CS1019" => FixMissingClosingParen(originalLine),
            "CS1026" => FixMissingClosingParen(originalLine),
            "CS1035" => FixMissingQuote(originalLine),
            "CS1036" => FixMissingOpeningParen(originalLine),
            "CS1039" => FixMissingQuote(originalLine),
            "CS1043" => FixMissingSemicolon(originalLine),
            _ => await FixWithLLM(originalLine, error, cancellationToken) ?? originalLine
        };

        lines[lineIndex] = fixedLine;
        return string.Join("\n", lines);
    }

    private string FixMissingSemicolon(string line)
    {
        var trimmed = line.TrimEnd();
        if (!trimmed.EndsWith(";") && !trimmed.EndsWith("{") && !trimmed.EndsWith("}") && 
            !trimmed.EndsWith(",") && !string.IsNullOrWhiteSpace(trimmed))
        {
            return line.TrimEnd() + ";";
        }
        return line;
    }

    private string FixMissingClosingParen(string line)
    {
        var openCount = line.Count(c => c == '(');
        var closeCount = line.Count(c => c == ')');
        if (openCount > closeCount)
        {
            return line.TrimEnd() + new string(')', openCount - closeCount);
        }
        return line;
    }

    private string FixMissingClosingBrace(string line, string[] allLines, int lineIndex)
    {
        // Count braces in the file up to this point
        var content = string.Join("\n", allLines.Take(lineIndex + 1));
        var openBraces = content.Count(c => c == '{');
        var closeBraces = content.Count(c => c == '}');
        
        if (openBraces > closeBraces)
        {
            // Add missing closing brace
            return line.TrimEnd() + "\n" + new string('}', openBraces - closeBraces);
        }
        return line;
    }

    private string FixMissingIdentifier(string line)
    {
        // Try to infer identifier from context
        if (line.Contains("new ") && !Regex.IsMatch(line, @"new\s+\w"))
        {
            return line.Replace("new ", "new object ");
        }
        return line;
    }

    private string FixMissingMethodName(string line)
    {
        // This is harder to fix automatically - would need more context
        return line;
    }

    private string FixMissingTypeName(string line)
    {
        // Try common fixes
        if (line.Contains("var ") && line.Contains(" = ") && !line.Contains("var "))
        {
            return line.Replace("var ", "object ");
        }
        return line;
    }

    private string FixInvalidCharacter(string line)
    {
        // Remove common invalid characters
        return line.Replace("\u200B", "").Replace("\uFEFF", "");
    }

    private string FixUnexpectedNewline(string line, string[] allLines, int lineIndex)
    {
        // Try to join with next line if it makes sense
        if (lineIndex + 1 < allLines.Length)
        {
            var nextLine = allLines[lineIndex + 1].Trim();
            if (!string.IsNullOrEmpty(nextLine) && !nextLine.StartsWith("//"))
            {
                return line.TrimEnd() + " " + nextLine;
            }
        }
        return line;
    }

    private string FixExtraCharacter(string line)
    {
        // Remove trailing invalid characters
        return line.TrimEnd().TrimEnd(';', ',', '.');
    }

    private string FixMissingAccessor(string line)
    {
        if (line.Contains("get") && !line.Contains("set"))
        {
            return line.Replace("get", "get; set");
        }
        if (line.Contains("set") && !line.Contains("get"))
        {
            return line.Replace("set", "get; set");
        }
        return line;
    }

    private string FixMissingQuote(string line)
    {
        var quoteCount = line.Count(c => c == '"');
        if (quoteCount % 2 != 0)
        {
            return line + "\"";
        }
        return line;
    }

    private string FixMissingOpeningParen(string line)
    {
        // This is context-dependent, hard to fix automatically
        return line;
    }

    /// <summary>
    /// Attempts to fix a syntax error using an LLM when rule-based fixes are not available.
    /// </summary>
    /// <param name="line">The line of code containing the error.</param>
    /// <param name="error">The syntax error to fix.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>The fixed line of code, or null if fixing failed.</returns>
    private async Task<string?> FixWithLLM(string line, SyntaxError error, CancellationToken cancellationToken)
    {
        if (Model == null) return null;

        try
        {
            var prompt = $@"Fix this C# syntax error:

Error: {error.ErrorCode}: {error.Message}
Line: {line}

Provide only the fixed line of code, nothing else.";

            var modelInput = new ModelInput(new[]
            {
                ("system", GetSystemPrompt()),
                ("user", prompt)
            });
            var modelOutput = await Model.CompleteAsync(modelInput, cancellationToken);
            return modelOutput.Text.Trim();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to fix error with LLM");
            return null;
        }
    }
}

/// <summary>
/// Result of code fixing operation.
/// 
/// Contains:
/// - List of successfully fixed file paths
/// - List of failed fixes with error messages
/// - Total count of fixed errors
/// 
/// Produced by CodeFixAgent after applying fixes to Unity scripts.
/// </summary>
public class CodeFixResult
{
    public List<string> FixedFiles { get; set; } = new();
    public List<string> FailedFixes { get; set; } = new();
    public int TotalFixed { get; set; }
}

