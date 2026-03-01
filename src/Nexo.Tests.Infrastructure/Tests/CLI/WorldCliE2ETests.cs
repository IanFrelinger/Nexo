using System.Diagnostics;
using System.Text.Json;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.Infrastructure.Tests.CLI;

/// <summary>
/// End-to-end smoke tests for `nexo world build` using echo providers.
/// </summary>
public sealed class WorldCliE2ETests : UnitTestBase
{
    private string? _repoRoot;
    private string? _tempDir;

    public override Task SetupAsync(CancellationToken cancellationToken = default)
    {
        _repoRoot = TestPaths.FindRepoRoot();
        _tempDir = Path.Combine(Path.GetTempPath(), "nexo-world-e2e", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        return Task.CompletedTask;
    }

    public override Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        if (_tempDir != null && Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { /* ignore */ }
        }
        return Task.CompletedTask;
    }

    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            AssertNotNull(_repoRoot, "Repo root should be resolved");
            AssertNotNull(_tempDir, "Temp dir should be created");

            await TestWorldBuildEchoAsync(cancellationToken);
            await TestWorldBuildGltfAsync(cancellationToken);
            await TestWorldBuildGlbAsync(cancellationToken);
            await TestWorldBuildChunkedTerrainAndInstancesAsync(cancellationToken);
            await TestWorldBuildTerrainLodAsync(cancellationToken);
            await TestWorldBuildChunkedTerrainLodsAsync(cancellationToken);
            await TestWorldBuildTriBudgetsAsync(cancellationToken);

