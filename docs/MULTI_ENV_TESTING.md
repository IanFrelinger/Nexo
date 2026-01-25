# Multi-Environment Testing Guide

This guide explains how to test the geospatial caching functionality across different virtual environments.

## Overview

The caching tests are designed to run across multiple environments to ensure compatibility:
- **Operating Systems**: Ubuntu, Alpine Linux, Debian
- **.NET Versions**: 7.0, 8.0
- **Test Types**: Unit tests, E2E smoke tests, Infrastructure tests

## Quick Start

### Run All Tests Locally

```bash
# Run caching tests on all configured environments
make test-caching-all

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
```

## Available Environments

| Environment | OS | .NET Version | Dockerfile |
|------------|----|--------------|------------|
| `ubuntu-8.0` | Ubuntu 22.04 | 8.0 | `.docker/Dockerfile.test-caching` |
| `ubuntu-7.0` | Ubuntu 22.04 | 7.0 | `.docker/Dockerfile.test-caching` |
| `alpine-8.0` | Alpine Linux | 8.0 | `.docker/Dockerfile.test-caching-alpine` |
| `debian-8.0` | Debian 12 | 8.0 | `.docker/Dockerfile.test-caching-debian` |

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
```

### Run Tests Manually

```bash
# Run unit tests
docker run --rm \
  -v "$(pwd)/test-results:/workspace/test-results" \
  nexo-caching-test:ubuntu-8.0 \
  dotnet test src/Nexo.Tests.GeospatialUnit/Nexo.Tests.GeospatialUnit.csproj \
    --filter 'FullyQualifiedName~CachingTests' \
    --logger 'console;verbosity=normal'

# Run E2E tests
docker run --rm \
  -v "$(pwd)/test-results:/workspace/test-results" \
  nexo-caching-test:ubuntu-8.0 \
  dotnet test src/Nexo.Tests.GeospatialE2E/Nexo.Tests.GeospatialE2E.csproj \
    --filter 'FullyQualifiedName~CachingSmokeTests' \
    --logger 'console;verbosity=normal'

# Run infrastructure tests
docker run --rm \
  -v "$(pwd)/test-results:/workspace/test-results" \
  nexo-caching-test:ubuntu-8.0 \
  dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj \
    --filter 'FullyQualifiedName~Caching' \
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

## Adding New Environments

To add a new test environment:

1. Create a new Dockerfile in `.docker/`:
   ```dockerfile
   ARG DOTNET_VERSION=8.0
   FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-{os-variant}
   # ... rest of Dockerfile
   ```

2. Add environment to `scripts/test-caching-multi-env.sh`:
   ```bash
   ENV_CONFIGS["new-env"]=".docker/Dockerfile.test-caching-new|8.0|OS Name"
   ```

3. Add to GitHub Actions matrix in `.github/workflows/test-caching-multi-env.yml`

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
