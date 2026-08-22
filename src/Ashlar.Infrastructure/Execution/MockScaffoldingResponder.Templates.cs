using System.Text.Json;
using System.Text.RegularExpressions;

namespace Ashlar.Infrastructure.Execution;

/// <summary>Embedded template sources for mock scaffolding code generation responses.</summary>
internal static partial class MockScaffoldingResponder
{
    private static string BuildSystemContextSource(bool includeJump, bool includeSprint)
    {
        var jumpFields = includeJump
            ? """
    public bool JumpPressed { get; init; }
    public float JumpForce { get; init; } = 8f;
"""
            : string.Empty;
        var sprintFields = includeSprint
            ? """
    public bool SprintPressed { get; init; }
    public float SprintSpeedMultiplier { get; init; } = 1.5f;
"""
            : string.Empty;

        return $$"""
namespace Ashlar.Unity.Generated;

public sealed class SystemContext
{
    public float DeltaTime { get; init; }
    public float CurrentTimeSeconds { get; init; }
    public bool DashPressed { get; init; }
    public float DashSpeed { get; init; } = 12f;
    public float DashDurationSeconds { get; init; } = 0.2f;
    public float DashCooldownSeconds { get; init; } = 1.0f;
{{jumpFields}}{{sprintFields}}
}
""";
    }

    private static string BuildDashSystemBaselineSource() => """
namespace Ashlar.Unity.Generated;

public sealed class DashAbilitySystem : IGeneratedGameplaySystem
{
    private float _remainingDashSeconds;
    private float _cooldownEndsAtSeconds;

    public string Id => "dash-ability";
    public string DisplayName => "Dash Ability";

    public void Tick(SystemContext context)
    {
        if (context.DashPressed && context.CurrentTimeSeconds >= _cooldownEndsAtSeconds)
        {
            _remainingDashSeconds = context.DashDurationSeconds;
            _cooldownEndsAtSeconds = context.CurrentTimeSeconds + context.DashCooldownSeconds;
        }

        if (_remainingDashSeconds > 0f)
            _remainingDashSeconds -= context.DeltaTime;
    }
}
""";

    private static string BuildDashSystemNuancedSource() => """
namespace Ashlar.Unity.Generated;

public sealed class DashAbilitySystem : IGeneratedGameplaySystem
{
    private float _remainingDashSeconds;
    private float _cooldownEndsAtSeconds;
    private GeneratedSystemErrorState _errorState = GeneratedSystemErrorState.None;

    public string Id => "dash-ability";
    public string DisplayName => "Dash Ability";

    public void Tick(SystemContext context)
    {
        if (context.DashPressed && context.CurrentTimeSeconds >= _cooldownEndsAtSeconds)
        {
            _remainingDashSeconds = context.DashDurationSeconds;
            _cooldownEndsAtSeconds = context.CurrentTimeSeconds + context.DashCooldownSeconds;
        }

        if (_remainingDashSeconds > 0f)
            _remainingDashSeconds -= context.DeltaTime;
    }

    public GeneratedSystemInspectorSnapshot Inspect(string generatedCode) =>
        new(
            SystemId: Id,
            RawGeneratedCode: generatedCode,
            ErrorState: _errorState,
            GeneratedAtUtc: System.DateTimeOffset.UtcNow);
}
""";

    private static string BuildJumpSystemSource() => """
namespace Ashlar.Unity.Generated;

public sealed class JumpAbilitySystem : IGeneratedGameplaySystem
{
    private bool _airborne;
    private float _verticalVelocity;

    public string Id => "jump-ability";
    public string DisplayName => "Jump Ability";

    public void Tick(SystemContext context)
    {
        if (context.JumpPressed && !_airborne)
        {
            _airborne = true;
            _verticalVelocity = context.JumpForce;
        }

        if (_airborne)
        {
            _verticalVelocity -= 9.8f * context.DeltaTime;
            if (_verticalVelocity <= 0f)
                _airborne = false;
        }
    }
}
""";

    private static string BuildSprintSystemSource() => """
namespace Ashlar.Unity.Generated;

public sealed class SprintAbilitySystem : IGeneratedGameplaySystem
{
    public string Id => "sprint-ability";
    public string DisplayName => "Sprint Ability";
    public float CurrentSpeedMultiplier { get; private set; } = 1f;

    public void Tick(SystemContext context)
    {
        CurrentSpeedMultiplier = context.SprintPressed ? context.SprintSpeedMultiplier : 1f;
    }
}
""";

    private static string BuildAbilityRegistrySource() => """
namespace Ashlar.Unity.Generated;

public sealed class AbilityRegistry
{
    private readonly Dictionary<string, IGeneratedGameplaySystem> _systems = new(StringComparer.Ordinal);

    public AbilityRegistry(IEnumerable<IGeneratedGameplaySystem> systems)
    {
        foreach (var system in systems)
            _systems[system.Id] = system;
    }

    public IReadOnlyCollection<IGeneratedGameplaySystem> All => _systems.Values;

    public bool TryGet(string id, out IGeneratedGameplaySystem? system)
        => _systems.TryGetValue(id, out system);
}
""";

    private static string BuildPersonalUserProfileSource() => """
namespace Ashlar.Personal.Generated;

public sealed record UserProfile(
    string UserId,
    string DisplayName,
    string TimeZoneId,
    string Locale);
""";

    private static string BuildPersonalUserPreferencesSource() => """
namespace Ashlar.Personal.Generated;

public sealed record UserPreferences(
    bool StartWithTodayView,
    bool EnableReminderNotifications,
    int DefaultFocusMinutes,
    int DailyGoalPoints);
""";

    private static string BuildPersonalTaskItemSource() => """
namespace Ashlar.Personal.Generated;

public sealed class PersonalTaskItem
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public bool Completed { get; private set; }
    public int Priority { get; init; } = 3;
    public System.DateTimeOffset? DueAtUtc { get; init; }

    public void MarkCompleted() => Completed = true;
}
""";

    private static string BuildPersonalReminderSource() => """
namespace Ashlar.Personal.Generated;

public sealed class PersonalReminder
{
    public required string Id { get; init; }
    public required string TaskId { get; init; }
    public required System.DateTimeOffset FireAtUtc { get; init; }
    public bool Sent { get; private set; }

    public bool ShouldFire(System.DateTimeOffset nowUtc) => !Sent && nowUtc >= FireAtUtc;
    public void MarkSent() => Sent = true;
}
""";

    private static string BuildPersonalProgressDashboardSource() => """
namespace Ashlar.Personal.Generated;

public sealed class ProgressDashboard
{
    public static int CalculateDailyPoints(System.Collections.Generic.IEnumerable<PersonalTaskItem> tasks)
    {
        if (tasks is null)
            return 0;

        var points = 0;
        foreach (var task in tasks)
        {
            if (!task.Completed)
                continue;
            points += task.Priority switch
            {
                <= 1 => 5,
                2 => 3,
                _ => 1
            };
        }
        return points;
    }
}
""";

    private static string BuildPersonalAppReadmeSource() => """
# Personal App Scaffold (Generated)

This generated package models a personal productivity app that keeps the Ashlar backend infrastructure unchanged.

Artifacts:
- User profile and preference models
- Task and reminder models
- Progress dashboard scoring logic
- Composable CLI extension commands for profile/preferences/tasks/reminders/dashboard
- Generated structure tests for each extension command and the composed bundle
""";

