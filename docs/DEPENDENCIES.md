# Dependency Map

## Layer Dependencies

```
Tests → Infrastructure → Application → Domain
  ↓         ↓              ↓           ↓
  └─────────┴──────────────┴───────────┘
           No reverse dependencies
```

## Project Dependencies

### Nexo.Tests
- **Depends on**: Nexo.Infrastructure
- **Purpose**: Integration and unit testing
- **Packages**: xUnit, Moq, FluentAssertions

### Nexo.Infrastructure
- **Depends on**: Nexo.Core.Application
- **Purpose**: External integrations and implementations
- **Packages**: System.Reactive, Microsoft.Extensions.Logging

### Nexo.Core.Application
- **Depends on**: Nexo.Core.Domain
- **Purpose**: Application logic and use cases
- **Packages**: Microsoft.Extensions.Logging.Abstractions

### Nexo.Core.Domain
- **Depends on**: None (Core layer)
- **Purpose**: Business entities and domain logic
- **Packages**: None

## Interface Dependencies

### Container Domain
```
IContainerOrchestrator
├── ICommandExecutor
├── ILogger
└── ContainerRuntime (ValueObject)

IExecuteInContainerUseCase
├── IContainerOrchestrator
└── ICommandValidator

IContainerDevelopmentEnvironment
├── IContainerOrchestrator
└── DevelopmentSession (Model)
```

### Platform Domain
```
IPlatformAdapter
├── ILogger
└── PlatformInfo (Model)

IRuntimeEnvironment
├── RuntimeExecutionRequest (Model)
└── RuntimeExecutionResult (Model)
```

### Plugin Domain
```
IPlugin
├── IServiceProvider
└── None (Base interface)

IPluginManager
├── IPlugin
└── IServiceProvider

IPluginLoader
├── IPlugin
└── IFileSystem
```

### Agent Domain
```
IAgent
├── AgentId (ValueObject)
├── AgentName (ValueObject)
├── AgentRole (ValueObject)
└── AgentStatus (Enum)

IAnalyzerService
├── CodeAnalysisResult (Model)
├── ProjectAnalysisResult (Model)
└── AnalysisStatus (Enum)
```

### Project Domain
```
IProjectRepository
├── Project (Entity)
├── ProjectId (ValueObject)
└── ProjectStatus (Enum)

IProjectInitializer
├── Project (Entity)
└── IFileSystem

IInitializeProjectUseCase
├── IProjectRepository
├── IFileSystem
├── IContainerOrchestrator
├── IProjectInitializer[]
└── InitializeProjectRequest (Model)
```

### Validation Domain
```
ICommandValidator
├── CommandRequest (Model)
├── CommandResult (Model)
└── ValidationResult (Model)

ITemplateService
├── CodeTemplate (Model)
├── TemplateMetadata (Model)
├── TemplateValidationResult (Model)
└── TemplateSearchCriteria (Model)
```

## Implementation Dependencies

### Container Adapters
```
DockerContainerOrchestrator : IContainerOrchestrator
├── ICommandExecutor
├── ILogger<DockerContainerOrchestrator>
└── ContainerRuntime (ValueObject)

PodmanContainerOrchestrator : IContainerOrchestrator
├── ICommandExecutor
├── ILogger<PodmanContainerOrchestrator>
└── ContainerRuntime (ValueObject)
```

### Platform Adapters
```
WindowsPlatformAdapter : IPlatformAdapter
├── ILogger<WindowsPlatformAdapter>
└── PlatformInfo (Model)

LinuxPlatformAdapter : IPlatformAdapter
├── ILogger<LinuxPlatformAdapter>
└── PlatformInfo (Model)

MacOSPlatformAdapter : IPlatformAdapter
├── ILogger<MacOSPlatformAdapter>
└── PlatformInfo (Model)
```

