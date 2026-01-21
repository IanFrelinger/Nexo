using System.Text.Json;
using Nexo.Abstractions;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Tools.GeoTerrain;

namespace Nexo.Tests.Infrastructure.Tests.Tools;

public sealed class GeoTerrainToolsTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestAnalyzeToolAcceptsSyntheticTileAsync(cancellationToken);
            await TestToObjToolWritesObjAsync(cancellationToken);

            return new TestResult
            {
                TestName = nameof(GeoTerrainToolsTests),
                Category = "Tools",
                Passed = true,
                Message = "GeoTerrain tools tests passed"
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = nameof(GeoTerrainToolsTests),
                Category = "Tools",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestAnalyzeToolAcceptsSyntheticTileAsync(CancellationToken ct)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "nexo-geoterrain-tools", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);

        // Minimal valid HGT: 2x2 samples => 4 samples => 8 bytes.
        // Use big-endian int16 values: 10, 20, 30, 40.
        var hgtRel = "N00E000.hgt";
        var hgtPath = Path.Combine(tmp, hgtRel);
        await File.WriteAllBytesAsync(hgtPath, new byte[]
        {
            0x00, 0x0A,
            0x00, 0x14,
            0x00, 0x1E,
            0x00, 0x28
        }, ct);

        var tool = new SrtmHgtAnalyzeTool();
        var call = new ToolCall(tool.Id, JsonSerializer.SerializeToElement(new { root = tmp, path = hgtRel }));
        var res = await tool.InvokeAsync(call, new WorldSnapshot(0, new Dictionary<string, object?>()), ct);

        var json = JsonSerializer.Serialize(res.Payload);
        using var doc = JsonDocument.Parse(json);
        AssertTrue(doc.RootElement.GetProperty("ok").GetBoolean(), "Expected ok=true");
        AssertEqual(2, doc.RootElement.GetProperty("Size").GetInt32());
        AssertEqual(4, doc.RootElement.GetProperty("SampleCount").GetInt32());
        AssertEqual(10, doc.RootElement.GetProperty("MinMeters").GetInt16());
        AssertEqual(40, doc.RootElement.GetProperty("MaxMeters").GetInt16());
    }

    private async Task TestToObjToolWritesObjAsync(CancellationToken ct)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "nexo-geoterrain-tools", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);

        var hgtRel = "N00E000.hgt";
        var objRel = "out/terrain.obj";
        var hgtPath = Path.Combine(tmp, hgtRel);

        // Flat 2x2 tile at 0m.
        await File.WriteAllBytesAsync(hgtPath, new byte[]
        {
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00
        }, ct);

        var tool = new SrtmHgtToObjTool();
        var call = new ToolCall(tool.Id, JsonSerializer.SerializeToElement(new
        {
            root = tmp,
            hgtPath = hgtRel,
            objPath = objRel,
            verticalScale = 1.0,
            treatNoDataAsZero = false
        }));

        var res = await tool.InvokeAsync(call, new WorldSnapshot(0, new Dictionary<string, object?>()), ct);
        var objPath = Path.Combine(tmp, objRel);
        AssertTrue(File.Exists(objPath), "Expected OBJ file to be written");

        var content = await File.ReadAllTextAsync(objPath, ct);
        AssertTrue(content.Contains("v "), "Expected vertex lines");
        AssertTrue(content.Contains("f "), "Expected face lines");

        var json = JsonSerializer.Serialize(res.Payload);
        using var doc = JsonDocument.Parse(json);
        AssertTrue(doc.RootElement.GetProperty("ok").GetBoolean(), "Expected ok=true");
        AssertEqual(4, doc.RootElement.GetProperty("VertexCount").GetInt32());
        AssertEqual(2, doc.RootElement.GetProperty("TriangleCount").GetInt32());
    }
}

