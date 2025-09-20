using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Demo.Scripts.Utils;

/// <summary>
/// Runs comprehensive validation checks for the demo environment
/// </summary>
public class ValidationRunner
{
    private readonly DependencyManager _dependencyManager;
    private readonly BuildManager _buildManager;

    public ValidationRunner()
    {
        _dependencyManager = new DependencyManager();
        _buildManager = new BuildManager();
    }

    /// <summary>
    /// Runs all validation checks
    /// </summary>
    public async Task<ValidationResult> RunValidationAsync(string projectRoot = null)
    {
        projectRoot ??= Directory.GetCurrentDirectory();
        
        var result = new ValidationResult
        {
            ProjectRoot = projectRoot,
            StartTime = DateTime.UtcNow
        };

        try
        {
            // Check .NET 8 availability
            result.Checks["dotnet"] = await ValidateDotNetAsync();
            
            // Check MAUI workloads
            result.Checks["maui-workloads"] = await ValidateMauiWorkloadsAsync();
            
            // Check Docker
            result.Checks["docker"] = await ValidateDockerAsync();
            
            // Check project structure
            result.Checks["project-structure"] = ValidateProjectStructure(projectRoot);
            
            // Check build capability
            result.Checks["build"] = await ValidateBuildAsync(projectRoot);
            
            // Check fixtures
            result.Checks["fixtures"] = ValidateFixtures(projectRoot);
            
            // Check tests
            result.Checks["tests"] = await ValidateTestsAsync(projectRoot);
            
            // Check ports
            result.Checks["ports"] = ValidatePorts();
            
            result.EndTime = DateTime.UtcNow;
            result.Success = result.Checks.Values.All(c => c.Success);
            
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
            result.EndTime = DateTime.UtcNow;
            return result;
        }
    }

    /// <summary>
    /// Validates .NET 8 availability
    /// </summary>
    private async Task<ValidationCheck> ValidateDotNetAsync()
    {
        try
        {
            if (!SystemUtils.CommandExists("dotnet"))
            {
                return new ValidationCheck
                {
                    Name = ".NET 8",
                    Success = false,
                    Message = ".NET SDK not found",
                    Suggestion = "Install .NET 8 SDK from https://dotnet.microsoft.com/download"
                };
            }

            var version = await SystemUtils.GetCommandVersionAsync("dotnet");
            if (version.Contains("8."))
            {
                return new ValidationCheck
                {
                    Name = ".NET 8",
                    Success = true,
                    Message = $"Found .NET version: {version}",
                    Suggestion = null
                };
            }
            else
            {
                return new ValidationCheck
                {
                    Name = ".NET 8",
                    Success = false,
                    Message = $"Found .NET version: {version}, but .NET 8 is required",
                    Suggestion = "Install .NET 8 SDK from https://dotnet.microsoft.com/download"
                };
            }
        }
        catch (Exception ex)
        {
            return new ValidationCheck
            {
                Name = ".NET 8",
                Success = false,
                Message = $"Error checking .NET: {ex.Message}",
                Suggestion = "Install .NET 8 SDK from https://dotnet.microsoft.com/download"
            };
        }
    }

    /// <summary>
    /// Validates MAUI workloads
    /// </summary>
    private async Task<ValidationCheck> ValidateMauiWorkloadsAsync()
    {
        try
        {
            var result = await SystemUtils.RunWithTimeoutAsync(
                "dotnet", 
                "workload list", 
                TimeSpan.FromSeconds(30));

            if (!result.Success)
            {
                return new ValidationCheck
                {
                    Name = "MAUI Workloads",
                    Success = false,
                    Message = "Failed to list workloads",
                    Suggestion = "Run 'dotnet workload install maui'"
                };
            }

            var workloads = new[] { "maui", "maccatalyst", "android" };
            var installedWorkloads = workloads.Where(w => result.StandardOutput.Contains(w)).ToArray();

            if (installedWorkloads.Length == workloads.Length)
            {
                return new ValidationCheck
                {
                    Name = "MAUI Workloads",
                    Success = true,
                    Message = $"All required workloads installed: {string.Join(", ", installedWorkloads)}",
                    Suggestion = null
                };
            }
            else
            {
                var missing = workloads.Except(installedWorkloads).ToArray();
                return new ValidationCheck
                {
                    Name = "MAUI Workloads",
                    Success = false,
                    Message = $"Missing workloads: {string.Join(", ", missing)}",
                    Suggestion = $"Run 'dotnet workload install {string.Join(" ", missing)}'"
                };
            }
        }
        catch (Exception ex)
        {
            return new ValidationCheck
            {
                Name = "MAUI Workloads",
                Success = false,
                Message = $"Error checking workloads: {ex.Message}",
                Suggestion = "Run 'dotnet workload install maui'"
            };
        }
    }

