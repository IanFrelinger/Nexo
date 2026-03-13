using Nexo.CLI.Runtime;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.CLI.Tests.Commands;

public sealed class AdaptiveRuntimePlanResolverTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            TestResolveVisualUiGoal();
            TestResolveFunctionalGoalDefaults();
            TestManifestPolicyAndGoalEnrichment();
            TestManifestLoaderInlineJson();

            return Task.FromResult(new TestResult
            {
                Name = nameof(AdaptiveRuntimePlanResolverTests),
                Category = "CLI",
                Passed = true,
                Message = "Adaptive runtime plan resolver tests passed"
            });
        }
        catch (AssertionException ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof(AdaptiveRuntimePlanResolverTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof(AdaptiveRuntimePlanResolverTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
    }

    private void TestResolveVisualUiGoal()
    {
        var manifest = AdaptiveRuntimeManifest.Default();
        var plan = AdaptiveRuntimePlanResolver.Resolve(
            "Create a slick Avalonia UI with visual QA and adaptive transformed layout",
            manifest,
            bootstrapProfileOverride: "auto",
            qaPolicyOverride: "prod");

        AssertEqual("self-extend-visual", plan.BootstrapProfile);
        AssertEqual("prod", plan.QaPolicyProfile);
        AssertTrue(plan.RunAestheticQa, "Aesthetic QA should be enabled for visual UI goals.");
        AssertTrue(plan.RunVisualQa, "Visual QA should be enabled for visual UI goals.");
        AssertEqual("strict", plan.VisualQaFallbackPolicy);
    }

    private void TestResolveFunctionalGoalDefaults()
    {
        var manifest = AdaptiveRuntimeManifest.Default();
        var plan = AdaptiveRuntimePlanResolver.Resolve(
            "Generate composable backend command handlers and tests",
            manifest,
            bootstrapProfileOverride: "auto",
            qaPolicyOverride: "auto");

        AssertEqual("self-extend-functional", plan.BootstrapProfile);
        AssertEqual("demo", plan.QaPolicyProfile);
        AssertEqual("functional", plan.Focus);
        AssertFalse(plan.RunAestheticQa);
        AssertFalse(plan.RunVisualQa);
        AssertEqual("degrade", plan.VisualQaFallbackPolicy);
    }

    private void TestManifestPolicyAndGoalEnrichment()
    {
        var manifest = new AdaptiveRuntimeManifest
        {
            DomainPacks = ["personal", "ui"],
            UiCapabilities = ["dynamic-layout", "visual-qa"],
            QaPolicyProfile = "research",
            Preferences = new Dictionary<string, string>
            {
                ["tone"] = "concise",
                ["theme"] = "dark"
            }
        };

        var plan = AdaptiveRuntimePlanResolver.Resolve(
            "Scaffold an adaptive personal software workspace",
            manifest,
            bootstrapProfileOverride: "auto",
            qaPolicyOverride: "auto");
        var enriched = AdaptiveRuntimePlanResolver.EnrichGoal("Build adaptive personal runtime", manifest, plan);

        AssertEqual("research", plan.QaPolicyProfile);
        AssertTrue(plan.RunAestheticQa, "Personal runtime goals should include aesthetic QA.");
        AssertTrue(plan.RunVisualQa, "Manifest UI capabilities should enable visual QA.");
        AssertTrue(enriched.Contains("domain-packs: personal, ui", StringComparison.OrdinalIgnoreCase), "Enriched goal should include domain pack context.");
        AssertTrue(enriched.Contains("preferences: tone=concise", StringComparison.OrdinalIgnoreCase), "Enriched goal should include preferences.");
    }

    private void TestManifestLoaderInlineJson()
    {
        const string json = """
        {
          "domainPacks": ["Personal", "Ui", "personal"],
          "uiCapabilities": ["Visual-QA", "dynamic-layout"],
          "qaPolicyProfile": "DEMO",
          "preferences": {
            "Theme": "dark"
          }
        }
        """;

        var manifest = AdaptiveRuntimeManifestLoader.Load(path: null, json: json);
        AssertEqual(2, manifest.DomainPacks.Length);
        AssertEqual(2, manifest.UiCapabilities.Length);
        AssertEqual("demo", manifest.QaPolicyProfile);
        AssertEqual("dark", manifest.Preferences.Values.First());
    }
}
