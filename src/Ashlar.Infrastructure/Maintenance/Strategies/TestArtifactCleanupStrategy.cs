using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Maintenance.Models;
using Ashlar.Core.Application.Maintenance.Ports;

namespace Ashlar.Infrastructure.Maintenance.Strategies;

/// <summary>
/// Cleans test artifacts: TestResults dirs under src, and root test-results folder.
/// Uses System.IO only; no process control.
/// </summary>
public sealed class TestArtifactCleanupStrategy : IArtifactCleanupStrategy
{

    private readonly ILogger<TestArtifactCleanupStrategy> _logger;

    /// <summary>Initializes a new test artifact cleanup strategy.</summary>
    public TestArtifactCleanupStrategy(ILogger<TestArtifactCleanupStrategy> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Id.</summary>
    public string Id => IdConstant;
    /// <summary>Description.</summary>
    public string Description => "Removes TestResults directories and test-results folder from the repository.";

    private const string IdConstant = "test-artifacts";

    /// <summary>Clean asynchronously.</summary>
    public Task<ArtifactCleanupResult> CleanAsync(ArtifactCleanupContext context, CancellationToken ct = default)
    {
        var repoRoot = context.RepoRoot ?? Directory.GetCurrentDirectory();
        var pathsDeleted = new List<string>();
        var errors = new List<string>();
        long bytesReclaimed = 0;

        try
        {
            var srcDir = Path.Combine(repoRoot, "src");
            if (Directory.Exists(srcDir))
            {
                foreach (var dir in Directory.GetDirectories(srcDir, "TestResults", SearchOption.AllDirectories))
                {
                    try
                    {
                        var size = GetDirectorySize(dir);
                        Directory.Delete(dir, recursive: true);
                        pathsDeleted.Add(dir);
                        bytesReclaimed += size;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{dir}: {ex.Message}");
                        _logger.LogWarning(ex, "Failed to delete TestResults dir: {Dir}", dir);
                    }
                }
            }

            var testResultsRoot = Path.Combine(repoRoot, "test-results");
            if (Directory.Exists(testResultsRoot))
            {
                try
                {
                    var size = GetDirectorySize(testResultsRoot);
                    Directory.Delete(testResultsRoot, recursive: true);
                    pathsDeleted.Add(testResultsRoot);
                    bytesReclaimed += size;
                }
                catch (Exception ex)
                {
                    errors.Add($"{testResultsRoot}: {ex.Message}");
                    _logger.LogWarning(ex, "Failed to delete test-results: {Path}", testResultsRoot);
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add(ex.Message);
            _logger.LogError(ex, "Test artifact cleanup failed");
        }

        return Task.FromResult(new ArtifactCleanupResult(IdConstant, bytesReclaimed, pathsDeleted, errors));
    }

    private static long GetDirectorySize(string path)
    {
        long size = 0;
        try
        {
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                try
                {
                    size += new FileInfo(file).Length;
                }
                catch
                {
                    // Ignore
                }
            }
        }
        catch
        {
            // Ignore
        }
        return size;
    }
}
