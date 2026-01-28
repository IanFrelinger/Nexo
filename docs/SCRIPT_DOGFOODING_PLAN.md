# Script Dogfooding Plan

**Goal:** Replace all bash scripts in `scripts/` with Nexo CLI commands that use Nexo's own capabilities.

## Strategy

Instead of external tools (bash, docker CLI, etc.), all scripts will use:
- **Nexo's Execution Platform Abstraction** (`IExecutionPlatform`) - for Docker/container operations
- **Nexo's Test Runner** (`TestRunnerAdapter`) - for test execution
- **Nexo's Orchestration** - for complex workflows
- **Nexo CLI Commands** - for all operations

## Implementation Status

### ✅ Completed

1. **`nexo test`** - Multi-platform test execution
   - Replaces: `test-caching-multi-env.sh`, `test-framework-multi-env.sh`, `test-ai-agent-multi-env.sh`, etc.
   - Uses: `IExecutionPlatform`, `DockerExecutionPlatform`, `TestRunnerAdapter`
   - Features:
     - Test across multiple platforms (ubuntu, alpine, debian, android, ios, unity, windows, macos)
     - Uses execution platform abstraction (Docker, Rancher, Kubernetes)
     - Native execution for platforms that don't support containers
     - JSON output for CI/CD integration

2. **`nexo test local`** - Local test execution
   - Replaces: `test-local.sh`
   - Uses: `TestRunnerAdapter` from Application layer

### 🚧 In Progress

3. **`nexo docker`** - Docker operations using execution platform
   - Will replace: Direct docker CLI calls in scripts
   - Uses: `IExecutionPlatform` abstraction
   - Commands:
     - `nexo docker build` - Build images
     - `nexo docker run` - Run containers
     - `nexo docker clean` - Clean up images/containers

4. **`nexo build`** - Build operations
   - Will replace: `build-portable.sh`
   - Uses: Nexo's build capabilities

5. **`nexo ci`** - CI/CD workflows
   - Will replace: `ci-verify.sh`, `check-promotion.sh`, etc.
   - Uses: Nexo orchestration for complex workflows

6. **`nexo aggregate`** - Test result aggregation
   - Will replace: `aggregate-junit.sh`, `aggregate-junit-cs.sh`
   - Uses: Nexo's test result parsing and aggregation

## Script Mapping

### Test Scripts → `nexo test`

| Old Script | New Command |
|------------|-------------|
| `test-caching-multi-env.sh` | `nexo test --platforms ubuntu alpine debian android` |
| `test-framework-multi-env.sh` | `nexo test --project Nexo.Tests.Infrastructure --platforms ubuntu alpine debian` |
| `test-ai-agent-multi-env.sh` | `nexo test --project Nexo.Tests.GeospatialE2E --filter "GeospatialAIAgentTests"` |
| `test-local.sh` | `nexo test local` |
| `test-android.sh` | `nexo test --platforms android` |
| `test-ios.sh` | `nexo test --platforms ios` |
| `test-caching-ios.sh` | `nexo test --platforms ios` |
| `test-caching-unity.sh` | `nexo test --platforms unity` |
| `test-framework-unity.sh` | `nexo test --project Nexo.Tests.Infrastructure --platforms unity` |

### Build Scripts → `nexo build` (to be implemented)

| Old Script | New Command |
|------------|-------------|
| `build-portable.sh` | `nexo build --portable` |

### CI Scripts → `nexo ci` (to be implemented)

| Old Script | New Command |
|------------|-------------|
| `ci-verify.sh` | `nexo ci verify` |
| `check-promotion.sh` | `nexo ci check-promotion` |
| `check-promotion-cs.sh` | `nexo ci check-promotion` |

### Utility Scripts → Various commands (to be implemented)

| Old Script | New Command |
|------------|-------------|
| `aggregate-junit.sh` | `nexo aggregate junit` |
| `aggregate-junit-cs.sh` | `nexo aggregate junit` |
| `artifact-diff.sh` | `nexo diff artifacts` |

## Benefits

1. **Self-Contained**: No external tool dependencies
2. **Cross-Platform**: Works on Windows, Linux, macOS
3. **Type-Safe**: Compile-time checking
4. **Debuggable**: Can debug in C#
5. **Consistent**: Same patterns throughout
6. **Extensible**: Easy to add new platforms/features

## Migration Path

1. ✅ Create `nexo test` command
2. ⏳ Create `nexo docker` command
3. ⏳ Create `nexo build` command
4. ⏳ Create `nexo ci` command
5. ⏳ Create `nexo aggregate` command
6. ⏳ Update documentation
7. ⏳ Deprecate old scripts (keep for backward compatibility)
8. ⏳ Remove old scripts after migration period
