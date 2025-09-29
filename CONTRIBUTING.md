# Contributing to Nexo

Thank you for your interest in contributing to Nexo! This guide will help you get started with contributing to the project.

## Development Setup

### Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 or VS Code
- Git
- Node.js 18+ (for commitlint)

### Getting Started

1. **Fork the repository** on GitHub
2. **Clone your fork** locally:
   ```bash
   git clone https://github.com/your-username/Nexo.git
   cd Nexo
   ```

3. **Install dependencies**:
   ```bash
   dotnet restore
   npm install
   ```

4. **Build the solution**:
   ```bash
   dotnet build
   ```

5. **Run tests** to ensure everything works:
   ```bash
   dotnet test
   ```

## Development Workflow

### 1. Create a Feature Branch

```bash
git checkout -b feature/your-feature-name
```

**Branch naming conventions**:
- `feature/description` - New features
- `fix/description` - Bug fixes
- `docs/description` - Documentation updates
- `refactor/description` - Code refactoring

### 2. Make Your Changes

- Follow the [coding standards](#coding-standards)
- Write tests for new functionality
- Update documentation as needed
- Ensure all tests pass

### 3. Commit Your Changes

Use [conventional commits](#commit-convention):

```bash
git add .
git commit -m "feat: add new development tool for file operations"
```

### 4. Push and Create Pull Request

```bash
git push origin feature/your-feature-name
```

Then create a pull request on GitHub.

## Coding Standards

### General Principles

- **Maximum 200 lines per class**
- **Single Responsibility Principle**
- **Composition over inheritance**
- **Clear, descriptive naming**
- **Comprehensive documentation**

### Code Style

- Use **async/await** for I/O operations
- Include **cancellation token** support
- Follow **C# naming conventions**
- Use **var** for implicit typing when type is obvious
- Prefer **readonly** fields where possible

### Architecture Compliance

- **No circular dependencies** between layers
- **Single ownership** of interfaces and factories
- **Type-value system** instead of enums in domain
- **Examples isolation** in non-packable project

### Testing Requirements

- **Unit tests** for all new functionality
- **Integration tests** for cross-component features
- **Architecture tests** for new architectural patterns
- **Test coverage** should not decrease

## Commit Convention

This project uses [Conventional Commits](https://www.conventionalcommits.org/):

### Format

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

### Types

- `feat`: A new feature
- `fix`: A bug fix
- `docs`: Documentation only changes
- `style`: Changes that do not affect the meaning of the code
- `refactor`: A code change that neither fixes a bug nor adds a feature
- `perf`: A code change that improves performance
- `test`: Adding missing tests or correcting existing tests
- `build`: Changes that affect the build system or external dependencies
- `ci`: Changes to our CI configuration files and scripts
- `chore`: Other changes that don't modify src or test files
- `revert`: Reverts a previous commit

### Examples

```bash
feat: add new development tool for file operations
fix: resolve memory leak in agent orchestration
docs: update API documentation for security scanning
test: add integration tests for CLI commands
ci: add commitlint validation to GitHub Actions
refactor: simplify command orchestration logic
perf: optimize agent memory usage
```

## Pull Request Process

### Before Submitting

1. **Ensure all tests pass**:
   ```bash
   dotnet test
   ```

2. **Run architecture validation**:
   ```bash
   dotnet test tests/Nexo.Tests.Architecture
   ```

3. **Check code analysis**:
   ```bash
   dotnet build -c Release -warnaserror
   ```

4. **Validate commit messages**:
   ```bash
   npx commitlint --from HEAD~1 --to HEAD
   ```

### Pull Request Template

When creating a pull request, please include:

- **Description**: What changes were made and why
- **Type**: Feature, bug fix, documentation, etc.
- **Testing**: How the changes were tested
- **Breaking Changes**: Any breaking changes and migration path
- **Checklist**: Confirm all requirements are met

### Review Process

1. **Automated checks** must pass (CI, tests, linting)
2. **Code review** by maintainers
3. **Architecture validation** must pass
4. **Documentation** updates if needed

## Project Structure

### Source Code (`src/`)

- **Nexo.Abstractions**: Core interfaces and contracts
- **Nexo.Runtime**: Agent runtime and execution
- **Nexo.Core.Application**: Application layer
- **Nexo.Core.Domain**: Domain layer
- **Nexo.Tools.Dev**: Development tools
- **Nexo.Policies.Dev**: Development policies
- **Nexo.Agents.Dev**: Development agents
- **Nexo.Examples**: Example implementations (non-packable)

### Tests (`tests/`)

- **Nexo.Tests.Architecture**: Architectural validation
- **Nexo.Tests.Integration**: Integration tests
- **Other test projects**: Unit and functional tests

### Documentation (`docs/`)

- **architecture.md**: Architectural patterns and principles
- **api/**: API reference documentation
- **contributing.md**: This file

## Architecture Guidelines

### Adding New Components

1. **Identify the correct layer** (Presentation, Application, Domain, Infrastructure)
2. **Create appropriate interfaces** in Abstractions if needed
3. **Follow single responsibility** principle
4. **Add comprehensive tests**
5. **Update documentation**

### Adding New Tools

1. **Implement ITool interface**
2. **Define ToolSchema** with proper JSON schema
3. **Add policy integration** if needed
4. **Create integration tests**
5. **Update tool documentation**

### Adding New Policies

1. **Implement IPolicy interface**
2. **Define clear policy rules**
3. **Add policy tests**
4. **Document policy behavior**
5. **Consider policy composition**

### Adding New Agents

1. **Implement IAgent interface**
2. **Define agent capabilities**
3. **Add agent tests**
4. **Document agent behavior**
5. **Consider agent orchestration**

## Testing Guidelines

### Unit Tests

- **Test individual components** in isolation
- **Mock dependencies** appropriately
- **Test both success and failure** scenarios
- **Use descriptive test names**

### Integration Tests

- **Test component interactions**
- **Use real implementations** where appropriate
- **Test end-to-end workflows**
- **Verify system behavior**

### Architecture Tests

- **Validate architectural rules**
- **Prevent regressions**
- **Enforce design principles**
- **Document architectural constraints**

## Documentation Guidelines

### Code Documentation

- **XML documentation** for public APIs
- **Inline comments** for complex logic
- **README files** for each project
- **Examples** in code where helpful

### Architecture Documentation

- **Update architecture.md** for significant changes
- **Document design decisions**
- **Include diagrams** where helpful
- **Keep documentation current**

## Release Process

### Versioning

This project uses [Semantic Versioning](https://semver.org/):

- **MAJOR**: Breaking changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes (backward compatible)

### Release Checklist

1. **All tests pass**
2. **Architecture validation passes**
3. **Documentation is updated**
4. **Breaking changes documented**
5. **Migration guide provided** (if needed)

## Getting Help

### Questions and Discussions

- **GitHub Discussions**: For general questions and discussions
- **GitHub Issues**: For bug reports and feature requests
- **Pull Request Comments**: For specific code review discussions

### Resources

- **Architecture Guide**: [docs/architecture.md](docs/architecture.md)
- **API Reference**: [docs/api/](docs/api/)
- **Examples**: [src/Nexo.Examples/](src/Nexo.Examples/)

## Code of Conduct

This project follows the [Contributor Covenant Code of Conduct](https://www.contributor-covenant.org/). Please be respectful and inclusive in all interactions.

---

Thank you for contributing to Nexo! 🚀
