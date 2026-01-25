using FluentAssertions;
using Microsoft.Extensions.Logging;
using Nexo.Infrastructure.Testing.CodeAnalysis;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.BaseFramework;

/// <summary>
/// Smoke tests for code compilation and decompilation capabilities.
/// 
/// Validates that the code analysis infrastructure is available and functional
/// as part of the base framework tests that run across all platforms.
/// </summary>
public class CodeAnalysisSmokeTests : IDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ICodeAnalysisService _codeAnalysisService;
    private readonly string _tempDir;

    public CodeAnalysisSmokeTests()
    {
        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        _codeAnalysisService = new RoslynCodeAnalysisService(_loggerFactory.CreateLogger<RoslynCodeAnalysisService>());
        _tempDir = Path.Combine(Path.GetTempPath(), "nexo-smoke-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void CodeAnalysisService_ShouldBeAvailable()
    {
        // Arrange & Act
        var service = _codeAnalysisService;

        // Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<ICodeAnalysisService>();
    }

    [Fact]
    public async Task Compile_SimpleCode_ShouldSucceed()
    {
        // Arrange
        var sourceCode = @"
public class SimpleTest
{
    public int Value => 42;
}
";
        var outputPath = Path.Combine(_tempDir, "SimpleTest.dll");

        // Act
        var result = await _codeAnalysisService.CompileAsync(
            sourceCode,
            "SimpleTest",
            outputPath,
            null,
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue("Simple code should compile successfully");
        File.Exists(outputPath).Should().BeTrue("Compiled assembly should exist");
    }

    [Fact]
    public async Task AnalyzeAssembly_ShouldExtractBasicMetadata()
    {
        // Arrange - Compile a simple assembly first
        var sourceCode = @"
public class MetadataTest
{
    public void TestMethod() { }
}
";
        var assemblyPath = Path.Combine(_tempDir, "MetadataTest.dll");
        
        var compileResult = await _codeAnalysisService.CompileAsync(
            sourceCode,
            "MetadataTest",
            assemblyPath,
            null,
            CancellationToken.None);
        
        compileResult.Success.Should().BeTrue("Assembly must compile for analysis test");

        // Act
        var result = await _codeAnalysisService.AnalyzeAssemblyAsync(
            assemblyPath,
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue("Assembly analysis should succeed");
        result.AssemblyName.Should().NotBeNullOrEmpty("Assembly name should be extracted");
        result.Types.Should().NotBeEmpty("Assembly should contain types");
    }

    [Fact]
    public async Task Decompile_CompiledAssembly_ShouldProduceSource()
    {
        // Arrange - Compile an assembly first
        var sourceCode = @"
public class DecompileTest
{
    public string Message => ""Test"";
}
";
        var assemblyPath = Path.Combine(_tempDir, "DecompileTest.dll");
        var decompiledPath = Path.Combine(_tempDir, "DecompileTest.cs");
        
        var compileResult = await _codeAnalysisService.CompileAsync(
            sourceCode,
            "DecompileTest",
            assemblyPath,
            null,
            CancellationToken.None);
        
        compileResult.Success.Should().BeTrue("Assembly must compile for decompilation test");

        // Act
        var result = await _codeAnalysisService.DecompileAsync(
            assemblyPath,
            decompiledPath,
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue("Decompilation should succeed");
        result.SourceCode.Should().NotBeNullOrEmpty("Decompiled source should not be empty");
        File.Exists(decompiledPath).Should().BeTrue("Decompiled file should exist");
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
