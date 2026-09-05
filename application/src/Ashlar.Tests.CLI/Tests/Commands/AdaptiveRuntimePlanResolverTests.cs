using Ashlar.CLI.Runtime;
using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Application.Testing.Models;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>Tests for adaptive runtime plan resolver.</summary>
public sealed class AdaptiveRuntimePlanResolverTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            /// <summary>Test resolve visual ui goal.</summary>
            TestResolveVisualUiGoal();
            /// <summary>Test resolve functional goal defaults.</summary>
            TestResolveFunctionalGoalDefaults();
            /// <summary>Test manifest policy and goal enrichment.</summary>
            TestManifestPolicyAndGoalEnrichment();
            /// <summary>Test manifest loader inline json.</summary>
            TestManifestLoaderInlineJson();
            /// <summary>Test manifest loader rejects invalid qa policy.</summary>
            TestManifestLoaderRejectsInvalidQaPolicy();
            /// <summary>Test resolve rejects invalid manifest qa policy.</summary>
            TestResolveRejectsInvalidManifestQaPolicy();
            /// <summary>Test release policy uses strict visual fallback.</summary>
            TestReleasePolicyUsesStrictVisualFallback();

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

        /// <summary>Assert equal.</summary>
        AssertEqual("self-extend-visual", plan.BootstrapProfile);
        /// <summary>Assert equal.</summary>
        AssertEqual("prod", plan.QaPolicyProfile);
        /// <summary>Assert true.</summary>
        /// <param name="goals."">Goals.".</param>
        AssertTrue(plan.RunAestheticQa, "Aesthetic QA should be enabled for visual UI goals.");
        /// <summary>Assert true.</summary>
        /// <param name="goals."">Goals.".</param>
        AssertTrue(plan.RunVisualQa, "Visual QA should be enabled for visual UI goals.");
        /// <summary>Assert equal.</summary>
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

        /// <summary>Assert equal.</summary>
        AssertEqual("self-extend-functional", plan.BootstrapProfile);
        /// <summary>Assert equal.</summary>
        AssertEqual("demo", plan.QaPolicyProfile);
        /// <summary>Assert equal.</summary>
        AssertEqual("functional", plan.Focus);
        /// <summary>Assert false.</summary>
        AssertFalse(plan.RunAestheticQa);
        /// <summary>Assert false.</summary>
        AssertFalse(plan.RunVisualQa);
        /// <summary>Assert equal.</summary>
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

        /// <summary>Assert equal.</summary>
        AssertEqual("research", plan.QaPolicyProfile);
        /// <summary>Assert true.</summary>
        /// <param name="QA."">Qa.".</param>
        AssertTrue(plan.RunAestheticQa, "Personal runtime goals should include aesthetic QA.");
        /// <summary>Assert true.</summary>
        /// <param name="QA."">Qa.".</param>
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
        /// <summary>Assert equal.</summary>
        AssertEqual(2, manifest.DomainPacks.Length);
        /// <summary>Assert equal.</summary>
        AssertEqual(2, manifest.UiCapabilities.Length);
        /// <summary>Assert equal.</summary>
        AssertEqual("demo", manifest.QaPolicyProfile);
        AssertEqual("dark", manifest.Preferences.Values.First());
    }

    private void TestManifestLoaderRejectsInvalidQaPolicy()
    {
        try
        {
            AdaptiveRuntimeManifestLoader.Load(path: null, json: """{"qaPolicyProfile":"xyz"}""");
            AssertTrue(false, "An invalid qaPolicyProfile must be refused at load.");
        }
        catch (InvalidOperationException ex)
        {
            AssertTrue(ex.Message.Contains("Invalid qaPolicyProfile", StringComparison.Ordinal),
                "An invalid qaPolicyProfile must be refused legibly.");
        }
    }

    private void TestResolveRejectsInvalidManifestQaPolicy()
    {
        var manifest = new AdaptiveRuntimeManifest { QaPolicyProfile = "xyz" };
        try
        {
            AdaptiveRuntimePlanResolver.Resolve(
                "Generate composable backend command handlers and tests",
                manifest,
                bootstrapProfileOverride: "auto",
                qaPolicyOverride: "auto");
            AssertTrue(false, "An invalid manifest qaPolicyProfile must be refused by Resolve.");
        }
        catch (ArgumentException ex)
        {
            AssertTrue(ex.Message.Contains("Invalid qaPolicyProfile", StringComparison.Ordinal),
                "An invalid manifest qaPolicyProfile must be refused legibly.");
        }
    }

    private void TestReleasePolicyUsesStrictVisualFallback()
    {
        var manifest = AdaptiveRuntimeManifest.Default();
        var plan = AdaptiveRuntimePlanResolver.Resolve(
            "Create a visual transform dashboard with UI adaptation",
            manifest,
            bootstrapProfileOverride: "auto",
            qaPolicyOverride: "release");

        /// <summary>Assert equal.</summary>
        AssertEqual("release", plan.QaPolicyProfile);
        /// <summary>Assert equal.</summary>
        AssertEqual("strict", plan.VisualQaFallbackPolicy);
        /// <summary>Assert true.</summary>
        /// <param name="QA."">Qa.".</param>
        AssertTrue(plan.RunVisualQa, "Release policy on visual goals should still require visual QA.");
    }
}