            return new TestResult
            {
                Name = nameof(WorldCliE2ETests),
                Category = "CLI",
                Passed = true,
                Message = "World CLI E2E tests passed"
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(WorldCliE2ETests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestWorldBuildEchoAsync(CancellationToken ct)
    {
        var outDir = Path.Combine(_tempDir!, "world");
        var (code, stdout, stderr) = await RunDotnetAsync(
            workingDir: _repoRoot!,
            args: $"run --project src/Nexo.CLI -- world build --bounds 0,0,0.001,0.001 --out-dir \"{outDir}\" --terrain-elevation-provider echo --vector-provider echo --format-json",
            ct);

        AssertEqual(0, code, $"world build should exit 0. stderr: {stderr}");

        AssertTrue(File.Exists(Path.Combine(outDir, "terrain.obj")), "terrain.obj should exist");
        AssertTrue(File.Exists(Path.Combine(outDir, "buildings.obj")), "buildings.obj should exist");
        AssertTrue(File.Exists(Path.Combine(outDir, "roads.obj")), "roads.obj should exist");
        AssertTrue(File.Exists(Path.Combine(outDir, "water.obj")), "water.obj should exist");
        AssertTrue(File.Exists(Path.Combine(outDir, "world.mtl")), "world.mtl should exist");
        AssertTrue(File.Exists(Path.Combine(outDir, "instances.json")), "instances.json should exist");
        AssertTrue(File.Exists(Path.Combine(outDir, "materials.json")), "materials.json should exist");
        AssertTrue(File.Exists(Path.Combine(outDir, "manifest.json")), "manifest.json should exist");

        var terrainObj = await File.ReadAllTextAsync(Path.Combine(outDir, "terrain.obj"), ct);
        AssertTrue(terrainObj.Contains("v "), "terrain.obj should contain vertices");
        // Terrain should now be centered around origin (some negative coords expected).
        AssertTrue(terrainObj.Contains("v -"), "terrain.obj should contain negative coords after origin-centering");

        var json = ExtractJsonObject(stdout);
        using var doc = JsonDocument.Parse(json);
        AssertTrue(doc.RootElement.GetProperty("ok").GetBoolean(), "Expected ok=true in JSON output");

        await AssertWorldValidateAsync(outDir, ct);
    }

    private async Task TestWorldBuildChunkedTerrainAndInstancesAsync(CancellationToken ct)
    {
        var outDir = Path.Combine(_tempDir!, "world_chunked");
        var (code, stdout, stderr) = await RunDotnetAsync(
            workingDir: _repoRoot!,
            args: $"run --project src/Nexo.CLI -- world build --bounds 0,0,0.001,0.001 --out-dir \"{outDir}\" --terrain-elevation-provider echo --vector-provider echo --terrain-chunk-samples 8 --instances-chunk-samples 8 --format-json",
            ct);

        AssertEqual(0, code, $"world build (chunked) should exit 0. stderr: {stderr}");

        AssertTrue(!File.Exists(Path.Combine(outDir, "terrain.obj")), "terrain.obj should not exist when chunking is enabled");
        AssertTrue(Directory.Exists(Path.Combine(outDir, "terrain_chunks")), "terrain_chunks/ should exist");

        AssertTrue(!File.Exists(Path.Combine(outDir, "instances.json")), "instances.json should not exist when instance chunking is enabled");
        AssertTrue(Directory.Exists(Path.Combine(outDir, "instances_chunks")), "instances_chunks/ should exist");

        var json = ExtractJsonObject(stdout);
        using var doc = JsonDocument.Parse(json);
        AssertTrue(doc.RootElement.GetProperty("ok").GetBoolean(), "Expected ok=true in JSON output");

        await AssertWorldValidateAsync(outDir, ct);
    }

    private async Task TestWorldBuildGltfAsync(CancellationToken ct)
    {
        var outDir = Path.Combine(_tempDir!, "world_gltf");
        var (code, stdout, stderr) = await RunDotnetAsync(
            workingDir: _repoRoot!,
            args: $"run --project src/Nexo.CLI -- world build --bounds 0,0,0.001,0.001 --out-dir \"{outDir}\" --terrain-elevation-provider echo --vector-provider echo --mesh-format gltf --format-json",
            ct);

        AssertEqual(0, code, $"world build (gltf) should exit 0. stderr: {stderr}");

        AssertTrue(File.Exists(Path.Combine(outDir, "terrain.gltf")), "terrain.gltf should exist");
        AssertTrue(File.Exists(Path.Combine(outDir, "terrain.bin")), "terrain.bin should exist");
        AssertTrue(File.Exists(Path.Combine(outDir, "buildings.gltf")), "buildings.gltf should exist");
        AssertTrue(File.Exists(Path.Combine(outDir, "buildings.bin")), "buildings.bin should exist");
        AssertTrue(File.Exists(Path.Combine(outDir, "roads.gltf")), "roads.gltf should exist");
        AssertTrue(File.Exists(Path.Combine(outDir, "roads.bin")), "roads.bin should exist");
        AssertTrue(File.Exists(Path.Combine(outDir, "water.gltf")), "water.gltf should exist");
        AssertTrue(File.Exists(Path.Combine(outDir, "water.bin")), "water.bin should exist");

        AssertTrue(!File.Exists(Path.Combine(outDir, "world.mtl")), "world.mtl should not exist for glTF output");

        var terrainGltf = await File.ReadAllTextAsync(Path.Combine(outDir, "terrain.gltf"), ct);
        AssertTrue(terrainGltf.Contains("\"version\":\"2.0\""), "terrain.gltf should declare glTF 2.0 asset");

        var json = ExtractJsonObject(stdout);
        using var doc = JsonDocument.Parse(json);
        AssertTrue(doc.RootElement.GetProperty("ok").GetBoolean(), "Expected ok=true in JSON output");

        await AssertWorldValidateAsync(outDir, ct);
    }

    private async Task TestWorldBuildGlbAsync(CancellationToken ct)
    {
        var outDir = Path.Combine(_tempDir!, "world_glb");
        var (code, stdout, stderr) = await RunDotnetAsync(
            workingDir: _repoRoot!,
            args: $"run --project src/Nexo.CLI -- world build --bounds 0,0,0.001,0.001 --out-dir \"{outDir}\" --terrain-elevation-provider echo --vector-provider echo --mesh-format glb --format-json",
            ct);

        AssertEqual(0, code, $"world build (glb) should exit 0. stderr: {stderr}");
        AssertTrue(File.Exists(Path.Combine(outDir, "world.glb")), "world.glb should exist");

        var json = ExtractJsonObject(stdout);
        using var doc = JsonDocument.Parse(json);
        AssertTrue(doc.RootElement.GetProperty("ok").GetBoolean(), "Expected ok=true in JSON output");

        await AssertWorldValidateAsync(outDir, ct);
    }

    private async Task TestWorldBuildTerrainLodAsync(CancellationToken ct)
    {
        var outDir = Path.Combine(_tempDir!, "world_lod");
        var (code, _, stderr) = await RunDotnetAsync(
            workingDir: _repoRoot!,
            args: $"run --project src/Nexo.CLI -- world build --bounds 0,0,0.001,0.001 --out-dir \"{outDir}\" --terrain-elevation-provider echo --vector-provider echo --terrain-lod-factors 2 --format-json",
            ct);

        AssertEqual(0, code, $"world build (lod) should exit 0. stderr: {stderr}");
        AssertTrue(File.Exists(Path.Combine(outDir, "terrain.obj")), "terrain.obj should exist");
        AssertTrue(File.Exists(Path.Combine(outDir, "terrain_lods", "terrain_lod2.obj")), "terrain_lods/terrain_lod2.obj should exist");

        await AssertWorldValidateAsync(outDir, ct);
    }

    private async Task TestWorldBuildChunkedTerrainLodsAsync(CancellationToken ct)
    {
        var outDir = Path.Combine(_tempDir!, "world_chunked_lods");
        var (code, _, stderr) = await RunDotnetAsync(
            workingDir: _repoRoot!,
            args: $"run --project src/Nexo.CLI -- world build --bounds 0,0,0.001,0.001 --out-dir \"{outDir}\" --terrain-elevation-provider echo --vector-provider echo --terrain-chunk-samples 8 --terrain-lod-factors 2 --format-json",
            ct);

        AssertEqual(0, code, $"world build (chunked+lods) should exit 0. stderr: {stderr}");
        AssertTrue(Directory.Exists(Path.Combine(outDir, "terrain_chunks_lods", "lod2")), "terrain_chunks_lods/lod2 should exist");

        await AssertWorldValidateAsync(outDir, ct);
    }

    private async Task TestWorldBuildTriBudgetsAsync(CancellationToken ct)
    {
        var outDir = Path.Combine(_tempDir!, "world_tri");
        var (code, _, stderr) = await RunDotnetAsync(
            workingDir: _repoRoot!,
            args: $"run --project src/Nexo.CLI -- world build --bounds 0,0,0.001,0.001 --out-dir \"{outDir}\" --terrain-elevation-provider echo --vector-provider echo --lod-tri-budgets 1 --format-json",
            ct);

        AssertEqual(0, code, $"world build (tri budgets) should exit 0. stderr: {stderr}");
        AssertTrue(File.Exists(Path.Combine(outDir, "mesh_lods", "terrain_tri1.obj")), "mesh_lods/terrain_tri1.obj should exist");

        await AssertWorldValidateAsync(outDir, ct);

        var outDir2 = Path.Combine(_tempDir!, "world_tri_chunked");
        var (code2, _, stderr2) = await RunDotnetAsync(
            workingDir: _repoRoot!,
            args: $"run --project src/Nexo.CLI -- world build --bounds 0,0,0.001,0.001 --out-dir \"{outDir2}\" --terrain-elevation-provider echo --vector-provider echo --terrain-chunk-samples 8 --lod-tri-budgets 1 --format-json",
            ct);

        AssertEqual(0, code2, $"world build (tri budgets chunked) should exit 0. stderr: {stderr2}");
        AssertTrue(Directory.Exists(Path.Combine(outDir2, "terrain_chunks_tri", "tri1")), "terrain_chunks_tri/tri1 should exist");

        await AssertWorldValidateAsync(outDir2, ct);
    }

    private async Task AssertWorldValidateAsync(string outDir, CancellationToken ct)
    {
        var (code, stdout, stderr) = await RunDotnetAsync(
            workingDir: _repoRoot!,
            args: $"run --project src/Nexo.CLI -- world validate --bundle-dir \"{outDir}\" --format-json",
            ct);

        AssertEqual(0, code, $"world validate should exit 0. stderr: {stderr}");
        var json = ExtractJsonObject(stdout);
        using var doc = JsonDocument.Parse(json);
        AssertTrue(doc.RootElement.GetProperty("ok").GetBoolean(), "Expected ok=true in validate JSON output");
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunDotnetAsync(string workingDir, string args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = args,
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var p = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet process");
        var stdoutTask = p.StandardOutput.ReadToEndAsync();
        var stderrTask = p.StandardError.ReadToEndAsync();

        await p.WaitForExitAsync(ct);
        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return (p.ExitCode, stdout, stderr);
    }

    private static string ExtractJsonObject(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end < 0 || end <= start)
        {
            throw new InvalidOperationException("No JSON object found in output");
        }
        return text.Substring(start, end - start + 1);
    }
}

