# Docker-Based Cross-Runtime Testing Infrastructure

This directory contains a comprehensive Docker-based testing infrastructure for running Nexo Pipeline tests across different runtime environments in isolated containers.

## Overview

The Docker testing infrastructure provides:

- **Isolated Runtime Environments** - Each runtime runs in its own Docker container
- **True Cross-Runtime Testing** - Tests run against actual runtime environments
- **Automated Test Execution** - Scripts to build, run, and manage test containers
- **Code Coverage Collection** - Coverage reports for each runtime environment
- **Test Results Aggregation** - Combined results from all runtime environments

## Architecture

### Container Structure

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   .NET 8.0      │    │   .NET Core     │    │      Mono       │    │     Unity       │
│   Container     │    │   3.1 Container │    │   Container     │    │   Container     │
│                 │    │                 │    │                 │    │                 │
│ • .NET SDK 8.0  │    │ • .NET Core 3.1 │    │ • Mono Runtime  │    │ • Unity Editor  │
│ • Test Runner   │    │ • CoreCLR       │    │ • .NET SDK 6.0  │    │ • .NET SDK 8.0  │
│ • Coverage      │    │ • Test Runner   │    │ • Test Runner   │    │ • Test Runner   │
│ • Results       │    │ • Coverage      │    │ • Coverage      │    │ • Coverage      │
└─────────────────┘    └─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │                       │
         └───────────────────────┼───────────────────────┼───────────────────────┘
                                 │                       │
                    ┌─────────────────────────────────────────────────────────────┐
                    │                    Test Results Directory                   │
                    │                                                             │
                    │ • /test-results/dotnet/                                    │
                    │ • /test-results/coreclr/                                   │
                    │ • /test-results/mono/                                      │
                    │ • /test-results/unity/                                     │
                    │ • /test-results/cross-runtime/                             │
                    │ • /test-results/report/                                    │
                    └─────────────────────────────────────────────────────────────┘
