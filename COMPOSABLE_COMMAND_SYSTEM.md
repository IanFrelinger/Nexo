# Nexo Framework - Composable Command System

## Overview

The Nexo Framework now features a **composable, self-contained command system** that eliminates enums, uses only interface types, and dependency-wraps all operations including loops and LINQ. This creates a highly modular, testable, and maintainable architecture.

## Key Principles

### 1. **No Enums - Interface Types Only**
- All status values, priorities, and categories are interface-based
- Type safety through interfaces rather than enum values
- Extensible and mockable for testing

### 2. **Dependency-Wrapped Operations**
- All loops, LINQ operations, and string manipulations are wrapped in interfaces
- Enables easy mocking and testing
- Allows for custom implementations and optimizations

### 3. **Composable Commands**
- Self-contained commands with clear dependencies
- Easy to combine and orchestrate
- Built-in validation and rollback capabilities

## Architecture

### Core Interfaces

#### `IComposableCommand`
```csharp
public interface IComposableCommand
{
    ICommandIdentifier Id { get; }
    ICommandName Name { get; }
    ICommandDescription Description { get; }
    ICommandStatus Status { get; }
    ICommandDependencies Dependencies { get; }
    ICommandCapabilities Capabilities { get; }
    
    Task<ICommandResult> ExecuteAsync(ICommandContext context);
    Task<IValidationResult> ValidateAsync(ICommandContext context);
    Task<ICommandResult> RollbackAsync(ICommandContext context);
}
```

#### `ICommandIdentifier`
```csharp
public interface ICommandIdentifier
{
    string Value { get; }
    string Namespace { get; }
    string Version { get; }
    DateTime CreatedAt { get; }
}
```

#### `ICommandStatus`
```csharp
public interface ICommandStatus
{
    string State { get; }
    string Message { get; }
    DateTime LastUpdated { get; }
    bool IsExecutable { get; }
    bool IsCompleted { get; }
    bool IsFailed { get; }
}
```

### Dependency-Wrapped Operations

#### `ICollectionOperations` (Replaces LINQ)
```csharp
public interface ICollectionOperations
{
    Task<IReadOnlyList<T>> WhereAsync<T>(IReadOnlyList<T> source, Func<T, Task<bool>> predicate);
    Task<IReadOnlyList<TResult>> SelectAsync<T, TResult>(IReadOnlyList<T> source, Func<T, Task<TResult>> selector);
    Task ForEachAsync<T>(IReadOnlyList<T> source, Func<T, Task> action);
    Task<bool> AnyAsync<T>(IReadOnlyList<T> source, Func<T, Task<bool>> predicate);
    Task<bool> AllAsync<T>(IReadOnlyList<T> source, Func<T, Task<bool>> predicate);
    Task<T> FirstOrDefaultAsync<T>(IReadOnlyList<T> source, Func<T, Task<bool>> predicate);
    Task<int> CountAsync<T>(IReadOnlyList<T> source, Func<T, Task<bool>> predicate);
    Task<IReadOnlyDictionary<TKey, IReadOnlyList<T>>> GroupByAsync<T, TKey>(IReadOnlyList<T> source, Func<T, Task<TKey>> keySelector);
    Task<IReadOnlyList<T>> OrderByAsync<T, TKey>(IReadOnlyList<T> source, Func<T, Task<TKey>> keySelector);
    Task<IReadOnlyList<T>> TakeAsync<T>(IReadOnlyList<T> source, int count);
    Task<IReadOnlyList<T>> SkipAsync<T>(IReadOnlyList<T> source, int count);
}
```

#### `ILoopOperations` (Replaces for/while loops)
```csharp
public interface ILoopOperations
{
    Task ForAsync(int start, int end, Func<int, Task> action);
    Task ForAsync(int start, int end, int step, Func<int, Task> action);
    Task WhileAsync(Func<Task<bool>> condition, Func<Task> action);
    Task DoWhileAsync(Func<Task> action, Func<Task<bool>> condition);
    Task ForEachAsync<T>(IReadOnlyList<T> items, Func<T, int, Task> action);
    Task ForEachAsync<T>(IEnumerable<T> items, Func<T, int, Task> action);
    Task LoopAsync<T>(IReadOnlyList<T> items, Func<T, int, ILoopControl, Task> action);
}
```

#### `IStringOperations` (Replaces string methods)
```csharp
public interface IStringOperations
{
    Task<bool> IsNullOrEmptyAsync(string value);
    Task<bool> IsNullOrWhiteSpaceAsync(string value);
    Task<string> TrimAsync(string value);
    Task<string> ToUpperAsync(string value);
    Task<string> ToLowerAsync(string value);
    Task<IReadOnlyList<string>> SplitAsync(string value, string delimiter);
    Task<string> JoinAsync(IReadOnlyList<string> values, string delimiter);
    Task<string> ReplaceAsync(string value, string oldValue, string newValue);
    Task<bool> ContainsAsync(string value, string substring);
    Task<bool> StartsWithAsync(string value, string substring);
    Task<bool> EndsWithAsync(string value, string substring);
    Task<string> SubstringAsync(string value, int startIndex, int length = -1);
}
```

## Usage Examples

### 1. Creating a Composable Command

