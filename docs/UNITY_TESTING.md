# Unity Test Environment Guide

This guide explains how to test Nexo framework components in Unity environments.

## Overview

The Nexo framework is designed to be Unity-compatible, with core components targeting .NET Standard 2.0 to match Unity's runtime requirements.

## Unity Compatibility

### Framework Components

- **Nexo.Core.Domain** - Targets .NET Standard 2.0 (Unity compatible)
- **Nexo.Core.UI.Unity** - Unity-specific UI components
- **Nexo.Core.Application** - Application layer (may require .NET 8.0 for full features)

### Testing Strategy

1. **.NET Standard 2.0 Compatibility Tests** - Verify core components build for Unity
2. **Unity Test Runner** - Run tests within Unity Editor (if Unity is installed)
3. **Docker Unity Environment** - Test in containerized Unity environment

## Running Unity Tests

### Option 1: Native Unity Testing (Recommended)

```bash
# Run Unity compatibility tests
bash scripts/test-framework-unity.sh
```

This script will:
- Test .NET Standard 2.0 compatibility
- Build Unity UI components
- Run Unity Test Runner (if Unity is installed)

### Option 2: Docker Unity Environment

```bash
# Test in Docker Unity environment
bash scripts/test-framework-multi-env.sh --env unity-8.0
```

### Option 3: All Environments (Including Unity)

```bash
# Run tests on all environments including Unity
bash scripts/test-framework-multi-env.sh --all
```

## Unity Test Project Setup

To create a Unity test project:

1. **Install Unity Hub** and create a new Unity project (2021.3 LTS or later)

2. **Add Nexo packages** to your Unity project:
   - Copy `src/Nexo.Core.Domain` to `Assets/Packages/Nexo.Core.Domain`
   - Copy `src/Nexo.Core.UI.Unity` to `Assets/Packages/Nexo.Core.UI.Unity`

3. **Install Unity Test Framework**:
   - Open Package Manager
   - Add package: `com.unity.test-framework`

4. **Create test assemblies**:
   - Create `Assets/Tests/EditMode` folder
   - Create test scripts that reference Nexo components

## Test Coverage

### Unity-Compatible Tests

- ✅ Domain layer tests (Nexo.Core.Domain)
- ✅ Unity UI component tests (Nexo.Core.UI.Unity)
- ✅ .NET Standard 2.0 compatibility verification

### Unity Test Runner

If Unity is installed, the test script will:
- Detect Unity installation
- Run EditMode tests
- Generate test results in XML format

## Requirements

### For Native Testing
- .NET SDK 8.0
- Unity Hub (optional, for full Unity Test Runner)

### For Docker Testing
- Docker Desktop
- Unity Docker image (configured in Dockerfile)

## Troubleshooting

### Unity Not Found

If Unity is not detected:
- Install Unity Hub from https://unity.com/download
- The script will still test .NET Standard 2.0 compatibility

### Test Failures

- Check that .NET Standard 2.0 target framework is available
- Verify Unity project structure if using Unity Test Runner
- Review logs in `test-results/framework/unity/`

## Integration with CI/CD

Unity tests can be integrated into CI/CD pipelines:

```yaml
# Example GitHub Actions workflow
- name: Test Unity Compatibility
  run: bash scripts/test-framework-unity.sh
```

## Next Steps

- Create Unity test project with comprehensive test coverage
- Set up Unity Cloud Build integration
- Add Unity-specific performance tests
