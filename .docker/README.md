# Local Docker Testing

This directory contains Docker setup for running Nexo CLI unit tests locally before pushing to the repository.

## Quick Start

Run tests locally using Docker:

```bash
./scripts/test-local.sh
```

This will:
1. Build a Docker image with .NET 8.0 SDK
2. Run all unit tests in the container
3. Extract and display test results
4. Save results to `test-results/` directory

## Options

### Run on specific platform

```bash
./scripts/test-local.sh --platform ubuntu
```

### Run all platforms (if you have multiple Docker images)

```bash
./scripts/test-local.sh --all
```

### Quick test (skip build cache)

```bash
./scripts/test-local.sh --quick
```

## Manual Docker Usage

### Build the test image

```bash
docker build -f .docker/Dockerfile.test -t nexo-test:ubuntu .
```

### Run tests manually

```bash
docker run --rm \
  -v "$(pwd)/test-results:/workspace/test-results" \
  nexo-test:ubuntu \
  bash -c "cd src/Nexo.CLI && dotnet run --project Nexo.CLI.csproj -- test --format-json"
```

## Test Results

Test results are saved to `test-results/` directory:
- `ubuntu-results.json` - JSON test results
- `ubuntu-logs.txt` - Test execution logs

## Platform Support

Currently supports:
- **Ubuntu** (via Docker Linux containers)

Note: Windows and macOS testing requires:
- Windows: Windows containers (Docker Desktop with WSL2 backend)
- macOS: macOS runners (not available in Docker, use GitHub Actions)

For full cross-platform testing, use GitHub Actions which runs on native Windows/macOS runners.

## Integration with CI/CD

The local Docker tests mirror the GitHub Actions workflow (`.github/workflows/unit-tests.yml`), so passing locally should mean passing in CI.

## Troubleshooting

### Docker not running
```bash
# Start Docker Desktop or Docker daemon
```

### Permission denied
```bash
chmod +x scripts/test-local.sh
```

### Build fails
```bash
# Clean Docker cache
docker system prune -a
```

### Test results not found
Check `test-results/` directory and logs in `test-results/*-logs.txt`

