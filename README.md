# Nexo

![Build](https://github.com/IanFrelinger/Nexo/actions/workflows/build.yml/badge.svg)
![Tests](https://github.com/IanFrelinger/Nexo/actions/workflows/tests.yml/badge.svg)
![Analyzers](https://github.com/IanFrelinger/Nexo/actions/workflows/analyzers.yml/badge.svg)
![Coverage](https://github.com/IanFrelinger/Nexo/actions/workflows/coverage.yml/badge.svg)

**Agent-First .NET Development Framework with Comprehensive Tooling**

Nexo is a modern, agent-first development framework built for .NET that provides intelligent automation, comprehensive tooling, and robust architectural patterns. It combines AI agents, development tools, and policy enforcement to create a complete development platform.

⸻

## Key Features

- **Agent-First Architecture**: AI agents as first-class citizens with cross-platform support
- **CLI Tool**: Packable dotnet tool with subcommands, JSON output, and rich help
- **Development Tools**: Comprehensive tooling for build, test, file operations, and git workflows
- **Policy Enforcement**: Security and workflow policies with configurable rules
- **Clean Architecture**: Hexagonal architecture with enforced layering rules
- **Assembly Analysis**: Advanced .NET assembly analysis, decompilation, and security scanning
- **TDD Workflows**: Built-in Test-Driven Development support with intelligent agents
- **Quality Gates**: Comprehensive testing, linting, and architectural validation
- **Contract Testing**: Idempotency, timeout, and policy enforcement testing framework
- **Fast Development**: Solution filters for rapid iteration and focused builds
- **🎨 Framework-Agnostic UI Primitives**: Cross-framework pattern extraction and reuse system

⸻

## 🎨 Framework-Agnostic UI Primitives System

A production-quality demonstration of cross-framework pattern extraction and reuse.

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Design Tokens Layer                      │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐  │
│  │   Colors        │  │   Typography    │  │   Spacing   │  │
│  │   (Semantic)    │  │   (Hierarchy)   │  │   (4pt Grid)│  │
│  └─────────────────┘  └─────────────────┘  └─────────────┘  │
└─────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────┐
│                 Primitives Layer (Nexo.Core.UI)             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐  │
│  │   Button        │  │   Input         │  │   Card      │  │
│  │   (Variants)    │  │   (Types)       │  │   (Layouts) │  │
│  └─────────────────┘  └─────────────────┘  └─────────────┘  │
└─────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────┐
│                Framework-Specific Renderers                  │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐  │
│  │   Avalonia      │  │   Unity Editor  │  │   Future   │  │
│  │   (XAML)        │  │   (IMGUI)       │  │   (WPF/MAUI)│  │
│  └─────────────────┘  └─────────────────┘  └─────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### Key Metrics

| Metric | Value |
|--------|-------|
| Frameworks Supported | 2 (Avalonia, Unity) |
| Primitives Available | 3 (Button, Input, Card) |
| Code Reuse | ~80% |
| Development Time Saved | ~60% for subsequent frameworks |
| Design Tokens | 50+ semantic color/typography tokens |
| Test Coverage | 42 passing tests across 5 projects |

### What This Proves

This system demonstrates that:
- **Framework-agnostic pattern extraction is viable** - Common UI patterns can be abstracted across vastly different frameworks
- **Cross-framework code generation maintains consistency** - Identical behavior and styling across platforms
- **Design systems can scale across different paradigms** - XAML (Avalonia) vs IMGUI (Unity) vs future frameworks
- **Manual process took ~20 hours - automation would reduce to <1 hour** - Proving the ROI for AI-powered "Forge" system

### Framework Support

- **✅ Avalonia**: Cross-platform desktop (XAML-based)
- **✅ Unity Editor**: Game engine editor (IMGUI-based)  
- **🔄 WPF**: Windows desktop (planned)
- **🔄 MAUI**: Cross-platform mobile/desktop (planned)
- **🔄 Web/React**: Browser-based (planned)

See [DESIGN_DECISIONS.md](docs/DESIGN_DECISIONS.md) for detailed architecture analysis and [METRICS.md](docs/METRICS.md) for quantified development metrics.

⸻

## Architecture

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
│   ├── Nexo.Tests.Contracts/       # Contract tests (idempotency, timeouts, policies)
│   ├── Nexo.Tests.Integration/     # Integration tests
│   └── [other test projects]
└── docs/                           # Comprehensive documentation
```

⸻

## Quick Start

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

### DirectorStudio Quickstart
- Open Unity → **Nexo → Director Runbook**, set Prompt/Seed → Run.
- CLI: `scripts/run-with-config.sh` (uses `nexo.pipeline.json`).
- CI: `scripts/ci-verify.sh` (JUnit XML produced by UTF or smoke).

## Review Mode Quickstart

```bash
# Always-green review run (creates artifacts + JUnit + summary)
./scripts/run-for-review.sh

# Aggregate multi-seed JUnit into one file for CI
./scripts/aggregate-junit.sh

# Build the presentation bundle
./scripts/present-bundle.sh
```

**Strict PR CI:**
```bash
./scripts/ci-verify.sh
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
# Install the CLI tool globally
dotnet tool install --global Nexo.CLI

# Show help
nexo --help

# Analyze code and policies
nexo analyze --path . --format-json

# Run architecture validation
nexo validate --filter "Category=Architecture"

# Run an agent
nexo agent --name CodeWriter --input request.json --format-json

# Run development agent in heal mode
dotnet run --project src/Nexo.Demo.DevCLI -- mode heal

# Run development agent in extend mode (TDD)
dotnet run --project src/Nexo.Demo.DevCLI -- mode extend
```

⸻

## CLI Tool

Nexo provides a powerful command-line interface that can be installed as a global dotnet tool.

### Installation

```bash
# Install globally
dotnet tool install --global Nexo.CLI

# Verify installation
nexo --version
```

### Commands

#### `nexo analyze`
Run code/assembly analyzers and policies with comprehensive validation.

```bash
# Analyze current directory
nexo analyze

# Analyze specific path
nexo analyze --path /path/to/project

# Get JSON output for CI/automation
nexo analyze --format-json
```

#### `nexo validate`
Run architecture tests and contract checks quickly.

```bash
# Run all validations
nexo validate

# Filter by test category
nexo validate --filter "Category=Architecture"

# Get JSON output
nexo validate --format-json
```

#### `nexo agent`
Execute agent actions with optional input files.

```bash
# Run agent with name
nexo agent --name CodeWriter

# Run with input file
nexo agent --name CodeWriter --input request.json

# Get JSON output
nexo agent --name CodeWriter --format-json
```

### Exit Codes

- `0`: Success
- `2`: Validation failed
- `3`: Policy violation
- `10`: Unexpected error

### JSON Output

When using `--format-json`, the CLI outputs structured JSON:

```json
{
  "ok": true,
  "data": { "message": "No violations" },
  "error": null
}
```

⸻

## AI Agents

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

## Development Tools

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

## Policy System

### Development Policies

- **PathAllowlist**: Restrict file operations to allowed paths
- **MaxWriteSize**: Limit file write sizes for security
- **BuildMustPassBeforeCommit**: Ensure builds pass before commits

### Policy Features

- **Configurable Rules**: Easy-to-configure policy rules
- **Runtime Enforcement**: Policies enforced during agent execution
- **Audit Logging**: Complete audit trail of policy decisions

⸻

## Architecture Validation

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

## Documentation

- **[Architecture Guide](docs/architecture.md)**: Detailed architectural patterns
- **[API Reference](docs/api/)**: Complete API documentation
- **[Examples](src/Nexo.Examples/)**: Working code examples
- **[Contributing Guide](docs/contributing.md)**: How to contribute to Nexo

⸻

## Testing

### Test Categories

- **Unit Tests**: Individual component testing
- **Integration Tests**: Cross-component testing
- **Architecture Tests**: Architectural rule validation (18 comprehensive tests)
- **Contract Tests**: Behavioral contracts (idempotency, timeouts, policies)
- **End-to-End Tests**: Complete workflow testing

### Contract Testing

Contract tests ensure behavioral guarantees across the system:

- **Idempotency**: Operations produce the same result when run multiple times
- **Timeout Enforcement**: Operations respect configured timeouts
- **Policy Compliance**: Tools and agents respect security and workflow policies
- **Audit Logging**: All operations generate proper audit trails
- **Binary File Protection**: Prevents overwriting of binary files (.dll, .exe, etc.)
- **Write Size Limits**: Enforces maximum write sizes per operation

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

Nexo includes solution filters for rapid development iteration:

```bash
# CLI-focused development (includes CLI + core dependencies)
dotnet build Nexo.cli.slnf -c Debug
dotnet test Nexo.cli.slnf

# Core libraries only (abstractions, runtime, application, domain)
dotnet build Nexo.core.slnf -c Debug
dotnet test Nexo.core.slnf

# All tests (architecture, contracts, integration, unit)
dotnet test Nexo.tests.slnf

# Run specific test categories with filters
dotnet test Nexo.tests.slnf --filter "Category=Architecture"
dotnet test Nexo.tests.slnf --filter "Category=Contract"
```

### Development Workflow

```bash
# Quick CLI development cycle
dotnet build Nexo.cli.slnf && dotnet test Nexo.cli.slnf

# Focus on core functionality
dotnet build Nexo.core.slnf && dotnet test Nexo.core.slnf

# Full test suite before commit
dotnet test Nexo.tests.slnf --collect:"XPlat Code Coverage"
```

⸻

## CI/CD & Quality Gates

Nexo enforces comprehensive quality gates through automated CI/CD pipelines.

### Automated Workflows

- **Build & Test**: Multi-platform testing with coverage collection
- **Code Analysis**: Static analysis, formatting, and style enforcement
- **Architecture Validation**: Automated architectural rule enforcement
- **Commit Validation**: Conventional commit format enforcement
- **CLI Publishing**: Automated NuGet package publishing on tags

### Quality Gates

- **✅ 18 Architecture Tests**: Enforce layering rules, single ownership, type-value system
- **✅ Warnings as Errors**: Production code must be warning-free
- **✅ Public API Protection**: Track and validate API surface changes
- **✅ Coverage Threshold**: 80% minimum code coverage requirement
- **✅ Contract Testing**: Idempotency, timeout, and policy enforcement
- **✅ Security Scanning**: Automated security vulnerability detection

### GitHub Actions

```yaml
# Required checks for PRs
- build-and-test.yml    # Build, test, coverage
- analyzers.yml         # Code analysis, formatting
- architecture.yml      # Architecture validation
- commitlint.yml        # Commit message validation
```

### Local Development

```bash
# Run all quality gates locally
dotnet build -warnaserror
dotnet test --collect:"XPlat Code Coverage"
dotnet format --verify-no-changes
npx commitlint --from HEAD~1 --to HEAD --verbose
```

⸻

## Contributing

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

## License

MIT License - see [LICENSE](LICENSE) file for details.

⸻

## Support

- **Issues**: [GitHub Issues](https://github.com/IanFrelinger/Nexo/issues)
- **Discussions**: [GitHub Discussions](https://github.com/IanFrelinger/Nexo/discussions)
- **Documentation**: [Project Wiki](https://github.com/IanFrelinger/Nexo/wiki)

⸻

## Roadmap

### ✅ Completed
- [x] CLI tool with subcommands and JSON output
- [x] Contract testing framework
- [x] Solution filters for fast development
- [x] Comprehensive CI/CD pipeline
- [x] Architecture validation (18 tests)
- [x] Quality gates and code analysis

### In Progress
- [ ] Real implementation of CLI commands
- [ ] Advanced contract test implementations
- [ ] Multi-targeting support (netstandard2.1)

### Planned
- [ ] Enhanced AI agent capabilities
- [ ] Web-based development interface
- [ ] Plugin system for custom tools
- [ ] Cloud-based agent execution
- [ ] dotnet new templates
- [ ] Integration with popular IDEs
- [ ] Advanced security scanning features

---

Built for the .NET community