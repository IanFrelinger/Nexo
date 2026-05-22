namespace Nexo.CLI.Runtime;

public static class AdaptiveRuntimePlanResolver
{
    private const string UiSmokeProjectPath = "docs/UiDomainDemoGenerated/avalonia/Nexo.Ui.AvaloniaHost/Nexo.Ui.AvaloniaHost.csproj";

    public static AdaptiveRuntimeExecutionPlan Resolve(
        string goal,
        AdaptiveRuntimeManifest manifest,
        string? bootstrapProfileOverride,
        string? qaPolicyOverride)
    {
        var normalizedGoal = (goal ?? string.Empty).Trim().ToLowerInvariant();
        var reasons = new List<string>();

        var uiIntent = ContainsAny(normalizedGoal, "ui", "ux", "avalonia", "maui", "chatbot", "render", "hotload", "feature");
        var explicitNonVisual = ContainsAny(normalizedGoal, "non-visual", "without visual", "no visual", "text-only");
        var visualIntent = !explicitNonVisual && ContainsAny(normalizedGoal, "visual", "screenshot", "aesthetic", "transform", "design");
        var personalIntent =
            ContainsAny(normalizedGoal, "personal", "productivity", "preferences", "profile", "dashboard") ||
            manifest.DomainPacks.Contains("personal", StringComparer.OrdinalIgnoreCase);

        var profile = NormalizeBootstrapProfile(bootstrapProfileOverride);
        if (profile == "auto")
        {
            if (visualIntent)
            {
                profile = "self-extend-visual";
                reasons.Add("auto-profile selected visual due to visual/aesthetic intent");
            }
            else if (uiIntent || personalIntent)
            {
                profile = "self-extend-aesthetic";
                reasons.Add("auto-profile selected aesthetic due to UI/personal intent");
            }
            else
            {
                profile = "self-extend-functional";
                reasons.Add("auto-profile selected functional baseline");
            }
        }
        else
        {
            reasons.Add($"bootstrap profile overridden to '{profile}'");
        }

        var qaPolicy = NormalizeQaPolicy(qaPolicyOverride);
        if (qaPolicy == "auto")
        {
            qaPolicy = NormalizeQaPolicy(manifest.QaPolicyProfile);
            if (qaPolicy == "auto")
                qaPolicy = "demo";
            reasons.Add($"qa policy resolved to '{qaPolicy}'");
        }
        else
        {
            reasons.Add($"qa policy overridden to '{qaPolicy}'");
        }

        var runAestheticQa =
            profile != "self-extend-functional" ||
            uiIntent ||
            personalIntent ||
            manifest.UiCapabilities.Length > 0;
        var runVisualQa =
            profile == "self-extend-visual" ||
            visualIntent ||
            manifest.UiCapabilities.Contains("visual-qa", StringComparer.OrdinalIgnoreCase);
        if (!runAestheticQa)
            runVisualQa = false;

        var focus = profile == "self-extend-functional"
            ? "functional"
            : (uiIntent || personalIntent ? "aesthetic" : "balanced");

        var maxIterations = 2;
        var stopOnFirstPass = true;
        var visualFallbackPolicy = "degrade";
        var requirePreflight = true;

        switch (qaPolicy)
        {
            case "release":
                maxIterations = 4;
                stopOnFirstPass = true;
                visualFallbackPolicy = "strict";
                break;
            case "prod":
                maxIterations = 3;
                stopOnFirstPass = true;
                visualFallbackPolicy = "strict";
                break;
            case "research":
                maxIterations = 5;
                stopOnFirstPass = false;
                visualFallbackPolicy = "degrade";
                break;
            default:
                // demo
                maxIterations = 2;
                stopOnFirstPass = true;
                visualFallbackPolicy = "degrade";
                break;
        }

        var phases = new List<string> { "planner", "builder", "qa-functional" };
        if (runAestheticQa)
            phases.Add("qa-aesthetic");

        return new AdaptiveRuntimeExecutionPlan
        {
            BootstrapProfile = profile,
            QaPolicyProfile = qaPolicy,
            Focus = focus,
            MaxIterations = maxIterations,
            StopOnFirstPass = stopOnFirstPass,
            RunFunctionalQa = true,
            RunAestheticQa = runAestheticQa,
            RunVisualQa = runVisualQa,
            VisualQaFallbackPolicy = visualFallbackPolicy,
            RequirePreflight = requirePreflight,
            AgentPhases = phases.ToArray(),
            Reasons = reasons.ToArray()
        };
    }

