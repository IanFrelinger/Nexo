using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Application.Testing.Models;
using System.Text.Json;

namespace Ashlar.Tests.CLI.Tests.Commands.SelfExtendGenerated;

/// <summary>Generated test template for UI domain knowledge retention scenarios (self-extend scaffold).</summary>
public sealed class UiDomainKnowledgeRetentionTests : UnitTestBase
{
    /// <summary>Execute asynchronously.</summary>
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            string? repoRoot = null;
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "src")) &&
                    Directory.Exists(Path.Combine(dir.FullName, "docs")))
                {
                    repoRoot = dir.FullName;
                    break;
                }
                dir = dir.Parent;
            }
            AssertTrue(!string.IsNullOrWhiteSpace(repoRoot), "Unable to resolve repository root from test context.");

            var uiRoot = Path.Combine(repoRoot!, "docs", "UiDomainDemoGenerated", "app");
            var hostRoot = Path.Combine(repoRoot!, "docs", "UiDomainDemoGenerated", "host");
            var avaloniaRoot = Path.Combine(repoRoot!, "docs", "UiDomainDemoGenerated", "avalonia");
            var htmlPath = Path.Combine(uiRoot, "index.html");
            var jsPath = Path.Combine(uiRoot, "app.js");
            var domainPath = Path.Combine(uiRoot, "domain-knowledge.json");
            var hostProgramPath = Path.Combine(hostRoot, "Program.cs");
            var hostProjectPath = Path.Combine(hostRoot, "UiDemoHost.csproj");
            var smokeProgramPath = Path.Combine(hostRoot, "SmokeProgram.cs");
            var avaloniaContractsPath = Path.Combine(avaloniaRoot, "Ashlar.Ui.Abstractions", "UiContracts.cs");
            var avaloniaHostProgramPath = Path.Combine(avaloniaRoot, "Ashlar.Ui.AvaloniaHost", "Program.cs");

            AssertTrue(File.Exists(htmlPath), "Expected index.html to exist.");
            AssertTrue(File.Exists(jsPath), "Expected app.js to exist.");
            AssertTrue(File.Exists(domainPath), "Expected domain-knowledge.json to exist.");
            AssertTrue(File.Exists(hostProgramPath), "Expected .NET host Program.cs to exist.");
            AssertTrue(File.Exists(hostProjectPath), "Expected .NET host project file to exist.");
            AssertTrue(File.Exists(smokeProgramPath), "Expected .NET smoke program to exist.");
            AssertTrue(File.Exists(avaloniaContractsPath), "Expected Avalonia abstraction contracts to exist.");
            AssertTrue(File.Exists(avaloniaHostProgramPath), "Expected Avalonia host Program.cs to exist.");

            var html = File.ReadAllText(htmlPath);
            var js = File.ReadAllText(jsPath);
            var domainJson = File.ReadAllText(domainPath);
            var hostProgram = File.ReadAllText(hostProgramPath);
            var avaloniaContracts = File.ReadAllText(avaloniaContractsPath);
            var avaloniaHostProgram = File.ReadAllText(avaloniaHostProgramPath);

            AssertTrue(html.Contains("Ashlar Chatbot", StringComparison.Ordinal), "UI should render chatbot section.");
            AssertTrue(html.Contains("Dynamically loaded features", StringComparison.Ordinal), "UI should render dynamic feature host section.");
            AssertTrue(js.Contains("explainAshlar", StringComparison.Ordinal), "JS should implement chatbot explainer behavior.");
            AssertTrue(js.Contains("/api/scaffold-feature", StringComparison.Ordinal), "JS should call scaffold API endpoint.");
            AssertTrue(js.Contains("import(moduleUrl)", StringComparison.Ordinal), "JS should dynamically import generated feature modules.");
            AssertTrue(hostProgram.Contains("MapPost(\"/api/scaffold-feature\"", StringComparison.Ordinal), ".NET host should expose scaffold endpoint.");
            AssertTrue(hostProgram.Contains("UseStaticFiles", StringComparison.Ordinal), ".NET host should serve static UI.");
            AssertTrue(avaloniaContracts.Contains("IUiFrameworkAdapter", StringComparison.Ordinal), "Avalonia stack should include cross-framework adapter contract.");
            AssertTrue(avaloniaContracts.Contains("CrossFrameworkCompatibility", StringComparison.Ordinal), "Avalonia stack should include compatibility contract notes.");
            AssertTrue(avaloniaHostProgram.Contains("AVALONIA_FEATURE_HOTLOAD", StringComparison.Ordinal), "Avalonia host should scaffold via Avalonia hotload objective.");
            AssertTrue(avaloniaHostProgram.Contains("AVALONIA_APP_TRANSFORM", StringComparison.Ordinal), "Avalonia host should support full app transformation objective.");
            AssertTrue(avaloniaHostProgram.Contains("Galactic Operations Console", StringComparison.Ordinal), "Avalonia host should render a distinct transformed application shell.");
            AssertTrue(avaloniaHostProgram.Contains("AvaloniaUiFrameworkAdapter", StringComparison.Ordinal), "Avalonia host should render nodes through framework adapter.");
            using var doc = JsonDocument.Parse(domainJson);
            var capabilities = doc.RootElement.GetProperty("capabilities");
            AssertTrue(capabilities.GetArrayLength() >= 4, "Expected at least 4 domain capabilities.");

            return Task.FromResult(new TestResult
            {
                Name = nameof(UiDomainKnowledgeRetentionTests),
                Category = "SelfExtendGenerated",
                Passed = true,
                Message = "UI demo retains domain knowledge and required artifacts."
            });
        }
        catch (AssertionException ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof(UiDomainKnowledgeRetentionTests),
                Category = "SelfExtendGenerated",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof(UiDomainKnowledgeRetentionTests),
                Category = "SelfExtendGenerated",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            });
        }
    }
}