    private static string BuildUiDemoReadmeSource() => """
# UI Demo Scaffold (Generated)

This generated demo provides an interactive browser chatbot with a retained domain-knowledge layer.

Outputs:
- `docs/UiDomainDemoGenerated/app/index.html` chat + feature studio UI shell
- `docs/UiDomainDemoGenerated/app/app.js` chatbot workflow + real scaffold/hot-load runtime through server API
- `docs/UiDomainDemoGenerated/app/domain-knowledge.json` retained domain knowledge catalog
- `docs/UiDomainDemoGenerated/host/Program.cs` .NET API/static host that invokes `ashlar self-extend` for feature scaffolding
- `docs/UiDomainDemoGenerated/host/SmokeProgram.cs` .NET smoke checker for host + UI wiring
- `docs/UiDomainDemoGenerated/avalonia/Ashlar.Ui.Abstractions` framework-neutral UI contracts (cross-framework abstraction layer)
- `docs/UiDomainDemoGenerated/avalonia/Ashlar.Ui.AvaloniaHost` Linux-compatible Avalonia desktop host with dynamic extension loading
- composable extension commands + generated structure tests
""";

    private static string BuildUiDemoHtmlSource() => """
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>Ashlar UI Demo</title>
  <link rel="stylesheet" href="./styles.css" />
</head>
<body>
  <main class="app-shell">
    <header>
      <h1>Ashlar Interactive Domain Demo</h1>
      <p id="status-line">Loading domain knowledge...</p>
    </header>

    <section class="panel">
      <h2>Ashlar Chatbot</h2>
      <div id="chat-log" class="chat-log"></div>
      <div class="row">
        <input id="chat-input" type="text" value="What is Ashlar?" />
        <button id="chat-send-btn" type="button">Send</button>
      </div>
    </section>

    <section class="panel">
      <h2>Retained domain knowledge</h2>
      <ul id="knowledge-list"></ul>
    </section>

    <section class="panel">
      <h2>Feature request studio</h2>
      <label for="feature-input">Request new feature</label>
      <div class="row">
        <input id="feature-input" type="text" value="Add a quest streak tracker widget for returning players" />
        <button id="scaffold-feature-btn" type="button">Scaffold + Hot-load</button>
      </div>
      <p id="feature-status" class="muted">Awaiting feature request.</p>
    </section>

    <section class="panel">
      <h2>Dynamically loaded features</h2>
      <div id="dynamic-feature-host" class="dynamic-feature-host"></div>
    </section>

    <section class="panel">
      <h2>Generated scaffold plan</h2>
      <pre id="output-pane"></pre>
    </section>
  </main>
  <script src="./app.js" defer></script>
</body>
</html>
""";

    private static string BuildUiDemoCssSource() => """
:root {
  color-scheme: dark;
  font-family: Arial, sans-serif;
}

body {
  margin: 0;
  background: #111827;
  color: #e5e7eb;
}

.app-shell {
  max-width: 920px;
  margin: 0 auto;
  padding: 24px;
}

.panel {
  background: #1f2937;
  border: 1px solid #374151;
  border-radius: 10px;
  padding: 14px;
  margin-top: 14px;
}

.row {
  display: flex;
  gap: 8px;
  align-items: center;
}

#chat-input,
#feature-input {
  flex: 1;
  margin-top: 8px;
  margin-bottom: 8px;
  padding: 10px;
  border-radius: 6px;
  border: 1px solid #4b5563;
  background: #111827;
  color: #e5e7eb;
}

#chat-send-btn,
#scaffold-feature-btn {
  padding: 8px 12px;
  border-radius: 6px;
  border: 1px solid #4b5563;
  background: #2563eb;
  color: #ffffff;
  cursor: pointer;
  white-space: nowrap;
}

#knowledge-list li {
  margin-bottom: 6px;
}

.chat-log {
  max-height: 220px;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 8px;
  background: #111827;
  border-radius: 6px;
  padding: 10px;
}

.chat-msg {
  padding: 8px 10px;
  border-radius: 8px;
  border: 1px solid #374151;
  white-space: pre-wrap;
}

.chat-user {
  background: #1e3a8a;
}

.chat-assistant {
  background: #0f766e;
}

.dynamic-feature-host {
  display: grid;
  gap: 10px;
}

.feature-card {
  border: 1px solid #4b5563;
  border-radius: 8px;
  padding: 10px;
  background: #111827;
}

.feature-chip-row {
  display: flex;
  gap: 6px;
  flex-wrap: wrap;
  margin-top: 8px;
}

.feature-chip {
  font-size: 12px;
  border: 1px solid #334155;
  border-radius: 999px;
  padding: 2px 8px;
}

.muted {
  color: #9ca3af;
}

#output-pane {
  background: #111827;
  border-radius: 6px;
  padding: 10px;
  min-height: 120px;
  white-space: pre-wrap;
}
""";

    private static string BuildUiDemoJsSource() => """
const statusLine = document.getElementById("status-line");
const knowledgeList = document.getElementById("knowledge-list");
const chatLog = document.getElementById("chat-log");
const chatInput = document.getElementById("chat-input");
const chatSendButton = document.getElementById("chat-send-btn");
const featureInput = document.getElementById("feature-input");
const scaffoldFeatureButton = document.getElementById("scaffold-feature-btn");
const featureStatus = document.getElementById("feature-status");
const dynamicFeatureHost = document.getElementById("dynamic-feature-host");
const outputPane = document.getElementById("output-pane");

let domainCatalog = [];
const dynamicFeatures = [];

function appendChatMessage(role, text) {
  const item = document.createElement("div");
  item.className = `chat-msg ${role === "user" ? "chat-user" : "chat-assistant"}`;
  item.textContent = text;
  chatLog.appendChild(item);
  chatLog.scrollTop = chatLog.scrollHeight;
}

async function loadDomainKnowledge() {
  const response = await fetch("./domain-knowledge.json");
  if (!response.ok) {
    throw new Error("Failed to load domain knowledge");
  }
  const data = await response.json();
  domainCatalog = data.capabilities ?? [];
  renderKnowledge();
  statusLine.textContent = `Domain knowledge ready: ${domainCatalog.length} capabilities loaded`;
  appendChatMessage("assistant", "Hi! I am the Ashlar demo assistant. Ask what Ashlar is, or request a feature to scaffold and hot-load.");
}

function renderKnowledge() {
  knowledgeList.innerHTML = "";
  for (const item of domainCatalog) {
    const li = document.createElement("li");
    li.textContent = `${item.id}: ${item.summary}`;
    knowledgeList.appendChild(li);
  }
}

function buildWorkflowDraft(requestText, selectedCapabilities) {
  return {
    request: requestText,
    retainedDomainKnowledge: selectedCapabilities,
    suggestedSteps: [
      "Analyze request against retained capability catalog",
      "Synthesize scaffolding plan and compose extension commands",
      "Hot-load generated feature into UI shell",
      "Run SelfExtendGenerated tests and UI smoke test"
    ]
  };
}

function explainAshlar(questionText) {
  const q = questionText.toLowerCase();
  if (q.includes("what is ashlar")) {
    return "Ashlar is an orchestration platform that composes domain capabilities, scaffolds features, and validates changes with built-in tests.";
  }
  if (q.includes("how") && q.includes("feature")) {
    return "In this demo, Ashlar maps your request to retained domain knowledge, composes scaffold commands, then hot-loads a new feature card into the UI.";
  }
  return "I can explain Ashlar or scaffold a feature. Try: 'What is Ashlar?' or 'Add feature: daily quest hints'.";
}

async function scaffoldFeatureHotload(featureRequest, source) {
  featureStatus.textContent = "Scaffolding feature through ashlar self-extend...";
  const response = await fetch("/api/scaffold-feature", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ featureRequest })
  });
  if (!response.ok) {
    const message = await response.text();
    throw new Error(`Scaffold request failed: ${message}`);
  }

  const payload = await response.json();
  const moduleUrl = `${payload.moduleUrl}?ts=${Date.now()}`;
  const loaded = await import(moduleUrl);
  if (typeof loaded.mountFeature !== "function") {
    throw new Error("Generated module is missing mountFeature export.");
  }

  const model = {
    featureId: payload.featureId,
    featureRequest: payload.featureRequest,
    summary: payload.summary,
    retainedDomainKnowledge: payload.retainedDomainKnowledge ?? [],
    source
  };
  dynamicFeatures.unshift(model);

  loaded.mountFeature(dynamicFeatureHost, model);

  const draft = buildWorkflowDraft(featureRequest, model.retainedDomainKnowledge);
  outputPane.textContent = JSON.stringify(
    {
      ...draft,
      featureId: model.featureId,
      moduleUrl: payload.moduleUrl
    },
    null,
    2
  );

  featureStatus.textContent = `Feature ${model.featureId} scaffolded and hot-loaded with ${model.retainedDomainKnowledge.length} domain capabilities.`;
  appendChatMessage("assistant", `Feature ready: ${model.featureId}. UI updated live with retained knowledge: ${model.retainedDomainKnowledge.join(", ")}`);
}

chatSendButton.addEventListener("click", () => {
  const text = chatInput.value.trim();
  if (!text) return;
  appendChatMessage("user", text);

  const normalized = text.toLowerCase();
  if (normalized.startsWith("add feature:") || normalized.startsWith("request feature:")) {
    const requestText = text.split(":").slice(1).join(":").trim();
    if (requestText.length > 0) {
      scaffoldFeatureHotload(requestText, "chatbot").catch(error => {
        featureStatus.textContent = `Scaffold failed: ${error.message}`;
        appendChatMessage("assistant", `Feature scaffold failed: ${error.message}`);
      });
    } else {
      appendChatMessage("assistant", "Please include a feature description after ':'");
    }
  } else {
    appendChatMessage("assistant", explainAshlar(text));
  }
});

scaffoldFeatureButton.addEventListener("click", () => {
  const requestText = featureInput.value.trim();
  if (!requestText) return;
  scaffoldFeatureHotload(requestText, "feature-studio").catch(error => {
    featureStatus.textContent = `Scaffold failed: ${error.message}`;
    appendChatMessage("assistant", `Feature scaffold failed: ${error.message}`);
  });
});

loadDomainKnowledge().catch(error => {
  statusLine.textContent = `Domain knowledge load failed: ${error.message}`;
  outputPane.textContent = "UI entered degraded mode; no domain capability catalog available.";
  appendChatMessage("assistant", "Domain knowledge failed to load. Feature scaffolding is unavailable.");
});
""";

