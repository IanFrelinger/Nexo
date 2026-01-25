# Multi-Environment Testing Guide

This guide explains how to test the geospatial caching functionality across different virtual environments.

## Overview

The geospatial smoke tests (including caching functionality) are designed to run across multiple environments to ensure compatibility:
- **Operating Systems**: Ubuntu, Alpine Linux, Debian, Windows, Android, iOS, Unity
- **.NET Versions**: 7.0, 8.0
- **Test Types**: E2E smoke tests (includes caching validation)

## Quick Start

### Run All Tests Locally

```bash
# Run geospatial smoke tests on all configured environments
make test-geospatial-smoke-all

# Or use the script directly
./scripts/test-caching-multi-env.sh --all
```

### Run Tests on Specific Environment

```bash
# Run on Ubuntu with .NET 8.0
./scripts/test-caching-multi-env.sh --env ubuntu-8.0

# Run on Alpine Linux
./scripts/test-caching-multi-env.sh --env alpine-8.0

# Run on Debian
./scripts/test-caching-multi-env.sh --env debian-8.0

# Run on Android
./scripts/test-caching-multi-env.sh --env android-8.0

# Run on Windows (requires Windows containers)
./scripts/test-caching-multi-env.sh --env windows-8.0

# Run on iOS (macOS only)
./scripts/test-caching-ios.sh

# Run on Unity
./scripts/test-caching-unity.sh
```

## Available Environments

| Environment | OS | .NET Version | Type | Configuration |
|------------|----|--------------|------|---------------|
| `ubuntu-8.0` | Ubuntu 22.04 | 8.0 | Docker | `.docker/Dockerfile.test-caching` |
| `ubuntu-7.0` | Ubuntu 22.04 | 7.0 | Docker | `.docker/Dockerfile.test-caching` |
| `alpine-8.0` | Alpine Linux | 8.0 | Docker | `.docker/Dockerfile.test-caching-alpine` |
| `debian-8.0` | Debian 12 | 8.0 | Docker | `.docker/Dockerfile.test-caching-debian` |
| `android-8.0` | Android (Linux) | 8.0 | Docker | `.docker/Dockerfile.test-caching-android` |
| `windows-8.0` | Windows Server | 8.0 | Docker | `.docker/Dockerfile.test-caching-windows` |
| `ios-8.0` | iOS (macOS) | 8.0 | Native | `scripts/test-caching-ios.sh` |
| `unity-8.0` | Unity Engine | 8.0 | Native | `scripts/test-caching-unity.sh` |

### Environment Types

- **Docker**: Runs in Docker containers (cross-platform)
- **Native**: Requires native environment setup
  - iOS: Requires macOS with Xcode
  - Unity: Requires Unity installation

## Manual Docker Usage

### Build Test Image

```bash
# Ubuntu
docker build -f .docker/Dockerfile.test-caching \
  --build-arg DOTNET_VERSION=8.0 \
  -t nexo-caching-test:ubuntu-8.0 .

# Alpine
docker build -f .docker/Dockerfile.test-caching-alpine \
  --build-arg DOTNET_VERSION=8.0 \
  -t nexo-caching-test:alpine-8.0 .

# Debian
docker build -f .docker/Dockerfile.test-caching-debian \
  --build-arg DOTNET_VERSION=8.0 \
  -t nexo-caching-test:debian-8.0 .

# Android
docker build -f .docker/Dockerfile.test-caching-android \
  --build-arg DOTNET_VERSION=8.0 \
  -t nexo-caching-test:android-8.0 .

# Windows (requires Windows containers)
docker build -f .docker/Dockerfile.test-caching-windows \
  --build-arg DOTNET_VERSION=8.0 \
  -t nexo-caching-test:windows-8.0 .
```

### Run Tests Manually

```bash
# Run smoke tests (includes caching validation)
docker run --rm \
  -v "$(pwd)/test-results:/workspace/test-results" \
  nexo-caching-test:ubuntu-8.0 \
  dotnet test src/Nexo.Tests.GeospatialE2E/Nexo.Tests.GeospatialE2E.csproj \
    --filter 'FullyQualifiedName~GeospatialE2ESmokeTests' \
    --logger 'console;verbosity=normal'
```

## CI/CD Integration

### GitHub Actions

The workflow `.github/workflows/test-caching-multi-env.yml` automatically runs caching tests on:
- Push to `master` or `main` branches
- Pull requests targeting `master` or `main`
- Manual workflow dispatch

