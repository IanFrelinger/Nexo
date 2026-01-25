# Code Analysis Portability

## Overview

The Nexo framework includes a portable code compilation and decompilation infrastructure that works across all .NET platforms without command-line dependencies.

## Architecture

### Core Interface: `ICodeAnalysisService`

All code analysis operations are abstracted through `ICodeAnalysisService`:

- **Compilation**: `CompileAsync()` - Compile C# source code to assemblies
- **Decompilation**: `DecompileAsync()` - Decompile assemblies back to C# source
- **Analysis**: `AnalyzeAssemblyAsync()` - Extract metadata from assemblies

### Implementation: `RoslynCodeAnalysisService`

**Location**: `src/Nexo.Infrastructure/Testing/CodeAnalysis/RoslynCodeAnalysisService.cs`

- Uses **Microsoft.CodeAnalysis (Roslyn)** for compilation
- Uses **ICSharpCode.Decompiler** for decompilation
- Uses **System.Reflection** for assembly analysis
- Fully portable - no command-line dependencies
- Works on Windows, Linux, macOS, mobile, Unity

## Integration with Tests

### Base Framework Tests

**`CodeAnalysisSmokeTests`** (`src/Nexo.Tests.Infrastructure/Tests/BaseFramework/CodeAnalysisSmokeTests.cs`)

- Validates code analysis infrastructure is available
- Tests compilation, decompilation, and analysis
- Runs as part of base framework tests across all platforms

### Portability Tests

**`CodeAnalysisPortabilityTests`** (`src/Nexo.Tests.Infrastructure/Tests/CodeAnalysis/CodeAnalysisPortabilityTests.cs`)

- Comprehensive tests for compilation/decompilation
- Round-trip testing (compile → decompile → verify)
- Error handling validation
- Portability verification

## Usage

### Dependency Injection

The service is registered in `Program.cs`:

```csharp
services.AddSingleton<ICodeAnalysisService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<RoslynCodeAnalysisService>>();
    return new RoslynCodeAnalysisService(logger);
});
```

### Compiling Code

```csharp
var service = serviceProvider.GetRequiredService<ICodeAnalysisService>();

var sourceCode = @"
public class TestClass
{
    public int Value => 42;
}
";

var result = await service.CompileAsync(
    sourceCode,
    "TestAssembly",
    "output/TestAssembly.dll",
    null,
    CancellationToken.None);

if (result.Success)
{
    // Assembly compiled successfully
    Console.WriteLine($"Assembly: {result.AssemblyPath}");
}
else
{
    // Handle errors
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Error: {error}");
    }
}
```

### Decompiling Assemblies

```csharp
var result = await service.DecompileAsync(
    "input/SomeAssembly.dll",
    "output/SomeAssembly.cs",
    CancellationToken.None);

if (result.Success)
{
    // Source code available
    Console.WriteLine(result.SourceCode);
}
```

### Analyzing Assemblies

```csharp
var result = await service.AnalyzeAssemblyAsync(
    "SomeAssembly.dll",
    CancellationToken.None);

if (result.Success)
{
    Console.WriteLine($"Assembly: {result.AssemblyName}");
    Console.WriteLine($"Version: {result.Version}");
    Console.WriteLine($"Types: {result.Types.Count()}");
    Console.WriteLine($"Methods: {result.Methods.Count()}");
}
```

## Portability Features

### ✅ No Command-Line Dependencies

- Uses Roslyn API directly (no `csc.exe` or `dotnet build`)
- Uses ICSharpCode.Decompiler API (no external decompiler tools)
- All operations are in-process

### ✅ Cross-Platform

- Works on Windows, Linux, macOS
- Works on mobile platforms (Android, iOS)
- Works in Unity (.NET Standard 2.0 compatible)

### ✅ Integrated Testing

- Smoke tests run as part of base framework tests
- Portability tests verify cross-platform compatibility
- Tests run in Docker containers for validation

## Test Coverage

### Compilation Tests

- ✅ Simple code compilation
- ✅ Compilation error reporting
- ✅ Warning collection
- ✅ Reference handling

### Decompilation Tests

- ✅ Assembly decompilation
- ✅ Missing assembly handling
- ✅ Round-trip testing (compile → decompile)

### Analysis Tests

- ✅ Metadata extraction
- ✅ Type enumeration
- ✅ Method enumeration
- ✅ Error handling

## Benefits

1. **Portability**: Works anywhere .NET runs
2. **No External Tools**: Pure .NET implementation
3. **Testable**: Easy to mock and test
4. **Integrated**: Part of base framework infrastructure
5. **Extensible**: Interface allows alternative implementations

## Future Enhancements

- **Alternative Implementations**: Other decompilers or compilers
- **Language Support**: VB.NET, F# compilation
- **Advanced Analysis**: IL analysis, dependency graphs
- **Performance**: Caching, parallel compilation
