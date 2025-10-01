# Director Studio

**Director Studio** is a Unity Editor tool that enables non-programmers to create game slices from natural-language briefs across any genre. Built on top of the Nexo agent-first orchestration framework, it provides offline AI capabilities for game development.

## 🎯 Features

### Core Capabilities
- **Natural Language Processing**: Convert game briefs into detailed game plans
- **Genre Agnostic**: Support for FPS, Platformer, RPG, and any other genre
- **Offline AI Integration**: Ollama (LLM), ComfyUI (Image), Piper (TTS)
- **Intelligent Validation**: Comprehensive game plan validation and auto-fix suggestions
- **Asset Generation**: Automatic creation of game assets, textures, and audio
- **Staging & Promotion**: Safe asset generation with atomic promotion workflow

### AI Adapters
- **Ollama LLM**: Offline language model for game plan generation and analysis
- **ComfyUI**: Offline image generation for textures and game assets
- **Piper TTS**: Offline text-to-speech for game audio and dialogue

### Validation System
- **Playability Validation**: Ensures game slices are completable
- **Mechanics Validation**: Verifies genre-specific mechanics are present
- **Performance Validation**: Checks against performance budgets
- **Asset Quality Validation**: Ensures generated assets meet quality standards

## 🚀 Quick Start

### Prerequisites
- Unity 2022.3.0f1 or later
- .NET 8.0 SDK
- Nexo framework (included as dependency)

### Installation
1. Clone the Nexo repository
2. Open Unity and import the Director Studio package
3. Navigate to **Nexo → Director Studio** in the Unity menu
4. Configure your offline AI adapters (optional)

### Basic Usage
1. **Create a Game Brief**: Enter a natural language description of your game slice
2. **Select Genre**: Choose a genre or let the system auto-detect
3. **Generate Plan**: Click "Plan Game Slice" to create a detailed game plan
4. **Validate**: Review validation results and apply auto-fixes if needed
5. **Generate Content**: Nexo agents will generate all game components as part of the pipeline

## 📁 Package Structure

```
Assets/NexoDirectorStudio/
├── Runtime/                    # Runtime code
│   ├── Adapters/              # AI adapter implementations
│   ├── Commands/             # Command pattern implementations
│   ├── DTO/                  # Data transfer objects
│   ├── Orchestration/        # Service composition
│   ├── Policies/             # Business rules and constraints
│   ├── Profiles/             # Genre-specific configurations
│   └── Validators/           # Validation logic
├── Editor/                    # Editor-only code
│   └── DirectorStudioWindow.cs # Main editor window
├── Tests/                      # Test suites
│   ├── EditMode/             # Edit mode tests
│   └── PlayMode/             # Play mode tests
└── Generated/                 # Generated assets (read-only)
```

## 🔧 Configuration

### Offline AI Adapters

#### Ollama (LLM)
```csharp
// Configure Ollama adapter
var ollamaAdapter = new OllamaAdapter(
    baseUrl: "http://localhost:11434",
    model: "llama2"
);
```

#### ComfyUI (Image Generation)
```csharp
// Configure ComfyUI adapter
var comfyuiAdapter = new ComfyUIAdapter(
    baseUrl: "http://localhost:8188",
    outputPath: "Assets/Generated/Textures"
);
```

#### Piper (TTS)
```csharp
// Configure Piper adapter
var piperAdapter = new PiperAdapter(
    piperPath: "piper",
    outputPath: "Assets/Generated/Audio",
    modelPath: "models"
);
```

### Genre Profiles
Director Studio includes built-in profiles for:
- **FPS**: First-person shooter mechanics and pacing
- **Platformer**: Jump mechanics and level design
- **RPG**: Character progression and narrative elements

## 🧪 Testing

### Test Categories
- **Unit Tests**: Individual component testing
- **Integration Tests**: End-to-end workflow testing
- **Smoke Tests**: Basic functionality verification
- **PlayMode Tests**: Unity-specific testing

### Running Tests
```bash
# Run all tests
dotnet test Tests/EditMode/
dotnet test Tests/PlayMode/

# Run specific test categories
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"
dotnet test --filter "Category=Smoke"
```

## 📊 CI/CD

The package includes comprehensive CI/CD workflows:

