# Cross-Platform Testing Integration

## Overview

Nexo now includes comprehensive cross-platform testing capabilities that allow you to run tests across multiple .NET runtimes and Unity environments using Docker containers. This integration provides a unified way to ensure your code works correctly across different platforms and environments.

## Features

### 🌐 Multi-Environment Testing
- **.NET 8.0 Linux**: Latest .NET runtime on Linux
- **.NET 7.0 Linux**: .NET 7.0 runtime on Linux  
- **.NET 6.0 Linux**: .NET 6.0 runtime on Linux
- **.NET Framework 4.8 Windows**: Legacy .NET Framework
- **Unity Linux**: Unity Engine compatibility testing
- **Unity Windows**: Unity Engine compatibility on Windows
- **Performance Testing**: Performance and coverage testing
- **Cross-Platform Builds**: Multi-architecture testing

### 🎮 Unity Engine Integration
- **.NET Standard 2.0 Compatibility**: Ensures Unity compatibility
- **Assembly Validation**: Verifies Unity assembly constraints
- **Runtime Testing**: Tests actual Unity runtime behavior
- **Cross-Platform Unity**: Tests on both Linux and Windows

### ⚡ Performance & Coverage
- **Code Coverage Collection**: Automatic coverage reporting
- **Performance Metrics**: Execution time and resource usage
- **Parallel Execution**: Run multiple environments simultaneously
- **Resource Monitoring**: CPU and memory usage tracking

## Quick Start

### 1. Setup Testing Infrastructure

```bash
# Setup the testing infrastructure
nexo test setup

# Force recreation of environments
nexo test setup --force

# Clean existing environments
nexo test setup --clean
```

### 2. List Available Environments

```bash
# List all available test environments
nexo test list
```

### 3. Run Tests

```bash
# Run all environments
nexo test run --environment all

# Run specific environment
nexo test run --environment dotnet8-linux

# Run Unity compatibility tests
nexo test run --environment unity-linux

# Run with code coverage
nexo test run --environment all --coverage

# Run in parallel
nexo test run --environment all --parallel

# Run with custom project path
nexo test run --environment all --project ./my-project
```

### 4. Validate Configuration

```bash
# Validate test configuration
nexo test validate

# Validate specific environment
nexo test validate --environment unity-linux

# Validate custom config file
nexo test validate --config ./my-test-config.json
```

### 5. Generate Reports

```bash
# Generate HTML report
nexo test report --format html

# Generate JSON report
nexo test report --format json

# Generate Markdown report
nexo test report --format markdown

# Include historical data
nexo test report --format html --history

# Custom output directory
nexo test report --format html --output ./reports
```

## Configuration

### Test Configuration File

Create a `test-config.json` file in your project root:

```json
{
  "name": "My Project Tests",
  "version": "1.0.0",
  "description": "Cross-platform tests for my project",
  "environments": {
    "dotnet8-linux": {
      "enabled": true,
      "timeout_minutes": 10
    },
    "unity-linux": {
      "enabled": true,
      "timeout_minutes": 15
    }
  },
  "execution": {
    "parallel_environments": false,
    "coverage_enabled": true,
    "coverage_threshold": 80.0
  },
  "reporting": {
    "output_format": "html",
    "output_directory": "./test-reports"
  }
}
```

### Environment Variables

```bash
# Docker settings
export DOCKER_BUILDKIT=1
export COMPOSE_DOCKER_CLI_BUILD=1

# Test settings
export NEXO_TEST_PARALLEL=true
export NEXO_TEST_COVERAGE=true
export NEXO_TEST_TIMEOUT=30
```

## Docker Integration

### Prerequisites

- Docker Desktop installed and running
- Docker Compose available
- Sufficient disk space for container images

### Container Services

The testing infrastructure uses the following Docker services:

- **test-dotnet8-linux**: .NET 8.0 Linux environment
- **test-unity-linux**: Unity compatibility Linux environment  
- **test-performance**: Performance testing environment
- **test-cross-platform**: Cross-platform build environment

### Custom Docker Configuration

You can customize the Docker setup by modifying `docker-compose.test-environments.yml`:

```yaml
services:
  test-dotnet8-linux:
    build:
      context: .
      dockerfile: docker-test-environments/dotnet8-linux.Dockerfile
    volumes:
      - .:/workspace
    environment:
      - DOTNET_VERSION=8.0
      - TEST_TIMEOUT=600
```

## Pipeline Integration

### Using Testing in Pipelines

The testing commands are fully integrated with Nexo's pipeline architecture:

```json
{
  "name": "CI/CD Pipeline",
  "commands": [
    {
      "id": "cross-platform-test",
      "name": "Cross-Platform Tests",
      "parameters": {
        "environment": "all",
        "parallel_execution": true,
        "enable_coverage": true
      }
    }
  ]
}
```

### Pipeline Execution

