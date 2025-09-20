using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Demo.Scripts.Utils;

/// <summary>
/// Manages building .NET projects with proper error handling and cleanup
/// </summary>
public class BuildManager
{
    private static readonly TimeSpan BuildTimeout = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan TestTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RestoreTimeout = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Builds a .NET project
    /// </summary>
    public async Task<BuildResult> BuildProjectAsync(
        string projectPath, 
        string configuration = "Release", 
        string framework = null,
        string additionalArgs = null)
    {
        if (string.IsNullOrEmpty(projectPath))
        {
            return new BuildResult
            {
                Success = false,
                Message = "No project path provided"
            };
        }

        if (!File.Exists(projectPath))
        {
            return new BuildResult
            {
                Success = false,
                Message = $"Project file not found: {projectPath}"
            };
        }

        try
        {
            // Build command
            var buildArgs = new List<string>
            {
                "build",
                $"\"{projectPath}\"",
                "-c", configuration,
                "--verbosity", "minimal",
                "--nologo"
            };

            if (!string.IsNullOrEmpty(framework))
            {
                buildArgs.AddRange(new[] { "-f", framework });
            }

            if (!string.IsNullOrEmpty(additionalArgs))
            {
                buildArgs.Add(additionalArgs);
            }

            var result = await SystemUtils.RunWithTimeoutAsync(
                "dotnet", 
                string.Join(" ", buildArgs), 
                BuildTimeout,
                Path.GetDirectoryName(projectPath));

            return new BuildResult
            {
                Success = result.Success,
                Message = result.Success ? "Build completed successfully" : result.StandardError,
                Output = result.StandardOutput,
                ErrorOutput = result.StandardError,
                ExitCode = result.ExitCode
            };
        }
        catch (Exception ex)
        {
            return new BuildResult
            {
                Success = false,
                Message = $"Exception during build: {ex.Message}",
                ExitCode = -1
            };
        }
    }

    /// <summary>
    /// Builds a .NET solution
    /// </summary>
    public async Task<BuildResult> BuildSolutionAsync(
        string solutionPath, 
        string configuration = "Release", 
        string additionalArgs = null)
    {
        if (string.IsNullOrEmpty(solutionPath))
        {
            return new BuildResult
            {
                Success = false,
                Message = "No solution path provided"
            };
        }

        if (!File.Exists(solutionPath))
        {
            return new BuildResult
            {
                Success = false,
                Message = $"Solution file not found: {solutionPath}"
            };
        }

        try
        {
            // Build command
            var buildArgs = new List<string>
            {
                "build",
                $"\"{solutionPath}\"",
                "-c", configuration,
                "--verbosity", "minimal",
                "--nologo"
            };

            if (!string.IsNullOrEmpty(additionalArgs))
            {
                buildArgs.Add(additionalArgs);
            }

            var result = await SystemUtils.RunWithTimeoutAsync(
                "dotnet", 
                string.Join(" ", buildArgs), 
                BuildTimeout,
                Path.GetDirectoryName(solutionPath));

            return new BuildResult
            {
                Success = result.Success,
                Message = result.Success ? "Solution build completed successfully" : result.StandardError,
                Output = result.StandardOutput,
                ErrorOutput = result.StandardError,
                ExitCode = result.ExitCode
            };
        }
        catch (Exception ex)
        {
            return new BuildResult
            {
                Success = false,
                Message = $"Exception during solution build: {ex.Message}",
                ExitCode = -1
            };
        }
    }