    private static string BuildUiDomainKnowledgeJsonSource() => """
{
  "capabilities": [
    {
      "id": "quest-tracking",
      "matchToken": "quest",
      "summary": "Tracks player quest state and completion milestones."
    },
    {
      "id": "inventory-events",
      "matchToken": "inventory",
      "summary": "Handles item grants, removals, and inventory notifications."
    },
    {
      "id": "ability-cooldowns",
      "matchToken": "ability",
      "summary": "Applies shared cooldown semantics for gameplay actions."
    },
    {
      "id": "onboarding-flows",
      "matchToken": "onboarding",
      "summary": "Designs first-time user flows and progressive feature unlocks."
    },
    {
      "id": "ui-notifications",
      "matchToken": "notification",
      "summary": "Renders actionable notifications and in-session prompts."
    }
  ]
}
""";

    private static string BuildUiDemoHostProjectSource() => """
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="Program.cs" />
  </ItemGroup>
</Project>
""";

    private static string BuildUiDemoHostProgramSource() => """
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(Environment.GetEnvironmentVariable("UI_DEMO_URLS") ?? "http://127.0.0.1:4173");
var app = builder.Build();

var appRoot = ResolveAppRoot();
var repoRoot = ResolveRepoRoot(appRoot);
var fileProvider = new PhysicalFileProvider(appRoot);

app.UseDefaultFiles(new DefaultFilesOptions
{
    FileProvider = fileProvider
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = fileProvider
});

app.MapPost("/api/scaffold-feature", async (ScaffoldFeatureRequest request, CancellationToken ct) =>
{
    var featureRequest = request.FeatureRequest?.Trim();
    if (string.IsNullOrWhiteSpace(featureRequest))
        return Results.BadRequest(new { ok = false, error = "featureRequest is required" });

    var run = await RunSelfExtendAsync(repoRoot, featureRequest, ct).ConfigureAwait(false);
    if (run.ExitCode != 0)
    {
        return Results.Json(new
        {
            ok = false,
            error = "self-extend failed",
            stdout = TrimTail(run.StdOut, 2000),
            stderr = TrimTail(run.StdErr, 2000)
        }, statusCode: 500);
    }

    var featureId = Slugify(featureRequest);
    var moduleFile = Path.Combine(appRoot, "generated", $"{featureId}.js");
    if (!File.Exists(moduleFile))
        return Results.Json(new { ok = false, error = $"generated module not found: {moduleFile}" }, statusCode: 500);

    var retained = MapCapabilities(appRoot, featureRequest);
    return Results.Ok(new
    {
        ok = true,
        featureId,
        featureRequest,
        moduleUrl = $"/generated/{featureId}.js",
        retainedDomainKnowledge = retained,
        summary = "Generated by Ashlar self-scaffold pipeline and hot-loaded into the active UI shell."
    });
});

app.Run();

static string ResolveAppRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir != null)
    {
        var candidate = Path.Combine(dir.FullName, "app", "index.html");
        if (File.Exists(candidate))
            return Path.Combine(dir.FullName, "app");
        dir = dir.Parent;
    }
    throw new InvalidOperationException("Unable to resolve app root.");
}

static string ResolveRepoRoot(string appRoot)
{
    var dir = new DirectoryInfo(appRoot);
    while (dir != null)
    {
        var hasSrc = Directory.Exists(Path.Combine(dir.FullName, "src"));
        var hasDocs = Directory.Exists(Path.Combine(dir.FullName, "docs"));
        if (hasSrc && hasDocs)
            return dir.FullName;
        dir = dir.Parent;
    }
    throw new InvalidOperationException("Unable to resolve repository root.");
}

static async Task<(int ExitCode, string StdOut, string StdErr)> RunSelfExtendAsync(string repoRoot, string featureRequest, CancellationToken ct)
{
    var goal = $"UI_FEATURE_HOTLOAD Feature request: {featureRequest}. Write output module under docs/UiDomainDemoGenerated/app/generated.";
    var psi = new ProcessStartInfo
    {
        FileName = "dotnet",
        WorkingDirectory = repoRoot,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };
    psi.ArgumentList.Add("run");
    psi.ArgumentList.Add("--project");
    psi.ArgumentList.Add("application/src/Ashlar.CLI");
    psi.ArgumentList.Add("--");
    psi.ArgumentList.Add("self-extend");
    psi.ArgumentList.Add("run");
    psi.ArgumentList.Add("--goal");
    psi.ArgumentList.Add(goal);
    psi.ArgumentList.Add("--repo-root");
    psi.ArgumentList.Add(repoRoot);
    psi.ArgumentList.Add("--provider");
    psi.ArgumentList.Add("mock-json");
    psi.ArgumentList.Add("--allow-mock");
    psi.ArgumentList.Add("--json");

    using var process = Process.Start(psi);
    if (process == null)
        return (1, string.Empty, "Failed to start dotnet process.");
    var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
    var stderrTask = process.StandardError.ReadToEndAsync(ct);
    await process.WaitForExitAsync(ct).ConfigureAwait(false);
    return (process.ExitCode, await stdoutTask.ConfigureAwait(false), await stderrTask.ConfigureAwait(false));
}

static string[] MapCapabilities(string appRoot, string featureRequest)
{
    var catalogPath = Path.Combine(appRoot, "domain-knowledge.json");
    var text = File.ReadAllText(catalogPath);
    using var doc = JsonDocument.Parse(text);
    var caps = doc.RootElement.GetProperty("capabilities");
    var request = featureRequest.ToLowerInvariant();
    var matched = new List<string>();
    foreach (var cap in caps.EnumerateArray())
    {
        var token = cap.TryGetProperty("matchToken", out var t) ? (t.GetString() ?? "") : "";
        var id = cap.TryGetProperty("id", out var i) ? (i.GetString() ?? "") : "";
        if (!string.IsNullOrWhiteSpace(token) && !string.IsNullOrWhiteSpace(id) && request.Contains(token, StringComparison.Ordinal))
            matched.Add(id);
    }
    if (matched.Count == 0)
        matched.AddRange(new[] { "quest-tracking", "onboarding-flows" });
    return matched.Distinct(StringComparer.Ordinal).ToArray();
}

static string Slugify(string value)
{
    if (string.IsNullOrWhiteSpace(value))
        return "feature_generated";
    var lower = value.ToLowerInvariant();
    lower = System.Text.RegularExpressions.Regex.Replace(lower, @"[^a-z0-9]+", "_");
    lower = lower.Trim('_');
    if (string.IsNullOrWhiteSpace(lower))
        lower = "feature_generated";
    if (char.IsDigit(lower[0]))
        lower = $"f_{lower}";
    return lower;
}

static string TrimTail(string text, int maxChars)
{
    if (string.IsNullOrEmpty(text) || text.Length <= maxChars)
        return text;
    return text[^maxChars..];
}

public sealed record ScaffoldFeatureRequest(string FeatureRequest);
""";

