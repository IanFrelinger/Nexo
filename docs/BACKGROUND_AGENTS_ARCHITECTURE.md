# Background Agents Architecture

## Overview

Optional embedded background agents that run continuously in the framework, with configurable commands, roles, hierarchies, and models. These agents are **dog fooded** - they use the framework's own agent infrastructure to manage themselves.

## Design Principles

1. **Optional**: Background agents are opt-in via configuration
2. **Embedded**: Part of the framework, not external services
3. **Configurable**: Commands, roles, hierarchies, models all configurable
4. **Dog Fooded**: Use framework's own `IAgent`, `Orchestrator`, `AgentFactory` infrastructure
5. **Background**: Run asynchronously without blocking main operations
6. **Hierarchical**: Support agent hierarchies (parent-child relationships)

## Architecture

### Core Components

```
┌─────────────────────────────────────────────────────────────┐
│              BackgroundAgentService (BackgroundService)      │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐   │
│  │     BackgroundAgentRegistry                          │   │
│  │  - Manages agent lifecycle                           │   │
│  │  - Tracks agent state                                │   │
│  │  - Handles agent communication                       │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐   │
│  │     BackgroundAgentConfigLoader                      │   │
│  │  - Loads agent configs from file/CLI                  │   │
│  │  - Validates configurations                          │   │
│  │  - Creates AgentSpawnSpec from configs               │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐   │
│  │     Uses Existing Infrastructure:                     │   │
│  │  - AgentFactory (creates agents)                     │   │
│  │  - Orchestrator (coordinates agents)                 │   │
│  │  - LifecycleManager (manages lifecycle)              │   │
│  │  - AgentBus (agent communication)                    │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### Configuration Model

```csharp
public class BackgroundAgentConfig
{
    public string Id { get; set; }                    // Unique agent identifier
    public string Name { get; set; }                  // Human-readable name
    public string Role { get; set; }                  // Agent role (e.g., "monitor", "analyzer", "optimizer")
    public string? ParentId { get; set; }             // Parent agent ID (for hierarchies)
    public string ModelProvider { get; set; }          // "openai", "azure", "ollama", "deterministic"
    public string? ModelName { get; set; }            // Specific model name (optional)
    public List<string> Commands { get; set; }        // Commands this agent can execute
    public Dictionary<string, object> Parameters { get; set; }  // Agent-specific parameters
    public BackgroundAgentSchedule Schedule { get; set; }       // When to run
    public bool Enabled { get; set; }                // Enable/disable this agent
}

public class BackgroundAgentSchedule
{
    public ScheduleType Type { get; set; }            // "continuous", "interval", "cron"
    public TimeSpan? Interval { get; set; }          // For interval type
    public string? CronExpression { get; set; }     // For cron type
    public TimeSpan? InitialDelay { get; set; }      // Delay before first run
}

public enum ScheduleType
{
    Continuous,  // Run continuously (think loop)
    Interval,    // Run at fixed intervals
    Cron         // Run on cron schedule
}
```

### Configuration File Format (JSON)

```json
{
  "backgroundAgents": {
    "enabled": true,
    "agents": [
      {
        "id": "health-monitor",
        "name": "Health Monitor Agent",
        "role": "monitor",
        "modelProvider": "deterministic",
        "commands": ["check-health", "report-metrics"],
        "schedule": {
          "type": "interval",
          "interval": "00:05:00"
        },
        "enabled": true
      },
      {
        "id": "code-analyzer",
        "name": "Code Analysis Agent",
        "role": "analyzer",
        "parentId": "health-monitor",
        "modelProvider": "openai",
        "modelName": "gpt-4",
        "commands": ["analyze-code", "detect-issues"],
        "schedule": {
          "type": "cron",
          "cronExpression": "0 */6 * * *"
        },
        "enabled": true,
        "parameters": {
          "analysisDepth": "thorough",
          "reportFormat": "json"
        }
      },
      {
        "id": "performance-optimizer",
        "name": "Performance Optimizer",
        "role": "optimizer",
        "modelProvider": "ollama",
        "modelName": "llama2",
        "commands": ["optimize-performance", "suggest-improvements"],
        "schedule": {
          "type": "continuous",
          "initialDelay": "00:01:00"
        },
        "enabled": false
      }
    ]
  }
}
```

## Implementation Approach

### Phase 1: Core Infrastructure

#### 1.1 BackgroundAgentService (BackgroundService)

```csharp
public class BackgroundAgentService : BackgroundService
{
    private readonly BackgroundAgentRegistry _registry;
    private readonly BackgroundAgentConfigLoader _configLoader;
    private readonly AgentFactory _agentFactory;
    private readonly Orchestrator _orchestrator;
    private readonly ILogger<BackgroundAgentService> _logger;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Load configurations
        var configs = await _configLoader.LoadAsync(stoppingToken);
        
