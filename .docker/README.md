# Docker Testing Infrastructure

This directory contains Dockerfiles for multi-platform testing of the Nexo framework.

## Overview

The Dockerfiles in this directory are used by the `nexo test` command to run tests across different platforms and environments. The framework automatically selects the appropriate Dockerfile based on the target platform.

## Usage

Dockerfiles are automatically used when running:

```bash
# Test on all platforms
nexo test --platforms ubuntu alpine debian android windows unity

# Test specific project
nexo test --project Nexo.Tests.Infrastructure

# Test with filter
nexo test --filter "FullyQualifiedName~GeospatialE2ESmokeTests"
```

## Dockerfile Types

- **`Dockerfile.test`**: Base test image for Ubuntu
- **`Dockerfile.test-framework*`**: Framework tests for various platforms (alpine, debian, android, windows, unity)
- **`Dockerfile.test-caching*`**: Caching tests for various platforms
- **`Dockerfile.test-visual-validation*`**: Visual validation tests

## Manual Usage

You can also use these Dockerfiles directly with Docker:

```bash
# Build test image
docker build -f .docker/Dockerfile.test -t nexo-test:ubuntu .

# Run tests
docker run --rm nexo-test:ubuntu
```

## Platform Support

The framework supports testing on:
- **Linux**: Ubuntu, Alpine, Debian
- **Mobile**: Android, iOS (native on macOS)
- **Windows**: Windows Server containers
- **Unity**: Unity-compatible .NET Standard 2.0 environment

For more information, see the [Multi-Platform Testing documentation](../docs/EXECUTION_PLATFORM_ABSTRACTION.md).
