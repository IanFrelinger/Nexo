# Nexo - Development Environment Orchestration Platform

## Project Overview

Nexo is a comprehensive development environment orchestration platform that provides containerized development workflows, project initialization, code analysis, and agent-based automation capabilities.

## Architecture

This project follows Clean Architecture principles with clear separation of concerns:

```
Nexo/
├── src/                          # Source code
│   ├── Nexo.Core.Domain/         # Domain layer (entities, value objects, domain logic)
│   ├── Nexo.Core.Application/    # Application layer (use cases, interfaces, services)
│   └── Nexo.Infrastructure/      # Infrastructure layer (implementations, adapters)
├── tests/                        # Test projects
│   └── Nexo.Tests/               # Integration and unit tests
├── docs/                         # Documentation
│   ├── README.md                 # Documentation index
│   ├── DEVELOPMENT_PROGRESS.md   # Current development status
│   ├── PROJECT_TRACKING.md       # Strategic project tracking
│   ├── epics/                    # Epic completion summaries
│   ├── development-summaries/    # Development progress reports
│   ├── strategies/               # Strategic planning documents
│   └── logs/                     # Build and execution logs
├── scripts/                      # Shell scripts
│   ├── README.md                 # Scripts documentation
│   ├── build/                    # Build scripts
│   └── test/                     # Test scripts
└── docker/                       # Docker files
    ├── README.md                 # Docker documentation
    ├── docker-compose.yml        # Main application containerization
    ├── docker-compose.test-environments.yml # Testing environment orchestration
    └── Dockerfile.unity-test     # Unity testing environment container
```

## 📚 Documentation

For comprehensive documentation, see the [docs/](./docs/) directory:

- **[Documentation Index](./docs/README.md)** - Complete documentation overview
- **[Development Progress](./docs/DEVELOPMENT_PROGRESS.md)** - Current status and achievements
- **[Project Tracking](./docs/PROJECT_TRACKING.md)** - Strategic vision and roadmap
- **[Epic Summaries](./docs/epics/)** - Detailed feature implementation reports
- **[Development Summaries](./docs/development-summaries/)** - Progress reports and integrations
- **[Strategic Documents](./docs/strategies/)** - Planning and solution strategies

### 🔧 [Scripts](./scripts/)
Build and test automation scripts:
- **[Scripts Documentation](./scripts/README.md)** - Complete scripts overview
- **[Build Scripts](./scripts/build/)** - Build and compilation scripts
- **[Test Scripts](./scripts/test/)** - Testing and validation scripts

### 🐳 [Docker](./docker/)
Containerization and deployment files:
- **[Docker Documentation](./docker/README.md)** - Complete Docker overview
- **[Docker Compose Files](./docker/)** - Application and test environment orchestration
- **[Dockerfiles](./docker/)** - Container definitions

## Project Structure

### Core Domain Layer (`src/Nexo.Core.Domain/`)

Contains the core business entities, value objects, and domain logic:

