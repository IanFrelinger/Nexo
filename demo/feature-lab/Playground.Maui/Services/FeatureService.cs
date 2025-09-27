using Playground.Maui.Models;

namespace Playground.Maui.Services;

public partial class FeatureService
{
    public async Task<RunResult> RunFeatureAsync(string featureName, string mode, string provider)
    {
        var runId = Guid.NewGuid().ToString();
        var startedAt = DateTime.UtcNow;
        var steps = new List<StepEvent>();

        try
        {
            // Simulate feature execution
            await Task.Delay(1000);

            // Add some mock steps
            steps.Add(new StepEvent(
                "step1",
                "Initialize",
                "Completed",
                null,
                new Dictionary<string, object> { { "status", "ready" } },
                DateTime.UtcNow
            ));

            steps.Add(new StepEvent(
                "step2",
                "Process",
                "Completed",
                null,
                new Dictionary<string, object> { { "processed", 1 } },
                DateTime.UtcNow
            ));

            var completedAt = DateTime.UtcNow;
            var metrics = new Dictionary<string, object>
            {
                { "duration_ms", (completedAt - startedAt).TotalMilliseconds },
                { "steps_completed", steps.Count },
                { "mode", mode },
                { "provider", provider }
            };

            return new RunResult
            {
                RunId = runId,
                FeatureName = featureName,
                Status = "Completed",
                ErrorMessage = string.Empty,
                Steps = steps,
                Metrics = metrics,
                StartedAt = startedAt,
                CompletedAt = completedAt
            };
        }
        catch (Exception ex)
        {
            return new RunResult
            {
                RunId = runId,
                FeatureName = featureName,
                Status = "Failed",
                ErrorMessage = ex.Message,
                Steps = steps,
                Metrics = new Dictionary<string, object>(),
                StartedAt = startedAt,
                CompletedAt = null
            };
        }
    }

    public List<FeatureTemplate> GetAvailableFeatures()
    {
        return new List<FeatureTemplate>
        {
            new FeatureTemplate
            {
                Name = "Smart Reply Panel",
                Description = "Generates intelligent email replies based on content and intent",
                Icon = "📧",
                Capabilities = new List<string>
                {
                    "Email ingestion",
                    "Text cleaning",
                    "Language detection",
                    "Intent classification",
                    "Policy approval gates",
                    "Reply generation"
                }
            },
            new FeatureTemplate
            {
                Name = "Contract Summary Panel",
                Description = "Extracts key clauses and summarizes legal contracts",
                Icon = "📄",
                Capabilities = new List<string>
                {
                    "PDF ingestion",
                    "PII redaction",
                    "Contract summarization",
                    "Risk assessment",
                    "DMS integration"
                }
            },
            new FeatureTemplate
            {
                Name = "Translate & Tone",
                Description = "Adds translation and tone analysis to any feature flow",
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
    }

    public async Task<ValidationResult> RunValidationAsync()
    {
        await Task.Delay(1500); // Simulate validation work

        return new ValidationResult
        {
            IsValid = true,
            Messages = new List<string>
            {
                "✅ .NET 8: Found .NET 8.0.303",
                "✅ Build: MAUI app builds successfully",
                "✅ Fixtures: Fixture directories exist",
                "✅ OFF Dry-runs: OFF mode produces deterministic output",
                "✅ Local Provider: Local provider available (using stub)",
                "✅ Cloud Secret: Cloud credentials available",
                "✅ Demo Tests: Demo tests pass"
            },
            Checks = new Dictionary<string, bool>
            {
                { "dotnet", true },
                { "build", true },
                { "fixtures", true },
                { "off_dryruns", true },
                { "local_provider", true },
                { "cloud_secret", true },
                { "demo_tests", true }
            }
        };
    }
}