        // Create and register agents
        foreach (var config in configs.Where(c => c.Enabled))
        {
            var agent = await CreateAgentFromConfigAsync(config, stoppingToken);
            await _registry.RegisterAsync(agent, config, stoppingToken);
        }
        
        // Start agent execution loops
        await _registry.StartAllAsync(stoppingToken);
        
        // Keep service running
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
```

#### 1.2 BackgroundAgentRegistry

```csharp
public class BackgroundAgentRegistry
{
    private readonly ConcurrentDictionary<string, BackgroundAgentInstance> _agents = new();
    private readonly AgentFactory _agentFactory;
    private readonly LifecycleManager _lifecycleManager;
    private readonly IAgentBus _agentBus;
    
    public async Task RegisterAsync(
        IAgent agent, 
        BackgroundAgentConfig config, 
        CancellationToken ct)
    {
        // Register with lifecycle manager
        var container = _agentFactory.CreateContainer(agent);
        await _lifecycleManager.RegisterAgentAsync(container, ct);
        
        // Create background instance
        var instance = new BackgroundAgentInstance
        {
            Agent = agent,
            Config = config,
            State = BackgroundAgentState.Idle
        };
        
        _agents[config.Id] = instance;
    }
    
    public async Task StartAllAsync(CancellationToken ct)
    {
        // Start each agent's execution loop based on schedule
        foreach (var instance in _agents.Values)
        {
            _ = Task.Run(() => ExecuteAgentLoopAsync(instance, ct), ct);
        }
    }
    
    private async Task ExecuteAgentLoopAsync(
        BackgroundAgentInstance instance, 
        CancellationToken ct)
    {
        // Handle initial delay
        if (instance.Config.Schedule.InitialDelay.HasValue)
        {
            await Task.Delay(instance.Config.Schedule.InitialDelay.Value, ct);
        }
        
        // Execute based on schedule type
        switch (instance.Config.Schedule.Type)
        {
            case ScheduleType.Continuous:
                await ExecuteContinuousAsync(instance, ct);
                break;
            case ScheduleType.Interval:
                await ExecuteIntervalAsync(instance, ct);
                break;
            case ScheduleType.Cron:
                await ExecuteCronAsync(instance, ct);
                break;
        }
    }
}
```

#### 1.3 BackgroundAgentConfigLoader

```csharp
public class BackgroundAgentConfigLoader
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BackgroundAgentConfigLoader> _logger;
    
    public async Task<List<BackgroundAgentConfig>> LoadAsync(CancellationToken ct)
    {
        // Load from appsettings.json or dedicated config file
        var section = _configuration.GetSection("BackgroundAgents:Agents");
        var configs = new List<BackgroundAgentConfig>();
        
        section.Bind(configs);
        
        // Validate configurations
        foreach (var config in configs)
        {
            ValidateConfig(config);
        }
        
        return configs;
    }
    
    private void ValidateConfig(BackgroundAgentConfig config)
    {
        if (string.IsNullOrEmpty(config.Id))
            throw new InvalidOperationException("Agent ID is required");
        
        if (config.Commands == null || config.Commands.Count == 0)
            throw new InvalidOperationException($"Agent {config.Id} must have at least one command");
        
        // Validate schedule
        ValidateSchedule(config.Schedule);
    }
}
```

### Phase 2: Agent Creation from Config

#### 2.1 Create AgentSpawnSpec from Config

```csharp
public class BackgroundAgentSpecBuilder
{
    public AgentSpawnSpec BuildSpec(BackgroundAgentConfig config)
    {
        // Build system prompt based on role and commands
        var systemPrompt = BuildSystemPrompt(config);
        
        // Determine dependencies (parent-child relationships)
        var dependencies = new List<string>();
        if (!string.IsNullOrEmpty(config.ParentId))
        {
            dependencies.Add(config.ParentId);
        }
        
        return new AgentSpawnSpec
        {
            AgentId = config.Id,
            AgentType = DetermineAgentType(config.Role),
            SystemPrompt = systemPrompt,
            Dependencies = dependencies,
            Parameters = config.Parameters ?? new Dictionary<string, object>()
        };
    }
    
