# Code Analysis Unity Docker Testing

## Overview

This guide explains how to test code analysis functionality in the Docker Unity environment to verify .NET Standard 2.0 compatibility.

## Prerequisites

1. **Docker Desktop** installed and running
2. **Internet connection** (for downloading Unity Docker image and .NET SDK)

## Quick Start

```bash
# Start Docker Desktop first, then run:
bash scripts/test-code-analysis-unity-docker.sh
```

## What Gets Tested

The Unity Docker environment tests:

1. **Code Analysis Platform Compatibility Tests**
   - Verifies Roslyn works in Unity environment
   - Verifies ICSharpCode.Decompiler works in Unity environment
   - Validates .NET Standard 2.0 compatibility

2. **Code Analysis Portability Tests**
   - Tests compilation functionality
   - Tests decompilation functionality
   - Tests assembly analysis
   - Validates round-trip code preservation

3. **Code Analysis Smoke Tests**
   - Basic availability checks
   - Service instantiation
   - Platform compatibility validation

## Docker Image Details

The Unity Docker image:
- **Base**: `unityci/editor:ubuntu-2022.3.0f1-linux-il2cpp-1.0.0`
- **.NET SDK**: 8.0
- **Target Framework**: .NET Standard 2.0 (Unity compatible)
- **Environment**: Ubuntu 22.04 with Unity Editor

## Test Execution

The test script:

1. **Builds** the Unity Docker image
2. **Runs** code analysis tests inside the container
3. **Validates** .NET Standard 2.0 compatibility
4. **Generates** test result files (TRX format)

## Test Results

Test results are saved to:
- `test-results/unity-code-analysis/unity-code-analysis-platform.trx`
- `test-results/unity-code-analysis/unity-code-analysis-portability.trx`
- `test-results/unity-code-analysis/unity-code-analysis-smoke.trx`
- `test-results/unity-code-analysis/unity-docker-test.log`

## Manual Testing

If you want to run tests manually:

```bash
# Build the image
docker build -f .docker/Dockerfile.test-framework-unity -t nexo-unity-test:latest .

# Run tests interactively
docker run -it --rm \
  -v "$(pwd)/test-results:/workspace/test-results" \
  nexo-unity-test:latest \
  bash

# Inside container, run tests:
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj \
  --filter 'FullyQualifiedName~CodeAnalysisPlatformCompatibilityTests' \
  --logger 'console;verbosity=normal'
```

## What This Validates

✅ **Roslyn Compatibility**: Microsoft.CodeAnalysis.CSharp 4.8.0 works in Unity environment  
✅ **Decompiler Compatibility**: ICSharpCode.Decompiler 8.0.0.7345 works in Unity environment  
✅ **Assembly Loading**: `Assembly.Load(byte[])` works (.NET Standard 2.0 compatible)  
✅ **Portability**: All code analysis features work without command-line dependencies  
✅ **Cross-Platform**: Code analysis works in Unity's .NET Standard 2.0 environment  

## Troubleshooting

### Docker Not Running
```
ERROR: Cannot connect to the Docker daemon
```
**Solution**: Start Docker Desktop and wait for it to fully start.

### Unity Image Download Fails
```
Error pulling image unityci/editor:...
```
**Solution**: Check internet connection. The Unity Docker image is large (~10GB) and may take time to download.

### Build Fails
```
error: Package restore failed
```
**Solution**: Check that all project files are present and `Directory.Packages.props` is correct.

### Tests Fail
```
Test execution failed
```
**Solution**: Check the test log files in `test-results/unity-code-analysis/` for detailed error messages.

## Integration with CI/CD

The Unity Docker tests can be integrated into CI/CD pipelines:

```yaml
# Example GitHub Actions
- name: Test Code Analysis in Unity
  run: |
    docker build -f .docker/Dockerfile.test-framework-unity -t nexo-unity-test .
    docker run --rm nexo-unity-test
```

## Next Steps

After verifying code analysis works in Unity Docker:
1. Test in actual Unity Editor (if available)
2. Test in Unity Cloud Build
3. Validate with Unity Test Runner
4. Add Unity-specific code analysis features if needed
