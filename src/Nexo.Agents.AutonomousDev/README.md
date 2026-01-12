# Nexo Autonomous Development Agent

An autonomous development agent that builds, tests with mock users, and iterates until complete.

## Design Philosophy

**Build → Test → Learn → Improve → Repeat**

This agent creates an autonomous development loop where AI:

1. **Understands** what needs to be built (from specs, tickets, or natural language)
2. **Generates** code, assets, or configurations
3. **Tests** using the Universal Testing Agent as a mock user
4. **Analyzes** feedback to identify what's wrong
5. **Iterates** until the goal is achieved

## Architecture

The agent follows an autonomous development cycle:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      AUTONOMOUS DEVELOPMENT LOOP                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────────────┐     ┌──────────────┐     ┌──────────────┐               │
│  │   PLAN       │────▶│   GENERATE   │────▶│   INTEGRATE  │               │
│  └──────────────┘     └──────────────┘     └──────┬───────┘               │
│                                                    │                        │
│         ┌──────────────────────────────────────────┘                        │
│         │                                                                   │
│         ▼                                                                   │
│  ┌──────────────┐     ┌──────────────┐     ┌──────────────┐               │
│  │   BUILD      │────▶│   TEST       │────▶│   ANALYZE    │               │
│  └──────────────┘     └──────────────┘     └──────┬───────┘               │
│                                                    │                        │
│         ┌──────────────────────────────────────────┘                        │
│         │                                                                   │
│         ▼                                                                   │
│  ┌──────────────┐                                                          │
│  │   DECIDE     │◀────────────────────────────────────────┐                │
│  │              │                                          │                │
│  │ Pass? Ship!  │     No ──▶ Iterate ──▶ Back to PLAN ────┘                │
│  │ Fail? Fix!   │                                                          │
│  └──────────────┘                                                          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Components

### Bricks

1. **SpecificationBrick** - Parses and understands requirements
2. **PlanningBrick** - Breaks specification into actionable tasks
3. **GenerationBrick** - Generates code/assets
4. **IntegrationBrick** - Applies changes to project
5. **BuildBrick** - Compiles/deploys
6. **TestingBrick** - Runs Universal Tester as mock user
7. **AnalysisBrick** - Analyzes feedback and decides next steps

### Adapters

- **GenericProjectAdapter** - File-based project operations
- **IProjectAdapter** - Interface for project-specific adapters

## Usage

### Basic Example

```csharp
var config = new DevTaskConfig
{
    Task = "Add a save/load system for player progress",
    ProjectPath = "C:\\Projects\\MyGame",
    ProjectType = ProjectType.UnityGame,
    AcceptanceCriteria = "Player can save, quit, reload, and continue from same point",
    TestPersona = MockUserPersona.Average,
    MaxIterations = 10,
    Autonomy = AutonomyLevel.SemiAutonomous
};

var agent = new AutonomousDevAgent(providerFactory, tester, logger);
var session = await agent.ExecuteAsync(config, context, cancellationToken);

Console.WriteLine($"Status: {session.Status}");
Console.WriteLine($"Iterations: {session.Iterations.Count}");
```

### Fix a Bug

```csharp
var config = new DevTaskConfig
{
    Task = "Fix the bug where enemies don't respawn after player death",
    ProjectPath = "C:\\Projects\\MyGame",
    TestPersona = MockUserPersona.Adversarial,
    Constraints = new[] { "Don't modify enemy AI behavior" }
};
```

### Build an API Endpoint

```csharp
var config = new DevTaskConfig
{
    Task = "Create REST API endpoint for user registration",
    ProjectPath = "/home/dev/my-api",
    ProjectType = ProjectType.DotNetApi,
    DetailedSpec = "POST /api/auth/register with { email, password, name }",
    AcceptanceCriteria = "Valid returns 201, invalid returns 400",
    TestPersona = MockUserPersona.Adversarial
};
```

## Configuration

### DevTaskConfig

- **Task** - What to build (natural language)
- **ProjectPath** - Path to the project
- **ProjectType** - Optional, AI will infer
- **DetailedSpec** - Optional detailed requirements
- **References** - Optional reference materials
- **AcceptanceCriteria** - How to verify completion
- **MaxIterations** - Maximum iterations before giving up
- **Autonomy** - Supervised, SemiAutonomous, or FullyAutonomous
- **TestTarget** - Target for Universal Tester (inferred if not provided)
- **TestPersona** - Mock user persona (Novice, Average, PowerUser, Adversarial, etc.)
- **Constraints** - Additional constraints or guidelines

## The Feedback Loop

The agent iterates through:

1. **Generate** - AI creates code/assets
2. **Integrate** - Apply changes to project
3. **Build** - Compile the project
4. **Test** - Universal Tester acts as mock user
5. **Analyze** - AI analyzes feedback and plans fixes
6. **Decide** - Continue iterating or complete

This continues until:
- All acceptance criteria are met (Complete)
- Maximum iterations reached (MaxIterations)
- Agent gets stuck (Stuck/NeedsRedesign)
- Human input needed (NeedsClarification)

## Dependencies

- Nexo.Agents.UniversalTester - For mock user testing
- Nexo.Core.Domain - Brick system
- Nexo.Infrastructure - LLM provider factory

## Future Enhancements

- [ ] Unity-specific project adapter
- [ ] .NET-specific project adapter
- [ ] React/Next.js project adapter
- [ ] Enhanced code generation with context
- [ ] Multi-file coordination
- [ ] Dependency management
- [ ] Version control integration
- [ ] Session persistence/resume
