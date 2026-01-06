using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Orchestration.Architect.Models;
using System.Text.Json;
using System.Text.RegularExpressions;
using ModelInput = Nexo.Abstractions.ModelInput;

namespace Nexo.Orchestration.Agents.Unity;

/// <summary>
/// Agent that analyzes Unity error logs and identifies syntax errors and issues.
/// 
/// Responsibilities:
/// - Parses Unity compilation logs for errors and warnings
/// - Identifies C# syntax errors (CS error codes)
/// - Provides suggested fixes for common errors
/// - Uses LLM to enhance error analysis when available
/// - Generates structured UnityErrorAnalysis output
/// 
/// Inherits from BaseDomainAgent for LLM integration.
/// Used in Unity development workflows to diagnose compilation issues.
/// </summary>
public class UnityErrorAnalysisAgent : BaseDomainAgent
{
    private readonly string? _unityLogPath;
    private readonly string? _projectPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnityErrorAnalysisAgent"/> class.
    /// </summary>
    /// <param name="spec">The agent's spawn specification.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="model">Optional LLM for enhanced error analysis.</param>
    public UnityErrorAnalysisAgent(
        AgentSpawnSpec spec,
        ILogger<BaseAgent> logger,
        IModel? model = null)
        : base(spec, logger, model)
    {
        // Extract parameters from description or goal
        _unityLogPath = ExtractParameter(spec.Description ?? spec.Goal, "unityLogPath") 
            ?? ExtractParameter(spec.Description ?? spec.Goal, "logPath");
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
    /// Initializes the Unity Error Analysis Agent.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    protected override Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Initializing Unity Error Analysis Agent: {AgentId}", Spec.AgentId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles dependencies being resolved for the Unity Error Analysis Agent.
    /// </summary>
    /// <param name="dependencyOutputs">Outputs from upstream agents.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    protected override Task OnDependenciesResolvedAsync(
        IReadOnlyDictionary<string, object> dependencyOutputs,
        CancellationToken cancellationToken)
    {
        Logger.LogDebug("Dependencies resolved for {AgentId}", Spec.AgentId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executes the Unity error analysis, parsing the log file and identifying errors.
    /// </summary>
    /// <param name="dependencyOutputs">Optional outputs from upstream agents.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="UnityErrorAnalysis"/> object containing parsed errors, warnings, and syntax errors.</returns>
    protected override async Task<object> OnExecuteAsync(
        IReadOnlyDictionary<string, object>? dependencyOutputs,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation("Analyzing Unity errors from log: {LogPath}", _unityLogPath);

        var analysisResult = new UnityErrorAnalysis
        {
            LogPath = _unityLogPath ?? "",
            ProjectPath = _projectPath ?? "",
            AnalyzedAt = DateTimeOffset.UtcNow,
            Errors = new List<UnityError>(),
            Warnings = new List<UnityWarning>(),
            SyntaxErrors = new List<SyntaxError>()
        };

        if (string.IsNullOrEmpty(_unityLogPath) || !File.Exists(_unityLogPath))
        {
            Logger.LogWarning("Unity log file not found: {LogPath}", _unityLogPath);
            analysisResult.HasErrors = false;
            return analysisResult;
        }

        try
        {
            var logContent = await File.ReadAllTextAsync(_unityLogPath, cancellationToken);
            analysisResult = AnalyzeUnityLog(logContent, analysisResult);
            
            // If we have a model, use it to provide more context
            if (Model != null && analysisResult.SyntaxErrors.Count > 0)
            {
                analysisResult = await EnhanceAnalysisWithLLM(analysisResult, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error analyzing Unity log");
            analysisResult.HasErrors = true;
            analysisResult.AnalysisError = ex.Message;
        }

        Logger.LogInformation("Analysis complete: {ErrorCount} errors, {WarningCount} warnings, {SyntaxErrorCount} syntax errors",
            analysisResult.Errors.Count, analysisResult.Warnings.Count, analysisResult.SyntaxErrors.Count);

        return analysisResult;
    }

    /// <summary>
    /// Shuts down the Unity Error Analysis Agent.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    protected override Task OnShutdownAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Shutting down Unity Error Analysis Agent: {AgentId}", Spec.AgentId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the system prompt for this domain.
    /// </summary>
    /// <returns>The system prompt string for Unity error analysis.</returns>
    protected override string GetSystemPrompt()
    {
        return @"You are a Unity error analysis expert. Your role is to analyze Unity compilation errors and provide detailed diagnostics, root cause analysis, and fix suggestions for C# syntax errors in Unity projects.";
    }

    /// <summary>
    /// Gets mock output when no model is available.
    /// </summary>
    /// <returns>An empty <see cref="UnityErrorAnalysis"/> object.</returns>
    protected override object GetMockOutput()
    {
        return new UnityErrorAnalysis
        {
            LogPath = _unityLogPath ?? "",
            ProjectPath = _projectPath ?? "",
            AnalyzedAt = DateTimeOffset.UtcNow,
            Errors = new List<UnityError>(),
            Warnings = new List<UnityWarning>(),
            SyntaxErrors = new List<SyntaxError>(),
            HasErrors = false
        };
    }

    /// <summary>
    /// Analyzes Unity log content and extracts errors, warnings, and syntax errors.
    /// </summary>
    /// <param name="logContent">The full content of the Unity log file.</param>
    /// <param name="result">The analysis result object to populate.</param>
    /// <returns>The populated <see cref="UnityErrorAnalysis"/> object.</returns>
    private UnityErrorAnalysis AnalyzeUnityLog(string logContent, UnityErrorAnalysis result)
    {
        var lines = logContent.Split('\n');
        
        // Patterns for Unity errors
        var errorPattern = new Regex(@"^(.*?\.cs)\((\d+),(\d+)\):\s*(error|warning)\s+([A-Z0-9]+):\s*(.+)$", RegexOptions.Multiline);
        var syntaxErrorPattern = new Regex(@"(CS\d{4}):\s*(.+?)(?:\n|$)", RegexOptions.Multiline);
        
        foreach (var line in lines)
        {
            // Match C# compiler errors
            var errorMatch = errorPattern.Match(line);
            if (errorMatch.Success)
            {
                var filePath = errorMatch.Groups[1].Value;
                var lineNumber = int.Parse(errorMatch.Groups[2].Value);
                var column = int.Parse(errorMatch.Groups[3].Value);
                var severity = errorMatch.Groups[4].Value;
                var errorCode = errorMatch.Groups[5].Value;
                var message = errorMatch.Groups[6].Value;

                if (severity == "error")
                {
                    var error = new UnityError
                    {
                        FilePath = filePath,
                        LineNumber = lineNumber,
                        Column = column,
                        ErrorCode = errorCode,
                        Message = message,
                        FullLine = line
                    };

                    result.Errors.Add(error);
                    result.HasErrors = true;

                    // Check if it's a syntax error
                    if (errorCode.StartsWith("CS") && IsSyntaxError(errorCode))
                    {
                        result.SyntaxErrors.Add(new SyntaxError
                        {
                            FilePath = filePath,
                            LineNumber = lineNumber,
                            Column = column,
                            ErrorCode = errorCode,
                            Message = message,
                            SuggestedFix = GetSuggestedFix(errorCode, message)
                        });
                    }
                }
                else if (severity == "warning")
                {
                    result.Warnings.Add(new UnityWarning
                    {
                        FilePath = filePath,
                        LineNumber = lineNumber,
                        Column = column,
                        WarningCode = errorCode,
                        Message = message
                    });
                }
            }

            // Match general Unity errors
            if (line.Contains("Exception:") || line.Contains("Error:"))
            {
                if (!result.Errors.Any(e => e.FullLine == line))
                {
                    result.Errors.Add(new UnityError
                    {
                        FilePath = "Unknown",
                        LineNumber = 0,
                        Column = 0,
                        ErrorCode = "UNITY_ERROR",
                        Message = line,
                        FullLine = line
                    });
                    result.HasErrors = true;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Determines if an error code represents a C# syntax error.
    /// </summary>
    /// <param name="errorCode">The C# compiler error code (e.g., "CS1001").</param>
    /// <returns>True if the error code is a syntax error, false otherwise.</returns>
    private bool IsSyntaxError(string errorCode)
    {
        // Common C# syntax error codes
        var syntaxErrorCodes = new[]
        {
            "CS1001", "CS1002", "CS1003", "CS1006", "CS1008", "CS1009", "CS1010", "CS1011", "CS1012",
            "CS1013", "CS1014", "CS1015", "CS1016", "CS1017", "CS1018", "CS1019", "CS1020", "CS1021",
            "CS1022", "CS1023", "CS1024", "CS1025", "CS1026", "CS1027", "CS1028", "CS1029", "CS1030",
            "CS1031", "CS1032", "CS1033", "CS1034", "CS1035", "CS1036", "CS1037", "CS1038", "CS1039",
            "CS1040", "CS1041", "CS1042", "CS1043", "CS1044", "CS1045", "CS1501", "CS1502", "CS1503",
            "CS1513", "CS1514", "CS1515", "CS1516", "CS1517", "CS1518", "CS1519", "CS1520", "CS1521",
            "CS1522", "CS1523", "CS1524", "CS1525", "CS1526", "CS1527", "CS1528", "CS1529", "CS1530"
        };
        
        return syntaxErrorCodes.Contains(errorCode);
    }

    /// <summary>
    /// Gets a suggested fix for a common C# syntax error code.
    /// </summary>
    /// <param name="errorCode">The C# compiler error code.</param>
    /// <param name="message">The error message.</param>
    /// <returns>A suggested fix string, or null if no suggestion is available.</returns>
    private string? GetSuggestedFix(string errorCode, string message)
    {
        // Provide basic suggestions for common errors
        return errorCode switch
        {
            "CS1001" => "Add identifier after 'new' keyword",
            "CS1002" => "Add semicolon",
            "CS1003" => "Add closing parenthesis or bracket",
            "CS1006" => "Add method name",
            "CS1008" => "Add type name",
            "CS1009" => "Remove invalid character",
            "CS1010" => "Add newline or closing brace",
            "CS1011" => "Add closing brace",
            "CS1012" => "Remove extra character",
            "CS1013" => "Add valid number",
            "CS1014" => "Add get or set accessor",
            "CS1015" => "Add type name",
            "CS1016" => "Add attribute name",
            "CS1017" => "Add closing parenthesis",
            "CS1018" => "Add identifier",
            "CS1019" => "Add closing parenthesis",
            "CS1020" => "Add preprocessor directive",
            "CS1021" => "Add integral constant",
            "CS1022" => "Add type or namespace",
            "CS1023" => "Add embedded statement",
            "CS1024" => "Add preprocessor directive",
            "CS1025" => "Add single-line comment",
            "CS1026" => "Add closing parenthesis",
            "CS1027" => "Add #endif directive",
            "CS1028" => "Add unexpected preprocessor directive",
            "CS1029" => "Add #error directive",
            "CS1030" => "Add #warning directive",
            "CS1031" => "Add type definition",
            "CS1032" => "Add preprocessor directive",
            "CS1033" => "Add source file",
            "CS1034" => "Add compiler option",
            "CS1035" => "Add ending quote",
            "CS1036" => "Add opening parenthesis",
            "CS1037" => "Add overloadable operator",
            "CS1038" => "Add #endregion directive",
            "CS1039" => "Add ending quote",
            "CS1040" => "Add preprocessor directive",
            "CS1041" => "Add identifier",
            "CS1042" => "Add keyword",
            "CS1043" => "Add semicolon",
            "CS1044" => "Add type",
            "CS1045" => "Add preprocessor directive",
            "CS1501" => "Check method signature matches call",
            "CS1502" => "Check argument types match parameters",
            "CS1503" => "Check argument count matches parameters",
            "CS1513" => "Add closing brace",
            "CS1514" => "Add expected token",
            "CS1515" => "Add 'in' keyword",
            "CS1516" => "Add expected token",
            "CS1517" => "Add valid preprocessor expression",
            "CS1518" => "Add expected token",
            "CS1519" => "Add invalid token",
            "CS1520" => "Add method body",
            "CS1521" => "Add invalid base type",
            "CS1522" => "Add empty switch block",
            "CS1523" => "Add expected token",
            "CS1524" => "Add expected token",
            "CS1525" => "Add invalid expression term",
            "CS1526" => "Add new expression",
            "CS1527" => "Add namespace element",
            "CS1528" => "Add expected token",
            "CS1529" => "Add using directive",
            "CS1530" => "Add keyword",
            _ => null
        };
    }

    /// <summary>
    /// Enhances error analysis using an LLM to provide more detailed diagnostics and fixes.
    /// </summary>
    /// <param name="analysis">The initial analysis result to enhance.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>The enhanced <see cref="UnityErrorAnalysis"/> object.</returns>
    private async Task<UnityErrorAnalysis> EnhanceAnalysisWithLLM(
        UnityErrorAnalysis analysis,
        CancellationToken cancellationToken)
    {
        if (Model == null) return analysis;

        try
        {
            var prompt = $@"Analyze these Unity C# compilation errors and provide detailed fixes:

{string.Join("\n", analysis.SyntaxErrors.Select(e => $"{e.FilePath}({e.LineNumber},{e.Column}): {e.ErrorCode}: {e.Message}"))}

For each error, provide:
1. Root cause analysis
2. Specific fix instructions
3. Code example if applicable

Format as JSON with structure:
{{
  ""fixes"": [
    {{
      ""filePath"": ""..."",
      ""lineNumber"": 0,
      ""errorCode"": ""..."",
      ""rootCause"": ""..."",
      ""fixInstructions"": ""..."",
      ""fixedCode"": ""...""
    }}
  ]
}}";

            var modelInput = new ModelInput(new[]
            {
                ("system", GetSystemPrompt()),
                ("user", prompt)
            });
            var modelOutput = await Model.CompleteAsync(modelInput, cancellationToken);
            var response = modelOutput.Text;
            
            // Parse LLM response and enhance syntax errors
            // This is a simplified version - in production, you'd want more robust parsing
            foreach (var syntaxError in analysis.SyntaxErrors)
            {
                if (string.IsNullOrEmpty(syntaxError.SuggestedFix))
                {
                    syntaxError.SuggestedFix = ExtractFixFromLLMResponse(response, syntaxError);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to enhance analysis with LLM");
        }

        return analysis;
    }

    /// <summary>
    /// Extracts a fix suggestion from an LLM response for a specific syntax error.
    /// </summary>
    /// <param name="response">The LLM response text.</param>
    /// <param name="error">The syntax error to extract a fix for.</param>
    /// <returns>The extracted fix suggestion, or null if not found.</returns>
    private string? ExtractFixFromLLMResponse(string response, SyntaxError error)
    {
        // Simple extraction - look for the error code in the response
        var pattern = new Regex($@"{error.ErrorCode}.*?fixInstructions["":\s]+([^""\n]+)", RegexOptions.IgnoreCase);
        var match = pattern.Match(response);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }
}

/// <summary>
/// Result of Unity error analysis.
/// 
/// Contains:
/// - Log and project paths
/// - Lists of errors, warnings, and syntax errors
/// - Analysis timestamp and error status
/// - Optional analysis error message
/// 
/// Produced by UnityErrorAnalysisAgent during log analysis.
/// </summary>
public class UnityErrorAnalysis
{
    public string LogPath { get; set; } = "";
    public string ProjectPath { get; set; } = "";
    public DateTimeOffset AnalyzedAt { get; set; }
    public bool HasErrors { get; set; }
    public List<UnityError> Errors { get; set; } = new();
    public List<UnityWarning> Warnings { get; set; } = new();
    public List<SyntaxError> SyntaxErrors { get; set; } = new();
    public string? AnalysisError { get; set; }
}

/// <summary>
/// Represents a Unity compilation error.
/// 
/// Contains:
/// - File path, line number, and column
/// - Error code and message
/// - Full error line text
/// 
/// Extracted from Unity compilation logs by UnityErrorAnalysisAgent.
/// </summary>
public class UnityError
{
    public string FilePath { get; set; } = "";
    public int LineNumber { get; set; }
    public int Column { get; set; }
    public string ErrorCode { get; set; } = "";
    public string Message { get; set; } = "";
    public string FullLine { get; set; } = "";
}

/// <summary>
/// Represents a Unity compilation warning.
/// 
/// Contains:
/// - File path, line number, and column
/// - Warning code and message
/// 
/// Extracted from Unity compilation logs by UnityErrorAnalysisAgent.
/// </summary>
public class UnityWarning
{
    public string FilePath { get; set; } = "";
    public int LineNumber { get; set; }
    public int Column { get; set; }
    public string WarningCode { get; set; } = "";
    public string Message { get; set; } = "";
}

/// <summary>
/// Represents a syntax error that can be automatically fixed.
/// 
/// Contains:
/// - File path, line number, and column
/// - Error code and message
/// - Optional suggested fix
/// 
/// Used by CodeFixAgent to automatically fix syntax errors.
/// Extracted from Unity compilation logs by UnityErrorAnalysisAgent.
/// </summary>
public class SyntaxError
{
    public string FilePath { get; set; } = "";
    public int LineNumber { get; set; }
    public int Column { get; set; }
    public string ErrorCode { get; set; } = "";
    public string Message { get; set; } = "";
    public string? SuggestedFix { get; set; }
}

