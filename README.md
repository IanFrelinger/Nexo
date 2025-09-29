# Nexo

**Agent-First .NET Development Framework with Comprehensive Tooling**

Nexo is a modern, agent-first development framework built for .NET that provides intelligent automation, comprehensive tooling, and robust architectural patterns. It combines AI agents, development tools, and policy enforcement to create a complete development platform.

⸻

## 🚀 Key Features

- **🤖 Agent-First Architecture**: AI agents as first-class citizens with cross-platform support
- **🛠️ Development Tools**: Comprehensive tooling for build, test, file operations, and git workflows
- **🔒 Policy Enforcement**: Security and workflow policies with configurable rules
- **🏗️ Clean Architecture**: Hexagonal architecture with enforced layering rules
- **📦 Assembly Analysis**: Advanced .NET assembly analysis, decompilation, and security scanning
- **🔄 TDD Workflows**: Built-in Test-Driven Development support with intelligent agents
- **✅ Quality Gates**: Comprehensive testing, linting, and architectural validation

⸻

## 🏗️ Architecture

### Core Components

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                       │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐  │
│  │   Nexo.CLI      │  │  Nexo.Demo.CLI  │  │  Web UI     │  │
│  └─────────────────┘  └─────────────────┘  └─────────────┘  │
└─────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────┐
│                  Application Layer                          │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐  │
│  │   Commands      │  │   Orchestrators │  │   Services  │  │
│  └─────────────────┘  └─────────────────┘  └─────────────┘  │
└─────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────┐
│                    Domain Layer                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐  │
│  │   AI Agents     │  │   Value Objects │  │   Entities  │  │
│  └─────────────────┘  └─────────────────┘  └─────────────┘  │
└─────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────┐
│                 Infrastructure Layer                        │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐  │
│  │   Tools         │  │   Policies      │  │   Runtime   │  │
│  └─────────────────┘  └─────────────────┘  └─────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### Project Structure

```
Nexo/
├── src/
│   ├── Nexo.Abstractions/          # Core interfaces and contracts
│   ├── Nexo.Runtime/               # Agent runtime and execution
│   ├── Nexo.Core.Application/      # Application layer (commands, orchestrators)
│   ├── Nexo.Core.Domain/           # Domain layer (agents, value objects)
│   ├── Nexo.Tools.Dev/             # Development tools (build, test, git)
│   ├── Nexo.Policies.Dev/          # Development policies (security, workflow)
│   ├── Nexo.Agents.Dev/            # Development agents (TDD, automation)
│   ├── Nexo.Tools.Assembly/        # Assembly analysis and decompilation
│   ├── Nexo.Demo.DevCLI/           # Demo CLI application
│   └── Nexo.Examples/              # Example implementations (non-packable)
├── tests/
│   ├── Nexo.Tests.Architecture/    # Architectural validation tests
│   ├── Nexo.Tests.Integration/     # Integration tests
│   └── [other test projects]
└── docs/                           # Comprehensive documentation
```

⸻

## 🚀 Quick Start

### Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 or VS Code
- Git

### Installation

#### CLI (dotnet tool)

```bash
# Install Nexo CLI globally
dotnet tool install --global Nexo.CLI

# Verify installation
nexo --help

# Run commands
nexo analyze --path .
nexo validate --filter "Category=Architecture" --format-json
nexo agent --name CodeWriter --input ./requests/new_feature.json --format-json
```

#### Development Setup

```bash
# Clone the repository
git clone https://github.com/IanFrelinger/Nexo.git
cd Nexo

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test

# Run the demo CLI
dotnet run --project src/Nexo.Demo.DevCLI
```

### Basic Usage

```bash
# Show version information
dotnet run --project src/Nexo.CLI -- version

# Show help
dotnet run --project src/Nexo.CLI -- help

# Analyze an assembly
dotnet run --project src/Nexo.CLI -- assembly analyze --path MyAssembly.dll

# Run development agent in heal mode
dotnet run --project src/Nexo.Demo.DevCLI -- mode heal

# Run development agent in extend mode (TDD)
dotnet run --project src/Nexo.Demo.DevCLI -- mode extend
```

⸻

## 🤖 AI Agents

### Development Agents

- **DevDirectorAgent**: Orchestrates development workflows with heal/extend modes
- **TDD Agent**: Implements Test-Driven Development workflows
- **Code Generation Agent**: Generates code based on specifications
- **Security Analysis Agent**: Analyzes code for security vulnerabilities

### Agent Capabilities

