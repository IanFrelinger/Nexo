using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Nexo.Infrastructure.Execution;

/// <summary>
/// Factory for creating and managing LLM providers.
/// </summary>
public class ProviderFactory : IProviderFactory
{
    private readonly ILogger<ProviderFactory> _logger;
    private readonly HashSet<string> _availableProviders = new()
    {
        // Real providers (may be wired later)
        "openai",
        "azure",
        "ollama",

        // Offline/demo providers
        "mock",
        "offline",
        "mock-json",
        "echo"
    };
    
    public ProviderFactory(ILogger<ProviderFactory> logger)
    {
        _logger = logger;
    }
    
    public bool IsProviderAvailable(string provider)
    {
        return _availableProviders.Contains(provider.ToLowerInvariant());
    }
    
    public async Task<string> ExecuteLLMAsync(
        string provider,
        string systemPrompt,
        string userPrompt,
        object config,
        CancellationToken cancellationToken = default)
    {
        provider = (provider ?? "mock").Trim().ToLowerInvariant();
        _logger.LogInformation("Executing LLM request with provider {Provider}", provider);
        
        // Simulate latency to keep progress reporting realistic
        await Task.Delay(30, cancellationToken);
        
        // Offline/demo-safe providers: always return parseable JSON tailored to the prompt.
        if (provider is "mock" or "offline" or "mock-json" or "echo")
        {
            return GenerateMockJsonResponse(systemPrompt, userPrompt);
        }
        
        // For now, treat unimplemented "real" providers as offline-safe to avoid breaking demos.
        // This keeps agentic flows functional in air-gapped environments.
        if (provider is "openai" or "azure" or "ollama")
        {
            _logger.LogWarning("Provider {Provider} not wired; falling back to mock-json response", provider);
            return GenerateMockJsonResponse(systemPrompt, userPrompt);
        }

        _logger.LogWarning("Unknown provider {Provider}; falling back to mock-json response", provider);
        return GenerateMockJsonResponse(systemPrompt, userPrompt);
    }

    private static string GenerateMockJsonResponse(string systemPrompt, string userPrompt)
    {
        systemPrompt ??= "";
        userPrompt ??= "";

        // Universal Tester bricks
        if (systemPrompt.Contains("universal testing agent analyzing", StringComparison.OrdinalIgnoreCase))
        {
            // UnderstandingBrick schema
            var obj = new
            {
                screenType = InferScreenType(userPrompt),
                currentContext = "Offline analysis (mock-json provider)",
                availableActions = Array.Empty<object>(),
                currentObjective = "Gather baseline evidence",
                progressPercent = InferProgressPercent(userPrompt),
                issues = Array.Empty<object>(),
                unexploredAreas = Array.Empty<string>(),
                confidence = 0.6
            };
            return JsonSerializer.Serialize(obj);
        }

        if (systemPrompt.Contains("deciding what action to take next in testing", StringComparison.OrdinalIgnoreCase))
        {
            var nextActionId = InferNextActionId(userPrompt);
            var obj = new
            {
                nextActionId,
                reasoning = "Offline/mock decision: pick first available action if any; otherwise wait",
                shouldStop = nextActionId == "wait"
            };
            return JsonSerializer.Serialize(obj);
        }

        if (systemPrompt.Contains("validating the result of a test action", StringComparison.OrdinalIgnoreCase))
        {
            var success = Regex.IsMatch(userPrompt, @"Execution Success:\s*True", RegexOptions.IgnoreCase);
            var obj = new
            {
                passed = success,
                reasoning = success ? "No errors indicated by execution result." : "Execution reported failure.",
                issues = success
                    ? Array.Empty<object>()
                    : new[] { new { type = "error", description = "Action execution failed", severity = "high" } },
                confidence = 0.7
            };
            return JsonSerializer.Serialize(obj);
        }

        if (systemPrompt.Contains("generating a test report summary", StringComparison.OrdinalIgnoreCase))
        {
            var obj = new
            {
                findings = new[] { "Offline/mock report: summary generated without network access." },
                recommendations = new[] { "Wire a real provider for richer summaries." }
            };
            return JsonSerializer.Serialize(obj);
        }

        // Autonomous Dev bricks
        if (systemPrompt.Contains("create a clear specification", StringComparison.OrdinalIgnoreCase)
            || systemPrompt.Contains("analyzing a development task to create a clear specification", StringComparison.OrdinalIgnoreCase))
        {
            var summary = InferTaskSummary(userPrompt);
            var obj = new
            {
                summary,
                changeType = "Test",
                functionalRequirements = new[]
                {
                    new { id = "req1", description = "Create a small, safe change suitable for demo runs.", priority = "Must", isMandatory = true }
                },
                nonFunctionalRequirements = Array.Empty<object>(),
                acceptanceCriteria = new[]
                {
                    new { id = "ac1", description = "Change is applied and project still builds.", testDescription = "Run build successfully", isAutomatable = true }
                },
                affectedAreas = new[] { "." },
                risks = new[] { "Offline provider returns template JSON; wire real providers for production." },
                complexity = 1,
                confidence = 0.7,
                openQuestions = Array.Empty<string>()
            };
            return JsonSerializer.Serialize(obj);
        }

        if (systemPrompt.Contains("planning how to implement a feature", StringComparison.OrdinalIgnoreCase))
        {
            // Create a plan that is safe for most projects: add/update a markdown note file.
            var targetFile = InferTargetFileFromPrompt(userPrompt) ?? "NEXO_AGENT_NOTES.md";
            var obj = new
            {
                tasks = new[]
                {
                    new
                    {
                        id = "task1",
                        title = "Write a demo notes file",
                        description = "Create/update a small markdown file for demo purposes (safe, non-breaking).",
                        type = "ModifyFile",
                        steps = new[] { "Write file content", "Build project" },
                        targetFiles = new[] { targetFile },
                        verificationMethod = "dotnet build (or project build) succeeds"
                    }
                },
                dependencies = Array.Empty<object>(),
                plannedChanges = new[]
                {
                    new { filePath = targetFile, changeType = "Modify", description = "Add demo note content" }
                },
                testStrategy = new
                {
                    testCases = new[] { "Build succeeds" },
                    persona = "Average",
                    userFlows = Array.Empty<string>(),
                    minimumPassRate = 0.9
                },
                estimatedDuration = "00:01:00"
            };
            return JsonSerializer.Serialize(obj);
        }

        if (systemPrompt.Contains("implementing a development task", StringComparison.OrdinalIgnoreCase))
        {
            var targetPath = InferTargetFileFromPrompt(userPrompt) ?? "NEXO_AGENT_NOTES.md";
            var content = $"# Nexo Demo Notes\n\nGenerated by offline/mock-json provider at {DateTime.UtcNow:O}.\n\nThis file is safe to modify and should not break builds.\n";
            var obj = new[]
            {
                new
                {
                    targetPath,
                    content,
                    description = "Offline demo artifact",
                    language = "markdown",
                    isFullFile = true,
                    confidence = 0.8,
                    uncertainties = Array.Empty<string>()
                }
            };
            return JsonSerializer.Serialize(obj);
        }

        if (systemPrompt.Contains("analyzing test results to provide developer feedback", StringComparison.OrdinalIgnoreCase))
        {
            var score = InferScoreFromPrompt(userPrompt);
            var overallSuccess = score >= 80;
            var obj = new
            {
                overallSuccess,
                acceptanceScore = score,
                criteriaResults = Array.Empty<object>(),
                userExperience = new
                {
                    narrative = "Offline/mock feedback generated.",
                    intuitivenessScore = 7,
                    userFrustrated = false,
                    timeToComplete = "00:00:10",
                    attemptCount = 1,
                    overallReaction = "OK"
                },
                issues = Array.Empty<object>(),
                actionableItems = Array.Empty<object>()
            };
            return JsonSerializer.Serialize(obj);
        }

        if (systemPrompt.Contains("analyzing test feedback to decide what to do next", StringComparison.OrdinalIgnoreCase))
        {
            var score = InferScoreFromPrompt(userPrompt);
            var decision = score >= 90 ? "Complete" : "Iterate";
            var obj = new
            {
                decision,
                reasoning = score >= 90 ? "Acceptance score is high enough." : "Acceptance score below threshold; iterate once.",
                plannedFixes = Array.Empty<object>(),
                confidence = 0.7,
                estimatedRemainingIterations = score >= 90 ? 0 : 1
            };
            return JsonSerializer.Serialize(obj);
        }

        // Fallback: return a benign JSON object
        return "{}";
    }

