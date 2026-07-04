using System.Text.Json;
using FluentAssertions;
using Nexo.Abstractions;
using Nexo.Tools.Assembly;
using Xunit;

namespace Nexo.Tests.Kernel;

/// <summary>Tests for assembly tools.</summary>
public class AssemblyToolsTests
{
    private static string CoreDllPath =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Nexo.Core.dll"));

    /// <summary>Call.</summary>
    /// <param name="id">Id.</param>
    /// <param name="args">Args.</param>
    private static ToolCall Call(string id, object args) =>
        new(id, JsonDocument.Parse(JsonSerializer.Serialize(args)).RootElement);

    private static WorldSnapshot Snap => new(0, new Dictionary<string, object?>());

    [Fact]
    public async Task AssemblyAnalyzeTool_reads_metadata_from_built_assembly()
    {
        File.Exists(CoreDllPath).Should().BeTrue(CoreDllPath);
        var tool = new AssemblyAnalyzeTool();
        tool.Id.Should().Be("assembly.analyze");
        tool.Schema.Id.Should().Be("assembly.analyze");

        var result = await tool.InvokeAsync(Call("assembly.analyze", new { path = CoreDllPath }), Snap, CancellationToken.None);
        result.Delta.Log.Should().ContainSingle().Which.Should().Contain("Nexo.Core");
        result.Payload.Should().NotBeNull();
        var payload = JsonSerializer.SerializeToElement(result.Payload);
        payload.TryGetProperty("Complexity", out var complexity).Should().BeTrue();
        complexity.GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AssemblyDecompileTool_returns_error_when_file_missing()
    {
        var tool = new AssemblyDecompileTool();
        var outDir = Path.Combine(Path.GetTempPath(), "nexo-decompile-" + Guid.NewGuid().ToString("N"));
        var result = await tool.InvokeAsync(
            Call("assembly.decompile", new { path = Path.Combine(outDir, "missing.dll"), output = outDir }),
            Snap,
            CancellationToken.None);
        result.Delta.Log.Should().ContainSingle().Which.Should().Contain("FAILED");
    }

    [Fact]
    public void Assembly_tools_expose_schema_metadata()
    {
        new AssemblyAnalyzeTool().Schema.Description.Should().Contain("metadata");
        new AssemblyDecompileTool().Schema.InputJsonSchema.Should().Contain("output");
        new AssemblySecurityScanTool().Schema.Description.Should().Contain("security");
    }

    [Fact]
    public async Task AssemblyDecompileTool_writes_stub_when_decompilation_fails()
    {
        var badDll = Path.Combine(Path.GetTempPath(), "nexo-bad-" + Guid.NewGuid() + ".dll");
        await File.WriteAllTextAsync(badDll, "not a real assembly");
        var outDir = Path.Combine(Path.GetTempPath(), "nexo-decompile-" + Guid.NewGuid().ToString("N"));
        try
        {
            var tool = new AssemblyDecompileTool();
            var result = await tool.InvokeAsync(
                Call("assembly.decompile", new { path = badDll, output = outDir }),
                Snap,
                CancellationToken.None);
            result.Delta.Log.Should().ContainSingle().Which.Should().Contain("decompile");
            var outFile = Directory.GetFiles(outDir, "*.cs").Single();
            (await File.ReadAllTextAsync(outFile)).Should().Contain("Decompilation failed");
        }
        finally
        {
            if (File.Exists(badDll)) File.Delete(badDll);
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }

    [Fact]
    public async Task AssemblyDecompileTool_writes_output_for_valid_assembly()
    {
        File.Exists(CoreDllPath).Should().BeTrue();
        var tool = new AssemblyDecompileTool();
        var outDir = Path.Combine(Path.GetTempPath(), "nexo-decompile-" + Guid.NewGuid().ToString("N"));
        try
        {
            var result = await tool.InvokeAsync(
                Call("assembly.decompile", new { path = CoreDllPath, output = outDir }),
                Snap,
                CancellationToken.None);
            result.Delta.Log.Should().ContainSingle().Which.Should().Contain("decompile");
            Directory.Exists(outDir).Should().BeTrue();
            Directory.GetFiles(outDir, "*.cs").Should().NotBeEmpty();
        }
        finally
        {
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }

    [Fact]
    public async Task AssemblySecurityScanTool_returns_error_when_file_missing()
    {
        var tool = new AssemblySecurityScanTool();
        var result = await tool.InvokeAsync(
            Call("assembly.security_scan", new { path = Path.Combine(Path.GetTempPath(), "missing-" + Guid.NewGuid() + ".dll") }),
            Snap,
            CancellationToken.None);
        result.Delta.Log.Should().ContainSingle().Which.Should().Contain("FAILED");
    }

    [Fact]
    public async Task AssemblySecurityScanTool_records_scan_error_for_invalid_assembly_bytes()
    {
        var badDll = Path.Combine(Path.GetTempPath(), "nexo-scan-" + Guid.NewGuid() + ".dll");
        await File.WriteAllTextAsync(badDll, "not a real assembly");
        try
        {
            var tool = new AssemblySecurityScanTool();
            var result = await tool.InvokeAsync(
                Call("assembly.security_scan", new { path = badDll }),
                Snap,
                CancellationToken.None);
            result.Delta.Log.Should().ContainSingle();
            result.Payload.Should().NotBeNull();
        }
        finally
        {
            if (File.Exists(badDll)) File.Delete(badDll);
        }
    }

    [Fact]
    public async Task AssemblySecurityScanTool_scans_built_assembly()
    {
        File.Exists(CoreDllPath).Should().BeTrue();
        var tool = new AssemblySecurityScanTool();
        tool.Id.Should().Be("assembly.security_scan");

        var result = await tool.InvokeAsync(
            Call("assembly.security_scan", new { path = CoreDllPath }),
            Snap,
            CancellationToken.None);
        result.Delta.Log.Should().NotBeEmpty();
        result.Payload.Should().NotBeNull();
    }
}