    private static string BuildUiDemoSmokeProjectSource() => """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="SmokeProgram.cs" />
  </ItemGroup>
</Project>
""";

    private static string BuildUiDemoSmokeProgramSource() => """
using System.Diagnostics;

var uiRoot = ResolveUiDemoRoot();
var appRoot = Path.Combine(uiRoot, "app");
var hostRoot = Path.Combine(uiRoot, "host");

var html = File.ReadAllText(Path.Combine(appRoot, "index.html"));
var js = File.ReadAllText(Path.Combine(appRoot, "app.js"));
var domainJson = File.ReadAllText(Path.Combine(appRoot, "domain-knowledge.json"));
var hostProgram = File.ReadAllText(Path.Combine(hostRoot, "Program.cs"));

Assert(html.Contains("chat-send-btn", StringComparison.Ordinal), "Expected chat send button in HTML");
Assert(html.Contains("scaffold-feature-btn", StringComparison.Ordinal), "Expected feature scaffold button in HTML");
Assert(js.Contains("/api/scaffold-feature", StringComparison.Ordinal), "Expected scaffold API call in JS");
Assert(js.Contains("import(moduleUrl)", StringComparison.Ordinal), "Expected dynamic module import in JS");
Assert(hostProgram.Contains("MapPost(\"/api/scaffold-feature\"", StringComparison.Ordinal), "Expected scaffold endpoint in .NET host");
Assert(hostProgram.Contains("UseStaticFiles", StringComparison.Ordinal), "Expected static file hosting in .NET host");
Assert(domainJson.Contains("\"capabilities\"", StringComparison.Ordinal), "Expected domain capability catalog");

Console.WriteLine("ui_smoke_test: ok");

static string ResolveUiDemoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir != null)
    {
        var appDir = Path.Combine(dir.FullName, "app");
        var hostDir = Path.Combine(dir.FullName, "host");
        if (Directory.Exists(appDir) && Directory.Exists(hostDir))
            return dir.FullName;
        dir = dir.Parent;
    }
    throw new InvalidOperationException("Unable to resolve UiDomainDemoGenerated root.");
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        Console.Error.WriteLine(message);
        Environment.ExitCode = 1;
        throw new InvalidOperationException(message);
    }
}
""";

    private static string BuildAvaloniaAbstractionsProjectSource() => """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
""";

    private static string BuildAvaloniaUiContractsSource() => """
namespace Ashlar.Ui.Abstractions;

public sealed record UiNode(
    string Kind,
    string Id,
    string Text,
    string? Command = null,
    string? AccentHex = null,
    double? Value = null,
    string? Layout = null,
    IReadOnlyList<UiNode>? Children = null);

public sealed record FeatureDescriptor(
    string FeatureId,
    string Title,
    string[] RetainedDomainKnowledge,
    string WowMessage,
    string SourcePath,
    string ExperienceMode,
    UiNode Root);

public interface IUiFrameworkAdapter<TControl>
{
    TControl Create(UiNode node, Action<string>? commandHandler = null);
}

public interface IFeatureScaffolder
{
    Task<FeatureDescriptor> ScaffoldAsync(string featureRequest, CancellationToken cancellationToken = default);
}

public static class CrossFrameworkCompatibility
{
    public const string ContractNotes =
        "Adapters implement IUiFrameworkAdapter<TControl> per framework (Avalonia, WPF, etc.) while sharing UiNode contracts.";
}
""";

    private static string BuildAvaloniaHostProjectSource() => """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Avalonia" />
    <PackageReference Include="Avalonia.Desktop" />
    <PackageReference Include="Avalonia.Themes.Fluent" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Ashlar.Ui.Abstractions\Ashlar.Ui.Abstractions.csproj" />
  </ItemGroup>
</Project>
""";

