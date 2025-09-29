# Nexo Framework - Quick Start Guide

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code
- Git
- Docker (optional)

### Installation
```bash
git clone <repository-url>
cd Nexo
dotnet restore
dotnet build
dotnet test
```

## Basic Usage

### 1. Running the CLI
```bash
# Show version
dotnet run --project src/Nexo.CLI version

# Show help
dotnet run --project src/Nexo.CLI help

# Execute a command
dotnet run --project src/Nexo.CLI execute --command "test"
```

### 2. Creating Your First Agent
```csharp
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.ValueObjects;
using Nexo.Shared.Values;

// Create a custom agent
public class MyCustomAgent : CrossPlatformAgent
{
    public MyCustomAgent() : base(
        AgentId.NewId(),
        new AgentName("My Custom Agent"),
        PlatformType.CrossPlatform,
        new List<AgentCapability> { AgentCapability.CodeGeneration },
        new List<string> { "Custom Development" },
        new Dictionary<string, object>())
    {
    }

    protected override async Task<AgentResult> OnExecuteAsync(AgentTask task)
    {
        // Your custom logic here
        return AgentResult.Success("Task completed", agentId: Id);
    }
}
```

### 3. Creating Your First Command
```csharp
using Nexo.Core.Application.Commands;
using Nexo.Shared.Results;

public class MyCustomCommand : ICommand<MyInput, MyOutput>
{
    public async Task<CommandResult<MyOutput>> ExecuteAsync(MyInput input)
    {
        // Your command logic here
        var result = new MyOutput { Success = true };
        return CommandResult<MyOutput>.Success(result);
    }
}

public class MyInput
{
    public string Data { get; set; } = string.Empty;
}

public class MyOutput
{
    public bool Success { get; set; }
    public string Result { get; set; } = string.Empty;
}
```

### 4. Using Centralized Systems

#### Results Management
```csharp
using Nexo.Shared.Results;

// Create a successful result
var result = ResultFactory.Success("Operation completed");

// Create a failed result
var errorResult = ResultFactory.Failure<string>("Operation failed");

// Chain results
var chainedResult = result.Chain(data => data.ToUpper());
```

#### Validation
```csharp
using Nexo.Shared.Validation;

// Validate an object
var validationResult = myObject.Validate();

// Validate required fields
var requiredValidation = myObject.ValidateRequired("Name", "Email");

// Combine validations
var combinedValidation = validationResult.Combine(requiredValidation);
```

#### Logging
```csharp
using Nexo.Shared.Logging;

// Log information
LoggingManager.LogInformation("Operation started");

// Log with properties
LoggingManager.LogInformation("User logged in", new Dictionary<string, object>
{
    ["UserId"] = "123",
    ["Timestamp"] = DateTime.UtcNow
});

// Log operation lifecycle
LoggingExtensions.LogOperationStart("Data Processing");
// ... do work ...
LoggingExtensions.LogOperationComplete("Data Processing", TimeSpan.FromSeconds(5));
```

#### Error Handling
```csharp
using Nexo.Shared.Errors;

try
{
    // Your code here
}
catch (Exception ex)
{
    var errorResult = ErrorHandlingManager.HandleException(ex, "MyOperation");
    // Handle error
}
```

#### Configuration
```csharp
using Nexo.Shared.Configuration;

// Register configuration
ConfigurationManager.RegisterConfiguration<MyConfiguration>("MyConfig");

// Set configuration
ConfigurationManager.SetConfiguration("MyConfig", new MyConfiguration
{
    Name = "My App",
    Version = "1.0.0"
});

// Get configuration
var config = ConfigurationManager.GetConfiguration<MyConfiguration>("MyConfig");
```

#### Factory Pattern
```csharp
using Nexo.Shared.Factories;

// Register a factory
FactoryManager.RegisterFactory<MyService>(() => new MyService());

// Create an instance
var service = FactoryManager.Create<MyService>();

// Create with dependencies
var serviceWithDeps = FactoryManager.CreateWithDependencies<MyService>();
```

## Advanced Usage

### 1. Agent Orchestration
```csharp
using Nexo.Core.Application.Agents;

// Create orchestrator
var orchestrator = new AgentOrchestrator();

// Register agents
await orchestrator.RegisterAgentAsync(new CodeGenerationAgent());
await orchestrator.RegisterAgentAsync(new SecurityAnalysisAgent());

// Execute coordinated task
var result = await orchestrator.ExecuteTaskAsync(new AgentTask
{
    Type = new TaskType("CodeReview"),
    Parameters = new Dictionary<string, object>
    {
        ["Code"] = "public class Test { }",
        ["Language"] = "C#"
    }
});
```

