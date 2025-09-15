# Nexo - AI-Powered Code Generation Platform

## 🚀 Overview

Nexo is a comprehensive AI-powered code generation platform that transforms natural language descriptions into executable C# tools. Built with Clean Architecture principles, it provides a complete pipeline from AI model integration to tool compilation, execution, and maintenance.

## ✨ Key Features

### 🤖 AI-Powered Code Generation
- **Multi-Provider Support**: OpenAI, Ollama (local), Azure OpenAI
- **Natural Language Processing**: Convert descriptions to compilable C# code
- **Intelligent Orchestration**: Automatic provider selection and fallback
- **Context-Aware Generation**: Maintains conversation context across sessions

### 🔧 Tool Lifecycle Management
- **Dynamic Compilation**: Roslyn-based C# compilation to executable assemblies
- **Plugin System**: Hot-reloadable tool plugins with dependency injection
- **Tool Persistence**: Save and retrieve generated tools across sessions
- **Tool Evolution**: Modify and improve existing tools over time
- **Tool Discovery**: List, search, and manage generated tools

### 🛡️ Safety & Quality Assurance
- **Enhanced Safety Validation**: Proactive checks for malicious patterns and security vulnerabilities
- **Code Quality Analysis**: Automated assessment with scoring and quality gates
- **Policy Engine**: Data-driven safety and quality rules with YAML configuration
- **Guided Generation**: Step-by-step workflow to ensure proper tool requirements

## 🏛️ Architecture

```
Nexo/
├── src/                          # Source code
│   ├── Nexo.Core.Domain/         # Domain entities, value objects, interfaces
│   ├── Nexo.Core.Application/    # Application services and use cases
│   ├── Nexo.Infrastructure/      # Infrastructure implementations
│   ├── Nexo.CLI/                 # Command-line interface
│   └── Nexo.Feature.*/          # Feature modules (AI, Analysis, Pipeline, etc.)
├── tests/                        # Test projects
├── policies/                     # Safety and quality policies
│   ├── safety/                   # Safety rules and allowlists
│   ├── quality/                  # Quality gates and scoring
│   └── schemas/                  # JSON schema validation
├── docker/                       # Containerization files
├── scripts/                      # Build and deployment scripts
└── examples/                     # Configuration examples
```

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK
- Docker (for container features)
- Git

### Installation
```bash
# Clone the repository
git clone https://github.com/IanFrelinger/Nexo.git
cd Nexo

# Restore dependencies
dotnet restore

# Build the project
dotnet build
```

### Basic Usage

#### Interactive Chat Mode
```bash
# Start interactive chat for tool generation
dotnet run --project src/Nexo.CLI

# Or use the nexo command (if installed)
nexo chat
```

#### Generate a Tool
```bash
# Generate a JSON formatter tool
nexo generate "Create a JSON formatter that takes a file path and pretty-prints the JSON"

# Generate with specific AI provider
nexo generate "Create a file organizer" --provider ollama --model llama2
```

#### Tool Management
```bash
# List all generated tools
nexo tools list

# Execute a generated tool
nexo tools run json-formatter input.json

# Evolve an existing tool
nexo tools evolve json-formatter "Add support for minification"
```

## 🔧 Configuration

### AI Provider Setup

#### OpenAI
```bash
export OPENAI_API_KEY="your-api-key"
export AI_PROVIDER=openai
export AI_MODEL=gpt-4
```

#### Ollama (Local)
```bash
# Install Ollama
curl -fsSL https://ollama.ai/install.sh | sh

# Pull a model
ollama pull llama2

# Configure Nexo
export AI_PROVIDER=ollama
export AI_MODEL=llama2
```

#### Azure OpenAI
```bash
export AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com/"
export AZURE_OPENAI_API_KEY="your-api-key"
export AI_PROVIDER=azure-openai
export AI_MODEL=gpt-4
```

### Policy Configuration

Nexo includes a comprehensive policy system for safety and quality:

```bash
# Run safety scan
nexo safety scan --policy policies/safety/default.yaml

# Run quality checks
nexo quality run --policy policies/quality/default.yaml --format sarif

# Apply complete policy pack
nexo policy apply --manifest policies/policy-pack.manifest.yaml
```

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/Nexo.Infrastructure.Tests/
```

## 🐳 Docker Support

```bash
# Build and run with Docker
docker-compose up --build

# Run tests in container
docker-compose -f docker-compose.testing.yml up --build
```


## 🔒 Security & Safety

### Safety Features
- **Sandboxed Execution**: Restricted filesystem and network access
- **Secret Detection**: Automatic detection of API keys and credentials
- **Malicious Code Detection**: Pattern-based security scanning
- **Network Restrictions**: Configurable domain and port allowlists

### Quality Assurance
- **Code Quality Scoring**: Automated assessment with configurable thresholds
- **Test Coverage**: Minimum 75% coverage requirement
- **Style Enforcement**: Consistent code formatting and standards
- **Dependency Auditing**: Security vulnerability scanning

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/amazing-feature`
3. Make your changes and add tests
4. Run the test suite: `dotnet test`
5. Run policy checks: `nexo policy apply`
6. Commit your changes: `git commit -m 'Add amazing feature'`
7. Push to the branch: `git push origin feature/amazing-feature`
8. Open a Pull Request

## 📄 License

[Add your license information here]

## 🆘 Support

- **Documentation**: Check the `docs/` directory for detailed guides
- **Issues**: Report bugs and request features on GitHub
- **Discussions**: Join community discussions for questions and ideas

## 🗺️ Roadmap

- **Enhanced AI Models**: Support for more AI providers and models
- **Visual Tool Builder**: GUI for tool creation and management
- **Team Collaboration**: Multi-user tool sharing and versioning
- **Enterprise Features**: Advanced security and compliance tools
- **Cloud Integration**: Seamless cloud deployment and scaling

---

**Nexo** - Transform your ideas into code with the power of AI! 🚀