The workflow tests all environments in parallel and publishes results.

### Local CI Simulation

```bash
# Simulate CI environment locally
./scripts/test-caching-multi-env.sh --all
```

## Test Results

Test results are stored in `test-results/caching/`:
- `{environment}-unit.log` - Unit test output
- `{environment}-e2e.log` - E2E test output
- `{environment}-infra.log` - Infrastructure test output
- `{environment}-output.log` - Combined output
- `{environment}-build.log` - Docker build logs
- `*.trx` - Test result XML files

## Troubleshooting

### Docker Build Fails

```bash
# Check build logs
cat test-results/caching/{environment}-build.log

# Try building manually
docker build -f .docker/Dockerfile.test-caching \
  --build-arg DOTNET_VERSION=8.0 \
  -t nexo-caching-test:ubuntu-8.0 .
```

### Tests Fail in Specific Environment

1. Check the environment-specific log file:
   ```bash
   cat test-results/caching/{environment}-output.log
   ```

2. Run tests interactively:
   ```bash
   docker run -it --rm \
     -v "$(pwd)/test-results:/workspace/test-results" \
     nexo-caching-test:ubuntu-8.0 \
     bash
   ```

3. Run tests manually inside container:
   ```bash
   dotnet test src/Nexo.Tests.GeospatialUnit/Nexo.Tests.GeospatialUnit.csproj \
     --filter 'FullyQualifiedName~CachingTests' \
     --logger 'console;verbosity=detailed'
   ```

### Permission Issues

```bash
# Ensure script is executable
chmod +x scripts/test-caching-multi-env.sh

# Check Docker permissions
docker ps
```

## Platform-Specific Notes

### iOS Testing
- **Requires**: macOS with Xcode
- **Script**: `scripts/test-caching-ios.sh`
- **Note**: Cannot run in Docker; requires native macOS execution

### Unity Testing
- **Requires**: Unity installation (any platform)
- **Script**: `scripts/test-caching-unity.sh`
- **Environment Variables**:
  - `UNITY_BIN`: Path to Unity executable (auto-detected if not set)
  - `UNITY_PROJECT`: Path to Unity project (optional, auto-detected)
- **Note**: Falls back to .NET-only tests if Unity is not available

### Windows Testing
- **Requires**: Windows containers (not available on Linux/macOS Docker)
- **Dockerfile**: `.docker/Dockerfile.test-caching-windows`
- **Note**: Requires Docker Desktop with Windows containers enabled

### Android Testing
- **Requires**: Docker (includes Android SDK)
- **Dockerfile**: `.docker/Dockerfile.test-caching-android`
- **Note**: Includes Android SDK and build tools

## Adding New Environments

To add a new test environment:

1. **For Docker environments**: Create a new Dockerfile in `.docker/`:
   ```dockerfile
   ARG DOTNET_VERSION=8.0
   FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-{os-variant}
   # ... rest of Dockerfile
   ```

2. **For native environments**: Create a test script in `scripts/`:
   ```bash
   #!/bin/bash
   # Script to run caching tests on {platform}
   # ... test execution logic
   ```

3. Add environment to `scripts/test-caching-multi-env.sh`:
   ```bash
   ENV_CONFIGS["new-env"]=".docker/Dockerfile.test-caching-new|8.0|OS Name|docker"
   # or for native:
   ENV_CONFIGS["new-env"]="scripts/test-caching-new.sh|8.0|OS Name|native"
   ```

4. Add to GitHub Actions matrix in `.github/workflows/test-caching-multi-env.yml`

## Best Practices

1. **Run locally first**: Test changes locally before pushing
2. **Check all environments**: Don't assume one environment represents all
3. **Review logs**: Check detailed logs for environment-specific issues
4. **Keep Dockerfiles updated**: Ensure base images are current
5. **Test incrementally**: Test one environment at a time during development

## Performance

- **Parallel execution**: All environments run in parallel in CI
- **Caching**: Docker layer caching speeds up builds
- **Duration**: Full test suite takes ~5-10 minutes across all environments

## Support

For issues or questions:
1. Check test logs in `test-results/caching/`
2. Review GitHub Actions workflow runs
3. Consult the main [TROUBLESHOOTING_GUIDE.md](../TROUBLESHOOTING_GUIDE.md)
