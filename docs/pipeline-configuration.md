# Pipeline Configuration

The Nexo Framework provides a comprehensive pipeline configuration system that allows you to define, manage, and execute complex workflows through configuration files, command-line arguments, and templates.

## Overview

Pipeline configurations define how commands, behaviors, and aggregators work together to accomplish development tasks. They support:

- **File-based configuration** - JSON configuration files
- **Command-line configuration** - Direct command-line parameter specification
- **Template system** - Pre-built configuration templates
- **Environment-specific settings** - Different configurations for development, production, etc.
- **Validation rules** - Comprehensive validation of configurations
- **Documentation generation** - Automatic documentation for configurations

## Quick Start

### Using Templates

The easiest way to get started is using built-in templates:

```bash
# List available templates
nexo pipeline list-templates

# Show template documentation
nexo pipeline show-template webapi

# Create configuration from template
nexo pipeline create webapi --param projectName=MyApi --param solutionName=MySolution --output my-pipeline.json
```

### Using Configuration Files

Create a pipeline configuration file and use it:

```bash
# Validate a configuration file
nexo pipeline validate --config my-pipeline.json

# Execute a configuration
nexo pipeline execute --config my-pipeline.json --env development
```

## Configuration Structure

A pipeline configuration consists of several key sections:

### Basic Information

```json
{
  "name": "My Pipeline",
  "version": "1.0.0",
  "description": "Description of what this pipeline does",
  "author": "Your Name",
  "tags": ["webapi", "aspnet", "container"]
}
```

### Execution Settings

```json
{
  "execution": {
    "maxParallelExecutions": 4,
    "commandTimeoutMs": 30000,
    "behaviorTimeoutMs": 60000,
    "aggregatorTimeoutMs": 120000,
    "maxRetries": 3,
    "retryDelayMs": 1000,
    "enableDetailedLogging": true,
    "enablePerformanceMonitoring": true,
    "enableExecutionHistory": true,
    "maxExecutionHistoryEntries": 100,
    "enableParallelExecution": true,
    "enableDependencyResolution": true,
    "enableResourceManagement": true,
    "maxMemoryUsageBytes": 1073741824,
    "maxCpuUsagePercentage": 90.0
  }
}
```

### Commands

Commands define individual operations:

```json
{
  "commands": [
    {
      "id": "init-project",
      "name": "Initialize Project",
      "description": "Initialize a new project",
      "category": "Project",
      "priority": "High",
      "parameters": {
        "template": "webapi",
        "projectName": "MyProject"
      },
      "dependencies": [],
      "canExecuteInParallel": false,
      "timeoutMs": 60000,
      "retry": {
        "maxRetries": 2,
        "delayMs": 2000,
        "backoffMultiplier": 1.5,
        "maxDelayMs": 10000
      },
      "validation": ["project-name-required"]
    }
  ]
}
```

### Behaviors

Behaviors group commands together:

```json
{
  "behaviors": [
    {
      "id": "project-setup",
      "name": "Project Setup",
      "description": "Complete project setup workflow",
      "executionStrategy": "Sequential",
      "commands": ["init-project", "add-container", "add-tests"],
      "dependencies": [],
      "conditions": ["project-not-exists"]
    }
  ]
}
```

### Aggregators

Aggregators orchestrate behaviors:

```json
{
  "aggregators": [
    {
      "id": "full-pipeline",
      "name": "Full Pipeline",
      "description": "Complete project creation pipeline",
      "executionStrategy": "Sequential",
      "behaviors": ["project-setup", "quality-check"],
      "dependencies": [],
      "resourceRequirements": {
        "minMemoryBytes": 536870912,
        "maxMemoryBytes": 2147483648,
        "minCpuCores": 2,
        "maxCpuCores": 8,
        "requiredDiskSpaceBytes": 1073741824
      }
    }
  ]
}
```

### Variables

Variables provide parameterization:

