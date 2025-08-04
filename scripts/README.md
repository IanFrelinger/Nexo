# Nexo Scripts Directory

This directory contains all shell scripts for building, testing, and managing the Nexo project.

## 📁 Directory Structure

### 🔨 [Build Scripts](./build/)
Scripts for building and compiling the project:
- **run-dotnet.sh** - Build and run .NET applications
- **run-mono.sh** - Build and run Mono applications

### 🧪 [Test Scripts](./test/)
Scripts for testing and validation:
- **run-all-tests.sh** - Execute all test suites across the project
- **test-web-cli.sh** - Test web CLI functionality

## 🚀 Usage Examples

### Building the Project
```bash
# Build with .NET
./scripts/build/run-dotnet.sh

# Build with Mono
./scripts/build/run-mono.sh
```

### Running Tests
```bash
# Run all tests
./scripts/test/run-all-tests.sh

# Test web CLI
./scripts/test/test-web-cli.sh
```

## 📋 Script Details

### Build Scripts

#### `run-dotnet.sh`
- **Purpose**: Build and run .NET applications
- **Usage**: `./scripts/build/run-dotnet.sh`
- **Target**: .NET 8.0, .NET Framework 4.8, .NET Standard 2.0

#### `run-mono.sh`
- **Purpose**: Build and run Mono applications
- **Usage**: `./scripts/build/run-mono.sh`
- **Target**: Mono runtime compatibility

### Test Scripts

#### `run-all-tests.sh`
- **Purpose**: Execute comprehensive test suite
- **Usage**: `./scripts/test/run-all-tests.sh`
- **Features**:
  - Runs all test projects
  - Cross-platform testing
  - Performance monitoring
  - Coverage reporting

#### `test-web-cli.sh`
- **Purpose**: Test web CLI functionality
- **Usage**: `./scripts/test/test-web-cli.sh`
- **Features**:
  - Web interface testing
  - CLI command validation
  - Integration testing

## 🔧 Script Development

### Adding New Scripts
1. Place scripts in appropriate subdirectory (`build/`, `test/`, `docker/`)
2. Make scripts executable: `chmod +x script-name.sh`
3. Update this README with script documentation
4. Follow naming convention: `action-target.sh`

### Script Guidelines
- Use shebang: `#!/bin/bash`
- Include error handling
- Add usage comments
- Make scripts idempotent when possible
- Use relative paths from project root

## 📊 Current Status

- **Build Scripts**: 2 scripts (dotnet, mono)
- **Test Scripts**: 2 scripts (all-tests, web-cli)
- **Total Scripts**: 4 scripts

## 🔄 Recent Updates

- **Organization**: Moved all scripts from root directory to organized structure
- **Documentation**: Added comprehensive script documentation
- **Structure**: Created logical categorization (build, test, docker)

---

**Last Updated**: January 27, 2025  
**Next Review**: February 3, 2025 