    /// <summary>
    /// Validates Docker availability
    /// </summary>
    private async Task<ValidationCheck> ValidateDockerAsync()
    {
        try
        {
            if (!SystemUtils.CommandExists("docker"))
            {
                return new ValidationCheck
                {
                    Name = "Docker",
                    Success = false,
                    Message = "Docker not found",
                    Suggestion = "Install Docker from https://www.docker.com/get-started"
                };
            }

            var result = await SystemUtils.RunWithTimeoutAsync(
                "docker", 
                "--version", 
                TimeSpan.FromSeconds(10));

            if (result.Success)
            {
                return new ValidationCheck
                {
                    Name = "Docker",
                    Success = true,
                    Message = $"Docker available: {result.StandardOutput.Trim()}",
                    Suggestion = null
                };
            }
            else
            {
                return new ValidationCheck
                {
                    Name = "Docker",
                    Success = false,
                    Message = "Docker not responding",
                    Suggestion = "Start Docker Desktop or install Docker"
                };
            }
        }
        catch (Exception ex)
        {
            return new ValidationCheck
            {
                Name = "Docker",
                Success = false,
                Message = $"Error checking Docker: {ex.Message}",
                Suggestion = "Install Docker from https://www.docker.com/get-started"
            };
        }
    }

    /// <summary>
    /// Validates project structure
    /// </summary>
    private ValidationCheck ValidateProjectStructure(string projectRoot)
    {
        try
        {
            var requiredPaths = new[]
            {
                "demo/feature-lab/Playground.sln",
                "demo/feature-lab/Playground.Maui/Playground.Maui.csproj",
                "features/smart-reply/smart-reply.feature.yaml",
                "features/contract-summary/contract-summary.feature.yaml",
                "blocks/transform/translate_and_tone.block.yaml"
            };

            var missingPaths = new List<string>();
            foreach (var path in requiredPaths)
            {
                var fullPath = Path.Combine(projectRoot, path);
                if (!File.Exists(fullPath))
                {
                    missingPaths.Add(path);
                }
            }

            if (missingPaths.Count == 0)
            {
                return new ValidationCheck
                {
                    Name = "Project Structure",
                    Success = true,
                    Message = "All required files found",
                    Suggestion = null
                };
            }
            else
            {
                return new ValidationCheck
                {
                    Name = "Project Structure",
                    Success = false,
                    Message = $"Missing files: {string.Join(", ", missingPaths)}",
                    Suggestion = "Ensure you're running from the project root directory"
                };
            }
        }
        catch (Exception ex)
        {
            return new ValidationCheck
            {
                Name = "Project Structure",
                Success = false,
                Message = $"Error checking project structure: {ex.Message}",
                Suggestion = "Ensure you're running from the project root directory"
            };
        }
    }

    /// <summary>
    /// Validates build capability
    /// </summary>
    private async Task<ValidationCheck> ValidateBuildAsync(string projectRoot)
    {
        try
        {
            var solutionPath = Path.Combine(projectRoot, "demo/feature-lab/Playground.sln");
            var buildResult = await _buildManager.BuildSolutionAsync(solutionPath);

            if (buildResult.Success)
            {
                return new ValidationCheck
                {
                    Name = "Build",
                    Success = true,
                    Message = "Solution builds successfully",
                    Suggestion = null
                };
            }
            else
            {
                return new ValidationCheck
                {
                    Name = "Build",
                    Success = false,
                    Message = $"Build failed: {buildResult.Message}",
                    Suggestion = "Check for compilation errors and missing dependencies"
                };
            }
        }
        catch (Exception ex)
        {
            return new ValidationCheck
            {
                Name = "Build",
                Success = false,
                Message = $"Error during build: {ex.Message}",
                Suggestion = "Check for compilation errors and missing dependencies"
            };
        }
    }

