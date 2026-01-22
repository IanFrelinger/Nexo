using System.Diagnostics;
using System.Text.Json;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.Infrastructure.Tests.CLI;

/// <summary>
/// End-to-end smoke tests for GeoTerrain CLI commands (offline/echo provider).
/// </summary>
public sealed class GeoTerrainCliE2ETests : UnitTestBase
{
    private string? _repoRoot;
    private string? _tempDir;

    public override Task SetupAsync(CancellationToken cancellationToken = default)
    {
        _repoRoot = FindRepoRoot();
        _tempDir = Path.Combine(Path.GetTempPath(), "nexo-geoterrain-e2e", Guid.NewGuid().ToString("N"));
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

            await TestTileToObjEchoAsync(cancellationToken);
            await TestTileToContoursEchoAsync(cancellationToken);

            return new TestResult
            {
                TestName = nameof(GeoTerrainCliE2ETests),
                Category = "E2E",
                Passed = true,
                Message = "GeoTerrain CLI E2E tests passed"
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = nameof(GeoTerrainCliE2ETests),
                Category = "E2E",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestTileToObjEchoAsync(CancellationToken ct)
    {
        var outPath = Path.Combine(_tempDir!, "terrain.obj");
        var (code, stdout, stderr) = await RunDotnetAsync(
            workingDir: _repoRoot!,
            args: $"run --project src/Nexo.CLI -- geoterrain tile-to-obj --tile N00E000 --output \"{outPath}\" --elevation-provider echo --format-json",
            ct);

        AssertEqual(0, code, $"geoterrain tile-to-obj should exit 0. stderr: {stderr}");
        AssertTrue(File.Exists(outPath), "OBJ output file should exist");

        var obj = await File.ReadAllTextAsync(outPath, ct);
        AssertTrue(obj.Contains("v "), "OBJ should contain vertices");
        AssertTrue(obj.Contains("f "), "OBJ should contain faces");

        var json = ExtractJsonObject(stdout);
        using var doc = JsonDocument.Parse(json);
        AssertTrue(doc.RootElement.GetProperty("ok").GetBoolean(), "Expected ok=true in JSON output");
    }

    private async Task TestTileToContoursEchoAsync(CancellationToken ct)
    {
        var outPath = Path.Combine(_tempDir!, "contours.geojson");
        var (code, stdout, stderr) = await RunDotnetAsync(
            workingDir: _repoRoot!,
            args: $"run --project src/Nexo.CLI -- geoterrain tile-to-contours --tile N00E000 --output \"{outPath}\" --elevation-provider echo --interval-meters 1 --format-json",
            ct);

        AssertEqual(0, code, $"geoterrain tile-to-contours should exit 0. stderr: {stderr}");
        AssertTrue(File.Exists(outPath), "GeoJSON output file should exist");

        var geojson = await File.ReadAllTextAsync(outPath, ct);
        AssertTrue(geojson.Contains("\"FeatureCollection\""), "GeoJSON should be a FeatureCollection");
        AssertTrue(geojson.Contains("\"LineString\""), "GeoJSON should contain LineString features");

        var json = ExtractJsonObject(stdout);
        using var doc = JsonDocument.Parse(json);
        AssertTrue(doc.RootElement.GetProperty("ok").GetBoolean(), "Expected ok=true in JSON output");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var sln = Path.Combine(dir.FullName, "Nexo.sln");
            if (File.Exists(sln)) return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Could not locate Nexo.sln from test base directory");
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