    private static string BuildAvaloniaHostProgramSource() => """
using System.Diagnostics;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Themes.Fluent;
using Avalonia.X11;
using Ashlar.Ui.Abstractions;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Any(a => string.Equals(a, "--smoke", StringComparison.OrdinalIgnoreCase)))
            return RunSmoke();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return 0;
    }

    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new X11PlatformOptions
            {
                RenderingMode = new[] { X11RenderingMode.Software }
            })
            .LogToTrace();

    private static int RunSmoke()
    {
        try
        {
            var repoRoot = ResolveRepoRoot();
            var generatedRoot = ResolveGeneratedRoot(repoRoot);
            Directory.CreateDirectory(generatedRoot);
            var scaffolder = new AvaloniaFeatureScaffolder(repoRoot, generatedRoot);
            var descriptor = scaffolder.ScaffoldAsync("Add onboarding notification sidebar", CancellationToken.None)
                .GetAwaiter().GetResult();
            if (string.IsNullOrWhiteSpace(descriptor.FeatureId))
                throw new InvalidOperationException("Descriptor feature id missing.");
            if (descriptor.RetainedDomainKnowledge.Length == 0)
                throw new InvalidOperationException("Descriptor retained knowledge missing.");
            Console.WriteLine("avalonia_smoke: ok");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"avalonia_smoke: failed: {ex.Message}");
            return 1;
        }
    }

    private static string ResolveRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "src")) &&
                Directory.Exists(Path.Combine(dir.FullName, "docs")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Unable to resolve repository root.");
    }

    private static string ResolveGeneratedRoot(string repoRoot)
        => Path.Combine(repoRoot, "docs", "UiDomainDemoGenerated", "avalonia", "Ashlar.Ui.AvaloniaHost", "GeneratedExtensions");
}

public sealed class App : Application
{
    public App()
    {
        if (!Styles.Any(s => s is FluentTheme))
            Styles.Add(new FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var repoRoot = ResolveRepoRoot();
            var generatedRoot = Path.Combine(repoRoot, "docs", "UiDomainDemoGenerated", "avalonia", "Ashlar.Ui.AvaloniaHost", "GeneratedExtensions");
            Directory.CreateDirectory(generatedRoot);
            var adapter = new AvaloniaUiFrameworkAdapter();
            var scaffolder = new AvaloniaFeatureScaffolder(repoRoot, generatedRoot);
            desktop.MainWindow = new MainWindow(adapter, scaffolder);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static string ResolveRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "src")) &&
                Directory.Exists(Path.Combine(dir.FullName, "docs")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Unable to resolve repository root.");
    }
}

public sealed class MainWindow : Window
{
    private readonly IUiFrameworkAdapter<Control> _adapter;
    private readonly IFeatureScaffolder _scaffolder;
    private readonly StackPanel _dynamicHost = new() { Spacing = 8 };
    private readonly StackPanel _chatLog = new() { Spacing = 4 };
    private readonly TextBox _chatInput = new() { Text = "What is Ashlar?" };
    private readonly TextBox _featureInput = new() { Text = "Add onboarding notification sidebar" };
    private readonly TextBlock _status = new() { Text = "Ready." };
    private readonly TextBox _plan = new() { AcceptsReturn = true, IsReadOnly = true, Height = 180 };
    private readonly TextBox _transformLog = new() { IsReadOnly = true, AcceptsReturn = true, Height = 120, Text = "Awaiting transformed app actions..." };
    private readonly TextBlock _transformPhase = new() { Text = "Phase: Mission shell idle.", Foreground = Brushes.LightGreen };
    private readonly Control _baselineShell;

    public MainWindow(IUiFrameworkAdapter<Control> adapter, IFeatureScaffolder scaffolder)
    {
        _adapter = adapter;
        _scaffolder = scaffolder;
        Title = "Ashlar Avalonia Dynamic Extension Demo";
        Width = 1080;
        Height = 760;
        _baselineShell = BuildContent();
        Content = _baselineShell;
        AppendChat("assistant", "Hi! Ask what Ashlar is or scaffold a feature.");
    }

    private Control BuildContent()
    {
        var sendChat = new Button { Content = "Send" };
        sendChat.Click += (_, _) => AppendChat("assistant", ExplainAshlar(_chatInput.Text ?? string.Empty), includeUserInput: true);

        var scaffold = new Button { Content = "Scaffold + Hot-load" };
        scaffold.Click += async (_, _) => await ScaffoldAsync().ConfigureAwait(false);

        var root = new StackPanel { Spacing = 10, Margin = new Thickness(14) };
        root.Children.Add(new TextBlock
        {
            Text = "Ashlar Avalonia Cross-Framework UI Demo",
            FontSize = 22,
            FontWeight = FontWeight.Bold
        });
        root.Children.Add(_status);

        root.Children.Add(new Border
        {
            BorderBrush = Brushes.DimGray,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = "Chatbot" },
                    _chatLog,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        Children = { _chatInput, sendChat }
                    }
                }
            }
        });

        root.Children.Add(new Border
        {
            BorderBrush = Brushes.DimGray,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = "Feature Scaffolder" },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        Children = { _featureInput, scaffold }
                    }
                }
            }
        });

        root.Children.Add(new TextBlock { Text = "Dynamically Extended UI (Avalonia Adapter)" });
        root.Children.Add(_dynamicHost);
        root.Children.Add(new TextBlock { Text = "Scaffold Plan" });
        root.Children.Add(_plan);

        return new ScrollViewer { Content = root };
    }

    private async Task ScaffoldAsync()
    {
        var request = (_featureInput.Text ?? string.Empty).Trim();
        if (request.Length == 0)
            return;

        try
        {
            _status.Text = "Scaffolding feature through ashlar self-extend...";
            var descriptor = await _scaffolder.ScaffoldAsync(request).ConfigureAwait(true);
            if (string.Equals(descriptor.ExperienceMode, "transform", StringComparison.OrdinalIgnoreCase))
            {
                ApplyAppTransformation(descriptor);
                _status.Text = $"Application transformed via {descriptor.FeatureId}.";
                AppendChat("assistant", $"Full app transformed into {descriptor.Title}. Use Return to Ashlar shell to restore.");
            }
            else
            {
                var control = _adapter.Create(descriptor.Root, HandleCommand);
                _dynamicHost.Children.Insert(0, control);
                _status.Text = $"Feature {descriptor.FeatureId} loaded with {descriptor.RetainedDomainKnowledge.Length} domain tags.";
                AppendChat("assistant", $"Feature ready: {descriptor.FeatureId}. Knowledge: {string.Join(", ", descriptor.RetainedDomainKnowledge)}");
            }
            _plan.Text = JsonSerializer.Serialize(new
            {
                request,
                featureId = descriptor.FeatureId,
                retainedDomainKnowledge = descriptor.RetainedDomainKnowledge,
                descriptor.SourcePath,
                wow = descriptor.WowMessage,
                mode = descriptor.ExperienceMode
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _status.Text = $"Scaffold failed: {ex.Message}";
            AppendChat("assistant", $"Feature scaffold failed: {ex.Message}");
        }
    }

    private void AppendChat(string role, string message, bool includeUserInput = false)
    {
        if (includeUserInput)
            _chatLog.Children.Add(new TextBlock { Text = $"user: {_chatInput.Text}" });
        _chatLog.Children.Add(new TextBlock { Text = $"{role}: {message}" });
    }

    private void ApplyAppTransformation(FeatureDescriptor descriptor)
    {
        Title = $"Galactic Operations Console :: {descriptor.Title}";
        _transformPhase.Text = "Phase: Mission shell booted from scaffold.";
        _transformLog.Text = $"Transformed with descriptor: {descriptor.FeatureId}{Environment.NewLine}{descriptor.WowMessage}";

        var generatedExperience = _adapter.Create(descriptor.Root, HandleCommand);
        var wow = new Button
        {
            Content = "Trigger wow sequence",
            Background = Brushes.Cyan,
            Foreground = Brushes.Black,
            FontWeight = FontWeight.Bold,
            Padding = new Thickness(12, 8)
        };
        wow.Click += (_, _) => HandleCommand($"feature:{descriptor.FeatureId}:wow");
        var restore = new Button
        {
            Content = "Restore Ashlar Shell",
            Background = Brushes.Orange,
            Foreground = Brushes.Black,
            FontWeight = FontWeight.Bold,
            Padding = new Thickness(12, 8)
        };
        restore.Click += (_, _) => RestoreAshlarShell();
        var deploy = new Button
        {
            Content = "Deploy autonomous patch",
            Background = Brushes.LawnGreen,
            Foreground = Brushes.Black,
            FontWeight = FontWeight.Bold,
            Padding = new Thickness(12, 8)
        };
        deploy.Click += (_, _) => HandleCommand($"feature:{descriptor.FeatureId}:deploy");

        var appGrid = new Grid();
        appGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        appGrid.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
        appGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(240)));
        appGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));

        var headerActions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { wow, deploy, restore }
        };
        DockPanel.SetDock(headerActions, Dock.Right);

        var header = new Border
        {
            BorderBrush = Brushes.SlateBlue,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(14, 12),
            Child = new DockPanel
            {
                LastChildFill = false,
                Children =
                {
                    new StackPanel
                    {
                        Spacing = 4,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = "Galactic Operations Console",
                                FontSize = 24,
                                FontWeight = FontWeight.Bold,
                                Foreground = Brushes.White
                            },
                            new TextBlock
                            {
                                Text = descriptor.Title,
                                Foreground = Brushes.LightGray
                            },
                            new TextBlock
                            {
                                Text = descriptor.WowMessage,
                                Foreground = Brushes.DeepSkyBlue
                            },
                            _transformPhase
                        }
                    },
                    headerActions
                }
            }
        };
        Grid.SetRow(header, 0);
        Grid.SetColumn(header, 0);
        Grid.SetColumnSpan(header, 2);
        appGrid.Children.Add(header);

        var navRail = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#141d32")),
            BorderBrush = Brushes.SlateBlue,
            BorderThickness = new Thickness(0, 0, 1, 0),
            Padding = new Thickness(10),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = "Mission Nav", FontSize = 18, FontWeight = FontWeight.Bold, Foreground = Brushes.White },
                    CreateTransformNavButton("Overview", "nav:overview"),
                    CreateTransformNavButton("Fleet Grid", "nav:fleet"),
                    CreateTransformNavButton("Signal Radar", "nav:signals"),
                    CreateTransformNavButton("Workflow Graph", "nav:workflow"),
                    CreateTransformNavButton("Threat Intel", "nav:intel")
                }
            }
        };
        Grid.SetRow(navRail, 1);
        Grid.SetColumn(navRail, 0);
        appGrid.Children.Add(navRail);

        var workspace = new Grid { Margin = new Thickness(12) };
        workspace.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        workspace.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
        workspace.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        var metrics = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Children =
            {
                CreateMetricCard("Active fleets", "12", Brushes.DeepSkyBlue),
                CreateMetricCard("Mission sync", "99.2%", Brushes.LawnGreen),
                CreateMetricCard("Latency window", "24ms", Brushes.Gold),
                CreateMetricCard("Risk score", "LOW", Brushes.HotPink)
            }
        };
        Grid.SetRow(metrics, 0);
        workspace.Children.Add(metrics);

        var missionBoard = new Grid { Margin = new Thickness(0, 10, 0, 10) };
        missionBoard.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(3, GridUnitType.Star)));
        missionBoard.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(2, GridUnitType.Star)));

        var generatedPanel = new Border
        {
            BorderBrush = Brushes.SlateBlue,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Scaffolded Application Module",
                        FontSize = 18,
                        FontWeight = FontWeight.Bold,
                        Foreground = Brushes.White
                    },
                    generatedExperience
                }
            }
        };
        Grid.SetColumn(generatedPanel, 0);
        missionBoard.Children.Add(generatedPanel);

        var incidentFeed = new Border
        {
            BorderBrush = Brushes.SlateBlue,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Margin = new Thickness(10, 0, 0, 0),
            Padding = new Thickness(10),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Live Incident Queue",
                        FontSize = 18,
                        FontWeight = FontWeight.Bold,
                        Foreground = Brushes.White
                    },
                    new ListBox
                    {
                        Height = 210,
                        ItemsSource = new[]
                        {
                            "Signal anomaly detected in sector Delta-9",
                            "Autonomous drone mesh rerouted after weather spike",
                            "Cold-start recovery completed by generated fallback lane",
                            "New domain capability badge applied to mission planner"
                        }
                    }
                }
            }
        };
        Grid.SetColumn(incidentFeed, 1);
        missionBoard.Children.Add(incidentFeed);

        Grid.SetRow(missionBoard, 1);
        workspace.Children.Add(missionBoard);

        var actionLogPanel = new Border
        {
            BorderBrush = Brushes.SlateBlue,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10),
            Child = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    new TextBlock { Text = "Mission command log", FontSize = 16, FontWeight = FontWeight.Bold, Foreground = Brushes.White },
                    _transformLog
                }
            }
        };
        Grid.SetRow(actionLogPanel, 2);
        workspace.Children.Add(actionLogPanel);

        Grid.SetRow(workspace, 1);
        Grid.SetColumn(workspace, 1);
        appGrid.Children.Add(workspace);

        Content = new Border
        {
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(Color.Parse("#0c1124"), 0),
                    new GradientStop(Color.Parse("#111b38"), 0.5),
                    new GradientStop(Color.Parse("#1b0d2a"), 1)
                }
            },
            Child = appGrid
        };
    }

    private Button CreateTransformNavButton(string label, string command)
    {
        var button = new Button
        {
            Content = label,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(10, 8),
            Background = new SolidColorBrush(Color.Parse("#1d2c52")),
            Foreground = Brushes.White
        };
        button.Click += (_, _) => HandleCommand(command);
        return button;
    }

    private static Border CreateMetricCard(string title, string value, IBrush accent)
    {
        return new Border
        {
            Width = 190,
            BorderBrush = accent,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10),
            Child = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = title, Foreground = Brushes.LightGray },
                    new TextBlock { Text = value, FontSize = 22, FontWeight = FontWeight.Bold, Foreground = accent }
                }
            }
        };
    }

    private void RestoreAshlarShell()
    {
        Title = "Ashlar Avalonia Dynamic Extension Demo";
        Content = _baselineShell;
        _status.Text = "Restored baseline shell.";
        _transformPhase.Text = "Phase: Mission shell idle.";
        AppendChat("assistant", "Baseline Ashlar shell restored.");
    }

    private void HandleCommand(string command)
    {
        if (string.Equals(command, "shell:restore", StringComparison.OrdinalIgnoreCase))
        {
            RestoreAshlarShell();
            return;
        }

        if (command.StartsWith("nav:", StringComparison.OrdinalIgnoreCase))
            _transformPhase.Text = $"Phase: Viewing {command["nav:".Length..]} station.";
        else if (command.Contains(":wow", StringComparison.OrdinalIgnoreCase))
            _transformPhase.Text = "Phase: WOW sequence executed across every station.";
        else if (command.Contains(":deploy", StringComparison.OrdinalIgnoreCase))
            _transformPhase.Text = "Phase: Autonomous patch deployment initiated.";
        else if (command.Contains(":launch", StringComparison.OrdinalIgnoreCase))
            _transformPhase.Text = "Phase: Simulation launch accepted.";
        else if (command.Contains(":boost", StringComparison.OrdinalIgnoreCase))
            _transformPhase.Text = "Phase: Autonomy boost pipeline stabilized.";

        var line = $"{DateTimeOffset.Now:HH:mm:ss} :: {command}";
        _transformLog.Text = string.IsNullOrWhiteSpace(_transformLog.Text)
            ? line
            : $"{_transformLog.Text}{Environment.NewLine}{line}";
        AppendChat("assistant", $"Action command: {command}");
    }

    private static string ExplainAshlar(string question)
    {
        var q = (question ?? string.Empty).ToLowerInvariant();
        if (q.Contains("what is ashlar", StringComparison.Ordinal))
            return "Ashlar is an orchestration system that scaffolds features and validates them with tests.";
        if (q.Contains("framework", StringComparison.Ordinal))
            return "UI abstractions keep feature descriptors framework-neutral so adapters can target Avalonia or other hosts.";
        return "Ask about Ashlar, or scaffold a feature to see live extension loading.";
    }
}

public sealed class AvaloniaUiFrameworkAdapter : IUiFrameworkAdapter<Control>
{
    public Control Create(UiNode node, Action<string>? commandHandler = null)
    {
        return node.Kind.ToLowerInvariant() switch
        {
            "panel" => CreatePanel(node, commandHandler),
            "text" => new TextBlock { Text = node.Text },
            "button" => CreateButton(node, commandHandler),
            "badge" => CreateBadge(node),
            "progress" => CreateProgress(node),
            _ => new TextBlock { Text = $"Unsupported node kind: {node.Kind}" }
        };
    }

    private Control CreatePanel(UiNode node, Action<string>? commandHandler)
    {
        var panel = new StackPanel { Spacing = 6 };
        panel.Children.Add(new TextBlock
        {
            Text = node.Text,
            FontWeight = FontWeight.SemiBold
        });
        if (node.Children != null)
        {
            var childContainer = new StackPanel
            {
                Spacing = 6,
                Orientation = string.Equals(node.Layout, "horizontal", StringComparison.OrdinalIgnoreCase)
                    ? Orientation.Horizontal
                    : Orientation.Vertical
            };
            foreach (var child in node.Children)
                childContainer.Children.Add(Create(child, commandHandler));
            panel.Children.Add(childContainer);
        }
        return new Border
        {
            BorderBrush = Brushes.SlateGray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8),
            Child = panel
        };
    }

    private Control CreateButton(UiNode node, Action<string>? commandHandler)
    {
        var button = new Button { Content = node.Text };
        if (!string.IsNullOrWhiteSpace(node.Command))
            button.Click += (_, _) => commandHandler?.Invoke(node.Command);
        return button;
    }

    private Control CreateBadge(UiNode node)
    {
        return new Border
        {
            CornerRadius = new CornerRadius(12),
            BorderBrush = Brushes.SlateBlue,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8, 2),
            Child = new TextBlock { Text = node.Text, FontSize = 12 }
        };
    }

    private Control CreateProgress(UiNode node)
    {
        var wrap = new StackPanel { Spacing = 2, Width = 200 };
        wrap.Children.Add(new TextBlock { Text = $"{node.Text}: {node.Value ?? 0:0}%" });
        wrap.Children.Add(new ProgressBar { Minimum = 0, Maximum = 100, Value = node.Value ?? 0, Height = 12 });
        return wrap;
    }
}

public sealed class AvaloniaFeatureScaffolder : IFeatureScaffolder
{
    private readonly string _repoRoot;
    private readonly string _generatedRoot;

    public AvaloniaFeatureScaffolder(string repoRoot, string generatedRoot)
    {
        _repoRoot = repoRoot;
        _generatedRoot = generatedRoot;
    }

    public async Task<FeatureDescriptor> ScaffoldAsync(string featureRequest, CancellationToken cancellationToken = default)
    {
        var objectiveToken = LooksLikeTransformRequest(featureRequest)
            ? "AVALONIA_APP_TRANSFORM"
            : "AVALONIA_FEATURE_HOTLOAD";
        var goal =
            $"{objectiveToken} Feature request: {featureRequest}. Write output descriptor under docs/UiDomainDemoGenerated/avalonia/Ashlar.Ui.AvaloniaHost/GeneratedExtensions.";
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = _repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add("--project");
        psi.ArgumentList.Add("application/src/Ashlar.CLI");
        psi.ArgumentList.Add("--");
        psi.ArgumentList.Add("self-extend");
        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add("--goal");
        psi.ArgumentList.Add(goal);
        psi.ArgumentList.Add("--repo-root");
        psi.ArgumentList.Add(_repoRoot);
        psi.ArgumentList.Add("--provider");
        psi.ArgumentList.Add("mock-json");
        psi.ArgumentList.Add("--allow-mock");
        psi.ArgumentList.Add("--json");

        using var process = Process.Start(psi);
        if (process == null)
            throw new InvalidOperationException("Failed to start dotnet process.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"self-extend failed: {stderr}\n{stdout}");

        var featureId = Slugify(featureRequest);
        var descriptorPath = Path.Combine(_generatedRoot, $"{featureId}.json");
        if (!File.Exists(descriptorPath))
            throw new InvalidOperationException($"Generated descriptor not found: {descriptorPath}");

        var descriptorText = await File.ReadAllTextAsync(descriptorPath, cancellationToken).ConfigureAwait(false);
        var descriptor = JsonSerializer.Deserialize<FeatureDescriptor>(descriptorText, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Descriptor deserialize failed.");
        return descriptor with { SourcePath = descriptorPath };
    }

    private static bool LooksLikeTransformRequest(string request)
    {
        if (string.IsNullOrWhiteSpace(request))
            return false;

        var lower = request.ToLowerInvariant();
        return lower.Contains("transform", StringComparison.Ordinal) ||
               lower.Contains("completely", StringComparison.Ordinal) ||
               lower.Contains("another application", StringComparison.Ordinal) ||
               lower.Contains("morph", StringComparison.Ordinal) ||
               lower.Contains("replace app", StringComparison.Ordinal);
    }

    private static string Slugify(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "feature_generated";
        var lower = value.ToLowerInvariant();
        lower = System.Text.RegularExpressions.Regex.Replace(lower, @"[^a-z0-9]+", "_");
        lower = lower.Trim('_');
        if (string.IsNullOrWhiteSpace(lower))
            lower = "feature_generated";
        if (char.IsDigit(lower[0]))
            lower = $"f_{lower}";
        return lower;
    }
}
""";