```json
{
  "variables": {
    "projectName": "MyProject",
    "solutionName": "MySolution",
    "containerRuntime": "docker",
    "testFramework": "xunit"
  }
}
```

### Environments

Environment-specific configurations:

```json
{
  "environments": {
    "development": {
      "variables": {
        "enableDebug": true,
        "logLevel": "Debug"
      },
      "execution": {
        "maxParallelExecutions": 2,
        "enableDetailedLogging": true
      },
      "commandOverrides": {
        "analyze-code": {
          "parameters": {
            "severity": "info"
          }
        }
      }
    },
    "production": {
      "variables": {
        "enableDebug": false,
        "logLevel": "Information"
      },
      "execution": {
        "maxParallelExecutions": 8,
        "enableDetailedLogging": false
      }
    }
  }
}
```

### Validation Rules

Validation rules ensure configuration correctness:

```json
{
  "validation": {
    "rules": [
      {
        "name": "project-name-required",
        "description": "Project name must be specified",
        "type": "required-field",
        "parameters": {
          "field": "projectName",
          "message": "Project name is required"
        },
        "severity": "Error"
      }
    ],
    "failOnError": true,
    "timeoutMs": 30000
  }
}
```

### Documentation

Documentation for the configuration:

```json
{
  "documentation": {
    "summary": "Brief description of the pipeline",
    "details": "Detailed description of what the pipeline does",
    "examples": [
      "nexo pipeline create webapi --projectName MyApi",
      "nexo pipeline validate --config pipeline.json"
    ],
    "tags": ["webapi", "aspnet", "container"],
    "links": [
      "https://docs.microsoft.com/en-us/aspnet/core/web-api/"
    ]
  }
}
```

## Built-in Templates

The framework includes several built-in templates:

### WebAPI Template

Creates a complete Web API project with containerization and testing:

```bash
nexo pipeline create webapi --param projectName=MyApi --param solutionName=MySolution
```

**Features:**
- ASP.NET Core Web API project
- Docker containerization
- Unit test project
- Proper project structure

### Console Template

Creates a console application with logging:

```bash
nexo pipeline create console --param projectName=MyConsole --param solutionName=MySolution
```

**Features:**
- Console application project
- Structured logging
- Command-line argument parsing

### Library Template

Creates a class library with NuGet support:

```bash
nexo pipeline create library --param projectName=MyLibrary --param packageId=MyCompany.MyLibrary
```

**Features:**
- Class library project
- Unit test project
- NuGet package configuration

### Test Template

Creates a test project with xUnit:

```bash
nexo pipeline create test --param projectName=MyTests --param solutionName=MySolution
```

**Features:**
- xUnit test project
- Common testing dependencies
- Test project structure

### Analysis Template

Performs comprehensive code analysis:

```bash
nexo pipeline create analysis --param projectPath=./src --param testFilter=*Tests
```

**Features:**
- Static code analysis
- Unit test execution
- Quality report generation

## Command-Line Usage

### Pipeline Configuration Commands

```bash
# List available templates
nexo pipeline list-templates

# Show template documentation
nexo pipeline show-template <template-name>

# Create configuration from template
nexo pipeline create <template-name> [options]

# Validate configuration
nexo pipeline validate [options]

# Save configuration to file
nexo pipeline save <path> [options]
```

### Options

- `--output <path>` - Output path for configuration file
- `--param <key=value>` - Template parameter
- `--config <path>` - Configuration file path
- `--env <environment>` - Environment name

### Examples

```bash
# Create WebAPI configuration
nexo pipeline create webapi --param projectName=MyApi --param solutionName=MySolution --output my-pipeline.json

# Validate configuration
nexo pipeline validate --config my-pipeline.json

# Create with custom parameters
nexo pipeline create webapi --param projectName=MyApi --param containerRuntime=podman --param testFramework=nunit

# Show template help
nexo pipeline show-template webapi
```

## Advanced Features