```

### Runtime Environments

#### 1. .NET 8.0 Container
- **Base Image**: `mcr.microsoft.com/dotnet/sdk:8.0`
- **Purpose**: Test .NET 8.0 specific functionality
- **Features**: Full .NET 8.0 feature support
- **Test Filter**: `RuntimeFactAttribute&SupportedRuntimes=DotNet`

#### 2. .NET Core 3.1 Container
- **Base Image**: `mcr.microsoft.com/dotnet/sdk:3.1`
- **Purpose**: Test CoreCLR runtime compatibility
- **Features**: .NET Core 3.1 feature set
- **Test Filter**: `RuntimeFactAttribute&SupportedRuntimes=CoreCLR`

#### 3. Mono Container
- **Base Image**: `mono:latest`
- **Purpose**: Test Mono runtime compatibility
- **Features**: Mono runtime with .NET SDK 6.0
- **Test Filter**: `RuntimeFactAttribute&SupportedRuntimes=Mono`

#### 4. Unity Container
- **Base Image**: `unityci/editor:2022.3.0f1-linux-il2cpp-1.1.1`
- **Purpose**: Test Unity Engine compatibility
- **Features**: Unity Editor with .NET SDK 8.0
- **Test Filter**: `RuntimeFactAttribute&SupportedRuntimes=Unity`

## Quick Start

### Prerequisites

1. **Docker** - Install Docker Desktop or Docker Engine
2. **Docker Compose** - Usually included with Docker Desktop
3. **Bash** - For running shell scripts (Linux/macOS/WSL)

### Running All Tests

```bash
# Make scripts executable
chmod +x tests/Nexo.Feature.Pipeline.Tests/Docker/*.sh

# Run all cross-runtime tests
./tests/Nexo.Feature.Pipeline.Tests/Docker/run-all-tests.sh
```

### Running Specific Runtime Tests

```bash
# Run only .NET tests
./tests/Nexo.Feature.Pipeline.Tests/Docker/run-dotnet-tests.sh

# Run only Unity tests (requires Unity credentials)
UNITY_EMAIL=your-email@example.com UNITY_PASSWORD=your-password \
  ./tests/Nexo.Feature.Pipeline.Tests/Docker/run-all-tests.sh
```

## Detailed Usage

### Docker Compose Approach

```bash
# Navigate to Docker directory
cd tests/Nexo.Feature.Pipeline.Tests/Docker

# Run all containers
docker-compose -f docker-compose.test.yml up --build

# Run specific service
docker-compose -f docker-compose.test.yml up dotnet-tests

# Clean up
docker-compose -f docker-compose.test.yml down --volumes
```

### Individual Container Approach

```bash
# Build specific image
docker build -f Dockerfile.dotnet -t nexo-dotnet-tests .

# Run container
docker run --rm -v $(pwd)/test-results:/workspace/test-results nexo-dotnet-tests

# Run with Unity credentials
docker run --rm \
  -e UNITY_EMAIL=your-email@example.com \
  -e UNITY_PASSWORD=your-password \
  -v $(pwd)/test-results:/workspace/test-results \
  nexo-unity-tests
```

## Script Options

### Main Test Runner (`run-all-tests.sh`)

```bash
# Show help
./run-all-tests.sh --help

# Build images only
./run-all-tests.sh --build-only

# Run tests only (skip build)
./run-all-tests.sh --run-only

# Clean up containers
./run-all-tests.sh --cleanup

# Show test results summary
./run-all-tests.sh --summary

# Run with Unity credentials
./run-all-tests.sh --unity-email user@example.com --unity-password pass
```

### Individual Runtime Runners

```bash
# .NET tests
./run-dotnet-tests.sh [--build-only|--run-only|--cleanup|--summary]

# Unity tests (create similar script)
./run-unity-tests.sh [--build-only|--run-only|--cleanup|--summary]
```

## Configuration

### Environment Variables

```bash
# Unity License Activation
export UNITY_EMAIL=your-email@example.com
export UNITY_PASSWORD=your-password

# Docker Configuration
export DOCKER_BUILDKIT=1
export COMPOSE_DOCKER_CLI_BUILD=1
```

### Test Results Structure

```
test-results/
├── dotnet/
│   ├── TestResults.xml
│   ├── coverage.info
│   └── coverage.cobertura.xml
├── coreclr/
│   ├── TestResults.xml
│   ├── coverage.info
│   └── coverage.cobertura.xml
├── mono/
│   ├── TestResults.xml
│   ├── coverage.info
│   └── coverage.cobertura.xml
├── unity/
│   ├── TestResults.xml
│   ├── coverage.info
│   └── coverage.cobertura.xml
├── cross-runtime/
│   ├── TestResults.xml
│   └── coverage.info
└── report/
    ├── index.html
    └── coverage report files
```

## Code Coverage

### Coverage Collection

Each container collects code coverage using Coverlet:

```bash
dotnet test \
  --collect:"XPlat Code Coverage" \
  --settings coverlet.runsettings
```

### Coverage Configuration

The `coverlet.runsettings` file configures:

- **Formats**: Cobertura, OpenCover, JSON
- **Exclusions**: Test files, runtime files, Docker files
- **Inclusions**: Nexo.Feature.Pipeline, Nexo.Core.*, Nexo.Shared
- **Source Link**: Enabled for better debugging

### Coverage Reports

```bash
# Generate HTML report
reportgenerator \
  -reports:test-results/**/coverage.info \
  -targetdir:test-results/report \
  -reporttypes:Html
```

## Unity Testing

### Unity License Activation

Unity tests require a valid Unity license:

```bash
# Set Unity credentials
export UNITY_EMAIL=your-email@example.com
export UNITY_PASSWORD=your-password

# Run Unity tests
./run-all-tests.sh --unity-email $UNITY_EMAIL --unity-password $UNITY_PASSWORD
```

### Unity Container Features

- **Unity Editor**: 2022.3.0f1 with IL2CPP
- **.NET SDK**: 8.0 for test execution
- **License Activation**: Automatic if credentials provided
- **Test Integration**: Unity Test Framework compatible

## Performance Considerations

### Container Optimization

```dockerfile
# Multi-stage builds for smaller images
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# Build stage

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
# Runtime stage with only necessary components
```

### Resource Limits

```yaml
# docker-compose.test.yml
services:
  dotnet-tests:
    deploy:
      resources:
        limits:
          memory: 2G
          cpus: '2'
```

### Caching

```yaml
# Volume caching for faster builds
volumes:
  dotnet-cache:
  unity-cache:
```

## Troubleshooting

### Common Issues

#### Docker Permission Issues
```bash
# Add user to docker group
sudo usermod -aG docker $USER
newgrp docker
```

#### Unity License Issues
```bash
# Check Unity activation logs
docker logs nexo-unity-tests