    private static string BuildAvaloniaHostReadmeSource() => """
# Ashlar Avalonia Dynamic Extension Demo

Linux-compatible desktop demo using Avalonia.

## Run UI
`dotnet run --project docs/UiDomainDemoGenerated/avalonia/Ashlar.Ui.AvaloniaHost/Ashlar.Ui.AvaloniaHost.csproj`

## Run smoke check (no GUI)
`dotnet run --project docs/UiDomainDemoGenerated/avalonia/Ashlar.Ui.AvaloniaHost/Ashlar.Ui.AvaloniaHost.csproj -- --smoke`

Feature requests are scaffolded through:
`ashlar self-extend run --goal "AVALONIA_FEATURE_HOTLOAD ..."`
or full application transformation:
`ashlar self-extend run --goal "AVALONIA_APP_TRANSFORM ..."`
""";

    private static string BuildAvaloniaFeatureDescriptorJson(string featureRequest, string featureId, string[] retainedCapabilities)
    {
        var descriptor = new
        {
            FeatureId = featureId,
            Title = featureRequest,
            RetainedDomainKnowledge = retainedCapabilities,
            WowMessage = "Live neon pulse command rail loaded from scaffolded descriptor.",
            SourcePath = $"GeneratedExtensions/{featureId}.json",
            ExperienceMode = "augment",
            Root = new
            {
                Kind = "panel",
                Id = $"panel-{featureId}",
                Text = featureRequest,
                Children = new object[]
                {
                    new { Kind = "text", Id = $"text-{featureId}", Text = "Dynamic extension loaded inside Avalonia framework." },
                    new { Kind = "progress", Id = $"progress-{featureId}", Text = "Readiness", Value = 88.0 },
                    new { Kind = "button", Id = $"button-{featureId}", Text = "Trigger wow action", Command = $"feature:{featureId}:wow" },
                    new { Kind = "badge", Id = $"badge-{featureId}", Text = string.Join(" | ", retainedCapabilities) }
                }
            }
        };

        return JsonSerializer.Serialize(descriptor, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string BuildAvaloniaAppTransformDescriptorJson(string featureRequest, string featureId, string[] retainedCapabilities)
    {
        var descriptor = new
        {
            FeatureId = featureId,
            Title = $"{featureRequest} Command Suite",
            RetainedDomainKnowledge = retainedCapabilities,
            WowMessage = "Entire shell morphed into a scaffolded mission operations app.",
            SourcePath = $"GeneratedExtensions/{featureId}.json",
            ExperienceMode = "transform",
            Root = new
            {
                Kind = "panel",
                Id = $"transform-root-{featureId}",
                Text = "Mission operations shell",
                Children = new object[]
                {
                    new { Kind = "text", Id = $"hero-{featureId}", Text = "Adaptive mission control generated live by Ashlar scaffolding." },
                    new
                    {
                        Kind = "panel",
                        Id = $"readiness-grid-{featureId}",
                        Text = "Operational readiness",
                        Layout = "horizontal",
                        Children = new object[]
                        {
                            new { Kind = "progress", Id = $"radar-{featureId}", Text = "Radar", Value = 94.0 },
                            new { Kind = "progress", Id = $"drone-{featureId}", Text = "Drone mesh", Value = 87.0 },
                            new { Kind = "progress", Id = $"safety-{featureId}", Text = "Safety rails", Value = 98.0 }
                        }
                    },
                    new
                    {
                        Kind = "panel",
                        Id = $"command-rail-{featureId}",
                        Text = "Command rail",
                        Layout = "horizontal",
                        Children = new object[]
                        {
                            new { Kind = "button", Id = $"cmd-launch-{featureId}", Text = "Launch simulation", Command = $"feature:{featureId}:launch" },
                            new { Kind = "button", Id = $"cmd-boost-{featureId}", Text = "Boost autonomy", Command = $"feature:{featureId}:boost" },
                            new { Kind = "button", Id = $"cmd-wow-{featureId}", Text = "Trigger wow sequence", Command = $"feature:{featureId}:wow" },
                            new { Kind = "button", Id = $"cmd-restore-{featureId}", Text = "Return to Ashlar shell", Command = "shell:restore" }
                        }
                    },
                    new
                    {
                        Kind = "panel",
                        Id = $"knowledge-{featureId}",
                        Text = "Retained domain intelligence",
                        Layout = "horizontal",
                        Children = retainedCapabilities.Select(cap => new { Kind = "badge", Id = $"cap-{featureId}-{SlugifyIdentifier(cap)}", Text = cap }).ToArray()
                    },
                    new
                    {
                        Kind = "panel",
                        Id = $"live-feed-{featureId}",
                        Text = "Live event feed",
                        Children = new object[]
                        {
                            new { Kind = "text", Id = $"event1-{featureId}", Text = "Signal lock achieved in generated subsystem lane." },
                            new { Kind = "text", Id = $"event2-{featureId}", Text = "Autonomous planner rebound from synthetic failure injection." },
                            new { Kind = "text", Id = $"event3-{featureId}", Text = "New UI capability surfaced from descriptor contract." }
                        }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(descriptor, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string BuildUiFeatureModuleSource(string featureRequest, string featureId, string[] retainedCapabilities)
    {
        var escapedRequest = JsonSerializer.Serialize(featureRequest);
        var escapedId = JsonSerializer.Serialize(featureId);
        var capabilityArrayLiteral = $"[{string.Join(", ", retainedCapabilities.Select(c => JsonSerializer.Serialize(c)))}]";
        return $$"""
export function mountFeature(host, context) {
  const card = document.createElement("article");
  card.className = "feature-card";

  const title = document.createElement("h3");
  title.textContent = context?.featureRequest ?? {{escapedRequest}};
  card.appendChild(title);

  const body = document.createElement("p");
  body.textContent = "Generated by Ashlar self-scaffold pipeline and hot-loaded into the active UI shell.";
  card.appendChild(body);

  const chips = document.createElement("div");
  chips.className = "feature-chip-row";
  const retained = (context?.retainedDomainKnowledge ?? {{capabilityArrayLiteral}});
  for (const cap of retained) {
    const chip = document.createElement("span");
    chip.className = "feature-chip";
    chip.textContent = cap;
    chips.appendChild(chip);
  }
  card.appendChild(chips);

  card.dataset.featureId = context?.featureId ?? {{escapedId}};
  host.prepend(card);
}
""";
    }

    private static string BuildUiDomainKnowledgeRetentionTestSource()
    {
        const string resourceName = "Ashlar.Infrastructure.Execution.Templates.UiDomainKnowledgeRetentionTests.template.cs";
        using var stream = typeof(ProviderFactory).Assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            throw new InvalidOperationException($"Embedded resource not found: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string BuildErrorStateSource() => """
namespace Ashlar.Unity.Generated;

public sealed record GeneratedSystemErrorState(
    bool HasCompileError,
    string Message,
    string? LastKnownGoodSystemId)
{
    public static GeneratedSystemErrorState None { get; } = new(false, string.Empty, null);
}
""";

    private static string BuildInspectorSnapshotSource() => """
namespace Ashlar.Unity.Generated;

public sealed record GeneratedSystemInspectorSnapshot(
    string SystemId,
    string RawGeneratedCode,
    GeneratedSystemErrorState ErrorState,
    System.DateTimeOffset GeneratedAtUtc);
""";

    private static string BuildComposableCommandContractSource() => """
namespace Ashlar.CLI.Commands.SelfExtendGenerated;

public interface IComposableExtensionCommand
{
    string ExtensionId { get; }
    IReadOnlyList<string> Dependencies { get; }
}
""";

    private static string BuildExtensionCommandSource(string className, string commandName, string extensionId, string[] dependencies)
    {
        var dependencyArrayExpr = dependencies.Length == 0
            ? "Array.Empty<string>()"
            : $"new[] {{ {string.Join(", ", dependencies.Select(d => $"\"{d}\""))} }}";
        return $$"""
using System.CommandLine;
using System.Text.Json;

namespace Ashlar.CLI.Commands.SelfExtendGenerated;

public sealed class {{className}} : Command, IComposableExtensionCommand
{
    public {{className}}() : base("{{commandName}}", "Self-extend generated extension command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new
            {
                ok = true,
                command = Name,
                extensionId = ExtensionId,
                dependencies = Dependencies
            }));
            Environment.ExitCode = 0;
        });
    }

    public string ExtensionId => "{{extensionId}}";
    public IReadOnlyList<string> Dependencies { get; } = {{dependencyArrayExpr}};
}
""";
    }

    private static string BuildBundleCommandSource((string ClassName, string CommandName)[] extensionCommands)
        => BuildBundleCommandSource("SelfExtendBundleCommand", "self-extend-bundle", extensionCommands);

    private static string BuildBundleCommandSource(
        string bundleClassName,
        string bundleCommandName,
        (string ClassName, string CommandName)[] extensionCommands)
    {
        var addLines = string.Join("\n", extensionCommands.Select(c => $"        AddCommand(new {c.ClassName}());"));
        return $$"""
using System.CommandLine;

namespace Ashlar.CLI.Commands.SelfExtendGenerated;

public sealed class {{bundleClassName}} : Command
{
    public {{bundleClassName}}() : base("{{bundleCommandName}}", "Composed bundle of generated extension commands")
    {
{{addLines}}
    }
}
""";
    }

    private static string BuildExtensionCommandStructureTestSource(
        string testClassName,
        string commandClassName,
        string expectedCommandName,
        string expectedExtensionId,
        string[] expectedDependencies)
    {
        var dependencyCount = expectedDependencies.Length;
        var dependencyAssertions = expectedDependencies.Length == 0
            ? "            AssertEqual(0, composable.Dependencies.Count, \"Dependencies should be empty for this extension\");"
            : string.Join("\n", expectedDependencies.Select(d =>
                $"            AssertTrue(composable.Dependencies.Contains(\"{d}\", StringComparer.Ordinal), \"Expected dependency '{d}'\");"));

        return $$"""
using Ashlar.CLI.Commands.SelfExtendGenerated;
using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Application.Testing.Models;

namespace Ashlar.Tests.CLI.Tests.Commands.SelfExtendGenerated;

public sealed class {{testClassName}} : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new {{commandClassName}}();
            AssertEqual("{{expectedCommandName}}", command.Name, "Command name should match scaffold");

            AssertTrue(command is IComposableExtensionCommand, "Command must implement IComposableExtensionCommand");
            var composable = (IComposableExtensionCommand)command;
            AssertEqual("{{expectedExtensionId}}", composable.ExtensionId, "ExtensionId should match scaffold");
            AssertEqual({{dependencyCount}}, composable.Dependencies.Count, "Dependency count should match scaffold");
{{dependencyAssertions}}

            return Task.FromResult(new TestResult
            {
                Name = nameof({{testClassName}}),
                Category = "SelfExtendGenerated",
                Passed = true,
                Message = "Generated command structure is valid."
            });
        }
        catch (AssertionException ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof({{testClassName}}),
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
                Name = nameof({{testClassName}}),
                Category = "SelfExtendGenerated",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            });
        }
    }
}
""";
    }

    private static string BuildBundleCommandStructureTestSource(string testClassName, string[] expectedCommands)
        => BuildBundleCommandStructureTestSource(
            testClassName,
            bundleClassName: "SelfExtendBundleCommand",
            expectedBundleCommandName: "self-extend-bundle",
            expectedCommands: expectedCommands);

    private static string BuildBundleCommandStructureTestSource(
        string testClassName,
        string bundleClassName,
        string expectedBundleCommandName,
        string[] expectedCommands)
    {
        var assertions = string.Join("\n", expectedCommands.Select(c =>
            $"            AssertTrue(command.Subcommands.Any(s => string.Equals(s.Name, \"{c}\", StringComparison.Ordinal)), \"Expected subcommand '{c}'\");"));
        return $$"""
using Ashlar.CLI.Commands.SelfExtendGenerated;
using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Application.Testing.Models;

namespace Ashlar.Tests.CLI.Tests.Commands.SelfExtendGenerated;

public sealed class {{testClassName}} : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new {{bundleClassName}}();
            AssertEqual("{{expectedBundleCommandName}}", command.Name, "Bundle command name should match scaffold");
{{assertions}}

            return Task.FromResult(new TestResult
            {
                Name = nameof({{testClassName}}),
                Category = "SelfExtendGenerated",
                Passed = true,
                Message = "Bundle command composition is valid."
            });
        }
        catch (AssertionException ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof({{testClassName}}),
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
                Name = nameof({{testClassName}}),
                Category = "SelfExtendGenerated",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            });
        }
    }
}
""";
    }

}