### 2. Command Orchestration
```csharp
using Nexo.Core.Application.Commands;

// Create orchestrator
var orchestrator = new CommandOrchestrator();

// Register commands
orchestrator.RegisterCommand<CreateProjectCommand, CreateProjectInput, CreateProjectOutput>();
orchestrator.RegisterCommand<ManageProjectAgentsCommand, ManageProjectAgentsInput, ManageProjectAgentsOutput>();

// Execute workflow
var result = await orchestrator.ExecuteWorkflowAsync(new List<ICommand<object, object>>
{
    new CreateProjectCommand(),
    new ManageProjectAgentsCommand()
});
```

### 3. Custom Extensions
```csharp
using Nexo.Shared.Extensions;

// String extensions
var result = myString.DefaultIfNullOrEmpty("Default Value");

// Collection extensions
myCollection.ForEach(item => Console.WriteLine(item));

// Task extensions
var result = await myTask.WithTimeout(TimeSpan.FromSeconds(30));

// Retry with backoff
var result = await myOperation.RetryWithBackoff(maxRetries: 3);
```

## Testing

### 1. Unit Testing
```csharp
using Xunit;
using Nexo.Shared.Results;

public class MyCommandTests
{
    [Fact]
    public async Task ExecuteAsync_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var command = new MyCommand();
        var input = new MyInput { Data = "test" };

        // Act
        var result = await command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
    }
}
```

### 2. Integration Testing
```csharp
using Xunit;
using Nexo.Core.Application.Agents;

public class AgentIntegrationTests
{
    [Fact]
    public async Task AgentOrchestration_ExecuteTask_ReturnsSuccess()
    {
        // Arrange
        var orchestrator = new AgentOrchestrator();
        await orchestrator.RegisterAgentAsync(new CodeGenerationAgent());

        // Act
        var result = await orchestrator.ExecuteTaskAsync(new AgentTask
        {
            Type = new TaskType("GenerateCode"),
            Parameters = new Dictionary<string, object>
            {
                ["Language"] = "C#",
                ["Template"] = "Console Application"
            }
        });

        // Assert
        Assert.True(result.IsSuccess);
    }
}
```

## Best Practices

### 1. Code Organization
- Keep classes under 200 lines
- Use single responsibility principle
- Implement proper error handling
- Add comprehensive logging

### 2. Testing
- Write unit tests for all classes
- Test error scenarios
- Use integration tests for workflows
- Maintain high code coverage

### 3. Performance
- Use async/await patterns
- Implement proper caching
- Monitor memory usage
- Profile critical paths

### 4. Security
- Validate all inputs
- Use secure communication
- Implement audit logging
- Follow security best practices

## Troubleshooting

### Common Issues

#### 1. Agent Registration Fails
```csharp
// Check agent initialization
var context = new AgentContext(PlatformType.CrossPlatform);
var initResult = await agent.InitializeAsync(context);
if (!initResult.IsSuccess)
{
    // Handle initialization failure
}
```

#### 2. Command Execution Fails
```csharp
// Check command validation
if (!command.CanExecute(input))
{
    // Handle validation failure
}

// Check command dependencies
var dependencies = command.Dependencies;
// Ensure dependencies are met
```

#### 3. Configuration Issues
```csharp
// Check configuration registration
var config = ConfigurationManager.GetConfiguration<MyConfiguration>("MyConfig");
if (config == null)
{
    // Register configuration first
    ConfigurationManager.RegisterConfiguration<MyConfiguration>("MyConfig");
}
```

## Next Steps

1. **Explore Examples**: Check the `examples/` directory for more complex scenarios
2. **Read Documentation**: Review the full documentation in `docs/`
3. **Join Community**: Participate in discussions and get help
4. **Contribute**: Submit issues and pull requests
5. **Build**: Create your own agents and commands

## Resources

- **Documentation**: `docs/` directory
- **Examples**: `examples/` directory
- **Tests**: `tests/` directory
- **API Reference**: Generated from XML documentation
- **Architecture**: `ARCHITECTURE_DIAGRAM.md`
- **Roadmap**: `IMPLEMENTATION_ROADMAP.md`

This quick start guide provides everything you need to begin building with the Nexo framework. The centralized systems ensure consistent patterns across your application while maintaining clean code standards.
