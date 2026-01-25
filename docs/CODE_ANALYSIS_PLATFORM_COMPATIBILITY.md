# Code Analysis Platform Compatibility

## Overview

This document details the platform compatibility of the code analysis infrastructure (compilation and decompilation) across all target environments.

## Target Platforms

The code analysis service is designed to work on:
- **Windows** (Windows 10/11, Windows Server)
- **Linux** (Ubuntu, Alpine, Debian)
- **macOS** (Intel and Apple Silicon)
- **Android** (via Docker containers)
- **iOS** (native macOS execution)
- **Unity** (.NET Standard 2.0)

## Dependencies

### Microsoft.CodeAnalysis.CSharp (Roslyn)

**Version**: 4.8.0

**Platform Support**:
- ✅ .NET 6.0+
- ✅ .NET 7.0
- ✅ .NET 8.0
- ✅ .NET Standard 2.0 (with limitations)
- ✅ .NET Standard 2.1
- ✅ Windows, Linux, macOS
- ✅ Mobile platforms (Android, iOS)

**Unity Compatibility**:
- ⚠️ **Limited**: Roslyn 4.8.0 targets .NET Standard 2.0, but some APIs may not be available
- **Recommendation**: Test in Unity environment to verify full compatibility
- **Alternative**: Use older Roslyn version (3.x) for full .NET Standard 2.0 support if needed

### ICSharpCode.Decompiler

**Version**: 8.0.0.7345

**Platform Support**:
- ✅ .NET 6.0+
- ✅ .NET 7.0
- ✅ .NET 8.0
- ✅ .NET Standard 2.0
- ✅ Windows, Linux, macOS
- ✅ Mobile platforms (Android, iOS)

**Unity Compatibility**:
- ✅ **Supported**: ICSharpCode.Decompiler 8.0.0 supports .NET Standard 2.0
- **Note**: Some advanced features may be limited in Unity contexts

### System.Reflection

**Platform Support**:
- ✅ All .NET platforms
- ⚠️ **iOS Limitations**: `Assembly.LoadFrom()` may have restrictions
- ⚠️ **Unity Limitations**: Some reflection APIs may be restricted

## Platform-Specific Considerations

### Windows ✅

**Status**: Fully Supported

- All APIs available
- No known limitations
- Full Roslyn and decompiler support

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

### iOS ⚠️

**Status**: Limited Support

**Limitations**:
- `Assembly.LoadFrom()` may not work in all iOS contexts
- Some reflection APIs may be restricted
- Requires native macOS execution (not device/simulator)

**Recommendations**:
- Use `MetadataLoadContext` for assembly analysis if available
- Test assembly loading in iOS-specific contexts
- Consider alternative analysis methods for iOS

### Unity ⚠️

**Status**: Limited Support

**Limitations**:
- .NET Standard 2.0 compatibility required
- Some Roslyn APIs may not be available
- `Assembly.LoadFrom()` may have restrictions
- Reflection capabilities may be limited

**Recommendations**:
- Test compilation/decompilation in Unity environment
- Use Unity-compatible Roslyn version if needed
- Consider using `MetadataLoadContext` for assembly analysis
- Verify ICSharpCode.Decompiler works in Unity context

## Compatibility Testing

### Automated Tests

**`CodeAnalysisPlatformCompatibilityTests`** validates:
- Service availability on current platform
- Compilation works on all platforms
- Analysis works on all platforms
- No command-line dependencies

### Multi-Platform Tests

Code analysis tests run as part of:
- **Base Framework Smoke Tests** - Validates infrastructure
- **Multi-Platform Tests** - Validates across Docker containers

### Manual Verification

To verify compatibility on a specific platform:

```csharp
var checker = PlatformCompatibilityChecker.CheckCompatibility();
Console.WriteLine($"Platform: {checker.Platform}");
Console.WriteLine($"Compatible: {checker.IsCompatible}");
foreach (var issue in checker.Issues)
{
    Console.WriteLine($"Issue: {issue}");
}
```

## Known Issues and Workarounds

### Issue: Assembly.LoadFrom() on iOS

**Problem**: iOS may restrict loading assemblies from arbitrary paths.

**Workaround**: Use `MetadataLoadContext` or `Assembly.LoadFile()` if available.

### Issue: Roslyn in Unity

**Problem**: Some Roslyn APIs may not be available in .NET Standard 2.0.

**Workaround**: 
- Use Roslyn 3.x for full .NET Standard 2.0 support
- Or limit to compilation only (skip advanced analysis)

### Issue: ICSharpCode.Decompiler in Unity

**Problem**: Some decompiler features may be limited.

**Workaround**: Basic decompilation should work; advanced features may need testing.

## Recommendations

### For Maximum Compatibility

1. **Test in Target Environment**: Always test code analysis in the actual target environment
2. **Use Fallbacks**: Implement fallback mechanisms for platform-specific limitations
3. **Graceful Degradation**: Handle platform limitations gracefully
4. **Version Selection**: Use compatible package versions for target frameworks

### For Unity Specifically

1. **Verify .NET Standard 2.0**: Ensure all dependencies support .NET Standard 2.0
2. **Test Assembly Loading**: Verify `Assembly.LoadFrom()` works in Unity context
3. **Consider Alternatives**: Use `MetadataLoadContext` for better compatibility
4. **Test Decompilation**: Verify ICSharpCode.Decompiler works in Unity

## Future Enhancements

- **Platform-Specific Implementations**: Create platform-specific code analysis services
- **MetadataLoadContext Support**: Add support for better assembly loading
- **Unity-Specific Service**: Create Unity-optimized code analysis service
- **Mobile-Optimized Service**: Create mobile-optimized versions
