# Nexo Feature Lab Demo Scripts

This directory contains both shell-based and C#-based utilities for running the Nexo Feature Lab demo.

## C#-Based Utilities (Recommended)

The C# utilities provide better cross-platform compatibility and more robust error handling.

### Quick Start

```bash
# Run the complete demo (MAUI)
./demo/scripts/run-demo-csharp.sh

# Run validation only
./demo/scripts/run-demo-csharp.sh validate

# Run with Blazor instead of MAUI
./demo/scripts/run-demo-csharp.sh run --blazor --port 8080
```

### Available Commands

- `validate` - Run comprehensive validation checks
- `seed` - Seed demo fixtures (emails and contracts)
- `build` - Build the solution
- `run` - Run the complete demo with options

### Demo Options

- `--maui` - Use MAUI application (default)
- `--blazor` - Use Blazor application
- `--skip-validation` - Skip validation checks
- `--no-fixtures` - Skip fixture seeding
- `--port <number>` - Specify port for Blazor app

## Shell-Based Utilities (Legacy)

The shell utilities are still available for compatibility:

- `validate-demo.sh` - Validation script
- `run-demo.sh` - Demo runner script
- `seed-fixtures.sh` - Fixture seeding script

## C# Utilities Architecture

### Core Components

1. **SystemUtils** - Low-level system operations with timeout protection
2. **DependencyManager** - Handles installation and verification of dependencies
3. **BuildManager** - Manages .NET project building with error handling
4. **ProcessManager** - Manages long-running processes with lifecycle control
5. **ValidationRunner** - Runs comprehensive validation checks
6. **FixtureSeeder** - Seeds demo data (emails and contracts)
7. **DemoRunner** - Orchestrates the complete demo process

### Key Features

- **Timeout Protection** - All operations have configurable timeouts
- **Retry Logic** - Automatic retry with exponential backoff
- **Cross-Platform** - Works on Windows, macOS, and Linux
- **Process Management** - Proper cleanup of background processes
- **Error Handling** - Comprehensive error reporting and suggestions
- **Dependency Management** - Automatic installation of required tools
- **Port Management** - Automatic port detection and conflict resolution

### Usage Examples

```bash
# Basic demo run
./demo/scripts/run-demo-csharp.sh

# Validation only
./demo/scripts/run-demo-csharp.sh validate

# Blazor demo on custom port
./demo/scripts/run-demo-csharp.sh run --blazor --port 8080

# MAUI demo without validation
./demo/scripts/run-demo-csharp.sh run --maui --skip-validation

# Seed fixtures only
./demo/scripts/run-demo-csharp.sh seed
```

### Error Handling

The C# utilities provide detailed error messages and suggestions for common issues:

- Missing dependencies with installation instructions
- Port conflicts with alternative port suggestions
- Build failures with specific error details
- Process management issues with cleanup instructions

### Configuration

The utilities automatically detect:
- Project root directory
- Available .NET SDK versions
- System package managers (brew, apt, yum, etc.)
- Available ports
- Process conflicts

### Troubleshooting

1. **Build Failures**: Check for missing dependencies or compilation errors
2. **Port Conflicts**: Use `--port` option to specify a different port
3. **Permission Issues**: The utilities will try to install dependencies without sudo when possible
4. **Process Hanging**: The utilities include timeout protection and automatic cleanup

### Development

To modify the C# utilities:

1. Edit files in `demo/scripts/lib/`
2. Rebuild with `dotnet build demo/scripts/lib/Nexo.Demo.Scripts.csproj`
3. Test with `./demo/scripts/run-demo-csharp.sh validate`

The utilities are designed to be modular and extensible, making it easy to add new validation checks or demo features.
