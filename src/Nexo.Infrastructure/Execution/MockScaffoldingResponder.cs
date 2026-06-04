using System.Text.Json;
using System.Text.RegularExpressions;

namespace Nexo.Infrastructure.Execution;

internal static partial class MockScaffoldingResponder
{
    internal static string Generate(string systemPrompt, string userPrompt)
    {
        systemPrompt ??= "";
        userPrompt ??= "";

        // Orchestration: Architect decomposition schema
        if (systemPrompt.Contains("Architect Agent that decomposes complex requests", StringComparison.OrdinalIgnoreCase))
        {
            var request =
                Regex.Match(userPrompt, @"^\s*Request:\s*(?<req>.+?)\s*$", RegexOptions.Multiline).Groups["req"].Value.Trim();
            if (string.IsNullOrWhiteSpace(request))
            {
                request = Regex.Match(userPrompt, @"^\s*Original request:\s*(?<req>.+?)\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
                    .Groups["req"].Value.Trim();
            }
            if (string.IsNullOrWhiteSpace(request))
            {
                request = "request";
            }

            // Minimal, schema-compliant decomposition with one agent.
            // This keeps orchestration functional in offline/demo mode without network access.
            var agent = new Dictionary<string, object?>
            {
                ["agentId"] = "gameplay-1",
                ["domain"] = "Gameplay",
                ["goal"] = $"Handle request: {request}",
                ["description"] = "Offline/mock decomposition (single-agent). Expand domains when using a real provider.",
                ["dependencies"] = Array.Empty<string>(),
                // IMPORTANT: omit outputSchema entirely (null triggers schema validation errors downstream)
                ["constraints"] = Array.Empty<object>(),
                ["resourceRequirements"] = new
                {
                    estimatedComputeSeconds = 30,
                    requiredContextTokens = 1000,
                    requiredMemoryMB = 256
                },
                ["priority"] = 1
            };

            var obj = new Dictionary<string, object?>
            {
                ["agents"] = new[] { agent },
                ["reasoning"] = "Offline/mock-json provider produced a minimal valid decomposition.",
                ["confidence"] = 0.55
            };

            return JsonSerializer.Serialize(obj);
        }

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

        // Self-extend tool-calling agent (used by background-agent extender + self-extend CLI command).
        if (systemPrompt.Contains("You are a self-extending code agent", StringComparison.OrdinalIgnoreCase))
        {
            var objective = ExtractObjective(userPrompt);
            if (LooksLikeUiFeatureHotloadObjective(objective))
            {
                return BuildUiFeatureHotloadToolCallsJson(systemPrompt, objective);
            }
            if (LooksLikeUnityBootstrapObjective(objective))
            {
                return BuildUnityBootstrapToolCallsJson(systemPrompt, objective);
            }
            if (LooksLikeUiDemoObjective(objective))
            {
                return BuildUiDemoToolCallsJson(systemPrompt);
            }
            if (LooksLikePersonalSoftwareObjective(objective))
            {
                return BuildPersonalAppToolCallsJson(systemPrompt);
            }
            if (LooksLikeComposableBackendObjective(objective))
            {
                return BuildComposableBackendToolCallsJson(systemPrompt);
            }

            // Explicitly return an empty tool call envelope for schema consistency.
            return JsonSerializer.Serialize(new { tool_calls = Array.Empty<object>() });
        }

        // Fallback: return a benign JSON object
        return "{}";
    }

    private static string ExtractObjective(string userPrompt)
    {
        if (string.IsNullOrWhiteSpace(userPrompt))
            return string.Empty;
        var match = Regex.Match(userPrompt, @"Objective:\s*(?<goal>[\s\S]+)$", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups["goal"].Value.Trim() : userPrompt.Trim();
    }

    private static bool LooksLikeUnityBootstrapObjective(string objective)
    {
        if (string.IsNullOrWhiteSpace(objective))
            return false;
        return objective.Contains("unity", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("mono", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("dash ability", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("gameplay system", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("weapon system", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("movement system", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("health system", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("fps", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("monobehaviour", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("scriptableobject", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("weapon", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("shooter", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("multiplayer", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeNuancedUnityObjective(string objective)
    {
        if (string.IsNullOrWhiteSpace(objective))
            return false;
        return objective.Contains("cooldown", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("error state", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("compile error", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("inspector", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("raw code", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikePersonalSoftwareObjective(string objective)
    {
        if (string.IsNullOrWhiteSpace(objective))
            return false;
        return objective.Contains("personal", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("productivity", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("profile", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("preferences", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("tasks", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("reminders", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("dashboard", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeComposableBackendObjective(string objective)
    {
        if (string.IsNullOrWhiteSpace(objective))
            return false;
        var hasComposable = objective.Contains("composable", StringComparison.OrdinalIgnoreCase);
        return hasComposable && (
            objective.Contains("backend", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("extension command", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("qa gate", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("adaptive command", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("non-visual", StringComparison.OrdinalIgnoreCase));
    }

    private static bool LooksLikeUiDemoObjective(string objective)
    {
        if (string.IsNullOrWhiteSpace(objective))
            return false;
        var hasUiIntent = objective.Contains("ui demo", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("simple demo app", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("web ui", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("demo app with a ui", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("interactive demo", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("chat bot interface", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("chatbot interface", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("avalonia", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("maui", StringComparison.OrdinalIgnoreCase);
        var hasDomainKnowledgeIntent = objective.Contains("domain knowledge", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("knowledge layer", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("retained in that layer", StringComparison.OrdinalIgnoreCase);
        var hasHotloadIntent = objective.Contains("request a new feature", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("spin it up", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("dynamically load", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("ui changes", StringComparison.OrdinalIgnoreCase);
        return hasUiIntent || hasDomainKnowledgeIntent || hasHotloadIntent;
    }

    private static bool LooksLikeUiFeatureHotloadObjective(string objective)
    {
        if (string.IsNullOrWhiteSpace(objective))
            return false;
        return objective.Contains("UI_FEATURE_HOTLOAD", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("AVALONIA_FEATURE_HOTLOAD", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("AVALONIA_APP_TRANSFORM", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("hot-loadable ui feature module", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildUiFeatureHotloadToolCallsJson(string systemPrompt, string objective)
    {
        var root = ResolveRepoRootFromSystemPrompt(systemPrompt);
        var request = ExtractUiFeatureRequest(objective);
        var slug = SlugifyIdentifier(request);
        var retainedCapabilities = MatchUiDomainCapabilities(request);

        if (objective.Contains("AVALONIA_FEATURE_HOTLOAD", StringComparison.OrdinalIgnoreCase))
        {
            var descriptorPath = $"docs/UiDomainDemoGenerated/avalonia/Nexo.Ui.AvaloniaHost/GeneratedExtensions/{slug}.json";
            var descriptorContent = BuildAvaloniaFeatureDescriptorJson(request, slug, retainedCapabilities);
            return JsonSerializer.Serialize(new
            {
                tool_calls = new[] { CreateWriteCall(root, descriptorPath, descriptorContent) }
            });
        }
        if (objective.Contains("AVALONIA_APP_TRANSFORM", StringComparison.OrdinalIgnoreCase))
        {
            var descriptorPath = $"docs/UiDomainDemoGenerated/avalonia/Nexo.Ui.AvaloniaHost/GeneratedExtensions/{slug}.json";
            var descriptorContent = BuildAvaloniaAppTransformDescriptorJson(request, slug, retainedCapabilities);
            return JsonSerializer.Serialize(new
            {
                tool_calls = new[] { CreateWriteCall(root, descriptorPath, descriptorContent) }
            });
        }

        var modulePath = $"docs/UiDomainDemoGenerated/app/generated/{slug}.js";
        var moduleSource = BuildUiFeatureModuleSource(request, slug, retainedCapabilities);

        var calls = new List<object>
        {
            CreateWriteCall(root, modulePath, moduleSource)
        };

        return JsonSerializer.Serialize(new { tool_calls = calls });
    }

    private static string ExtractUiFeatureRequest(string objective)
    {
        var match = Regex.Match(objective, @"Feature request:\s*(?<req>.+)$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        if (match.Success && !string.IsNullOrWhiteSpace(match.Groups["req"].Value))
        {
            var raw = match.Groups["req"].Value.Trim();
            raw = raw.Replace("\\n", " ", StringComparison.Ordinal);
            var markerIndex = raw.IndexOf("Write output module under", StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0)
                markerIndex = raw.IndexOf("Write output descriptor under", StringComparison.OrdinalIgnoreCase);
            if (markerIndex >= 0)
                raw = raw[..markerIndex].Trim();
            raw = raw.Trim().TrimEnd('.', ';');
            if (!string.IsNullOrWhiteSpace(raw))
                return raw;
        }
        return "Generated feature module";
    }

    private static string SlugifyIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "feature_generated";
        var normalized = value.ToLowerInvariant();
        normalized = Regex.Replace(normalized, @"[^a-z0-9]+", "_");
        normalized = normalized.Trim('_');
        if (string.IsNullOrWhiteSpace(normalized))
            normalized = "feature_generated";
        if (char.IsDigit(normalized[0]))
            normalized = $"f_{normalized}";
        return normalized;
    }

    private static string[] MatchUiDomainCapabilities(string requestText)
    {
        var request = requestText.ToLowerInvariant();
        var matches = new List<string>();
        var catalog = new (string Id, string Token)[]
        {
            ("quest-tracking", "quest"),
            ("inventory-events", "inventory"),
            ("ability-cooldowns", "ability"),
            ("onboarding-flows", "onboarding"),
            ("ui-notifications", "notification")
        };
        foreach (var item in catalog)
        {
            if (request.Contains(item.Token, StringComparison.Ordinal))
                matches.Add(item.Id);
        }
        if (matches.Count == 0)
            matches.AddRange(new[] { "quest-tracking", "onboarding-flows" });
        return matches.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static string BuildUiDemoToolCallsJson(string systemPrompt)
    {
        var root = ResolveRepoRootFromSystemPrompt(systemPrompt);
        var extensionCommands = new List<(string ClassName, string CommandName, string ExtensionId, string[] Dependencies)>
        {
            ("DomainKnowledgeExtensionCommand", "ext-domain-knowledge", "domain-knowledge", Array.Empty<string>()),
            ("UiShellExtensionCommand", "ext-ui-shell", "ui-shell", new[] { "domain-knowledge" }),
            ("UiWorkflowExtensionCommand", "ext-ui-workflow", "ui-workflow", new[] { "domain-knowledge", "ui-shell" }),
            ("FeatureHotloadExtensionCommand", "ext-feature-hotload", "feature-hotload", new[] { "domain-knowledge", "ui-shell", "ui-workflow" }),
        };

        var calls = new List<object>
        {
            CreateWriteCall(root, "docs/UiDomainDemoGenerated/README.md", BuildUiDemoReadmeSource()),
            CreateWriteCall(root, "docs/UiDomainDemoGenerated/app/index.html", BuildUiDemoHtmlSource()),
            CreateWriteCall(root, "docs/UiDomainDemoGenerated/app/styles.css", BuildUiDemoCssSource()),
            CreateWriteCall(root, "docs/UiDomainDemoGenerated/app/app.js", BuildUiDemoJsSource()),
            CreateWriteCall(root, "docs/UiDomainDemoGenerated/app/domain-knowledge.json", BuildUiDomainKnowledgeJsonSource()),
            CreateWriteCall(root, "docs/UiDomainDemoGenerated/host/UiDemoHost.csproj", BuildUiDemoHostProjectSource()),
            CreateWriteCall(root, "docs/UiDomainDemoGenerated/host/Program.cs", BuildUiDemoHostProgramSource()),
            CreateWriteCall(root, "docs/UiDomainDemoGenerated/host/UiDemoSmoke.csproj", BuildUiDemoSmokeProjectSource()),
            CreateWriteCall(root, "docs/UiDomainDemoGenerated/host/SmokeProgram.cs", BuildUiDemoSmokeProgramSource()),
            CreateWriteCall(root, "docs/UiDomainDemoGenerated/avalonia/Nexo.Ui.Abstractions/Nexo.Ui.Abstractions.csproj", BuildAvaloniaAbstractionsProjectSource()),
            CreateWriteCall(root, "docs/UiDomainDemoGenerated/avalonia/Nexo.Ui.Abstractions/UiContracts.cs", BuildAvaloniaUiContractsSource()),
            CreateWriteCall(root, "docs/UiDomainDemoGenerated/avalonia/Nexo.Ui.AvaloniaHost/Nexo.Ui.AvaloniaHost.csproj", BuildAvaloniaHostProjectSource()),
            CreateWriteCall(root, "docs/UiDomainDemoGenerated/avalonia/Nexo.Ui.AvaloniaHost/Program.cs", BuildAvaloniaHostProgramSource()),
            CreateWriteCall(root, "docs/UiDomainDemoGenerated/avalonia/Nexo.Ui.AvaloniaHost/GeneratedExtensions/.gitkeep", string.Empty),
            CreateWriteCall(root, "docs/UiDomainDemoGenerated/avalonia/Nexo.Ui.AvaloniaHost/README.md", BuildAvaloniaHostReadmeSource()),

            // Command-structure scaffolding for composable extension commands.
            CreateWriteCall(root, "application/src/Nexo.CLI/Commands/SelfExtendGenerated/IComposableExtensionCommand.cs", BuildComposableCommandContractSource()),
        };

        foreach (var ext in extensionCommands)
        {
            calls.Add(CreateWriteCall(
                root,
                $"application/src/Nexo.CLI/Commands/SelfExtendGenerated/{ext.ClassName}.cs",
                BuildExtensionCommandSource(ext.ClassName, ext.CommandName, ext.ExtensionId, ext.Dependencies)));
        }

        calls.Add(CreateWriteCall(
            root,
            "application/src/Nexo.CLI/Commands/SelfExtendGenerated/SelfExtendUiDemoBundleCommand.cs",
            BuildBundleCommandSource(
                bundleClassName: "SelfExtendUiDemoBundleCommand",
                bundleCommandName: "self-extend-ui-demo-bundle",
                extensionCommands: extensionCommands.Select(e => (e.ClassName, e.CommandName)).ToArray())));

        // Generated tests that validate extension command structure.
        foreach (var ext in extensionCommands)
        {
            calls.Add(CreateWriteCall(
                root,
                $"application/src/Nexo.Tests.CLI/Tests/Commands/SelfExtendGenerated/{ext.ClassName}StructureTests.cs",
                BuildExtensionCommandStructureTestSource($"{ext.ClassName}StructureTests", ext.ClassName, ext.CommandName, ext.ExtensionId, ext.Dependencies)));
        }

        calls.Add(CreateWriteCall(
            root,
            "application/src/Nexo.Tests.CLI/Tests/Commands/SelfExtendGenerated/SelfExtendUiDemoBundleCommandStructureTests.cs",
            BuildBundleCommandStructureTestSource(
                testClassName: "SelfExtendUiDemoBundleCommandStructureTests",
                bundleClassName: "SelfExtendUiDemoBundleCommand",
                expectedBundleCommandName: "self-extend-ui-demo-bundle",
                expectedCommands: extensionCommands.Select(e => e.CommandName).ToArray())));

        calls.Add(CreateWriteCall(
            root,
            "application/src/Nexo.Tests.CLI/Tests/Commands/SelfExtendGenerated/UiDomainKnowledgeRetentionTests.cs",
            LoadCanonicalGeneratedSource(
                root,
                "application/src/Nexo.Tests.CLI/Tests/Commands/SelfExtendGenerated/UiDomainKnowledgeRetentionTests.cs")));

        return JsonSerializer.Serialize(new { tool_calls = calls });
    }

    private static string BuildPersonalAppToolCallsJson(string systemPrompt)
    {
        var root = ResolveRepoRootFromSystemPrompt(systemPrompt);
        var extensionCommands = new List<(string ClassName, string CommandName, string ExtensionId, string[] Dependencies)>
        {
            ("ProfileExtensionCommand", "ext-profile", "profile", Array.Empty<string>()),
            ("PreferencesExtensionCommand", "ext-preferences", "preferences", new[] { "profile" }),
            ("TasksExtensionCommand", "ext-tasks", "tasks", new[] { "profile", "preferences" }),
            ("RemindersExtensionCommand", "ext-reminders", "reminders", new[] { "tasks" }),
            ("DashboardExtensionCommand", "ext-dashboard", "dashboard", new[] { "tasks", "reminders" }),
        };

        var calls = new List<object>
        {
            CreateWriteCall(root, "docs/PersonalAppGenerated/UserProfile.cs", BuildPersonalUserProfileSource()),
            CreateWriteCall(root, "docs/PersonalAppGenerated/UserPreferences.cs", BuildPersonalUserPreferencesSource()),
            CreateWriteCall(root, "docs/PersonalAppGenerated/PersonalTaskItem.cs", BuildPersonalTaskItemSource()),
            CreateWriteCall(root, "docs/PersonalAppGenerated/PersonalReminder.cs", BuildPersonalReminderSource()),
            CreateWriteCall(root, "docs/PersonalAppGenerated/ProgressDashboard.cs", BuildPersonalProgressDashboardSource()),
            CreateWriteCall(root, "docs/PersonalAppGenerated/README.md", BuildPersonalAppReadmeSource()),

            // Command-structure scaffolding for composable extension commands.
            CreateWriteCall(root, "application/src/Nexo.CLI/Commands/SelfExtendGenerated/IComposableExtensionCommand.cs", BuildComposableCommandContractSource()),
        };

        foreach (var ext in extensionCommands)
        {
            calls.Add(CreateWriteCall(
                root,
                $"application/src/Nexo.CLI/Commands/SelfExtendGenerated/{ext.ClassName}.cs",
                BuildExtensionCommandSource(ext.ClassName, ext.CommandName, ext.ExtensionId, ext.Dependencies)));
        }

        calls.Add(CreateWriteCall(
            root,
            "application/src/Nexo.CLI/Commands/SelfExtendGenerated/SelfExtendPersonalBundleCommand.cs",
            BuildBundleCommandSource(
                bundleClassName: "SelfExtendPersonalBundleCommand",
                bundleCommandName: "self-extend-personal-bundle",
                extensionCommands: extensionCommands.Select(e => (e.ClassName, e.CommandName)).ToArray())));

        // Generated tests that validate extension command structure.
        foreach (var ext in extensionCommands)
        {
            calls.Add(CreateWriteCall(
                root,
                $"application/src/Nexo.Tests.CLI/Tests/Commands/SelfExtendGenerated/{ext.ClassName}StructureTests.cs",
                BuildExtensionCommandStructureTestSource($"{ext.ClassName}StructureTests", ext.ClassName, ext.CommandName, ext.ExtensionId, ext.Dependencies)));
        }

        calls.Add(CreateWriteCall(
            root,
            "application/src/Nexo.Tests.CLI/Tests/Commands/SelfExtendGenerated/SelfExtendPersonalBundleCommandStructureTests.cs",
            BuildBundleCommandStructureTestSource(
                testClassName: "SelfExtendPersonalBundleCommandStructureTests",
                bundleClassName: "SelfExtendPersonalBundleCommand",
                expectedBundleCommandName: "self-extend-personal-bundle",
                extensionCommands.Select(e => e.CommandName).ToArray())));

        return JsonSerializer.Serialize(new { tool_calls = calls });
    }

    private static string BuildComposableBackendToolCallsJson(string systemPrompt)
    {
        var root = ResolveRepoRootFromSystemPrompt(systemPrompt);
        var extensionCommands = new List<(string ClassName, string CommandName, string ExtensionId, string[] Dependencies)>
        {
            ("PipelineValidateExtensionCommand", "ext-pipeline-validate", "pipeline-validate", Array.Empty<string>()),
            ("PipelineRunExtensionCommand", "ext-pipeline-run", "pipeline-run", new[] { "pipeline-validate" }),
            ("MeshScheduleExtensionCommand", "ext-mesh-schedule", "mesh-schedule", new[] { "pipeline-run" }),
        };

        var calls = new List<object>
        {
            CreateWriteCall(root, "application/src/Nexo.CLI/Commands/SelfExtendGenerated/IComposableExtensionCommand.cs", BuildComposableCommandContractSource()),
        };

        foreach (var ext in extensionCommands)
        {
            calls.Add(CreateWriteCall(
                root,
                $"application/src/Nexo.CLI/Commands/SelfExtendGenerated/{ext.ClassName}.cs",
                BuildExtensionCommandSource(ext.ClassName, ext.CommandName, ext.ExtensionId, ext.Dependencies)));
        }

        calls.Add(CreateWriteCall(
            root,
            "application/src/Nexo.CLI/Commands/SelfExtendGenerated/SelfExtendBackendBundleCommand.cs",
            BuildBundleCommandSource(
                bundleClassName: "SelfExtendBackendBundleCommand",
                bundleCommandName: "self-extend-backend-bundle",
                extensionCommands: extensionCommands.Select(e => (e.ClassName, e.CommandName)).ToArray())));

        foreach (var ext in extensionCommands)
        {
            calls.Add(CreateWriteCall(
                root,
                $"application/src/Nexo.Tests.CLI/Tests/Commands/SelfExtendGenerated/{ext.ClassName}StructureTests.cs",
                BuildExtensionCommandStructureTestSource($"{ext.ClassName}StructureTests", ext.ClassName, ext.CommandName, ext.ExtensionId, ext.Dependencies)));
        }

        calls.Add(CreateWriteCall(
            root,
            "application/src/Nexo.Tests.CLI/Tests/Commands/SelfExtendGenerated/SelfExtendBackendBundleCommandStructureTests.cs",
            BuildBundleCommandStructureTestSource(
                testClassName: "SelfExtendBackendBundleCommandStructureTests",
                bundleClassName: "SelfExtendBackendBundleCommand",
                expectedBundleCommandName: "self-extend-backend-bundle",
                expectedCommands: extensionCommands.Select(e => e.CommandName).ToArray())));

        return JsonSerializer.Serialize(new { tool_calls = calls });
    }

    private static string BuildUnityBootstrapToolCallsJson(string systemPrompt, string objective)
    {
        var root = ResolveRepoRootFromSystemPrompt(systemPrompt);
        var nuanced = LooksLikeNuancedUnityObjective(objective);
        var includeJump = objective.Contains("jump", StringComparison.OrdinalIgnoreCase);
        var includeSprint = objective.Contains("sprint", StringComparison.OrdinalIgnoreCase);
        var includeRegistry = objective.Contains("registry", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("compose", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("composed", StringComparison.OrdinalIgnoreCase);

        var interfaceContent = """
namespace Nexo.Unity.Generated;

public interface IGeneratedGameplaySystem
{
    string Id { get; }
    string DisplayName { get; }
    void Tick(SystemContext context);
}
""";

        var contextContent = BuildSystemContextSource(includeJump, includeSprint);
        var dashContent = nuanced ? BuildDashSystemNuancedSource() : BuildDashSystemBaselineSource();
        var jumpContent = BuildJumpSystemSource();
        var sprintContent = BuildSprintSystemSource();
        var registryContent = BuildAbilityRegistrySource();
        var errorStateContent = BuildErrorStateSource();
        var inspectorSnapshotContent = BuildInspectorSnapshotSource();

        var extensionCommands = new List<(string ClassName, string CommandName, string ExtensionId, string[] Dependencies)>
        {
            ("DashExtensionCommand", "ext-dash", "dash", Array.Empty<string>()),
            ("JumpExtensionCommand", "ext-jump", "jump", new[] { "dash" }),
            ("SprintExtensionCommand", "ext-sprint", "sprint", new[] { "dash" }),
            ("AbilityRegistryExtensionCommand", "ext-registry", "registry", new[] { "dash", "jump", "sprint" }),
        };

        var calls = new List<object>
        {
            CreateWriteCall(root, "Assets/Scripts/Generated/IGeneratedGameplaySystem.cs", interfaceContent),
            CreateWriteCall(root, "Assets/Scripts/Generated/SystemContext.cs", contextContent),
            CreateWriteCall(root, "Assets/Scripts/Generated/DashAbilitySystem.cs", dashContent),
        };

        if (includeJump)
            calls.Add(CreateWriteCall(root, "Assets/Scripts/Generated/JumpAbilitySystem.cs", jumpContent));
        if (includeSprint)
            calls.Add(CreateWriteCall(root, "Assets/Scripts/Generated/SprintAbilitySystem.cs", sprintContent));
        if (includeRegistry)
            calls.Add(CreateWriteCall(root, "Assets/Scripts/Generated/AbilityRegistry.cs", registryContent));

        if (nuanced)
        {
            calls.Add(CreateWriteCall(root, "Assets/Scripts/Generated/GeneratedSystemErrorState.cs", errorStateContent));
            calls.Add(CreateWriteCall(root, "Assets/Scripts/Generated/GeneratedSystemInspectorSnapshot.cs", inspectorSnapshotContent));
        }

        // Command-structure scaffolding for composition.
        calls.Add(CreateWriteCall(root, "application/src/Nexo.CLI/Commands/SelfExtendGenerated/IComposableExtensionCommand.cs", BuildComposableCommandContractSource()));
        foreach (var ext in extensionCommands)
        {
            calls.Add(CreateWriteCall(
                root,
                $"application/src/Nexo.CLI/Commands/SelfExtendGenerated/{ext.ClassName}.cs",
                BuildExtensionCommandSource(ext.ClassName, ext.CommandName, ext.ExtensionId, ext.Dependencies)));
        }
        calls.Add(CreateWriteCall(
            root,
            "application/src/Nexo.CLI/Commands/SelfExtendGenerated/SelfExtendBundleCommand.cs",
            BuildBundleCommandSource(extensionCommands.Select(e => (e.ClassName, e.CommandName)).ToArray())));

        // Generated tests that validate extension command structure.
        foreach (var ext in extensionCommands)
        {
            calls.Add(CreateWriteCall(
                root,
                $"application/src/Nexo.Tests.CLI/Tests/Commands/SelfExtendGenerated/{ext.ClassName}StructureTests.cs",
                BuildExtensionCommandStructureTestSource($"{ext.ClassName}StructureTests", ext.ClassName, ext.CommandName, ext.ExtensionId, ext.Dependencies)));
        }
        calls.Add(CreateWriteCall(
            root,
            "application/src/Nexo.Tests.CLI/Tests/Commands/SelfExtendGenerated/SelfExtendBundleCommandStructureTests.cs",
            BuildBundleCommandStructureTestSource(
                "SelfExtendBundleCommandStructureTests",
                extensionCommands.Select(e => e.CommandName).ToArray())));

        return JsonSerializer.Serialize(new { tool_calls = calls });
    }

    private static object CreateWriteCall(string root, string path, string content) => new
    {
        id = "repo.fs.write",
        arguments = new
        {
            root,
            path,
            content
        }
    };

    private static string ResolveRepoRootFromSystemPrompt(string systemPrompt)
    {
        var match = Regex.Match(systemPrompt, "\"RepoRoot\"\\s*:\\s*\"(?<root>[^\"]+)\"", RegexOptions.IgnoreCase);
        return match.Success && !string.IsNullOrWhiteSpace(match.Groups["root"].Value)
            ? match.Groups["root"].Value
            : ".";
    }

    private static string LoadCanonicalGeneratedSource(string root, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return BuildUiDomainKnowledgeRetentionTestSource();

        try
        {
            var baseRoot = string.IsNullOrWhiteSpace(root) ? "." : root;
            var fullRoot = Path.IsPathRooted(baseRoot) ? baseRoot : Path.GetFullPath(baseRoot);
            var fullPath = Path.GetFullPath(Path.Combine(fullRoot, relativePath));

            if (File.Exists(fullPath))
                return File.ReadAllText(fullPath);
        }
        catch
        {
            // Fall back to embedded template below.
        }

        return BuildUiDomainKnowledgeRetentionTestSource();
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

    /// <summary>
    /// Writes the UI domain demo baseline under <c>docs/UiDomainDemoGenerated/</c> when missing
    /// (e.g. <c>nexo validate</c> / UnitTestBridge without a prior <c>self-extend</c> run).
    /// </summary>
    internal static void EnsureUiDomainDemoBaseline(string repoRoot)
    {
        var marker = Path.Combine(repoRoot, "docs", "UiDomainDemoGenerated", "app", "index.html");
        if (File.Exists(marker))
            return;

        void Write(string relativePath, string content)
        {
            var full = Path.Combine(repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var dir = Path.GetDirectoryName(full);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(full, content);
        }

        Write("docs/UiDomainDemoGenerated/app/index.html", BuildUiDemoHtmlSource());
        Write("docs/UiDomainDemoGenerated/app/app.js", BuildUiDemoJsSource());
        Write("docs/UiDomainDemoGenerated/app/domain-knowledge.json", BuildUiDomainKnowledgeJsonSource());
        Write("docs/UiDomainDemoGenerated/host/Program.cs", BuildUiDemoHostProgramSource());
        Write("docs/UiDomainDemoGenerated/host/UiDemoHost.csproj", BuildUiDemoHostProjectSource());
        Write("docs/UiDomainDemoGenerated/host/SmokeProgram.cs", BuildUiDemoSmokeProgramSource());
        Write("docs/UiDomainDemoGenerated/avalonia/Nexo.Ui.Abstractions/UiContracts.cs", BuildAvaloniaUiContractsSource());
        Write("docs/UiDomainDemoGenerated/avalonia/Nexo.Ui.AvaloniaHost/Program.cs", BuildAvaloniaHostProgramSource());
    }

}