    public static SelfExtendWorkflowRuntimeSpec BuildRuntimeSpec(AdaptiveRuntimeExecutionPlan plan, string? functionalFilterOverride = null)
    {
        return new SelfExtendWorkflowRuntimeSpec
        {
            Workflow = new SelfExtendWorkflowSpec
            {
                MaxIterations = Math.Max(1, plan.MaxIterations),
                StopOnFirstPass = plan.StopOnFirstPass,
                Focus = NormalizeFocus(plan.Focus),
                EnableMultiAgentPhases = true,
                AgentPhases = plan.AgentPhases.Length == 0 ? ["planner", "builder", "qa-functional"] : plan.AgentPhases,
                RunFunctionalQa = plan.RunFunctionalQa,
                RunAestheticQa = plan.RunAestheticQa,
                RunVisualQa = plan.RunVisualQa,
                VisualQaFallbackPolicy = NormalizeFallback(plan.VisualQaFallbackPolicy),
                FunctionalTestFilter = string.IsNullOrWhiteSpace(functionalFilterOverride)
                    ? "SelfExtendGenerated"
                    : functionalFilterOverride.Trim(),
                AestheticTestFilter = "UiDomainKnowledgeRetentionTests",
                UiSmokeProjectPath = UiSmokeProjectPath,
                RequirePreflight = plan.RequirePreflight
            }
        };
    }

    public static string EnrichGoal(string goal, AdaptiveRuntimeManifest manifest, AdaptiveRuntimeExecutionPlan plan)
    {
        var baseGoal = (goal ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(baseGoal))
            return baseGoal;

        var domain = manifest.DomainPacks.Length == 0
            ? "none"
            : string.Join(", ", manifest.DomainPacks);
        var ui = manifest.UiCapabilities.Length == 0
            ? "none"
            : string.Join(", ", manifest.UiCapabilities);
        var prefs = manifest.Preferences.Count == 0
            ? "none"
            : string.Join(", ", manifest.Preferences.Select(kv => $"{kv.Key}={kv.Value}"));

        return
            $"{baseGoal}\n\n" +
            "Adaptive runtime context:\n" +
            $"- bootstrap-profile: {plan.BootstrapProfile}\n" +
            $"- qa-policy: {plan.QaPolicyProfile}\n" +
            $"- domain-packs: {domain}\n" +
            $"- ui-capabilities: {ui}\n" +
            $"- preferences: {prefs}\n" +
            "- instruction: retain domain knowledge and keep generated extensions composable.";
    }

    private static bool ContainsAny(string value, params string[] terms)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeBootstrapProfile(string? profile)
    {
        var normalized = (profile ?? "auto").Trim().ToLowerInvariant();
        return normalized switch
        {
            "self-extend-functional" => "self-extend-functional",
            "self-extend-aesthetic" => "self-extend-aesthetic",
            "self-extend-visual" => "self-extend-visual",
            _ => "auto"
        };
    }

    private static string NormalizeQaPolicy(string? policy)
    {
        var normalized = (policy ?? "auto").Trim().ToLowerInvariant();
        return normalized switch
        {
            "demo" => "demo",
            "release" => "release",
            "prod" => "prod",
            "research" => "research",
            _ => "auto"
        };
    }

    private static string NormalizeFocus(string? focus)
    {
        var normalized = (focus ?? "balanced").Trim().ToLowerInvariant();
        return normalized switch
        {
            "functional" => "functional",
            "aesthetic" => "aesthetic",
            _ => "balanced"
        };
    }

    private static string NormalizeFallback(string? fallback)
    {
        return string.Equals(fallback, "strict", StringComparison.OrdinalIgnoreCase) ? "strict" : "degrade";
    }
}
