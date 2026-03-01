using FluentAssertions;
using Microsoft.Extensions.Logging;
using Nexo.Infrastructure.Testing.CodeAnalysis;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.CodeAnalysis;

/// <summary>
/// Tests for code compilation and decompilation portability.
/// 
/// Validates that compilation/decompilation works across different platforms
/// and is truly portable (no command-line dependencies).
/// </summary>
public class CodeAnalysisPortabilityTests : IDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ICodeAnalysisService _codeAnalysisService;
    private readonly string _tempDir;

    public CodeAnalysisPortabilityTests()
    {
        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        _codeAnalysisService = new RoslynCodeAnalysisService(_loggerFactory.CreateLogger<RoslynCodeAnalysisService>());
        _tempDir = Path.Combine(Path.GetTempPath(), "nexo-code-analysis-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public async Task CompileAsync_ShouldCompileSimpleCSharpCode()
    {
        // Arrange
        var sourceCode = @"
using System;

public class TestClass
{
    public static void Main()
    {
        Console.WriteLine(""Hello, World!"");
    }
}
";
        var assemblyName = "TestAssembly";
        var outputPath = Path.Combine(_tempDir, $"{assemblyName}.dll");

        // Act
        var result = await _codeAnalysisService.CompileAsync(
            sourceCode,
            assemblyName,
            outputPath,
            null,
            CancellationToken.None);

        // Assert
        var errorDetail = result.Errors.Any() ? string.Join("; ", result.Errors) : "compilation failed";
        result.Success.Should().BeTrue(errorDetail);
        result.AssemblyPath.Should().Be(outputPath);
        File.Exists(outputPath).Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task CompileAsync_ShouldReportCompilationErrors()
    {
        // Arrange
        var sourceCode = @"
using System;

public class TestClass
{
    public static void Main()
    {
        InvalidSyntaxHere // This will cause a compilation error
    }
}
";
        var assemblyName = "InvalidAssembly";
        var outputPath = Path.Combine(_tempDir, $"{assemblyName}.dll");

        // Act
        var result = await _codeAnalysisService.CompileAsync(
            sourceCode,
            assemblyName,
            outputPath,
            null,
            CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors.Should().Contain(e => 
            e.Contains("error", StringComparison.OrdinalIgnoreCase) || 
            e.Contains("CS", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DecompileAsync_ShouldDecompileAssembly()
    {
        // Arrange - First compile an assembly
        var sourceCode = @"
using System;

public class TestClass
{
    public int Add(int a, int b) => a + b;
}
";
        var assemblyName = "DecompileTest";
        var assemblyPath = Path.Combine(_tempDir, $"{assemblyName}.dll");
        
        var compileResult = await _codeAnalysisService.CompileAsync(
            sourceCode,
            assemblyName,
            assemblyPath,
            null,
            CancellationToken.None);
        
        compileResult.Success.Should().BeTrue("Assembly must compile successfully for decompilation test");

        var outputPath = Path.Combine(_tempDir, "Decompiled.cs");

        // Act
        var result = await _codeAnalysisService.DecompileAsync(
            assemblyPath,
            outputPath,
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.SourceCode.Should().NotBeNullOrEmpty();
        result.OutputPath.Should().Be(outputPath);
        File.Exists(outputPath).Should().BeTrue();
        result.SourceCode.Should().Contain("TestClass");
        result.SourceCode.Should().Contain("Add");
    }

    [Fact]
    public async Task DecompileAsync_ShouldHandleMissingAssembly()
    {
        // Arrange
        var nonExistentAssembly = Path.Combine(_tempDir, "NonExistent.dll");
        var outputPath = Path.Combine(_tempDir, "Output.cs");

        // Act
        var result = await _codeAnalysisService.DecompileAsync(
            nonExistentAssembly,
            outputPath,
            CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task AnalyzeAssemblyAsync_ShouldExtractMetadata()
    {
        // Arrange - First compile an assembly
        var sourceCode = @"
using System;

public class TestClass
{
    public void Method1() { }
    private void Method2() { }
}

public class AnotherClass
{
    public static void StaticMethod() { }
}
";
        var assemblyName = "AnalysisTest";
        var assemblyPath = Path.Combine(_tempDir, $"{assemblyName}.dll");
        
        var compileResult = await _codeAnalysisService.CompileAsync(
            sourceCode,
            assemblyName,
            assemblyPath,
            null,
            CancellationToken.None);
        
        compileResult.Success.Should().BeTrue("Assembly must compile successfully for analysis test");

        // Act
        var result = await _codeAnalysisService.AnalyzeAssemblyAsync(
            assemblyPath,
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.AssemblyName.Should().Be(assemblyName);
        result.Types.Should().Contain(t => t.Contains("TestClass"));
        result.Types.Should().Contain(t => t.Contains("AnotherClass"));
        result.Methods.Should().Contain(m => m.Contains("Method1"));
        result.Methods.Should().Contain(m => m.Contains("StaticMethod"));
    }

    [Fact]
    public async Task AnalyzeAssemblyAsync_ShouldHandleMissingAssembly()
    {
        // Arrange
        var nonExistentAssembly = Path.Combine(_tempDir, "NonExistent.dll");

        // Act
        var result = await _codeAnalysisService.AnalyzeAssemblyAsync(
            nonExistentAssembly,
            CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task CompileAndDecompile_RoundTrip_ShouldPreserveCode()
    {
        // Arrange
        var originalSource = @"
using System;

public class RoundTripTest
{
    public string GetMessage() => ""Hello from round trip!"";
    public int Calculate(int x, int y) => x * y + 10;
}
";
        var assemblyName = "RoundTrip";
        var assemblyPath = Path.Combine(_tempDir, $"{assemblyName}.dll");
        var decompiledPath = Path.Combine(_tempDir, "RoundTrip_Decompiled.cs");

        // Act - Compile
        var compileResult = await _codeAnalysisService.CompileAsync(
            originalSource,
            assemblyName,
            assemblyPath,
            null,
            CancellationToken.None);
        
        compileResult.Success.Should().BeTrue("Compilation must succeed");

        // Act - Decompile
        var decompileResult = await _codeAnalysisService.DecompileAsync(
            assemblyPath,
            decompiledPath,
            CancellationToken.None);
        
        decompileResult.Success.Should().BeTrue("Decompilation must succeed");

        // Assert - Decompiled code should contain key elements
        decompileResult.SourceCode.Should().Contain("RoundTripTest");
        decompileResult.SourceCode.Should().Contain("GetMessage");
        decompileResult.SourceCode.Should().Contain("Calculate");
    }

    [Fact]
    public void CodeAnalysisService_ShouldBePortable()
    {
        // Arrange & Act - Should be instantiable without external dependencies
        var logger = _loggerFactory.CreateLogger<RoslynCodeAnalysisService>();
        var service = new RoslynCodeAnalysisService(logger);

        // Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<ICodeAnalysisService>();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    void IDisposable.Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