### Environment-Specific Configuration

Use different configurations for different environments:

```bash
# Use development environment
nexo pipeline execute --config pipeline.json --env development

# Use production environment
nexo pipeline execute --config pipeline.json --env production
```

### Configuration Merging

Merge multiple configuration sources:

```bash
# Merge file and command-line configurations
nexo pipeline execute --config base.json --config overrides.json --param debug=true
```

### Validation

Comprehensive validation ensures configuration correctness:

```bash
# Validate configuration
nexo pipeline validate --config pipeline.json

# Validate with specific rules
nexo pipeline validate --config pipeline.json --rules strict
```

### Documentation Generation

Generate documentation for configurations:

```bash
# Generate documentation
nexo pipeline docs --config pipeline.json --output docs/

# Generate markdown documentation
nexo pipeline docs --config pipeline.json --format markdown --output README.md
```

## Best Practices

### 1. Use Templates for Common Patterns

Start with built-in templates and customize as needed:

```bash
# Start with template
nexo pipeline create webapi --param projectName=MyApi

# Customize the generated configuration
# Edit the generated JSON file
# Use the customized configuration
```

### 2. Organize by Environment

Use environment-specific configurations:

```json
{
  "environments": {
    "development": {
      "variables": { "debug": true, "logLevel": "Debug" }
    },
    "staging": {
      "variables": { "debug": false, "logLevel": "Information" }
    },
    "production": {
      "variables": { "debug": false, "logLevel": "Warning" }
    }
  }
}
```

### 3. Validate Early and Often

Validate configurations before execution:

```bash
# Validate during development
nexo pipeline validate --config pipeline.json

# Validate before deployment
nexo pipeline validate --config pipeline.json --strict
```

### 4. Use Meaningful Names and Descriptions

Make configurations self-documenting:

```json
{
  "name": "E-commerce API Pipeline",
  "description": "Creates and configures a complete e-commerce API with authentication, database, and testing",
  "commands": [
    {
      "id": "create-api",
      "name": "Create E-commerce API",
      "description": "Creates the main API project with proper structure and dependencies"
    }
  ]
}
```

### 5. Leverage Variables for Reusability

Use variables to make configurations reusable:

```json
{
  "variables": {
    "projectName": "{{projectName}}",
    "solutionName": "{{solutionName}}",
    "databaseConnection": "{{databaseConnection}}"
  }
}
```

## Troubleshooting

### Common Issues

1. **Template Not Found**
   ```bash
   Error: Template 'unknown-template' not found
   ```
   **Solution:** Use `nexo pipeline list-templates` to see available templates.

2. **Validation Errors**
   ```bash
   Error: Configuration validation failed
   ```
   **Solution:** Check the validation output for specific errors and fix them.

3. **Missing Parameters**
   ```bash
   Error: Required parameter 'projectName' not provided
   ```
   **Solution:** Provide all required parameters using `--param` options.

4. **Environment Not Found**
   ```bash
   Error: Environment 'production' not found in configuration
   ```
   **Solution:** Check the `environments` section of your configuration file.

### Debug Mode

Enable debug mode for more detailed information:

```bash
nexo pipeline create webapi --param projectName=MyApi --debug
```

### Logging

Enable detailed logging:

```bash
nexo pipeline create webapi --param projectName=MyApi --log-level Debug
```

## Examples

See the `examples/` directory for complete configuration examples:

- `pipeline-config-example.json` - Comprehensive example showing all features
- `webapi-pipeline.json` - WebAPI-specific configuration
- `microservice-pipeline.json` - Microservice architecture configuration

## API Reference

For detailed API documentation, see the generated documentation or refer to the source code in:

- `src/Nexo.Core.Application/Models/Pipeline/` - Configuration models
- `src/Nexo.Core.Application/Interfaces/Pipeline/` - Service interfaces
- `src/Nexo.Infrastructure/Services/Pipeline/` - Service implementations 