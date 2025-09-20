# Central Command Aggregator

## 🎯 Overview

The Central Command Aggregator is a unified system that composes all commands from the Nexo project and provides a single entry point for execution. This demonstrates how complex command-line interfaces can be organized, discovered, and orchestrated.

## 🏗️ Architecture

### Core Components

1. **CentralCommandAggregator** - Main aggregator for all CLI commands
2. **DemoCommandAggregator** - Specialized aggregator for demo commands
3. **CentralDemoRunner** - Entry point demonstrating command composition

### Command Categories

The aggregator organizes commands into logical categories:

- **Core** - Basic CLI functionality (interactive, dashboard, status, help)
- **Project** - Project management and scaffolding
- **AI** - AI-powered features and operations
- **Development** - Development acceleration tools
- **Unity** - Unity game development tools
- **Pipeline** - Workflow and pipeline management
- **Testing** - Testing and validation
- **Configuration** - Configuration management
- **Demo** - Demo and showcase commands
- **Agents** - AI agent management
- **Iteration** - Iteration strategy management
- **Adaptation** - Real-time adaptation management
- **Web** - Web development and optimization
- **Model** - AI model management
- **Policy** - Policy and compliance management
- **Verification** - Verification and validation

## 🚀 Usage

### Basic Command Execution

```bash
# Start Feature Lab
dotnet run -- feature-lab start --platform blazor

# Run validation
dotnet run -- validation run

# Showcase Feature Factory
dotnet run -- showcase factory --type web

# Generate frontend application
dotnet run -- frontend generate "E-commerce app" --type mobile
```

### Command Discovery

```bash
# List all commands
dotnet run -- discover list

# List commands by category
dotnet run -- discover list --category showcase

# Search for commands
dotnet run -- discover search feature
```

### Command Orchestration

```bash
# Run command sequence
dotnet run -- orchestrate sequence validation run showcase all

# Run predefined workflow
dotnet run -- orchestrate workflow full-demo

# Run workflow with parameters
dotnet run -- orchestrate workflow frontend-generation
```

## 🔄 Command Composition

### Sequential Execution

Commands can be executed in sequence:

```bash
dotnet run -- orchestrate sequence \
  validation run \
  feature-lab start --platform blazor \
  showcase factory --type web \
  showcase smart-reply \
  showcase contract-summary
```

### Workflow Execution

Predefined workflows combine multiple commands:

```yaml
full-demo:
  - validation run
  - feature-lab start --platform blazor
  - showcase all

quick-showcase:
  - showcase factory --type web
  - showcase smart-reply
  - showcase contract-summary

frontend-generation:
  - frontend generate "E-commerce app" --type web
  - frontend generate "Mobile banking app" --type mobile
  - frontend generate "Desktop productivity app" --type desktop
```

### Conditional Execution (Future)

```bash
# Conditional execution based on results
dotnet run -- orchestrate conditional \
  validation run --if env-check \
  showcase all --if validation-pass

# Parallel execution
dotnet run -- orchestrate parallel \
  showcase factory \
  showcase smart-reply \
  showcase contract-summary
```

## 🎭 Demo Commands

### Feature Lab Commands

```bash
# Start Feature Lab
dotnet run -- feature-lab start --platform blazor --port 5000
dotnet run -- feature-lab start --platform maui
dotnet run -- feature-lab start --platform console

# Check status
dotnet run -- feature-lab status

# Stop Feature Lab
dotnet run -- feature-lab stop
```

### Validation Commands

```bash
# Full validation
dotnet run -- validation run

# Skip tests
dotnet run -- validation run --skip-tests

# Environment check only
dotnet run -- validation env

# Dependencies check only
dotnet run -- validation deps
```

### Showcase Commands

```bash
# Complete showcase
dotnet run -- showcase all
dotnet run -- showcase all --interactive

# Individual showcases
dotnet run -- showcase factory --type web
dotnet run -- showcase factory --type mobile
dotnet run -- showcase smart-reply
dotnet run -- showcase contract-summary
```

### Frontend Generation

```bash
# Generate different frontend types
dotnet run -- frontend generate "E-commerce platform" --type web --output ./web-app
dotnet run -- frontend generate "Banking app" --type mobile --output ./mobile-app
dotnet run -- frontend generate "Productivity suite" --type desktop --output ./desktop-app
dotnet run -- frontend generate "CLI tool" --type console --output ./cli-app
dotnet run -- frontend generate "2D platformer" --type game --output ./game-app

# List available types
dotnet run -- frontend list-types
```

## 🔍 Command Discovery

### List Commands

```bash
# All commands
dotnet run -- discover list

# By category
dotnet run -- discover list --category showcase
dotnet run -- discover list --category frontend
dotnet run -- discover list --category validation
```

### Search Commands

```bash
# Search by name
dotnet run -- discover search feature

# Search by description
dotnet run -- discover search validation

# Search by category
dotnet run -- discover search showcase
```

## 🎯 Benefits

### 1. **Unified Interface**
- Single entry point for all commands
- Consistent command structure
- Centralized help and documentation

### 2. **Command Discovery**
- Easy to find available commands
- Search functionality
- Categorized command listing

### 3. **Command Orchestration**
- Sequential command execution
- Predefined workflows
- Complex multi-step operations

### 4. **Extensibility**
- Easy to add new command categories
- Simple command registration
- Flexible command composition

### 5. **Maintainability**
- Centralized command management
- Consistent error handling
- Unified logging and monitoring

## 🛠️ Implementation

### Adding New Commands

1. **Create Command Class**
```csharp
public class MyCommand
{
    public Command CreateMyCommand()
    {
        var command = new Command("my-command", "My command description");
        // Add subcommands and options
        return command;
    }
}
```

2. **Register in Aggregator**
```csharp
private void InitializeCommandCategories()
{
    var myCommands = new CommandCategory("my", "My command category");
    myCommands.AddCommand("do-something", "Do something useful", "my do-something <param>");
    _commandCategories["my"] = myCommands;
}
```

3. **Add to Root Command**
```csharp
rootCommand.AddCommand(myCommand.CreateMyCommand());
```

### Adding New Workflows

```csharp
private Dictionary<string, Workflow> GetPredefinedWorkflows()
{
    return new Dictionary<string, Workflow>
    {
        ["my-workflow"] = new Workflow
        {
            Name = "My Workflow",
            Description = "My custom workflow",
            Commands = new[]
            {
                "my-command do-something",
                "other-command execute",
                "final-command complete"
            }
        }
    };
}
```

## 📊 Command Statistics

The aggregator provides insights into command usage:

- **Total Commands**: 50+ commands across 15 categories
- **Command Categories**: 15 logical groupings
- **Predefined Workflows**: 4 ready-to-use workflows
- **Frontend Types**: 5 supported frontend types
- **Demo Scenarios**: 10+ demo scenarios

## 🎉 Conclusion

The Central Command Aggregator demonstrates how complex command-line interfaces can be organized, discovered, and orchestrated. It provides:

- **Unified access** to all project commands
- **Intelligent discovery** of available functionality
- **Powerful orchestration** of complex workflows
- **Extensible architecture** for future growth

This system makes the Nexo project more accessible and easier to use, while providing a foundation for advanced command composition and automation.
