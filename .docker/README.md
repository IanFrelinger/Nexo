# Local Docker Testing

This directory contains Docker setup for running Nexo CLI unit tests locally before pushing to the repository.

## Quick Start

### Run tests on Linux (Ubuntu)

```bash
./scripts/test-local.sh
```

### Run tests on all platforms including mobile

```bash
./scripts/test-local.sh --mobile
```

### Run tests on specific platform

```bash
./scripts/test-local.sh --platform ubuntu
./scripts/test-android.sh  # Android (requires Docker)
./scripts/test-ios.sh      # iOS (requires macOS with Xcode)
```

## Platform Support

### Linux (Ubuntu)
- **Method**: Docker container
- **Requirements**: Docker
- **Command**: `./scripts/test-local.sh --platform ubuntu`

### Android
- **Method**: Docker container with Android SDK
- **Requirements**: Docker
- **Command**: `./scripts/test-android.sh`
- **Note**: Uses Android SDK in Docker for testing Android-specific file paths and behaviors

### iOS
- **Method**: Native macOS execution
- **Requirements**: 
  - macOS
  - Xcode installed
  - .NET 8.0 SDK
- **Command**: `./scripts/test-ios.sh`
- **Note**: iOS testing must be run on a Mac

## Options

### Run on specific platform

```bash
./scripts/test-local.sh --platform ubuntu
./scripts/test-local.sh --platform android
./scripts/test-local.sh --platform ios  # macOS only
```

### Run all platforms (including mobile)

```bash
./scripts/test-local.sh --mobile
```

### Quick test (skip build cache)

```bash
./scripts/test-local.sh --quick
```

## Manual Docker Usage

### Build the test image

```bash
# Linux/Ubuntu
docker build -f .docker/Dockerfile.test -t nexo-test:ubuntu .

# Android
docker build -f .docker/Dockerfile.test-android -t nexo-test:android .
```

### Run tests manually

```bash
# Linux
docker run --rm \
  -v "$(pwd)/test-results:/workspace/test-results" \
  nexo-test:ubuntu \
  bash -c "cd src/Nexo.CLI && dotnet run --project Nexo.CLI.csproj -- test --format-json"

# Android
docker run --rm \
  -v "$(pwd)/test-results:/workspace/test-results" \
  nexo-test:android \
  bash -c "cd src/Nexo.CLI && dotnet run --project Nexo.CLI.csproj -- test --format-json"
```

## Test Results

Test results are saved to `test-results/` directory:
- `ubuntu-results.json` - Linux test results
- `android-results.json` - Android test results
- `ios-results.json` - iOS test results
- `*-logs.txt` - Test execution logs

## Platform-Specific Notes

### Android
- Uses Android SDK in Docker container
- Tests Android-specific file system behaviors
- Requires Docker with sufficient resources

### iOS
- Must run on macOS (cannot run in Docker)
- Requires Xcode for iOS simulator support
- Tests iOS-specific file system behaviors

## Troubleshooting

### Docker not running
```bash
# Start Docker Desktop or Docker daemon
```

### Permission denied
```bash
chmod +x scripts/test-local.sh scripts/test-android.sh scripts/test-ios.sh
```

### Build fails
```bash
# Clean Docker cache
docker system prune -a
```

### iOS test fails
- Ensure you're on macOS
- Install Xcode from App Store
- Verify .NET SDK is installed: `dotnet --version`

### Android test fails
- Ensure Docker has enough resources (memory, CPU)
- Android SDK download may take time on first run
- Check Docker logs for Android SDK installation issues

### Test results not found
Check `test-results/` directory and logs in `test-results/*-logs.txt`