    /// <summary>
    /// Runs tests
    /// </summary>
    public async Task<TestResult> RunTestsAsync(
        string testProject, 
        string additionalArgs = null)
    {
        if (string.IsNullOrEmpty(testProject))
        {
            return new TestResult
            {
                Success = false,
                Message = "No test project provided"
            };
        }

        if (!File.Exists(testProject))
        {
            return new TestResult
            {
                Success = false,
                Message = $"Test project not found: {testProject}"
            };
        }

        try
        {
            // Test command
            var testArgs = new List<string>
            {
                "test",
                $"\"{testProject}\"",
                "--verbosity", "minimal",
                "--nologo",
                "--no-build",
                "--no-restore"
            };

            if (!string.IsNullOrEmpty(additionalArgs))
            {
                testArgs.Add(additionalArgs);
            }

            var result = await SystemUtils.RunWithTimeoutAsync(
                "dotnet", 
                string.Join(" ", testArgs), 
                TestTimeout,
                Path.GetDirectoryName(testProject));

            return new TestResult
            {
                Success = result.Success,
                Message = result.Success ? "Tests completed successfully" : result.StandardError,
                Output = result.StandardOutput,
                ErrorOutput = result.StandardError,
                ExitCode = result.ExitCode
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Success = false,
                Message = $"Exception during test execution: {ex.Message}",
                ExitCode = -1
            };
        }
    }

    /// <summary>
    /// Restores packages
    /// </summary>
    public async Task<RestoreResult> RestorePackagesAsync(string projectPath)
    {
        if (string.IsNullOrEmpty(projectPath))
        {
            return new RestoreResult
            {
                Success = false,
                Message = "No project path provided"
            };
        }

        try
        {
            var result = await SystemUtils.RunWithTimeoutAsync(
                "dotnet", 
                $"restore \"{projectPath}\" --verbosity minimal --nologo", 
                RestoreTimeout,
                Path.GetDirectoryName(projectPath));

            return new RestoreResult
            {
                Success = result.Success,
                Message = result.Success ? "Packages restored successfully" : result.StandardError,
                Output = result.StandardOutput,
                ErrorOutput = result.StandardError,
                ExitCode = result.ExitCode
            };
        }
        catch (Exception ex)
        {
            return new RestoreResult
            {
                Success = false,
                Message = $"Exception during package restore: {ex.Message}",
                ExitCode = -1
            };
        }
    }

    /// <summary>
    /// Publishes an application
    /// </summary>
    public async Task<PublishResult> PublishApplicationAsync(
        string projectPath,
        string outputDir = null,
        string configuration = "Release",
        string framework = null,
        string runtime = null)
    {
        if (string.IsNullOrEmpty(projectPath))
        {
            return new PublishResult
            {
                Success = false,
                Message = "No project path provided"
            };
        }

        outputDir ??= Path.Combine(Path.GetDirectoryName(projectPath), "publish");

        try
        {
            // Create output directory
            SystemUtils.SafeCreateDirectory(outputDir);

            // Publish command
            var publishArgs = new List<string>
            {
                "publish",
                $"\"{projectPath}\"",
                "-c", configuration,
                "-o", $"\"{outputDir}\"",
                "--verbosity", "minimal",
                "--nologo"
            };

            if (!string.IsNullOrEmpty(framework))
            {
                publishArgs.AddRange(new[] { "-f", framework });
            }

            if (!string.IsNullOrEmpty(runtime))
            {
                publishArgs.AddRange(new[] { "-r", runtime });
            }

            var result = await SystemUtils.RunWithTimeoutAsync(
                "dotnet", 
                string.Join(" ", publishArgs), 
                BuildTimeout,
                Path.GetDirectoryName(projectPath));

            return new PublishResult
            {
                Success = result.Success,
                Message = result.Success ? $"Application published successfully to {outputDir}" : result.StandardError,
                Output = result.StandardOutput,
                ErrorOutput = result.StandardError,
                ExitCode = result.ExitCode,
                OutputDirectory = outputDir
            };
        }
        catch (Exception ex)
        {
            return new PublishResult
            {
                Success = false,
                Message = $"Exception during publish: {ex.Message}",
                ExitCode = -1,
                OutputDirectory = outputDir
            };
        }
    }

