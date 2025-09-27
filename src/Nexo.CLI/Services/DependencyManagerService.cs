using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.CLI.Services
{
    public partial class DependencyManagerService
    {
        private readonly ILogger<DependencyManagerService> _logger;

        public DependencyManagerService(ILogger<DependencyManagerService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InstallDependenciesAsync(string projectPath, string[] technologies)
        {
            _logger.LogInformation("Installing dependencies for technologies: " + string.Join(", ", technologies));

            var packageManagers = DeterminePackageManagers(technologies);
            
            foreach (var manager in packageManagers)
            {
                await InstallWithPackageManager(projectPath, manager);
            }
        }

        private string[] DeterminePackageManagers(string[] technologies)
        {
            var managers = new HashSet<string>();

            foreach (var tech in technologies)
            {
                switch (tech.ToLower())
                {
                    case "react":
                    case "vue":
                    case "angular":
                    case "typescript":
                    case "javascript":
                    case "node.js":
                    case "express":
                        managers.Add("npm");
                        break;
                    case "unity":
                    case "c#":
                        managers.Add("unity");
                        break;
                    case "python":
                    case "django":
                    case "flask":
                        managers.Add("pip");
                        break;
                    case "rust":
                        managers.Add("cargo");
                        break;
                    case "go":
                        managers.Add("go");
                        break;
                }
            }

            return managers.ToArray();
        }

        private async Task InstallWithPackageManager(string projectPath, string packageManager)
        {
            try
            {
                switch (packageManager)
                {
                    case "npm":
                        await InstallNpmDependencies(projectPath);
                        break;
                    case "unity":
                        await InstallUnityPackages(projectPath);
                        break;
                    case "pip":
                        await InstallPipDependencies(projectPath);
                        break;
                    case "cargo":
                        await InstallCargoDependencies(projectPath);
                        break;
                    case "go":
                        await InstallGoDependencies(projectPath);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to install dependencies with {packageManager}: {ex.Message}");
            }
        }

        private async Task InstallNpmDependencies(string projectPath)
        {
            _logger.LogInformation("Installing npm dependencies...");

            if (!File.Exists(Path.Combine(projectPath, "package.json")))
            {
                _logger.LogWarning("No package.json found, skipping npm install");
                return;
            }

            await RunCommand("npm", "install", projectPath);
            _logger.LogInformation("✅ npm dependencies installed");
        }

        private async Task InstallUnityPackages(string projectPath)
        {
            _logger.LogInformation("Unity packages will be installed when opening in Unity Editor");
            _logger.LogInformation("✅ Unity package installation noted");
        }

        private async Task InstallPipDependencies(string projectPath)
        {
            _logger.LogInformation("Installing Python dependencies...");

            var requirementsFile = Path.Combine(projectPath, "requirements.txt");
            if (File.Exists(requirementsFile))
            {
                await RunCommand("pip", $"install -r {requirementsFile}", projectPath);
                _logger.LogInformation("✅ Python dependencies installed");
            }
            else
            {
                _logger.LogInformation("No requirements.txt found, skipping pip install");
            }
        }

        private async Task InstallCargoDependencies(string projectPath)
        {
            _logger.LogInformation("Installing Rust dependencies...");

            if (File.Exists(Path.Combine(projectPath, "Cargo.toml")))
            {
                await RunCommand("cargo", "build", projectPath);
                _logger.LogInformation("✅ Rust dependencies installed");
            }
            else
            {
                _logger.LogInformation("No Cargo.toml found, skipping cargo build");
            }
        }

        private async Task InstallGoDependencies(string projectPath)
        {
            _logger.LogInformation("Installing Go dependencies...");

            if (File.Exists(Path.Combine(projectPath, "go.mod")))
            {
                await RunCommand("go", "mod tidy", projectPath);
                await RunCommand("go", "mod download", projectPath);
                _logger.LogInformation("✅ Go dependencies installed");
            }
            else
            {
                _logger.LogInformation("No go.mod found, skipping go mod commands");
            }
        }

        private async Task RunCommand(string command, string arguments, string workingDirectory)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger.LogWarning($"Command failed: {command} {arguments}");
                    if (!string.IsNullOrEmpty(error))
                    {
                        _logger.LogWarning($"Error output: {error}");
                    }
                }
                else if (!string.IsNullOrEmpty(output))
                {
                    _logger.LogDebug($"Command output: {output}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to run command {command} {arguments}: {ex.Message}");
            }
        }

        public async Task<bool> CheckDependencyAvailabilityAsync(string[] technologies)
        {
            var packageManagers = DeterminePackageManagers(technologies);
            var allAvailable = true;

            foreach (var manager in packageManagers)
            {
                var isAvailable = await CheckPackageManagerAvailability(manager);
                if (!isAvailable)
                {
                    _logger.LogWarning($"{manager} is not available on this system");
                    allAvailable = false;
                }
            }

            return allAvailable;
        }

        private async Task<bool> CheckPackageManagerAvailability(string packageManager)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = packageManager,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();
                await process.WaitForExitAsync();

                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task CreateRequirementsFileAsync(string projectPath, string[] technologies)
        {
            var requirements = new List<string>();

            foreach (var tech in technologies)
            {
                switch (tech.ToLower())
                {
                    case "django":
                        requirements.AddRange(new[] { "Django>=4.0.0", "djangorestframework>=3.14.0" });
                        break;
                    case "flask":
                        requirements.AddRange(new[] { "Flask>=2.0.0", "Flask-CORS>=3.0.0" });
                        break;
                    case "fastapi":
                        requirements.AddRange(new[] { "fastapi>=0.100.0", "uvicorn>=0.20.0" });
                        break;
                    case "pandas":
                        requirements.Add("pandas>=1.5.0");
                        break;
                    case "numpy":
                        requirements.Add("numpy>=1.24.0");
                        break;
                }
            }

            if (requirements.Count > 0)
            {
                var requirementsPath = Path.Combine(projectPath, "requirements.txt");
                await File.WriteAllLinesAsync(requirementsPath, requirements);
                _logger.LogInformation($"Created requirements.txt with {requirements.Count} packages");
            }
        }

        public async Task CreateGoModFileAsync(string projectPath, string projectName)
        {
            var goModContent = $@"module {projectName}

go 1.21

require (
    // Add your dependencies here
)";

            var goModPath = Path.Combine(projectPath, "go.mod");
            await File.WriteAllTextAsync(goModPath, goModContent);
            _logger.LogInformation("Created go.mod file");
        }

        public async Task CreateCargoTomlAsync(string projectPath, string projectName)
        {
            var cargoTomlContent = $@"[package]
name = ""{projectName.ToLower().Replace(" ", "-")}""
version = ""0.1.0""
edition = ""2021""

[dependencies]
# Add your dependencies here";

            var cargoTomlPath = Path.Combine(projectPath, "Cargo.toml");
            await File.WriteAllTextAsync(cargoTomlPath, cargoTomlContent);
            _logger.LogInformation("Created Cargo.toml file");
        }
    }
}
