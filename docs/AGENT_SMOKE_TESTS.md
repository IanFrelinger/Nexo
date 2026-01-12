# Agent Smoke Tests

## Overview

Smoke tests have been created for both the Universal Testing Agent and Autonomous Development Agent to verify basic functionality and configuration validation.

## Test Files

### Universal Testing Agent Smoke Tests
**Location:** `src/Nexo.Tests.Infrastructure/Tests/Agents/UniversalTesterAgentSmokeTests.cs`

**Tests:**
1. **TestAgentInitialization** - Verifies the agent can be instantiated with required dependencies
2. **TestConfigValidation** - Validates configuration objects can be created with minimal and full configurations
3. **TestBasicExecution** - Verifies agent accepts valid configurations
4. **TestTargetTypeInference** - Tests target type detection for different target formats (URL, API, CLI)

### Autonomous Development Agent Smoke Tests
**Location:** `src/Nexo.Tests.Infrastructure/Tests/Agents/AutonomousDevAgentSmokeTests.cs`

**Tests:**
1. **TestAgentInitialization** - Verifies the agent can be instantiated with Universal Tester dependency
2. **TestConfigValidation** - Validates DevTaskConfig with minimal and full configurations
3. **TestProjectAdapterCreation** - Tests different project type configurations
4. **TestAutonomyLevels** - Verifies all autonomy levels (Supervised, SemiAutonomous, FullyAutonomous)
5. **TestMockUserPersonas** - Tests all mock user persona types

## Running the Tests

```bash
# Run all infrastructure tests
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj

# Run only agent smoke tests
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj --filter "FullyQualifiedName~AgentSmokeTests"
```

## Test Coverage

### Universal Testing Agent
- ✅ Agent initialization
- ✅ Configuration validation (minimal and full)
- ✅ Target type inference
- ✅ Testing depth options
- ✅ Constraints and setup instructions

### Autonomous Development Agent
- ✅ Agent initialization with dependencies
- ✅ Configuration validation (minimal and full)
- ✅ Project type handling
- ✅ Autonomy level configuration
- ✅ Mock user persona configuration
- ✅ Task configuration options

## Notes

These are **smoke tests** - they verify:
- Components can be instantiated
- Configuration objects can be created
- Basic properties are set correctly
- No runtime exceptions during initialization

They do **not** test:
- Full agent execution (requires actual adapters and providers)
- Integration with external systems
- End-to-end workflows

For full integration testing, see the CLI command tests and end-to-end test suites.