    private string BuildSystemPrompt(BackgroundAgentConfig config)
    {
        var prompt = $@"You are a {config.Role} agent named {config.Name}.

Your available commands are:
{string.Join("\n", config.Commands.Select(c => $"- {c}"))}

Execute your commands based on your role and the current system state.
Report your findings and take actions as appropriate for your role.";
        
        return prompt;
    }
    
    private string DetermineAgentType(string role)
    {
        return role.ToLowerInvariant() switch
        {
            "monitor" => "GenericAgent",
            "analyzer" => "CodeGenerationAgent",
            "optimizer" => "GenericAgent",
            _ => "GenericAgent"
        };
    }
}
```

### Phase 3: CLI Integration (Dog Fooding)

#### 3.1 CLI Commands for Background Agents

```csharp
// nexo background-agent list
public class ListBackgroundAgentsCommand : Command
{
    public override async Task<int> ExecuteAsync(InvocationContext ctx)
    {
        var registry = ctx.ServiceProvider.GetRequiredService<BackgroundAgentRegistry>();
        var agents = registry.GetAll();
        
        // Use framework's own formatting
        var console = ctx.ServiceProvider.GetRequiredService<CliConsole>();
        console.WriteLine("Background Agents:");
        foreach (var agent in agents)
        {
            console.WriteLine($"  {agent.Config.Id}: {agent.Config.Name} ({agent.State})");
        }
        
        return 0;
    }
}

// nexo background-agent add
public class AddBackgroundAgentCommand : Command
{
    public override async Task<int> ExecuteAsync(InvocationContext ctx)
    {
        // Use framework's orchestration to add agent
        var orchestrator = ctx.ServiceProvider.GetRequiredService<Orchestrator>();
        var request = $"Add background agent with configuration: {GetConfigFromArgs(ctx)}";
        
        var result = await orchestrator.OrchestrateAsync(request);
        
        // Agent is added via orchestration (dog fooding!)
        return result.Success ? 0 : 1;
    }
}

// nexo background-agent configure
public class ConfigureBackgroundAgentCommand : Command
{
    // Uses framework's own configuration system
}
```

### Phase 4: Self-Management (Advanced Dog Fooding)

#### 4.1 Meta-Agent for Managing Background Agents

```csharp
// A background agent that manages other background agents!
public class BackgroundAgentManagerAgent : BaseDomainAgent
{
    private readonly BackgroundAgentRegistry _registry;
    
    protected override async Task<object> OnExecuteAsync(
        IReadOnlyDictionary<string, object>? dependencyOutputs,
        CancellationToken cancellationToken)
    {
        // Use framework's own agent to manage agents
        var observation = new AgentObservation(new WorldSnapshot(0, new Dictionary<string, object?>
        {
            ["registry"] = _registry.GetAll(),
            ["systemHealth"] = GetSystemHealth()
        }));
        
        var actions = await ThinkAsync(observation, GetToolbox(), GetMemory(), cancellationToken);
        
        // Execute tool calls to manage agents
        foreach (var toolCall in actions.ToolCalls)
        {
            await ExecuteManagementCommand(toolCall, cancellationToken);
        }
        
        return new { Managed = true };
    }
    
    private IToolbox GetToolbox()
    {
        // Return toolbox with agent management tools
        return new AgentManagementToolbox(_registry);
    }
}
```

#### 4.2 Agent Management Tools

```csharp
public class AgentManagementToolbox : IToolbox
{
    private readonly BackgroundAgentRegistry _registry;
    private readonly List<ITool> _tools;
    
