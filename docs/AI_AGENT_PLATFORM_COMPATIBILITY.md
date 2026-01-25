# AI Agent Platform Compatibility

## Overview

This document details the platform compatibility of embedded AI agents (`AutonomousDevAgent` and `UniversalTesterAgent`) across all target environments.

## Target Platforms

The AI agents are designed to work on:
- **Windows** (Windows 10/11, Windows Server)
- **Linux** (Ubuntu, Alpine, Debian)
- **macOS** (Intel and Apple Silicon)
- **Android** (via Docker containers)
- **iOS** (native macOS execution)
- **Unity** (.NET Standard 2.0)

## Embedded AI Agents

### AutonomousDevAgent

**Location**: `src/Nexo.Agents.AutonomousDev/AutonomousDevAgent.cs`

**Dependencies**:
- ✅ `Nexo.Core.Domain` - Core domain layer
- ✅ `Nexo.Infrastructure` - Infrastructure layer
- ✅ `Microsoft.Extensions.Logging` - Logging (portable)
- ✅ `Nexo.Agents.UniversalTester` - Universal tester agent (for testing)

**Platform Support**:
- ✅ .NET 8.0
- ✅ Windows, Linux, macOS
- ✅ Mobile platforms (Android, iOS via Docker)
- ✅ Unity (via .NET Standard 2.0 compatible APIs)

**Portability**:
- ✅ Uses portable .NET APIs only
- ✅ No command-line dependencies
- ✅ No platform-specific APIs
- ✅ Fully portable across all platforms

### UniversalTesterAgent

**Location**: `src/Nexo.Agents.UniversalTester/UniversalTesterAgent.cs`

**Dependencies**:
- ✅ `Nexo.Core.Domain` - Core domain layer
- ✅ `Nexo.Infrastructure` - Infrastructure layer
- ✅ `Microsoft.Extensions.Logging` - Logging (portable)
- ⚠️ `Microsoft.Playwright` - **Optional** (only needed for web testing)
- ⚠️ `SixLabors.ImageSharp` - **Optional** (only needed for image processing)

**Platform Support**:
- ✅ .NET 8.0
- ✅ Windows, Linux, macOS
- ✅ Mobile platforms (Android, iOS via Docker)
- ✅ Unity (via .NET Standard 2.0 compatible APIs)

**Portability**:
- ✅ Uses portable .NET APIs only
- ✅ No command-line dependencies
- ✅ No platform-specific APIs
- ⚠️ Playwright requires browser installation (optional)
- ✅ Fully portable for core functionality

## Platform-Specific Considerations

### Windows ✅

**Status**: Fully Supported

- All APIs available
- No known limitations
- Full agent support

### Linux ✅

**Status**: Fully Supported

- All APIs available
- No known limitations
- Works in Docker containers

### macOS ✅

**Status**: Fully Supported

- All APIs available
- Works on both Intel and Apple Silicon
- Native execution for iOS testing

### Android ✅

**Status**: Supported via Docker

- Runs in Docker containers
- Full API support within container
- No native Android app limitations

### iOS ✅

**Status**: Supported

- Works via native macOS execution
- Full API support
- No iOS-specific limitations

### Unity ⚠️

**Status**: Supported (with limitations)

**Limitations**:
- Agents target .NET 8.0 (not .NET Standard 2.0)
- May need to be used via .NET Standard 2.0 compatible interfaces
- Playwright not available in Unity (optional dependency)

**Recommendations**:
- Use agents through abstraction layers
- Test in Unity environment to verify compatibility
- Playwright features will not work in Unity (web testing only)

## Dependencies

### Required Dependencies

1. **Microsoft.Extensions.Logging**
   - ✅ Portable across all platforms
   - ✅ .NET Standard 2.0 compatible
   - ✅ Works in Unity

2. **Nexo.Core.Domain**
   - ✅ Targets .NET Standard 2.0 and .NET 8.0
   - ✅ Unity compatible

3. **Nexo.Infrastructure**
   - ✅ Targets .NET 8.0
   - ✅ Uses .NET Standard 2.0 compatible APIs

### Optional Dependencies

1. **Microsoft.Playwright** (UniversalTesterAgent only)
   - ⚠️ **Optional**: Only needed for web testing
   - ⚠️ **Not available in Unity**: Requires browser installation
   - ✅ Core agent functionality works without it

2. **SixLabors.ImageSharp** (UniversalTesterAgent only)
   - ⚠️ **Optional**: Only needed for image processing
   - ✅ Portable across all platforms
   - ✅ Works in Unity

## Compatibility Testing

### Automated Tests

**`AgentPlatformCompatibilityTests`** validates:
- Agent availability on current platform
- Agent instantiation works on all platforms
- No command-line dependencies
- Portable library usage

### Multi-Platform Tests

AI agent tests run as part of:
- **Base Framework Smoke Tests** - Validates infrastructure
- **Multi-Platform Tests** - Validates across Docker containers

### Manual Verification

To verify compatibility on a specific platform:

```csharp
var checker = AgentPlatformCompatibilityChecker.CheckCompatibility();
Console.WriteLine($"Platform: {checker.Platform}");
Console.WriteLine($"Compatible: {checker.IsCompatible}");
foreach (var issue in checker.Issues)
{
    Console.WriteLine($"Issue: {issue}");
}
```

## Known Issues and Workarounds

### Issue: Playwright in Unity ⚠️

**Problem**: Microsoft.Playwright requires browser installation and is not available in Unity.

**Workaround**: 
- Playwright is optional - only needed for web testing
- Core UniversalTesterAgent functionality works without it
- Use other adapters (CLI, Desktop, Game) instead

### Issue: .NET 8.0 in Unity ⚠️

**Problem**: Agents target .NET 8.0, Unity uses .NET Standard 2.0.

**Workaround**: 
- Use agents through abstraction layers
- Test compatibility in Unity environment
- Consider creating Unity-specific agent wrappers if needed

## Recommendations

### For Maximum Compatibility

1. **Test in Target Environment**: Always test agents in the actual target environment
2. **Use Abstractions**: Use agent interfaces rather than concrete implementations
3. **Handle Optional Dependencies**: Gracefully handle missing optional dependencies
4. **Platform Detection**: Use platform detection to enable/disable features

### For Unity Specifically

1. **Use Abstractions**: Access agents through interfaces
2. **Test Compatibility**: Verify agents work in Unity environment
3. **Skip Optional Features**: Don't use Playwright features in Unity
4. **Consider Wrappers**: Create Unity-specific wrappers if needed

## Future Enhancements

- **Unity-Specific Agents**: Create Unity-optimized agent implementations
- **Platform-Specific Implementations**: Create platform-specific agent versions
- **Dependency Injection**: Improve DI support for platform-specific features
- **Feature Detection**: Add runtime feature detection for optional dependencies