# Manual Unity activation
docker run --rm -e UNITY_LICENSE=1 unityci/editor:2022.3.0f1-linux-il2cpp-1.1.1 \
  unity-editor -batchmode -quit -username $UNITY_EMAIL -password $UNITY_PASSWORD
```

#### Test Failures
```bash
# Check container logs
docker logs nexo-dotnet-tests

# Run container interactively
docker run -it --rm nexo-dotnet-tests /bin/bash
```

#### Memory Issues
```bash
# Increase Docker memory limit
# Docker Desktop: Settings > Resources > Memory
# Docker Engine: /etc/docker/daemon.json
{
  "default-shm-size": "2G"
}
```

### Debug Information

```bash
# Show container information
docker ps -a

# Show image information
docker images

# Show volume information
docker volume ls

# Show network information
docker network ls
```

## CI/CD Integration

### GitHub Actions

```yaml
name: Cross-Runtime Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Run .NET Tests
      run: |
        chmod +x tests/Nexo.Feature.Pipeline.Tests/Docker/run-dotnet-tests.sh
        ./tests/Nexo.Feature.Pipeline.Tests/Docker/run-dotnet-tests.sh
    
    - name: Upload Test Results
      uses: actions/upload-artifact@v3
      with:
        name: test-results
        path: test-results/
```

### Azure DevOps

```yaml
trigger:
- main

pool:
  vmImage: 'ubuntu-latest'

steps:
- task: Docker@2
  inputs:
    containerRegistry: 'docker-hub'
    repository: 'nexo-tests'
    command: 'buildAndPush'
    Dockerfile: 'tests/Nexo.Feature.Pipeline.Tests/Docker/Dockerfile.dotnet'

- script: |
    chmod +x tests/Nexo.Feature.Pipeline.Tests/Docker/run-all-tests.sh
    ./tests/Nexo.Feature.Pipeline.Tests/Docker/run-all-tests.sh
  displayName: 'Run Cross-Runtime Tests'
```

## Advanced Configuration

### Custom Test Filters

```bash
# Run specific test categories
dotnet test --filter "Category=Performance"

# Run tests by trait
dotnet test --filter "Trait=Runtime&DotNet"

# Exclude tests
dotnet test --filter "Category!=Integration"
```

### Parallel Execution

```yaml
# docker-compose.test.yml
services:
  dotnet-tests:
    environment:
      - DOTNET_PARALLEL_TESTS=4
```

### Custom Test Settings

```xml
<!-- TestSettings.runsettings -->
<RunSettings>
  <RunConfiguration>
    <MaxCpuCount>4</MaxCpuCount>
    <TestSessionTimeout>300000</TestSessionTimeout>
  </RunConfiguration>
</RunSettings>
```

## Monitoring and Logging

### Container Logs

```bash
# Follow logs in real-time
docker-compose -f docker-compose.test.yml logs -f

# Show logs for specific service
docker-compose -f docker-compose.test.yml logs dotnet-tests
```

### Performance Monitoring

```bash
# Monitor container resources
docker stats

# Show container resource usage
docker stats --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}"
```

## Future Enhancements

### Planned Features

- **Multi-Platform Support**: Windows and macOS containers
- **GPU Acceleration**: CUDA support for Unity tests
- **Test Parallelization**: Parallel test execution across containers
- **Performance Baselines**: Automated performance regression detection
- **Test Result Analytics**: Advanced test result analysis and reporting

### Integration Opportunities

- **Test Result Databases**: Store test results for historical analysis
- **Performance Monitoring**: Integration with APM tools
- **Security Scanning**: Container vulnerability scanning
- **Artifact Management**: Test artifact storage and retrieval

## Contributing

### Adding New Runtime Support

1. Create new Dockerfile (e.g., `Dockerfile.newruntime`)
2. Add service to `docker-compose.test.yml`
3. Create runtime-specific test script
4. Update documentation

### Modifying Test Configuration

1. Update `coverlet.runsettings` for coverage changes
2. Modify Dockerfile entrypoint scripts for test execution changes
3. Update shell scripts for new options

### Reporting Issues

1. Check container logs for error details
2. Verify runtime environment compatibility
3. Test with minimal configuration
4. Provide reproduction steps 