    /// <summary>
    /// Validates fixtures
    /// </summary>
    private ValidationCheck ValidateFixtures(string projectRoot)
    {
        try
        {
            var fixtureDirs = new[]
            {
                "demo/feature-lab/Playground.Server/wwwroot/fixtures/inbox",
                "demo/feature-lab/Playground.Server/wwwroot/fixtures/contracts"
            };

            var missingDirs = new List<string>();
            foreach (var dir in fixtureDirs)
            {
                var fullPath = Path.Combine(projectRoot, dir);
                if (!Directory.Exists(fullPath))
                {
                    missingDirs.Add(dir);
                }
            }

            if (missingDirs.Count == 0)
            {
                return new ValidationCheck
                {
                    Name = "Fixtures",
                    Success = true,
                    Message = "Fixture directories exist",
                    Suggestion = null
                };
            }
            else
            {
                return new ValidationCheck
                {
                    Name = "Fixtures",
                    Success = false,
                    Message = $"Missing fixture directories: {string.Join(", ", missingDirs)}",
                    Suggestion = "Run the fixture seeding script"
                };
            }
        }
        catch (Exception ex)
        {
            return new ValidationCheck
            {
                Name = "Fixtures",
                Success = false,
                Message = $"Error checking fixtures: {ex.Message}",
                Suggestion = "Run the fixture seeding script"
            };
        }
    }

    /// <summary>
    /// Validates tests
    /// </summary>
    private async Task<ValidationCheck> ValidateTestsAsync(string projectRoot)
    {
        try
        {
            var testProject = Path.Combine(projectRoot, "tests/FeatureLab.Demo.Tests/FeatureLab.Demo.Tests.csproj");
            
            if (!File.Exists(testProject))
            {
                return new ValidationCheck
                {
                    Name = "Tests",
                    Success = false,
                    Message = "Test project not found",
                    Suggestion = "Create the test project"
                };
            }

            var testResult = await _buildManager.RunTestsAsync(testProject);

            if (testResult.Success)
            {
                return new ValidationCheck
                {
                    Name = "Tests",
                    Success = true,
                    Message = "Tests pass successfully",
                    Suggestion = null
                };
            }
            else
            {
                return new ValidationCheck
                {
                    Name = "Tests",
                    Success = false,
                    Message = $"Tests failed: {testResult.Message}",
                    Suggestion = "Fix failing tests"
                };
            }
        }
        catch (Exception ex)
        {
            return new ValidationCheck
            {
                Name = "Tests",
                Success = false,
                Message = $"Error running tests: {ex.Message}",
                Suggestion = "Check test project configuration"
            };
        }
    }

    /// <summary>
    /// Validates port availability
    /// </summary>
    private ValidationCheck ValidatePorts()
    {
        try
        {
            var requiredPorts = new[] { 5000, 5001, 8000, 8001 };
            var unavailablePorts = new List<int>();

            foreach (var port in requiredPorts)
            {
                if (!SystemUtils.IsPortAvailable(port))
                {
                    unavailablePorts.Add(port);
                }
            }

            if (unavailablePorts.Count == 0)
            {
                return new ValidationCheck
                {
                    Name = "Ports",
                    Success = true,
                    Message = "Required ports are available",
                    Suggestion = null
                };
            }
            else
            {
                return new ValidationCheck
                {
                    Name = "Ports",
                    Success = false,
                    Message = $"Ports in use: {string.Join(", ", unavailablePorts)}",
                    Suggestion = "Stop applications using these ports or use different ports"
                };
            }
        }
        catch (Exception ex)
        {
            return new ValidationCheck
            {
                Name = "Ports",
                Success = false,
                Message = $"Error checking ports: {ex.Message}",
                Suggestion = "Check for port conflicts manually"
            };
        }
    }
}

/// <summary>
/// Result of a validation check
/// </summary>
public class ValidationCheck
{
    public string Name { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Suggestion { get; set; } = string.Empty;
}

/// <summary>
/// Result of validation run
/// </summary>
public class ValidationResult
{
    public string ProjectRoot { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool Success { get; set; }
    public string Error { get; set; } = string.Empty;
    public Dictionary<string, ValidationCheck> Checks { get; set; } = new();
    
    public TimeSpan Duration => EndTime - StartTime;
}