    public AgentManagementToolbox(BackgroundAgentRegistry registry)
    {
        _registry = registry;
        _tools = new List<ITool>
        {
            new EnableAgentTool(registry),
            new DisableAgentTool(registry),
            new RestartAgentTool(registry),
            new UpdateAgentConfigTool(registry)
        };
    }
    
    public IEnumerable<ToolSchema> Schemas() => _tools.Select(t => t.Schema);
    
    public async Task<ToolResult> InvokeAsync(ToolCall toolCall, WorldSnapshot s, CancellationToken ct)
    {
        var tool = _tools.FirstOrDefault(t => t.Id == toolCall.Id);
        if (tool == null)
            throw new InvalidOperationException($"Tool {toolCall.Id} not found");
        
        return await tool.InvokeAsync(toolCall, s, ct);
    }
    
    public IAgentMemory MemoryFor(IAgent agent) => new InMemoryAgentMemory();
}
```

## Configuration File Locations

1. **appsettings.json**: Default configuration
2. **background-agents.json**: Dedicated config file (optional)
3. **CLI commands**: Runtime configuration via `nexo background-agent configure`

## Integration Points

### Service Registration

```csharp
// In Program.cs or ServiceCollectionExtensions
services.AddBackgroundAgents(options =>
{
    options.ConfigFile = "background-agents.json";
    options.Enabled = true;
});

// Registers:
// - BackgroundAgentService (BackgroundService)
// - BackgroundAgentRegistry
// - BackgroundAgentConfigLoader
// - BackgroundAgentSpecBuilder
```

### Existing Infrastructure Reuse

- **AgentFactory**: Creates agents from configs
- **Orchestrator**: Coordinates agent execution
- **LifecycleManager**: Manages agent lifecycle
- **AgentBus**: Agent-to-agent communication
- **HealthCheckService**: Monitor agent health
- **Metrics**: Track agent performance

## Example Use Cases

### 1. Health Monitor Agent

```json
{
  "id": "health-monitor",
  "role": "monitor",
  "modelProvider": "deterministic",
  "commands": ["check-health", "report-metrics"],
  "schedule": { "type": "interval", "interval": "00:05:00" }
}
```

### 2. Code Quality Agent

```json
{
  "id": "code-quality",
  "role": "analyzer",
  "modelProvider": "openai",
  "commands": ["analyze-code", "suggest-improvements"],
  "schedule": { "type": "cron", "cronExpression": "0 0 * * *" }
}
```

### 3. Performance Optimizer Agent

```json
{
  "id": "perf-optimizer",
  "role": "optimizer",
  "parentId": "health-monitor",
  "modelProvider": "ollama",
  "commands": ["analyze-performance", "optimize"],
  "schedule": { "type": "continuous" }
}
```

## Benefits of Dog Fooding

1. **Self-Management**: Framework agents manage framework agents
2. **Consistency**: Same abstractions used internally and externally
3. **Testability**: Can test agent management using framework's own test infrastructure
4. **Extensibility**: Easy to add new agent types using existing patterns
5. **Observability**: Framework's own metrics/monitoring applies to background agents

## Implementation Phases

### Phase 1: Core Infrastructure (Week 1)
- BackgroundAgentService
- BackgroundAgentRegistry
- BackgroundAgentConfigLoader
- Basic configuration file support

### Phase 2: Agent Creation (Week 2)
- BackgroundAgentSpecBuilder
- Integration with AgentFactory
- Schedule execution (interval, cron, continuous)

### Phase 3: CLI Integration (Week 3)
- `nexo background-agent list`
- `nexo background-agent add`
- `nexo background-agent configure`
- `nexo background-agent enable/disable`

### Phase 4: Self-Management (Week 4)
- Meta-agent for agent management
- Agent management tools
- Self-configuration capabilities

## Testing Strategy

1. **Unit Tests**: Test each component in isolation
2. **Integration Tests**: Test agent creation and execution
3. **E2E Tests**: Test full background agent lifecycle
4. **Dog Food Tests**: Use framework's own testing infrastructure to test background agents

## Next Steps

1. Create `Nexo.BackgroundAgents` project
2. Implement core infrastructure (Phase 1)
3. Add configuration file support
4. Integrate with existing orchestration
5. Add CLI commands
6. Implement self-management (meta-agent)