- **Build**: Multi-platform compilation and validation
- **Tests**: Unit, integration, and PlayMode testing
- **Analyzers**: Code analysis and quality checks
- **Coverage**: Code coverage reporting with 80% threshold

### Workflow Status
[![Build](https://github.com/your-org/Nexo/workflows/Build%20Director%20Studio/badge.svg)](https://github.com/your-org/Nexo/actions/workflows/build.yml)
[![Tests](https://github.com/your-org/Nexo/workflows/Test%20Director%20Studio/badge.svg)](https://github.com/your-org/Nexo/actions/workflows/tests.yml)
[![Coverage](https://github.com/your-org/Nexo/workflows/Code%20Coverage/badge.svg)](https://github.com/your-org/Nexo/actions/workflows/coverage.yml)

## 🏗️ Architecture

### Agent-First Design
Director Studio is built on the Nexo agent-first orchestration framework, providing:
- **Command Pattern**: All operations are commands with clear inputs/outputs
- **Validation Pipeline**: Comprehensive validation with auto-fix capabilities
- **Staging & Promotion**: Safe asset generation with atomic operations
- **Dependency Injection**: Clean separation of concerns

### Key Components
- **DirectorStudioService**: Main service composition and DI container
- **Command Handlers**: Implementations of game slice generation commands
- **Validators**: Comprehensive validation system for game plans
- **Adapters**: Offline AI integration for LLM, image, and audio generation

## 🔒 Security & Constraints

### Asset Generation Constraints
- **Path Allowlist**: All generated assets must be under `Assets/Generated/**`
- **Size Limits**: Maximum 200MB per generation run
- **Staging Workflow**: Assets are staged before promotion to prevent corruption
- **Audit Logging**: All operations are logged with seeds and versions

### Validation Constraints
- **Deterministic Generation**: Same inputs always produce same outputs
- **Performance Budgets**: Genre-specific performance constraints
- **Accessibility**: Built-in accessibility validation and defaults

## 📈 Performance

### Performance Budgets
Each genre profile includes performance budgets:
- **Triangles**: Maximum triangle count for geometry
- **Draw Calls**: Maximum draw calls per frame
- **Memory**: Maximum memory usage
- **Physics**: Maximum physics objects

### Optimization Features
- **Lazy Loading**: Assets are loaded only when needed
- **Caching**: Intelligent caching of generated content
- **Batch Processing**: Efficient batch operations for multiple assets

## 🤝 Contributing

### Development Setup
1. Clone the repository
2. Install Unity 2022.3.0f1 or later
3. Install .NET 8.0 SDK
4. Open the project in Unity
5. Run tests to verify setup

### Code Standards
- **Conventional Commits**: Use conventional commit messages
- **Test Coverage**: Maintain 80%+ test coverage
- **Code Analysis**: Pass all static analysis checks
- **Documentation**: Document all public APIs

### Pull Request Process
1. Create a feature branch
2. Implement changes with tests
3. Ensure all CI checks pass
4. Submit pull request with description
5. Address review feedback

## 📚 Documentation

### Additional Resources
- [Development Plan](docs/DirectorStudio_DevPlan.md) - Comprehensive development documentation
- [Architecture Guide](docs/architecture.md) - System architecture overview
- [API Reference](docs/api-reference.md) - Complete API documentation
- [Troubleshooting](docs/troubleshooting.md) - Common issues and solutions

### Examples
- [Basic Usage](examples/basic-usage.md) - Simple game slice generation
- [Advanced Features](examples/advanced-features.md) - Complex workflows
- [Custom Adapters](examples/custom-adapters.md) - Creating custom AI adapters

## 📄 License

This project is part of the Nexo framework and follows the same licensing terms.

## 🆘 Support

### Getting Help
- **Issues**: Report bugs and request features on GitHub
- **Discussions**: Ask questions and share ideas
- **Documentation**: Check the comprehensive documentation
- **Examples**: Review the example implementations

### Troubleshooting
- **Health Checks**: Verify AI adapters are running and accessible
- **Logs**: Check Unity console for detailed error messages
- **Validation**: Review validation reports for issues
- **Performance**: Monitor performance budgets and constraints

---

**Director Studio** - Empowering non-programmers to create amazing game slices with the power of AI and agent-first orchestration! 🎮✨