### Command Services
```
ProcessCommandExecutor : ICommandExecutor
├── ILogger<ProcessCommandExecutor>
└── SemaphoreSlim (Concurrency control)

StreamingCommandService : IStreamingCommandService
├── ICommandExecutor
├── ICommandValidator
├── ILogger<StreamingCommandService>
└── SemaphoreSlim (Concurrency control)
```

### Validation Services
```
CommandValidator : ICommandValidator
├── ILogger<CommandValidator>
└── Validation rules

EnhancedCommandValidator : ICommandValidator
├── ILogger<EnhancedCommandValidator>
├── ICommandValidator (Decorator pattern)
└── Enhanced validation rules
```

### Plugin Services
```
BasePlugin : IPlugin
├── IServiceProvider (Protected)
└── Plugin lifecycle management

PluginLoader : IPluginLoader
├── IFileSystem
├── ILogger<PluginLoader>
└── Plugin discovery logic
```

### Agent Services
```
BaseAgent : IAgent
├── ILogger
├── Agent (Entity)
├── AgentId (ValueObject)
├── AgentName (ValueObject)
└── AgentRole (ValueObject)
```

### Project Services
```
InMemoryProjectRepository : IProjectRepository
├── ConcurrentDictionary<ProjectId, Project>
└── Thread-safe operations

DotNetProjectInitializer : IProjectInitializer
├── IFileSystem
├── ILogger<DotNetProjectInitializer>
└── .NET project templates
```

## Factory Dependencies

```
PlatformAdapterFactory
├── ILoggerFactory
├── RuntimeInformation (System)
└── Platform detection logic
```

## Cross-Cutting Dependencies

### Logging
- **Used by**: All services and adapters
- **Implementation**: Microsoft.Extensions.Logging
- **Configuration**: Dependency injection

### Configuration
- **Used by**: Infrastructure services
- **Implementation**: JsonConfigurationProvider
- **Storage**: File system

### File System
- **Used by**: Multiple services
- **Implementation**: FileSystemAdapter
- **Abstraction**: IFileSystem interface

## Dependency Injection Setup

### Application Layer Registration
```csharp
services.AddScoped<IInitializeProjectUseCase, InitializeProjectUseCase>();
services.AddScoped<IExecuteInContainerUseCase, ExecuteInContainerUseCase>();
```

### Infrastructure Layer Registration
```csharp
services.AddSingleton<IFileSystem, FileSystemAdapter>();
services.AddSingleton<ICommandExecutor, ProcessCommandExecutor>();
services.AddSingleton<IProjectRepository, InMemoryProjectRepository>();
services.AddSingleton<IConfigurationProvider, JsonConfigurationProvider>();
```

### Platform-Specific Registration
```csharp
// Container orchestration
services.AddSingleton<IContainerOrchestrator, DockerContainerOrchestrator>();
// or
services.AddSingleton<IContainerOrchestrator, PodmanContainerOrchestrator>();

// Platform adapter
services.AddSingleton<IPlatformAdapter>(sp => 
    PlatformAdapterFactory.Create(sp.GetRequiredService<ILoggerFactory>()));
```

## Circular Dependency Prevention

### Rules
1. Domain layer has no dependencies on other layers
2. Application layer only depends on Domain layer
3. Infrastructure layer depends on Application layer
4. Tests depend on Infrastructure layer

### Validation
- Use static analysis tools to detect circular dependencies
- Enforce dependency direction in CI/CD pipeline
- Regular dependency audits

## Performance Considerations

### Dependency Resolution
- Use singleton for stateless services
- Use scoped for request-specific services
- Use transient for lightweight services

### Memory Management
- Dispose of IDisposable dependencies
- Use weak references where appropriate
- Monitor dependency injection container size

## Testing Dependencies

### Mock Dependencies
- External services (Docker, file system)
- Time-consuming operations
- Non-deterministic behavior

### Real Dependencies
- Domain logic
- Simple value objects
- Pure functions

### Integration Testing
- Database repositories
- File system operations
- Container orchestration 