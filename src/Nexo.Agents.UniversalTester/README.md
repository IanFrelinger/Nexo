# Nexo Universal Testing Agent

A universal testing agent that can test any application - web, games, desktop, APIs, CLIs - using AI to understand and interact with applications.

## Design Philosophy

**One agent. Any application. AI figures out the rest.**

This isn't a "web testing agent" or a "game testing agent" - it's a **Universal Testing Agent** that uses AI to:

1. **Understand** what it's looking at (game? form? dashboard? CLI?)
2. **Discover** what actions are possible
3. **Explore** intelligently based on goals
4. **Validate** outcomes against expectations
5. **Report** findings in context-appropriate ways

## Architecture

The agent follows a **Perception → Understanding → Action → Validation** loop:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        UNIVERSAL TESTING AGENT                          │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌─────────────┐   ┌─────────────┐   ┌─────────────┐   ┌─────────────┐ │
│  │ PERCEIVE    │   │ UNDERSTAND  │   │ ACT         │   │ VALIDATE    │ │
│  │             │──▶│             │──▶│             │──▶│             │ │
│  │ Screenshot  │   │ AI analyzes │   │ AI decides  │   │ AI checks   │ │
│  │ DOM/State   │   │ context &   │   │ next action │   │ if result   │ │
│  │ Logs        │   │ affordances │   │ & executes  │   │ matches     │ │
│  │ Audio       │   │             │   │             │   │ expectation │ │
│  └─────────────┘   └─────────────┘   └─────────────┘   └─────────────┘ │
│         │                                                     │         │
│         └─────────────────────────────────────────────────────┘         │
│                              LOOP                                       │
└─────────────────────────────────────────────────────────────────────────┘
```

## Components

### Bricks

1. **PerceptionBrick** - Captures current state from any target
2. **UnderstandingBrick** - AI analyzes what it's seeing
3. **ExplorationBrick** - AI decides what action to take next
4. **ActionExecutorBrick** - Executes actions on the target
5. **ValidationBrick** - AI validates if the action produced expected results
6. **ReportingBrick** - Generates comprehensive test reports

### Adapters

- **WebAdapter** - Playwright for web applications
- **GameAdapter** - Unity/game engine integration (via TCP/WebSocket)
- **ApiAdapter** - HTTP client for REST APIs
- **CliAdapter** - Process + stdin/stdout for CLI applications
- **DesktopAdapter** - Desktop apps: Windows (FlaUI/UIA when available), macOS (screenshot via `screencapture`, actions via AppleScript), Linux (screenshot via `scrot`, Wait only)

## Usage

### Basic Example

```csharp
var config = new UniversalTesterConfig
{
    Target = "https://my-app.com",
    Goal = "Test the checkout flow and make sure payment works",
    Depth = TestingDepth.Standard,
    MaxDuration = TimeSpan.FromMinutes(10)
};

var agent = new UniversalTesterAgent(providerFactory, logger);
var report = await agent.ExecuteAsync(config, context, cancellationToken);

Console.WriteLine($"Score: {report.Summary.OverallScore}");
Console.WriteLine($"Issues: {report.Summary.Failed}");
```

### Test a Game

```csharp
var config = new UniversalTesterConfig
{
    Target = "C:\\Games\\MyGame\\MyGame.exe",
    Goal = "Play through the tutorial level and report any bugs",
    Depth = TestingDepth.Thorough,
    Constraints = new[] { "Don't skip cutscenes", "Try all tutorial prompts" }
};
```

### Test an API

```csharp
var config = new UniversalTesterConfig
{
    Target = "api://https://api.example.com",
    Goal = "Verify all CRUD operations work correctly",
    SetupInstructions = "Authenticate first using POST /auth/login"
};
```

## Configuration

### UniversalTesterConfig

- **Target** - What to test (URL, file path, API endpoint, CLI command)
- **Goal** - Natural language description of what to test
- **TargetType** - Optional, AI will infer if not provided
- **Constraints** - Things to look for or avoid
- **Depth** - How thorough (Quick, Standard, Thorough, Exhaustive)
- **MaxDuration** - Maximum time to spend testing
- **SetupInstructions** - Optional authentication/setup instructions
- **SuccessCriteria** - Optional description of what success looks like

## Implementation Details

The agent uses the Nexo brick system with both deterministic and agentic implementations:

- **Deterministic** - Fast, rule-based execution (e.g., capturing screenshots, executing actions)
- **Agentic** - AI-powered reasoning (e.g., understanding context, deciding actions, validating outcomes)

Bricks can switch between implementations based on context (air-gapped environments, audit mode, etc.).

## Dependencies

- Microsoft.Playwright - Web automation
- SixLabors.ImageSharp - Image processing for visual comparison
- Nexo.Core.Domain - Brick system
- Nexo.Infrastructure - LLM provider factory

## Desktop UI interaction

When testing a desktop app (e.g. the Nexo Guide), the agent:

- **Windows**: Uses FlaUI (Nexo.Agents.UniversalTester.Windows) when the assembly is present for full UI Automation (screenshot, element discovery, click/type by selector).
- **macOS**: Captures the screen with `screencapture`, sends the screenshot to a vision-capable LLM (Understanding brick), and executes click/type via AppleScript using pixel coordinates returned by the model.
- **Linux**: Captures the screen with `scrot` if available; action execution is limited to Wait (optional: add `xdotool` for click/type).

Run the agentic Guide test with a display and Ollama (e.g. via Docker) so the agent can see the app and interact with it: `./scripts/run-guide-agentic-test-docker.sh`.

### Vision model for human-like UI testing

To have the agent **see the screen** (screenshot), identify buttons and inputs, and simulate a human user:

- Use a **vision-capable model** for the Understanding brick. With Ollama, set **OLLAMA_VISION_MODEL** to a vision model; the script uses **llava:7b** by default.
- The Docker script pulls both a text model (e.g. `llama3.2:3b`) and a vision model (`llava:7b`). Vision is used only when analyzing the screenshot; text model is used for exploration/reporting.
- Alternatives: `llava:13b`, `llava-llama3`, `llama3.2-vision` (see [Ollama vision models](https://ollama.com/blog/vision-models)).
- Without a vision model, the agent falls back to fixed coordinate-based actions (less accurate).

## Future Enhancements

- [x] Desktop screenshot + coordinate-based actions (macOS AppleScript, Windows FlaUI)
- [ ] Mobile app testing (via emulators)
- [ ] Enhanced game integration (Unity plugin)
- [ ] Visual regression testing
- [ ] Performance profiling
- [ ] Accessibility testing
- [ ] Multi-browser support