- **Cross-Platform**: Native support for Windows, macOS, Linux
- **Tool Integration**: Seamless integration with development tools
- **Policy Enforcement**: Automatic policy compliance checking
- **Memory Management**: Persistent agent memory and context

⸻

## 🛠️ Development Tools

### Build & Test Tools

- **DotnetBuildTool**: Execute `dotnet build` commands
- **DotnetTestTool**: Run test suites with reporting
- **RepoFsEnsureFileTool**: Create files if missing (TDD support)
- **RepoFsWriteTool**: Write content to files
- **RepoGitCommitTool**: Git commit operations

### Assembly Analysis Tools

- **AssemblyAnalyzeTool**: Extract assembly metadata and dependencies
- **AssemblyDecompileTool**: Decompile .NET assemblies to source code
- **AssemblySecurityScanTool**: Security vulnerability scanning

⸻

## 🔒 Policy System

### Development Policies

- **PathAllowlist**: Restrict file operations to allowed paths
- **MaxWriteSize**: Limit file write sizes for security
- **BuildMustPassBeforeCommit**: Ensure builds pass before commits

### Policy Features

- **Configurable Rules**: Easy-to-configure policy rules
- **Runtime Enforcement**: Policies enforced during agent execution
- **Audit Logging**: Complete audit trail of policy decisions

⸻

## 🏗️ Architecture Validation

### Enforced Rules

- **Layering Rules**: Strict dependency layering between assemblies
- **Single Ownership**: One `ICommand` interface, one `AgentFactory` class
- **Type-Value System**: No enums in domain layer, use value objects
- **Examples Isolation**: Example code isolated in non-packable project
- **Duplicate Prevention**: No duplicate public type names across assemblies

### Quality Gates

- **Architecture Tests**: Automated architectural validation
- **Public API Protection**: API surface change tracking
- **Commit Hygiene**: Conventional commit format enforcement
- **Code Analysis**: Comprehensive static analysis

⸻

## 📚 Documentation

- **[Architecture Guide](docs/architecture.md)**: Detailed architectural patterns
- **[API Reference](docs/api/)**: Complete API documentation
- **[Examples](src/Nexo.Examples/)**: Working code examples
- **[Contributing Guide](docs/contributing.md)**: How to contribute to Nexo

⸻

## 🧪 Testing

### Test Categories

- **Unit Tests**: Individual component testing
- **Integration Tests**: Cross-component testing
- **Architecture Tests**: Architectural rule validation
- **Contract Tests**: Behavioral contracts (idempotency, timeouts, policies)
- **End-to-End Tests**: Complete workflow testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test categories
dotnet test --filter "Category=Architecture"
dotnet test --filter "Category=Contract"
dotnet test --filter "Category=Integration"
dotnet test --filter "Category=E2E"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run tests with solution filters for faster iteration
dotnet test Nexo.tests.slnf --filter "Category=Architecture"
dotnet test Nexo.cli.slnf --filter "Category=Contract"
```

### Solution Filters (Fast Development)

```bash
# CLI-focused development
dotnet build Nexo.cli.slnf -c Debug
dotnet test Nexo.cli.slnf

# Core libraries only
dotnet build Nexo.core.slnf -c Debug
dotnet test Nexo.core.slnf

# All tests
dotnet test Nexo.tests.slnf
```

⸻

## 🤝 Contributing

### Development Workflow

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Add tests for new functionality
5. Follow conventional commit format
6. Submit a pull request

### Commit Convention

This project uses [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

**Types**: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`

**Examples**:
```bash
feat: add new development tool for file operations
fix: resolve memory leak in agent orchestration
docs: update architecture documentation
test: add integration tests for CLI commands
ci: add commitlint validation to GitHub Actions
```

⸻

## 📄 License

MIT License - see [LICENSE](LICENSE) file for details.

⸻

## 🆘 Support

- **Issues**: [GitHub Issues](https://github.com/IanFrelinger/Nexo/issues)
- **Discussions**: [GitHub Discussions](https://github.com/IanFrelinger/Nexo/discussions)
- **Documentation**: [Project Wiki](https://github.com/IanFrelinger/Nexo/wiki)

⸻

## 🗺️ Roadmap

- [ ] Enhanced AI agent capabilities
- [ ] Web-based development interface
- [ ] Plugin system for custom tools
- [ ] Cloud-based agent execution
- [ ] Integration with popular IDEs
- [ ] Advanced security scanning features

---

**Built with ❤️ for the .NET community**