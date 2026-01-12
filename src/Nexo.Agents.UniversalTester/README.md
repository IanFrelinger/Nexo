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
- **DesktopAdapter** - Windows UI Automation (placeholder)

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

## Future Enhancements

- [ ] Full desktop automation support (Windows UI Automation)
- [ ] Mobile app testing (via emulators)
- [ ] Enhanced game integration (Unity plugin)
- [ ] Visual regression testing
- [ ] Performance profiling
- [ ] Accessibility testing
- [ ] Multi-browser support