- **Entities/**: Core business entities (Agent, Project, Sprint, SprintTask)
- **ValueObjects/**: Immutable value objects (AgentId, ProjectId, etc.)
- **Enums/**: Domain-specific enumerations
- **Exceptions/**: Domain-specific exceptions

### Core Application Layer (`src/Nexo.Core.Application/`)

Contains application logic, use cases, and interfaces organized by domain:

#### Interfaces/
- **Agent/**: Agent-related interfaces
- **Analysis/**: Code analysis interfaces
- **Caching/**: Caching system interfaces
- **Container/**: Container orchestration interfaces
- **Platform/**: Platform-specific interfaces
- **Plugin/**: Plugin system interfaces
- **Project/**: Project management interfaces
- **Template/**: Template system interfaces
- **Validation/**: Validation interfaces

#### Models/
- **Agent/**: Agent-related models
- **Analysis/**: Code analysis models
- **Container/**: Container-related models
- **Platform/**: Platform-specific models
- **Plugin/**: Plugin system models
- **Project/**: Project management models
- **Template/**: Template system models
- **Validation/**: Validation models

#### UseCases/
- **Agent/**: Agent-related use cases
- **Analysis/**: Code analysis use cases
- **Container/**: Container orchestration use cases
- **Project/**: Project management use cases
- **Template/**: Template system use cases

#### Services/
- **Agent/**: Agent-related services
- **Analysis/**: Code analysis services
- **Caching/**: Caching system services (CacheStrategy, CachingAsyncProcessor)
- **Container/**: Container orchestration services
- **Plugin/**: Plugin system services
- **Project/**: Project management services
- **Template/**: Template system services
- **Validation/**: Validation services

### Infrastructure Layer (`src/Nexo.Infrastructure/`)

Contains implementations, adapters, and external integrations:

#### Adapters/
- **Command/**: Command execution adapters
- **Configuration/**: Configuration management adapters
- **Container/**: Container orchestration adapters (Docker, Podman)
- **FileSystem/**: File system operation adapters
- **Platform/**: Platform-specific adapters

#### Services/
- **Agent/**: Agent service implementations
- **Analysis/**: Code analysis service implementations
- **Caching/**: Caching service implementations (RedisCacheStrategy)
- **Command/**: Command execution services
- **Plugin/**: Plugin system implementations
- **Project/**: Project management services
- **Template/**: Template system implementations
- **Validation/**: Validation service implementations

#### Repositories/
- **Project/**: Project data access implementations
- **Agent/**: Agent data access implementations
- **Sprint/**: Sprint data access implementations

#### Initializers/
- **Project/**: Project initialization implementations
- **Container/**: Container initialization implementations
- **Platform/**: Platform initialization implementations

#### Validators/
- **Command/**: Command validation implementations
- **Project/**: Project validation implementations
- **Template/**: Template validation implementations

## Key Features

### Container Orchestration
- Support for Docker and Podman
- Container lifecycle management
- Volume mounting and port mapping
- Development session management

### Project Management
- Project initialization workflows
- Template-based project creation
- Project configuration management
- Sprint and task tracking

### Code Analysis
- Static code analysis
- Architectural compliance checking
- Code improvement recommendations
- Issue categorization and prioritization

### Agent System
- Multi-agent architecture
- Role-based agent capabilities
- Agent communication and coordination
- Task delegation and execution

### Plugin System
- Extensible plugin architecture
- Plugin lifecycle management
- Service injection and dependency management
- Hot-reload capabilities

### Platform Abstraction
- Cross-platform support (Windows, macOS, Linux)
- Runtime environment detection
- Platform-specific optimizations
- Unified API across platforms

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- Docker or Podman (for container features)
- Git

### Building the Project
```bash
dotnet restore
dotnet build
```

### Running Tests
```bash
dotnet test
```

### Running the Application
```bash
dotnet run --project src/Nexo.Infrastructure
```

## Development Guidelines

### Architecture Principles
1. **Dependency Inversion**: High-level modules should not depend on low-level modules
2. **Single Responsibility**: Each class should have one reason to change
3. **Open/Closed**: Open for extension, closed for modification
4. **Interface Segregation**: Clients should not be forced to depend on interfaces they don't use
5. **Dependency Injection**: Use constructor injection for dependencies

### Code Organization
- Keep related functionality together
- Use clear, descriptive names for folders and files
- Group interfaces, models, and implementations by domain
- Maintain consistent naming conventions

### Testing Strategy
- Unit tests for domain logic
- Integration tests for use cases
- End-to-end tests for critical workflows
- Mock external dependencies

## Contributing

1. Follow the established architecture patterns
2. Add appropriate tests for new functionality
3. Update documentation for new features
4. Use meaningful commit messages
5. Create feature branches for significant changes

## Development Progress

For detailed information about our development progress, current status, and roadmap, see [DEVELOPMENT_PROGRESS.md](docs/DEVELOPMENT_PROGRESS.md).

### Recent Achievements
- ✅ **Cross-Platform Testing Integration** - Full CLI integration with multi-environment support
- ✅ **AI Integration** - Multi-provider support with intelligent orchestration
- ✅ **Container Orchestration** - Docker integration for development environments
- ✅ **Caching System** - Dual backend support with compositional design

### Current Focus
- 🔄 **Docker Execution Integration** - Implementing real Docker container execution for testing
- 🎯 **Smart Test Orchestration** - Intelligent test selection and parallel execution
- 📊 **Enhanced Reporting** - Interactive reports with CI/CD integration

## License

[Add your license information here] 

# Caching System

Nexo provides a flexible, compositional caching system that supports both in-memory and distributed Redis caching. The caching system is designed to be decoupled from core services using the decorator pattern.

## Features

- **Dual Backend Support**: In-memory and Redis caching
- **Compositional Design**: Caching is implemented as a decorator around `IAsyncProcessor`
- **Semantic Cache Keys**: Intelligent key generation based on normalized input and context
- **Configurable TTL**: Time-based expiration with configurable defaults
- **Graceful Degradation**: Automatic fallback when Redis is unavailable
- **Multi-target Compatibility**: Works with .NET 8, .NET Framework 4.8, and .NET Standard 2.0

## Quick Start

### Environment Configuration

```bash
# Use in-memory caching (default)
export CACHE_BACKEND=inmemory
export CACHE_TTL_SECONDS=300

# Use Redis caching
export CACHE_BACKEND=redis
export REDIS_CONNECTION_STRING=localhost:6379
export REDIS_KEY_PREFIX=nexo:cache:
export CACHE_TTL_SECONDS=600
```

### CLI Configuration

```bash
# Show current cache configuration
nexo config show

# Set cache backend and TTL
nexo config set-cache redis 600
```

### Usage in Code

```csharp
// Create configured cache strategy
var settings = new CacheSettings { Backend = "redis" };
var cacheService = new CacheConfigurationService(settings);
var cache = cacheService.CreateCacheStrategy<string, ModelResponse>();

// Compose with caching
var cachingProcessor = new CachingAsyncProcessor<ModelRequest, string, ModelResponse>(
    coreProcessor,
    cache,
    keySelector: request => SemanticCacheKeyGenerator.Generate(request.Input)
);

// Use the caching processor
var response = await cachingProcessor.ProcessAsync(request);
```

For detailed documentation, see [CACHING.md](docs/CACHING.md).

# AI Provider/Model Configuration and Usage

Nexo supports flexible configuration for AI model providers and models, allowing you to select and override your preferred provider/model at runtime.

## Configuration Methods

### 1. Environment Variables
Set your preferred provider and model globally for all CLI commands:

```
export AI_PROVIDER=openai
export AI_MODEL=gpt-4
```

### 2. CLI Options
Override preferences per command using `--provider` and `--model` options:

```
nexo analyze myfile.cs --ai --provider ollama --model llama2
nexo ai suggest "public class Foo {}" --provider openai --model gpt-3.5-turbo
```

If a CLI option is provided, it takes precedence over environment variables.

### 3. Fallback Logic
If no provider/model is specified, Nexo will automatically select the best available model for the requested task and type, using health checks and provider capabilities.

## Supported Providers and Models
- **OpenAI**: GPT-3.5, GPT-4, etc.
- **Ollama**: Local models (e.g., llama2, codellama)
- **Azure OpenAI**: Azure-hosted GPT models

You can add new providers by implementing the `IModelProvider` interface and registering them in the orchestrator.

## Example Usage

**Analyze code with AI (using environment variable defaults):**
```
nexo analyze myfile.cs --ai
```

**Analyze code with a specific provider/model:**
```
nexo analyze myfile.cs --ai --provider openai --model gpt-4
```

**Get AI-powered code suggestions:**
```
nexo ai suggest "public void Bar() {}" --provider ollama --model llama2
```

**Initialize a project with AI assistance:**
```
nexo project init MyProject --ai --provider azure-openai --model gpt-4
```

## How It Works
- The orchestrator registers all available providers at startup.
- When a command is run, Nexo checks for CLI options, then environment variables, then falls back to auto-selection.
- If the preferred provider/model is unavailable, Nexo will try other healthy providers.

## Extending Providers
To add a new provider:
1. Implement the `IModelProvider` interface.
2. Register your provider in the orchestrator (see `Program.cs`).
3. Optionally, document your provider in this section. 