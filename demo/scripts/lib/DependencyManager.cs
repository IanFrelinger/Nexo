using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Demo.Scripts.Utils;

/// <summary>
/// Manages installation and verification of dependencies
/// </summary>
public class DependencyManager
{
    private readonly Dictionary<string, string> _dependencies = new()
    {
        ["dotnet"] = "dotnet --version",
        ["docker"] = "docker --version",
        ["git"] = "git --version",
        ["curl"] = "curl --version",
        ["wget"] = "wget --version",
        ["jq"] = "jq --version",
        ["node"] = "node --version",
        ["npm"] = "npm --version",
        ["python3"] = "python3 --version",
        ["pip3"] = "pip3 --version"
    };

    /// <summary>
    /// Checks if a dependency is installed
    /// </summary>
    public async Task<DependencyCheckResult> CheckDependencyAsync(string dependencyName)
    {
        if (!_dependencies.ContainsKey(dependencyName))
        {
            return new DependencyCheckResult
            {
                Name = dependencyName,
                IsInstalled = false,
                Version = "Unknown",
                Error = "No check command defined for this dependency"
            };
        }

        if (!SystemUtils.CommandExists(dependencyName))
        {
            return new DependencyCheckResult
            {
                Name = dependencyName,
                IsInstalled = false,
                Version = "Not installed",
                Error = null
            };
        }

        try
        {
            var version = await SystemUtils.GetCommandVersionAsync(dependencyName);
            return new DependencyCheckResult
            {
                Name = dependencyName,
                IsInstalled = true,
                Version = version,
                Error = null
            };
        }
        catch (Exception ex)
        {
            return new DependencyCheckResult
            {
                Name = dependencyName,
                IsInstalled = true,
                Version = "Unknown",
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Installs a dependency
    /// </summary>
    public async Task<InstallResult> InstallDependencyAsync(string dependencyName, string packageName = null)
    {
        packageName ??= dependencyName;

        // Check if already installed
        var checkResult = await CheckDependencyAsync(dependencyName);
        if (checkResult.IsInstalled)
        {
            return new InstallResult
            {
                Success = true,
                Message = $"{dependencyName} is already installed (version: {checkResult.Version})"
            };
        }

        // Special handling for different dependencies
        return dependencyName switch
        {
            "dotnet" => await InstallDotNetAsync(),
            "docker" => await InstallDockerAsync(),
            "node" => await InstallNodeJsAsync(),
            _ => await InstallGenericPackageAsync(dependencyName, packageName)
        };
    }

    /// <summary>
    /// Installs .NET SDK
    /// </summary>
    private async Task<InstallResult> InstallDotNetAsync()
    {
        try
        {
            var platform = Environment.OSVersion.Platform;
            var arch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") ?? 
                      Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432") ?? "x64";

            // Download and run .NET install script
            var installScript = Path.GetTempFileName();
            var downloadResult = await SystemUtils.RunWithTimeoutAsync(
                "curl", 
                "-sSL https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh -o " + installScript,
                TimeSpan.FromMinutes(2));

            if (!downloadResult.Success)
            {
                return new InstallResult
                {
                    Success = false,
                    Message = "Failed to download .NET install script"
                };
            }

            // Run install script
            var installResult = await SystemUtils.RunWithTimeoutAsync(
                "bash", 
                installScript + " --channel 8.0",
                TimeSpan.FromMinutes(5));

            // Clean up
            File.Delete(installScript);

            if (installResult.Success)
            {
                return new InstallResult
                {
                    Success = true,
                    Message = ".NET SDK installed successfully"
                };
            }
            else
            {
                return new InstallResult
                {
                    Success = false,
                    Message = $"Failed to install .NET SDK: {installResult.StandardError}"
                };
            }
        }
        catch (Exception ex)
        {
            return new InstallResult
            {
                Success = false,
                Message = $"Exception during .NET installation: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Installs Docker
    /// </summary>
    private async Task<InstallResult> InstallDockerAsync()
    {
        try
        {
            var platform = Environment.OSVersion.Platform;
            
            if (platform == PlatformID.Win32NT)
            {
                // Windows - try chocolatey or winget
                if (SystemUtils.CommandExists("choco"))
                {
                    var result = await SystemUtils.SafeExecuteAsync("choco", "install docker-desktop -y");
                    return new InstallResult
                    {
                        Success = result.Success,
                        Message = result.Success ? "Docker installed via Chocolatey" : result.StandardError
                    };
                }
                else if (SystemUtils.CommandExists("winget"))
                {
                    var result = await SystemUtils.SafeExecuteAsync("winget", "install Docker.DockerDesktop");
                    return new InstallResult
                    {
                        Success = result.Success,
                        Message = result.Success ? "Docker installed via Winget" : result.StandardError
                    };
                }
                else
                {
                    return new InstallResult
                    {
                        Success = false,
                        Message = "Docker installation on Windows requires Chocolatey, Winget, or manual installation"
                    };
                }
            }
            else
            {
                // Unix-like systems - use official script
                var installScript = Path.GetTempFileName();
                var downloadResult = await SystemUtils.RunWithTimeoutAsync(
                    "curl", 
                    "-fsSL https://get.docker.com -o " + installScript,
                    TimeSpan.FromMinutes(2));

                if (!downloadResult.Success)
                {
                    return new InstallResult
                    {
                        Success = false,
                        Message = "Failed to download Docker install script"
                    };
                }

                var installResult = await SystemUtils.RunWithTimeoutAsync(
                    "bash", 
                    installScript,
                    TimeSpan.FromMinutes(5));

                File.Delete(installScript);

                return new InstallResult
                {
                    Success = installResult.Success,
                    Message = installResult.Success ? "Docker installed successfully" : installResult.StandardError
                };
            }
        }
        catch (Exception ex)
        {
            return new InstallResult
            {
                Success = false,
                Message = $"Exception during Docker installation: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Installs Node.js
    /// </summary>
    private async Task<InstallResult> InstallNodeJsAsync()
    {
        try
        {
            // Try package manager first
            var packageManager = DetectPackageManager();
            if (packageManager != "unknown")
            {
                var installCommand = GetPackageInstallCommand(packageManager);
                var result = await SystemUtils.SafeExecuteAsync(installCommand, "nodejs npm");
                
                if (result.Success)
                {
                    return new InstallResult
                    {
                        Success = true,
                        Message = $"Node.js installed via {packageManager}"
                    };
                }
            }

            // Fallback to nvm
            var nvmResult = await SystemUtils.RunWithTimeoutAsync(
                "curl", 
                "-o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.0/install.sh | bash",
                TimeSpan.FromMinutes(2));

            if (nvmResult.Success)
            {
                // Source nvm and install Node.js
                var installResult = await SystemUtils.RunWithTimeoutAsync(
                    "bash", 
                    "-c 'source ~/.nvm/nvm.sh && nvm install --lts && nvm use --lts'",
                    TimeSpan.FromMinutes(5));

                return new InstallResult
                {
                    Success = installResult.Success,
                    Message = installResult.Success ? "Node.js installed via nvm" : installResult.StandardError
                };
            }
            else
            {
                return new InstallResult
                {
                    Success = false,
                    Message = "Failed to install nvm"
                };
            }
        }
        catch (Exception ex)
        {
            return new InstallResult
            {
                Success = false,
                Message = $"Exception during Node.js installation: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Installs a generic package
    /// </summary>
    private async Task<InstallResult> InstallGenericPackageAsync(string dependencyName, string packageName)
    {
        try
        {
            var packageManager = DetectPackageManager();
            if (packageManager == "unknown")
            {
                return new InstallResult
                {
                    Success = false,
                    Message = "Unknown package manager, cannot install package"
                };
            }

            var installCommand = GetPackageInstallCommand(packageManager);
            var result = await SystemUtils.SafeExecuteAsync(installCommand, packageName);

            return new InstallResult
            {
                Success = result.Success,
                Message = result.Success ? $"{dependencyName} installed successfully" : result.StandardError
            };
        }
        catch (Exception ex)
        {
            return new InstallResult
            {
                Success = false,
                Message = $"Exception during package installation: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Detects the package manager
    /// </summary>
    private string DetectPackageManager()
    {
        if (SystemUtils.CommandExists("brew")) return "brew";
        if (SystemUtils.CommandExists("apt-get")) return "apt";
        if (SystemUtils.CommandExists("yum")) return "yum";
        if (SystemUtils.CommandExists("dnf")) return "dnf";
        if (SystemUtils.CommandExists("pacman")) return "pacman";
        if (SystemUtils.CommandExists("zypper")) return "zypper";
        if (SystemUtils.CommandExists("choco")) return "choco";
        if (SystemUtils.CommandExists("winget")) return "winget";
        
        return "unknown";
    }

    /// <summary>
    /// Gets the package install command for a package manager
    /// </summary>
    private string GetPackageInstallCommand(string packageManager)
    {
        return packageManager switch
        {
            "brew" => "brew install",
            "apt" => "apt-get update && apt-get install -y",
            "yum" or "dnf" => $"{packageManager} install -y",
            "pacman" => "pacman -S --noconfirm",
            "zypper" => "zypper install -y",
            "choco" => "choco install -y",
            "winget" => "winget install",
            _ => "unknown"
        };
    }

    /// <summary>
    /// Verifies all required dependencies
    /// </summary>
    public async Task<VerificationResult> VerifyDependenciesAsync(params string[] requiredDependencies)
    {
        var results = new List<DependencyCheckResult>();
        var missingDependencies = new List<string>();

        foreach (var dep in requiredDependencies)
        {
            var result = await CheckDependencyAsync(dep);
            results.Add(result);
            
            if (!result.IsInstalled)
            {
                missingDependencies.Add(dep);
            }
        }

        return new VerificationResult
        {
            AllInstalled = missingDependencies.Count == 0,
            MissingDependencies = missingDependencies,
            DependencyResults = results
        };
    }

    /// <summary>
    /// Installs missing dependencies
    /// </summary>
    public async Task<InstallResult> InstallMissingDependenciesAsync(params string[] requiredDependencies)
    {
        var verification = await VerifyDependenciesAsync(requiredDependencies);
        
        if (verification.AllInstalled)
        {
            return new InstallResult
            {
                Success = true,
                Message = "All dependencies are already installed"
            };
        }

        var failedInstalls = new List<string>();
        
        foreach (var dep in verification.MissingDependencies)
        {
            var installResult = await InstallDependencyAsync(dep);
            if (!installResult.Success)
            {
                failedInstalls.Add(dep);
            }
        }

        return new InstallResult
        {
            Success = failedInstalls.Count == 0,
            Message = failedInstalls.Count == 0 
                ? "All dependencies installed successfully" 
                : $"Failed to install: {string.Join(", ", failedInstalls)}"
        };
    }
}

/// <summary>
/// Result of a dependency check
/// </summary>
public class DependencyCheckResult
{
    public string Name { get; set; } = string.Empty;
    public bool IsInstalled { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}

/// <summary>
/// Result of an installation attempt
/// </summary>
public class InstallResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Result of dependency verification
/// </summary>
public class VerificationResult
{
    public bool AllInstalled { get; set; }
    public List<string> MissingDependencies { get; set; } = new();
    public List<DependencyCheckResult> DependencyResults { get; set; } = new();
}