    private static string InferScreenType(string prompt)
    {
        if (prompt.Contains("URL:", StringComparison.OrdinalIgnoreCase)) return "Web";
        if (prompt.Contains("Terminal", StringComparison.OrdinalIgnoreCase)) return "CLI";
        return "Unknown";
    }

    private static int InferProgressPercent(string prompt)
    {
        // If prompt mentions "Goal achieved" we can push progress up a bit; else keep low.
        return prompt.Contains("Goal", StringComparison.OrdinalIgnoreCase) ? 30 : 10;
    }

    private static string InferNextActionId(string prompt)
    {
        // Parse "- <id>:" lines under Available Actions
        var match = Regex.Match(prompt, @"^\-\s*(?<id>[^:\r\n]+)\s*:", RegexOptions.Multiline);
        return match.Success ? match.Groups["id"].Value.Trim() : "wait";
    }

    private static string InferTaskSummary(string prompt)
    {
        // Try to find "## Task Description" section
        var match = Regex.Match(prompt, @"## Task Description\s*(?<task>[\s\S]*?)(\n## |\z)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var task = match.Groups["task"].Value.Trim();
            return task.Length > 120 ? task[..120] + "…" : task;
        }
        return "Demo task (offline)";
    }

    private static string? InferTargetFileFromPrompt(string prompt)
    {
        // For generation prompt: lines under "## Target Files"
        var match = Regex.Match(prompt, @"## Target Files\s*(?<block>[\s\S]*?)(\n## |\z)", RegexOptions.IgnoreCase);
        if (!match.Success) return null;

        var block = match.Groups["block"].Value;
        var line = block.Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.StartsWith("- "));
        if (line == null) return null;
        return line[2..].Trim();
    }

    private static double InferScoreFromPrompt(string prompt)
    {
        // Try to parse "Score: <number>%"
        var match = Regex.Match(prompt, @"Score:\s*(?<score>\d+)", RegexOptions.IgnoreCase);
        if (match.Success && double.TryParse(match.Groups["score"].Value, out var score)) return score;
        return 85;
    }
}