    /// <summary>
    /// Cleans build artifacts
    /// </summary>
    public CleanResult CleanBuildArtifacts(string projectPath)
    {
        if (string.IsNullOrEmpty(projectPath))
        {
            return new CleanResult
            {
                Success = false,
                Message = "No project path provided"
            };
        }

        try
        {
            var projectDir = Path.GetDirectoryName(projectPath);
            var cleanDirs = new[] { "bin", "obj", "out", "dist", "build" };
            var cleanedDirs = new List<string>();

            // Clean project-level artifacts
            foreach (var dir in cleanDirs)
            {
                var cleanPath = Path.Combine(projectDir, dir);
                if (Directory.Exists(cleanPath))
                {
                    Directory.Delete(cleanPath, true);
                    cleanedDirs.Add(cleanPath);
                }
            }

            // Clean solution-level artifacts if it's a solution
            if (Path.GetExtension(projectPath).Equals(".sln", StringComparison.OrdinalIgnoreCase))
            {
                var solutionDir = Path.GetDirectoryName(projectPath);
                var projectFiles = Directory.GetFiles(solutionDir, "*.csproj", SearchOption.AllDirectories);

                foreach (var projFile in projectFiles)
                {
                    var projDir = Path.GetDirectoryName(projFile);
                    foreach (var dir in cleanDirs)
                    {
                        var cleanPath = Path.Combine(projDir, dir);
                        if (Directory.Exists(cleanPath))
                        {
                            Directory.Delete(cleanPath, true);
                            cleanedDirs.Add(cleanPath);
                        }
                    }
                }
            }

            return new CleanResult
            {
                Success = true,
                Message = $"Cleaned {cleanedDirs.Count} directories",
                CleanedDirectories = cleanedDirs
            };
        }
        catch (Exception ex)
        {
            return new CleanResult
            {
                Success = false,
                Message = $"Exception during cleanup: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Builds with dependency check and restore
    /// </summary>
    public async Task<BuildResult> BuildWithDependenciesAsync(
        string projectPath,
        string configuration = "Release",
        string framework = null,
        bool runTests = true)
    {
        if (string.IsNullOrEmpty(projectPath))
        {
            return new BuildResult
            {
                Success = false,
                Message = "No project path provided"
            };
        }

        // Check .NET SDK
        if (!SystemUtils.CommandExists("dotnet"))
        {
            return new BuildResult
            {
                Success = false,
                Message = ".NET SDK is required for building"
            };
        }

        // Restore packages
        var restoreResult = await RestorePackagesAsync(projectPath);
        if (!restoreResult.Success)
        {
            return new BuildResult
            {
                Success = false,
                Message = $"Failed to restore packages: {restoreResult.Message}"
            };
        }

        // Build project
        var buildResult = await BuildProjectAsync(projectPath, configuration, framework);
        if (!buildResult.Success)
        {
            return buildResult;
        }

        // Run tests if requested and it's a test project
        if (runTests && projectPath.Contains("Test", StringComparison.OrdinalIgnoreCase))
        {
            var testResult = await RunTestsAsync(projectPath);
            if (!testResult.Success)
            {
                // Don't fail the build for test failures, just warn
                buildResult.Message += $"\nWarning: Tests failed - {testResult.Message}";
            }
        }

        return buildResult;
    }
}

/// <summary>
/// Result of a build operation
/// </summary>
public class BuildResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
    public string ErrorOutput { get; set; } = string.Empty;
    public int ExitCode { get; set; }
}

/// <summary>
/// Result of a test run
/// </summary>
public class TestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
    public string ErrorOutput { get; set; } = string.Empty;
    public int ExitCode { get; set; }
}

/// <summary>
/// Result of a package restore
/// </summary>
public class RestoreResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
    public string ErrorOutput { get; set; } = string.Empty;
    public int ExitCode { get; set; }
}

/// <summary>
/// Result of a publish operation
/// </summary>
public class PublishResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
    public string ErrorOutput { get; set; } = string.Empty;
    public int ExitCode { get; set; }
    public string OutputDirectory { get; set; } = string.Empty;
}

/// <summary>
/// Result of a cleanup operation
/// </summary>
public class CleanResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> CleanedDirectories { get; set; } = new();
}