```csharp
public class MyComposableCommand : BaseComposableCommand
{
    public override ICommandIdentifier Id { get; }
    public override ICommandName Name { get; }
    public override ICommandDescription Description { get; }
    public override ICommandStatus Status { get; }
    public override ICommandDependencies Dependencies { get; }
    public override ICommandCapabilities Capabilities { get; }
    
    public MyComposableCommand(
        ICollectionOperations collectionOps = null,
        ILoopOperations loopOps = null,
        IStringOperations stringOps = null) 
        : base(collectionOps, loopOps, stringOps)
    {
        Id = new CommandIdentifier("MyCommand", "MyNamespace", "1.0.0");
        Name = new CommandName("MyCommand", "My Command", "MyCategory");
        Description = new CommandDescription("A custom composable command");
        Status = CommandStatus.Ready();
        Dependencies = CommandDependencies.None();
        Capabilities = CommandCapabilities.None();
    }
    
    public override async Task<ICommandResult> ExecuteAsync(ICommandContext context)
    {
        try
        {
            // Use dependency-wrapped operations
            var input = context.Parameters.TryGetValue("input", out var value) ? value?.ToString() : "Default";
            var processed = await _stringOps.ToUpperAsync(input);
            var trimmed = await _stringOps.TrimAsync(processed);
            
            var resultData = new { Original = input, Processed = trimmed };
            return CreateSuccessResult("Command executed successfully", resultData);
        }
        catch (Exception ex)
        {
            return CreateFailureResult($"Command failed: {ex.Message}");
        }
    }
}
```

### 2. Using the Orchestrator

```csharp
// Create orchestrator
var orchestrator = new ComposableOrchestrator();

// Create and register commands
var command1 = await factory.CreateCommandAsync<MyComposableCommand>();
var command2 = await factory.CreateCommandAsync<AnotherComposableCommand>();

await orchestrator.RegisterCommandAsync(command1);
await orchestrator.RegisterCommandAsync(command2);

// Execute single command
var context = CommandContext.Create(command1.Id, ("input", "Hello World"));
var result = await orchestrator.ExecuteCommandAsync(command1.Id, context);

// Execute commands in sequence
var commandIds = new List<ICommandIdentifier> { command1.Id, command2.Id };
var results = await orchestrator.ExecuteSequenceAsync(commandIds, context);

// Execute commands in parallel
var parallelResults = await orchestrator.ExecuteParallelAsync(commandIds, context);
```

### 3. Dependency Injection

```csharp
// Create dependency container
var container = new DependencyContainer();

// Register default operations
await container.RegisterInstanceAsync<ICollectionOperations>(new CollectionOperations());
await container.RegisterInstanceAsync<ILoopOperations>(new LoopOperations());
await container.RegisterInstanceAsync<IStringOperations>(new StringOperations());

// Create factory with container
var factory = new ComposableCommandFactory(container);

// Create command with injected dependencies
var command = await factory.CreateCommandAsync<MyComposableCommand>();
```

### 4. Custom Operations

```csharp
// Create custom operations
public class CustomCollectionOperations : ICollectionOperations
{
    public async Task<IReadOnlyList<T>> WhereAsync<T>(IReadOnlyList<T> source, Func<T, Task<bool>> predicate)
    {
        // Custom implementation with logging, caching, etc.
        var results = new List<T>();
        foreach (var item in source)
        {
            if (await predicate(item))
            {
                results.Add(item);
            }
        }
        return results;
    }
    
    // Implement other methods...
}

// Use custom operations
var customOps = new CustomCollectionOperations();
var command = await factory.CreateCommandAsync<MyComposableCommand>(customOps);
```

## Benefits

### 1. **Testability**
- All operations are interface-based and mockable
- Easy to create unit tests with controlled behavior
- No hidden dependencies or static calls

### 2. **Modularity**
- Commands are self-contained and composable
- Easy to combine and orchestrate
- Clear separation of concerns

### 3. **Extensibility**
- Custom operations can be implemented
- Easy to add new capabilities
- Interface-based design allows for multiple implementations

### 4. **Maintainability**
- No enums means no magic numbers or strings
- Interface types provide better IntelliSense
- Clear dependency relationships

### 5. **Performance**
- Operations can be optimized per implementation
- Caching and other optimizations can be added transparently
- Async/await patterns throughout

## Migration from Old System

### Before (Enum-based)
```csharp
public enum CommandStatus
{
    Ready,
    Executing,
    Completed,
    Failed
}

var status = CommandStatus.Ready;
if (status == CommandStatus.Completed)
{
    // Handle completion
}
```

### After (Interface-based)
```csharp
public interface ICommandStatus
{
    string State { get; }
    bool IsCompleted { get; }
    // ... other properties
}

var status = CommandStatus.Ready();
if (status.IsCompleted)
{
    // Handle completion
}
```

### Before (LINQ)
```csharp
var results = items.Where(x => x.IsValid).Select(x => x.Process()).ToList();
```

### After (Dependency-wrapped)
```csharp
var validItems = await _collectionOps.WhereAsync(items, async x => await x.IsValidAsync());
var results = await _collectionOps.SelectAsync(validItems, async x => await x.ProcessAsync());
```

## Best Practices

### 1. **Command Design**
- Keep commands focused and single-purpose
- Use clear, descriptive names and descriptions
- Define explicit dependencies and capabilities

### 2. **Operation Implementation**
- Make all operations async for consistency
- Handle null values gracefully
- Provide meaningful error messages

### 3. **Testing**
- Mock all dependencies in unit tests
- Test both success and failure scenarios
- Verify rollback behavior

### 4. **Performance**
- Consider caching for expensive operations
- Use appropriate data structures
- Profile and optimize critical paths

## Future Enhancements

### 1. **Advanced Orchestration**
- Workflow engines with conditional logic
- Event-driven command execution
- Dynamic command discovery

### 2. **Monitoring and Observability**
- Command execution metrics
- Performance profiling
- Health checks

### 3. **Persistence**
- Command state persistence
- Audit trails
- Recovery mechanisms

### 4. **Security**
- Command authorization
- Input validation
- Audit logging

This composable command system provides a solid foundation for building complex, maintainable applications while maintaining the flexibility and testability that modern software development requires.
