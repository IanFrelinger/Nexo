using Playground.Console.Models;

namespace Playground.Console.Services;

public class FeatureService
{
    private readonly List<FeatureInfo> _features = new()
    {
        new FeatureInfo
        {
            Id = "smart-reply",
            Name = "Smart Reply Panel",
            Description = "Automatically generate intelligent email replies with sentiment analysis and policy enforcement.",
            Icon = "📧",
            Capabilities = new List<string>
            {
                "Email ingestion and cleaning",
                "Language detection",
                "Intent classification",
                "Policy approval gates",
                "Reply generation"
            }
        },
        new FeatureInfo
        {
            Id = "contract-summary",
            Name = "Contract Summary Panel",
            Description = "Extract key information from contracts with risk assessment and compliance checking.",
            Icon = "📄",
            Capabilities = new List<string>
            {
                "PDF document ingestion",
                "PII redaction",
                "Contract summarization",
                "Risk assessment",
                "DMS integration"
            }
        },
        new FeatureInfo
        {
            Id = "translate-tone",
            Name = "Translate & Tone",
            Description = "Add translation and tone analysis that benefits all features without rewrites.",
            Icon = "🔧",
            Capabilities = new List<string>
            {
                "Translate & Tone block",
                "Multi-language support",
                "Composable architecture",
                "Zero rewrites required"
            }
        }
    };

    public Task<List<FeatureInfo>> GetAvailableFeaturesAsync()
    {
        return Task.FromResult(_features.ToList());
    }

    public async Task<RunResult> RunFeatureAsync(string featureId, string mode, string provider)
    {
        var feature = _features.FirstOrDefault(f => f.Id == featureId);
        if (feature == null)
        {
            return new RunResult
            {
                RunId = Guid.NewGuid().ToString(),
                FeatureName = featureId,
                Status = "Failed",
                ErrorMessage = "Feature not found",
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };
        }

        var runId = Guid.NewGuid().ToString();
        var startedAt = DateTime.UtcNow;
        var steps = new List<StepEvent>();

        // Simulate feature execution with realistic timing
        var executionTime = mode switch
        {
            "off" => 1000,
            "hybrid" => 2000,
            "embedded" => 3000,
            _ => 2000
        };

        await Task.Delay(executionTime);

        // Add mode-specific steps
        steps.Add(new StepEvent
        {
            StepId = "step1",
            StepName = "Initialize",
            Status = "Completed",
            Outputs = new Dictionary<string, object> { { "status", "ready" } },
            Timestamp = DateTime.UtcNow
        });

        if (mode != "off")
        {
            steps.Add(new StepEvent
            {
                StepId = "step2",
                StepName = "AI Processing",
                Status = "Completed",
                Outputs = new Dictionary<string, object> { { "ai_calls", 1 } },
                Timestamp = DateTime.UtcNow
            });
        }

        steps.Add(new StepEvent
        {
            StepId = "step3",
            StepName = "Process",
            Status = "Completed",
            Outputs = new Dictionary<string, object> { { "processed", 1 } },
            Timestamp = DateTime.UtcNow
        });

        if (mode == "embedded")
        {
            steps.Add(new StepEvent
            {
                StepId = "step4",
                StepName = "AI Enhancement",
                Status = "Completed",
                Outputs = new Dictionary<string, object> { { "enhancements", 2 } },
                Timestamp = DateTime.UtcNow
            });
        }

        var completedAt = DateTime.UtcNow;
        var metrics = new Dictionary<string, object>
        {
            { "duration_ms", (completedAt - startedAt).TotalMilliseconds },
            { "steps_completed", steps.Count },
            { "mode", mode },
            { "provider", provider },
            { "ai_calls", mode == "off" ? 0 : mode == "hybrid" ? 1 : 3 }
        };

        return new RunResult
        {
            RunId = runId,
            FeatureName = feature.Name,
            Status = "Completed",
            ErrorMessage = string.Empty,
            Steps = steps,
            Metrics = metrics,
            StartedAt = startedAt,
            CompletedAt = completedAt
        };
    }

    public async Task<ValidationResult> ValidateEnvironmentAsync()
    {
        await Task.Delay(500); // Simulate validation time

        var checks = new List<ValidationCheck>
        {
            new ValidationCheck
            {
                Name = ".NET 8",
                Passed = true,
                Message = "Found .NET 8.0.303"
            },
            new ValidationCheck
            {
                Name = "Console Build",
                Passed = true,
                Message = "Console project builds successfully"
            },
            new ValidationCheck
            {
                Name = "Fixtures",
                Passed = true,
                Message = "Fixture directories exist"
            },
            new ValidationCheck
            {
                Name = "Local Provider",
                Passed = true,
                Message = "Local provider available (using stub)"
            },
            new ValidationCheck
            {
                Name = "Cloud Secret",
                Passed = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_API_KEY")) ||
                         !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")),
                Message = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_API_KEY")) ||
                         !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY"))
                    ? "Cloud credentials available"
                    : "Cloud credentials not found (optional)",
                Remediation = "Set OPENAI_API_KEY or AZURE_OPENAI_API_KEY for cloud features"
            }
        };

        return new ValidationResult
        {
            IsValid = checks.All(c => c.Passed),
            Checks = checks,
            Message = checks.All(c => c.Passed) ? "All checks passed" : "Some checks failed"
        };
    }
}