```bash
# Execute testing pipeline
nexo pipeline execute --config pipeline-with-tests.json

# Execute with specific environment
nexo pipeline execute --config pipeline-with-tests.json --mode testing
```

## Advanced Usage

### Custom Test Environments

Create custom test environments by extending the Docker configuration:

```dockerfile
# Custom test environment
FROM mcr.microsoft.com/dotnet/sdk:8.0

# Install custom dependencies
RUN apt-get update && apt-get install -y \
    custom-package \
    && rm -rf /var/lib/apt/lists/*

# Set up test environment
WORKDIR /workspace
COPY . .

# Run tests
CMD ["dotnet", "test", "--logger", "trx"]
```

### Integration with CI/CD

```yaml
# GitHub Actions example
name: Cross-Platform Tests
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Run Cross-Platform Tests
        run: |
          nexo test run --environment all --parallel --coverage
      - name: Upload Test Results
        uses: actions/upload-artifact@v3
        with:
          name: test-results
          path: test-reports/
```

### Performance Testing

```bash
# Run performance tests with detailed metrics
nexo test run --environment performance --coverage

# Monitor resource usage
nexo test run --environment all --parallel --monitor-resources
```

## Troubleshooting

### Common Issues

1. **Docker not running**
   ```bash
   # Check Docker status
   docker --version
   docker-compose --version
   ```

2. **Insufficient disk space**
   ```bash
   # Clean up Docker images
   docker system prune -a
   ```

3. **Test timeouts**
   ```bash
   # Increase timeout
   nexo test run --environment all --timeout 30
   ```

4. **Unity compatibility issues**
   ```bash
   # Check Unity test project
   nexo test validate --environment unity-linux
   ```

### Debug Mode

```bash
# Enable debug logging
export NEXO_LOG_LEVEL=Debug
nexo test run --environment dotnet8-linux
```

### Cleanup

```bash
# Clean up test environments
docker-compose -f docker-compose.test-environments.yml down --remove-orphans

# Clean up test artifacts
rm -rf test-reports/
rm -rf TestResults/
```

## Best Practices

### 1. Test Organization
- Group related tests by feature
- Use descriptive test names
- Maintain test isolation

### 2. Performance Optimization
- Run tests in parallel when possible
- Use appropriate timeouts
- Monitor resource usage

### 3. Coverage Goals
- Aim for 80%+ code coverage
- Focus on critical paths
- Include edge cases

### 4. Environment Management
- Keep Docker images updated
- Use specific version tags
- Clean up unused resources

### 5. Reporting
- Generate reports after each run
- Archive historical data
- Share results with team

## Examples

### Complete Test Workflow

```bash
# 1. Setup infrastructure
nexo test setup

# 2. Validate configuration
nexo test validate

# 3. Run comprehensive tests
nexo test run --environment all --parallel --coverage

# 4. Generate detailed report
nexo test report --format html --history

# 5. Cleanup
docker-compose -f docker-compose.test-environments.yml down
```

### Unity-Specific Testing

```bash
# Test Unity compatibility
nexo test run --environment unity-linux

# Test Unity on Windows
nexo test run --environment unity-windows

# Validate Unity project structure
nexo test validate --environment unity-linux
```

### Performance Testing

```bash
# Run performance tests
nexo test run --environment performance

# Monitor resource usage
nexo test run --environment all --monitor-resources

# Generate performance report
nexo test report --format html --include-metrics
```

## Integration with Other Tools

### IDE Integration
- **VS Code**: Use Nexo extension for test execution
- **Rider**: Configure external tools for Nexo commands
- **Visual Studio**: Add Nexo commands to build events

### CI/CD Integration
- **GitHub Actions**: Use provided workflow templates
- **Azure DevOps**: Configure build pipelines
- **Jenkins**: Add Nexo commands to build scripts

### Monitoring Integration
- **Application Insights**: Send test metrics
- **Prometheus**: Export performance data
- **Grafana**: Create test dashboards

## Future Enhancements

### Planned Features
- **Cloud Testing**: Run tests on cloud platforms
- **Mobile Testing**: iOS and Android compatibility
- **Web Testing**: Browser automation integration
- **Security Testing**: Automated security scans
- **Load Testing**: Performance under load

### Roadmap
- **Q1 2024**: Cloud testing support
- **Q2 2024**: Mobile platform testing
- **Q3 2024**: Advanced reporting features
- **Q4 2024**: AI-powered test optimization

## Support

### Documentation
- [Pipeline Architecture](PIPELINE_ARCHITECTURE.md)
- [Docker Configuration](DOCKER_CONFIGURATION.md)
- [Unity Integration](UNITY_INTEGRATION.md)

### Community
- GitHub Issues: Report bugs and request features
- Discussions: Share experiences and best practices
- Wiki: Community-maintained documentation

### Getting Help
- Check troubleshooting section above
- Review example configurations
- Search existing issues
- Create new issue with detailed information 